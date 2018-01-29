﻿using Harmony;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BetterPlacing
{
    internal class BetterPlacing
    {
        private const float COLLIDER_OFFSET = 0.001f;
        private const float CONTACT_DISTANCE = 0.01f;
        private const float RAYCAST_DISTANCE = 0.1f;

        private static FieldInfo fieldInfo;
        private static Vector3 lastValidPosition;
        private static Quaternion lastValidRotation;
        private static PlayerManager playerManager;
        private static Quaternion rotation;

        public static void OnLoad()
        {
            Debug.Log("[Better-Placing]: Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            AddTranslations();
            PlaceableFurniture.Initialize();
        }

        internal static void AddFurnitureToPhysicalCollisionMask()
        {
            Utils.m_PhysicalCollisionLayerMask |= 1 << vp_Layer.InteractiveProp;
        }

        internal static void AddGearItemsToPhysicalCollisionMask()
        {
            Utils.m_PhysicalCollisionLayerMask |= 1 << vp_Layer.Gear;
        }

        internal static void InitializeRotation(PlayerManager playerManager)
        {
            BetterPlacing.playerManager = playerManager;
            fieldInfo = AccessTools.Field(playerManager.GetType(), "m_RotationInCameraSpace");

            rotation = (Quaternion)fieldInfo.GetValue(playerManager);
        }

        internal static bool IsBlockedFromAbove(GameObject gameObject)
        {
            RaycastHit hitInfo = new RaycastHit();
            Ray ray = new Ray(Vector3.zero, Vector3.down);

            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (Collider eachCollider in colliders)
            {
                List<GameObject> gearItemsAbove = GetGearItemsAbove(gameObject, eachCollider);
                foreach (var eachGearItem in gearItemsAbove)
                {
                    ray.origin = eachGearItem.transform.position + Vector3.up * COLLIDER_OFFSET;
                    if (eachCollider.Raycast(ray, out hitInfo, CONTACT_DISTANCE))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsPlacableFurniture(BreakDown breakDown)
        {
            return IsPlaceableFurniture(breakDown == null ? null : breakDown.gameObject);
        }

        internal static bool IsPlaceableFurniture(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            return PlaceableFurniture.GetPrefab(gameObject.name) != null;
        }

        internal static bool IsStackableGearItem(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            Bed bed = gameObject.GetComponent<Bed>();
            if (bed != null && bed.GetState() == BedRollState.Placed)
            {
                return false;
            }

            GearItem gearItem = gameObject.GetComponent<GearItem>();
            if (gearItem == null)
            {
                return false;
            }

            return true;
        }

        internal static void PrepareGearItem(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            FixBoxCollider(gameObject);
            RemoveNoCollidePlayer(gameObject);
        }

        internal static void PrepareGearItems()
        {
            GearItem[] gearItems = Resources.FindObjectsOfTypeAll<GearItem>();
            foreach (GearItem eachGearItem in gearItems)
            {
                if (IsStackableGearItem(eachGearItem.gameObject))
                {
                    PrepareGearItem(eachGearItem.gameObject);
                }
            }
        }

        internal static void PreparePlacableFurniture(GameObject gameObject)
        {
            if (!gameObject.GetComponent<Renderer>().isPartOfStaticBatch)
            {
                return;
            }

            GameObject prefab = PlaceableFurniture.GetPrefab(gameObject.name);
            if (prefab == null)
            {
                return;
            }

            MeshFilter templateMeshFilter = prefab.GetComponentInChildren<MeshFilter>();
            gameObject.GetComponent<MeshFilter>().mesh = templateMeshFilter.mesh;

            BoxCollider[] templateBoxColliders = prefab.GetComponents<BoxCollider>();
            foreach (BoxCollider eachTemplateBoxCollider in templateBoxColliders)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = eachTemplateBoxCollider.center;
                boxCollider.size = eachTemplateBoxCollider.size;
                boxCollider.material = eachTemplateBoxCollider.material;
            }
        }

        internal static void RemoveFurnitureFromPhysicalCollisionMask()
        {
            Utils.m_PhysicalCollisionLayerMask &= ~(1 << vp_Layer.InteractiveProp);
        }

        internal static void RemoveGearItemsFromPhysicalCollisionMask()
        {
            Utils.m_PhysicalCollisionLayerMask &= ~(1 << vp_Layer.Gear);
        }

        internal static void RestoreFurnitureLayers(GameObject furniture)
        {
            vp_Layer.Set(furniture, vp_Layer.InteractiveProp, true);
            vp_Layer.Set(furniture, vp_Layer.Default, false);
        }

        internal static void RestoreLastValidTransform(GameObject gameObject)
        {
            gameObject.transform.position = lastValidPosition;
            gameObject.transform.rotation = lastValidRotation;
        }

        internal static void Rotate(GameObject gameObject, float yAngle)
        {
            gameObject.transform.Rotate(gameObject.transform.up, yAngle, Space.World);

            SetRotation(Quaternion.Inverse(GameManager.GetMainCamera().transform.rotation) * gameObject.transform.rotation);
        }

        internal static void SignalItemBlocked()
        {
            GameAudioManager.PlayGUIError();
            HUDMessage.AddMessage(Localization.Get("GAMEPLAY_BlockedByItemAbove"), false);
        }

        internal static void SnapToPositionBelow(GameObject gameObject)
        {
            GameObject gearItemBelow = GetGearItemBelow(gameObject, RAYCAST_DISTANCE);
            if (gearItemBelow != null)
            {
                Vector3 relativePosition = gameObject.transform.position - gearItemBelow.transform.position;
                Vector3 projectedRelativePosition = Vector3.Project(relativePosition, gearItemBelow.transform.up);

                gameObject.transform.position = gearItemBelow.transform.position + projectedRelativePosition;
            }
        }

        internal static void SnapToRotationBelow(GameObject gameObject)
        {
            GameObject gearItemBelow = GetGearItemBelow(gameObject, RAYCAST_DISTANCE);
            if (gearItemBelow != null)
            {
                SetRotation(Quaternion.Inverse(GameManager.GetMainCamera().transform.rotation) * gearItemBelow.transform.rotation);
            }
        }

        internal static void StoreValidTransform(GameObject gameObject)
        {
            lastValidPosition = gameObject.transform.position;
            lastValidRotation = gameObject.transform.rotation;
        }

        private static void AddTranslations()
        {
            string[] knownLanguages = Localization.knownLanguages;
            string[] translations = new string[knownLanguages.Length];
            for (int i = 0; i < knownLanguages.Length; i++)
            {
                switch (knownLanguages[i])
                {
                    case "English":
                        translations[i] = "Blocked by item above";
                        break;

                    case "German":
                        translations[i] = "Blockiert durch Gegenstand darüber";
                        break;

                    case "Russian":
                        translations[i] = "Заблокировано предметом выше";
                        break;

                    default:
                        translations[i] = "Blocked by item above\nHelp me translate this!\nVisit https://github.com/WulfMarius/Better-Placing";
                        break;
                }
            }

            Localization.dictionary.Add("GAMEPLAY_BlockedByItemAbove", translations);
        }

        private static void FixBoxCollider(GameObject gameObject)
        {
            BoxCollider boxCollider = gameObject.GetComponentInChildren<BoxCollider>();
            if (boxCollider == null)
            {
                return;
            }

            float meshHeight = -1;

            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter eachMeshFilter in meshFilters)
            {
                GameObject transformObject = new GameObject();
                transformObject.transform.localRotation = eachMeshFilter.transform.localRotation;
                transformObject.transform.localScale = eachMeshFilter.transform.localScale;
                meshHeight = Mathf.Max(meshHeight, Mathf.Abs(transformObject.transform.TransformVector(eachMeshFilter.mesh.bounds.size).y));
            }

            if (meshHeight <= 0)
            {
                return;
            }

            boxCollider.center = new Vector3(boxCollider.center.x, meshHeight / 2f + COLLIDER_OFFSET, boxCollider.center.z);
            boxCollider.size = new Vector3(boxCollider.size.x, meshHeight - COLLIDER_OFFSET, boxCollider.size.z);
        }

        private static GameObject GetGearItemBelow(GameObject gameObject, float maxDistance)
        {
            RaycastHit[] hits = Physics.RaycastAll(gameObject.transform.position + gameObject.transform.up * CONTACT_DISTANCE, -gameObject.transform.up, maxDistance, 1 << vp_Layer.Gear);
            foreach (RaycastHit eachHit in hits)
            {
                if (eachHit.transform != gameObject.transform)
                {
                    return eachHit.collider.gameObject;
                }
            }

            return null;
        }

        private static List<GameObject> GetGearItemsAbove(GameObject gameObject, Collider boxCollider)
        {
            var bounds = boxCollider.bounds;
            var origin = bounds.center;
            var halfExtents = bounds.extents;
            halfExtents.y = CONTACT_DISTANCE;
            var direction = gameObject.transform.up;
            var maxDistance = bounds.extents.y + RAYCAST_DISTANCE;

            List<GameObject> result = new List<GameObject>();

            RaycastHit[] hits = Physics.BoxCastAll(origin, halfExtents, direction, Quaternion.identity, maxDistance);
            foreach (RaycastHit eachHit in hits)
            {
                if (eachHit.transform.IsChildOf(gameObject.transform))
                {
                    continue;
                }

                if (eachHit.collider.GetComponentInParent<GearItem>() == null)
                {
                    continue;
                }

                result.Add(eachHit.collider.gameObject);
            }

            return result;
        }

        private static void RemoveNoCollidePlayer(GameObject gameObject)
        {
            foreach (Transform eachTransform in gameObject.transform)
            {
                if (eachTransform.gameObject.layer == vp_Layer.NoCollidePlayer)
                {
                    Object.Destroy(eachTransform.gameObject);
                }
            }
        }

        private static void SetRotation(Quaternion rotation)
        {
            BetterPlacing.rotation = rotation;
            fieldInfo.SetValue(playerManager, rotation);
        }
    }
}