using Harmony;
using System.Reflection;
using UnityEngine;

namespace BetterPlacing
{
    internal class PlacingRotation
    {
        private static Quaternion rotation;

        private static PlayerManager playerManager;
        private static FieldInfo fieldInfo;

        internal static void InitializeRotation(PlayerManager playerManager)
        {
            PlacingRotation.playerManager = playerManager;
            fieldInfo = AccessTools.Field(playerManager.GetType(), "m_RotationInCameraSpace");

            rotation = (Quaternion)fieldInfo.GetValue(playerManager);
        }

        internal static void Rotate(float xAngle, float yAngle, float zAngle)
        {
            rotation *= Quaternion.Euler(xAngle, yAngle, zAngle);

            fieldInfo.SetValue(playerManager, rotation);
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "DoPositionCheck")]
    class PlayerManager_DoPositionCheck
    {
        public static void Postfix(PlayerManager __instance)
        {
            if (Input.mouseScrollDelta.y == 0)
            {
                return;
            }

            float yAngle = Input.mouseScrollDelta.y;
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                yAngle *= 5;
            }

            PlacingRotation.Rotate(0, yAngle, 0);
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "StartPlaceMesh")]
    class PlayerManager_StartPlaceMesh
    {
        public static void Postfix(PlayerManager __instance)
        {
            PlacingRotation.InitializeRotation(__instance);
        }
    }
}
