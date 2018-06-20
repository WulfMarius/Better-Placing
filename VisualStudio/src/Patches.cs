using Harmony;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BetterPlacing
{
    [HarmonyPatch(typeof(BreakDown), "Deserialize")]
    internal class BreakDown_Deserialize
    {
        private static bool Prefix(BreakDown __instance, string text)
        {
            if (text == null || !BetterPlacing.IsPlacableFurniture(__instance))
            {
                return true;
            }

            ModBreakDownSaveData saveData = Utils.DeserializeObject<ModBreakDownSaveData>(text);
            if (saveData.m_HasBeenBrokenDown)
            {
                return true;
            }

            BetterPlacing.PreparePlacableFurniture(__instance.gameObject);

            __instance.transform.parent.position = saveData.m_Position;
            __instance.gameObject.SetActive(true);
            if (saveData.m_Rotation.x != 0 || saveData.m_Rotation.y != 0 || saveData.m_Rotation.z != 0)
            {
                __instance.transform.parent.rotation = Quaternion.Euler(saveData.m_Rotation);
            }

            return false;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call)
                {
                    continue;
                }

                MethodInfo methodInfo = codes[i].operand as MethodInfo;
                if (methodInfo == null || methodInfo.Name != "DeserializeObject" || methodInfo.DeclaringType != typeof(Utils) || !methodInfo.IsGenericMethod)
                {
                    continue;
                }

                System.Type[] genericArguments = methodInfo.GetGenericArguments();
                if (genericArguments.Length != 1 || genericArguments[0] != typeof(BreakDownSaveData))
                {
                    continue;
                }

                methodInfo = methodInfo.GetBaseDefinition().MakeGenericMethod(typeof(ModBreakDownSaveData));
                codes[i].operand = methodInfo;
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(BreakDown), "DeserializeAll")]
    internal class BreakDown_DeserializeAll
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call)
                {
                    continue;
                }

                MethodInfo methodInfo = codes[i].operand as MethodInfo;
                if (methodInfo == null || methodInfo.Name != "DeserializeObject" || methodInfo.DeclaringType != typeof(Utils) || !methodInfo.IsGenericMethod)
                {
                    continue;
                }

                System.Type[] genericArguments = methodInfo.GetGenericArguments();
                if (genericArguments.Length != 1 || genericArguments[0] != typeof(BreakDownSaveData))
                {
                    continue;
                }

                methodInfo = methodInfo.GetBaseDefinition().MakeGenericMethod(typeof(ModBreakDownSaveData));
                codes[i].operand = methodInfo;
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(BreakDown), "ProcessInteraction")]
    internal class BreakDown_ProcessInteraction
    {
        public static bool Prefix(BreakDown __instance, ref bool __result)
        {
            if (BetterPlacing.IsBlockedFromAbove(__instance.gameObject))
            {
                BetterPlacing.SignalItemBlocked();
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BreakDown), "Serialize")]
    internal class BreakDown_Serialize
    {
        public static bool Prefix(BreakDown __instance, ref string __result)
        {
            if (!BetterPlacing.IsPlacableFurniture(__instance))
            {
                return true;
            }

            ModBreakDownSaveData saveData = new ModBreakDownSaveData();
            saveData.m_Position = __instance.transform.position;
            saveData.m_Rotation = __instance.transform.rotation.eulerAngles;
            saveData.m_HasBeenBrokenDown = !__instance.gameObject.activeSelf;
            saveData.m_Guid = Utils.GetGuidFromGameObject(__instance.gameObject);

            __result = Utils.SerializeObject(saveData);
            return false;
        }
    }

    [HarmonyPatch(typeof(GearManager), "Reset")]
    internal class GearManager_Reset
    {
        public static void Prefix()
        {
            BetterPlacing.PrepareGearItems();
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "DoPositionCheck")]
    internal class PlayerManager_DoPositionCheck
    {
        public static void Prefix(PlayerManager __instance)
        {
            var gameObject = __instance.GetObjectToPlace();

            if (BetterPlacing.IsStackableGearItem(gameObject))
            {
                BetterPlacing.AddGearItemsToPhysicalCollisionMask();
            }
            else if (BetterPlacing.IsPlaceableFurniture(gameObject))
            {
                BetterPlacing.RemoveFurnitureFromPhysicalCollisionMask();
            }
        }

        private static void Postfix(PlayerManager __instance, ref MeshLocationCategory __result)
        {
            GameObject gameObject = __instance.GetObjectToPlace();

            if (__result != MeshLocationCategory.Valid && Input.GetKey(KeyCode.L))
            {
                BetterPlacing.RestoreLastValidTransform(gameObject);
                __result = MeshLocationCategory.Valid;
            }

            if (__result == MeshLocationCategory.Valid)
            {
                BetterPlacing.StoreValidTransform(gameObject);

                if (Input.GetKey(KeyCode.P))
                {
                    BetterPlacing.SnapToPositionBelow(gameObject);
                }
            }

            if (Input.GetKey(KeyCode.R))
            {
                BetterPlacing.SnapToRotationBelow(gameObject);
            }

            if (Input.mouseScrollDelta.y != 0)
            {
                float yAngle = Input.mouseScrollDelta.y;
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    yAngle *= 5;
                }

                BetterPlacing.Rotate(gameObject, yAngle);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "CleanUpPlaceMesh")]
    internal class PlayerManager_GetLayerMaskForPlaceMeshRaycast
    {
        private static void Prefix(PlayerManager __instance)
        {
            var gameObject = __instance.GetObjectToPlace();
            if (BetterPlacing.IsStackableGearItem(gameObject))
            {
                BetterPlacing.RemoveGearItemsFromPhysicalCollisionMask();
            }
            else if (BetterPlacing.IsPlaceableFurniture(gameObject))
            {
                BetterPlacing.AddFurnitureToPhysicalCollisionMask();
                BetterPlacing.RestoreFurnitureLayers(gameObject);
            }

            CookingPotItem[] items = Object.FindObjectsOfType<CookingPotItem>();
            foreach (var eachItem in items)
            {
                vp_Layer.Set(eachItem.gameObject, vp_Layer.Gear, true);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "InteractiveObjectsProcessAltFire")]
    internal class PlayerManager_InteractiveObjectsProcessAltFire
    {
        public static bool Prefix(PlayerManager __instance)
        {
            var gameObject = __instance.m_InteractiveObjectUnderCrosshair;

            if (BetterPlacing.IsPlaceableFurniture(gameObject))
            {
                BetterPlacing.PreparePlacableFurniture(gameObject);

                LODGroup lodGroup = gameObject.GetComponentInParent<LODGroup>();
                if (lodGroup != null)
                {
                    gameObject = lodGroup.gameObject;
                }

                __instance.StartPlaceMesh(gameObject, 5f, false);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "ObjectToPlaceOverlapsWithObjectsThatBlockPlacement")]
    internal class PlayerManager_ObjectToPlaceOverlapsWithObjectsThatBlockPlacement
    {
        public static bool Prefix(PlayerManager __instance, ref bool __result)
        {
            BetterPlacing.AddNpcToPhysicalCollisionMask();

            var gameObject = __instance.GetObjectToPlace();
            if (!BetterPlacing.IsPlaceableFurniture(gameObject))
            {
                return true;
            }

            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (var eachCollider in colliders)
            {
                Collider[] otherColliders = Physics.OverlapSphere(eachCollider.bounds.center, eachCollider.bounds.size.magnitude / 2, 918016);
                foreach (var eachOtherCollider in otherColliders)
                {
                    if (!eachOtherCollider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (eachOtherCollider.transform.IsChildOf(gameObject.transform))
                    {
                        continue;
                    }

                    Vector3 direction;
                    float distance;

                    if (Physics.ComputePenetration(eachCollider, eachCollider.transform.position, eachCollider.transform.rotation, eachOtherCollider, eachOtherCollider.transform.position, eachOtherCollider.transform.rotation, out direction, out distance))
                    {
                        __result = true;
                        return false;
                    }
                }
            }

            __result = false;
            return false;
        }

        public static void Postfix()
        {
            BetterPlacing.RemoveNpcFromPhysiclaCollisionMask();
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "ProcessInspectablePickupItem")]
    internal class PlayerManager_ProcessInspectablePickupItem
    {
        private static bool Prefix(GearItem pickupItem, ref bool __result)
        {
            if (BetterPlacing.IsBlockedFromAbove(pickupItem.gameObject))
            {
                BetterPlacing.SignalItemBlocked();
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "StartPlaceMesh")]
    internal class PlayerManager_StartPlaceMesh
    {
        private static void Postfix(PlayerManager __instance, GameObject objectToPlace, bool __result)
        {
            if (__result)
            {
                BetterPlacing.InitializeRotation(__instance);

                if (BetterPlacing.IsStackableGearItem(objectToPlace))
                {
                    objectToPlace.layer = vp_Layer.NPC;
                }
                else if (BetterPlacing.IsPlaceableFurniture(objectToPlace))
                {
                    vp_Layer.Set(objectToPlace, vp_Layer.NPC, true);
                }
            }
        }

        private static bool Prefix(PlayerManager __instance, GameObject objectToPlace, ref bool __result)
        {
            if (BetterPlacing.IsBlockedFromAbove(objectToPlace))
            {
                BetterPlacing.SignalItemBlocked();
                __result = false;
                return false;
            }

            CookingPotItem[] items = Object.FindObjectsOfType<CookingPotItem>();
            foreach (var eachItem in items)
            {
                if (eachItem.AttachedFireIsBurning())
                {
                    vp_Layer.Set(eachItem.gameObject, vp_Layer.NPC, true);
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Campfire), "Awake")]
    internal class Campfire_Awake
    {
        internal static void Postfix(Campfire __instance)
        {
            BetterPlacing.ChangeLayer(__instance.gameObject, vp_Layer.Gear, vp_Layer.NPC);
        }
    }

    [HarmonyPatch(typeof(GearPlacePoint), "AddDefaultCapsuleCollider")]
    internal class GearPlacePoint_AddDefaultCapsuleCollider
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codeInstructions.Count; i++)
            {
                CodeInstruction codeInstruction = codeInstructions[i];

                if (codeInstruction.opcode != OpCodes.Call && codeInstruction.opcode != OpCodes.Callvirt)
                {
                    continue;
                }

                MethodInfo methodInfo = codeInstruction.operand as MethodInfo;
                if (methodInfo == null)
                {
                    continue;
                }

                if (methodInfo.Name == "set_layer" && methodInfo.DeclaringType == typeof(GameObject) && methodInfo.GetParameters().Length == 1)
                {
                    codeInstructions[i - 1].operand = vp_Layer.NPC;
                    break;
                }
            }

            return instructions;
        }
    }

    [HarmonyPatch(typeof(Wind), "IsPositionOccludedFromWind")]
    internal class Wind_IsPositionOccludedFromWind
    {
        public static void Postfix()
        {
            BetterPlacing.RemoveGearItemsFromPhysicalCollisionMask();
        }

        public static void Prefix()
        {
            BetterPlacing.AddGearItemsToPhysicalCollisionMask();
        }
    }
}