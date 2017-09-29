using Harmony;
using UnityEngine;

namespace BetterPlacing
{
    [HarmonyPatch(typeof(PlayerManager), "DoPositionCheck")]
    class PlayerManager_DoPositionCheck
    {
        static void Postfix(PlayerManager __instance, ref MeshLocationCategory __result)
        {
            GameObject gameObject = __instance.GetObjectToPlace();

            if (__result != MeshLocationCategory.Valid && Input.GetKey(KeyCode.L))
            {
                Placing.RestoreLastValidTransform(gameObject);
                __result = MeshLocationCategory.Valid;
            }

            if (__result == MeshLocationCategory.Valid)
            {
                Placing.StoreValidTransform(gameObject);

                if (Input.GetKey(KeyCode.P))
                {
                    Placing.SnapToPositionBelow(gameObject);
                }
            }

            if (Input.GetKey(KeyCode.R))
            {
                Placing.SnapToRotationBelow(gameObject);
            }

            if (Input.mouseScrollDelta.y != 0)
            {
                float yAngle = Input.mouseScrollDelta.y;
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    yAngle *= 5;
                }

                Placing.Rotate(0, yAngle, 0);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "CleanUpPlaceMesh")]
    class PlayerManager_GetLayerMaskForPlaceMeshRaycast
    {
        static void Postfix()
        {
            Placing.RemoveGearItemsFromPhysicalCollisionMask();
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "ProcessInspectablePickupItem")]
    class PlayerManager_ProcessInspectablePickupItem
    {
        static bool Prefix(GearItem pickupItem, ref bool __result)
        {
            if (Placing.IsBlockedFromAbove(pickupItem.gameObject))
            {
                Placing.SignalItemBlocked();
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "StartPlaceMesh")]
    class PlayerManager_StartPlaceMesh
    {
        static void Postfix(PlayerManager __instance, GameObject objectToPlace, bool __result)
        {
            if (__result)
            {
                Placing.AddGearItemsToPhysicalCollisionMask();
                Placing.InitializeRotation(__instance);

                // workaround for a bug in PlayerManager.PrepareGhostedObject which resets the layer to vp_Layer.Gear after setting it to vp_Layer.IgnoreRaycast
                Utils.ChangeLayersForGearItem(objectToPlace, vp_Layer.IgnoreRaycast);
            }
        }

        static bool Prefix(PlayerManager __instance, GameObject objectToPlace, ref bool __result)
        {
            if (Placing.IsBlockedFromAbove(objectToPlace))
            {
                Placing.SignalItemBlocked();
                __result = false;
                return false;
            }

            return true;
        }
    }
}
