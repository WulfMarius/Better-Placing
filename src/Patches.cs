using Harmony;
using UnityEngine;

namespace BetterPlacing
{
    [HarmonyPatch(typeof(Panel_MainMenu), "Awake")]
    internal class Panel_MainMenu_Awake
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
            if (BetterPlacing.IsPlacingStackableGearItem(__instance))
            {
                BetterPlacing.AddGearItemsToPhysicalCollisionMask();
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

                BetterPlacing.Rotate(0, yAngle, 0);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "CleanUpPlaceMesh")]
    internal class PlayerManager_GetLayerMaskForPlaceMeshRaycast
    {
        private static void Postfix()
        {
            BetterPlacing.RemoveGearItemsFromPhysicalCollisionMask();
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

                if (BetterPlacing.IsPlacingStackableGearItem(__instance))
                {
                    Utils.ChangeLayersForGearItem(objectToPlace, vp_Layer.IgnoreRaycast);
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

            return true;
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