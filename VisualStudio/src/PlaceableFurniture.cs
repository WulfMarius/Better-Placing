using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BetterPlacing
{
    internal class PlaceableFurniture
    {
        private static AssetBundle assetBundle;

        private static Dictionary<string, string> prefabNames = new Dictionary<string, string>();

        internal static GameObject GetPrefab(string gameObjectName)
        {
            foreach (string eachPrefabName in prefabNames.Keys)
            {
                if (gameObjectName.ToLower().StartsWith(eachPrefabName))
                {
                    return assetBundle.LoadAsset<GameObject>(prefabNames[eachPrefabName]);
                }
            }

            return null;
        }

        internal static bool HasPrefab(string gameObjectName)
        {
            foreach (string eachPrefabName in prefabNames.Keys)
            {
                if (gameObjectName.ToLower().StartsWith(eachPrefabName))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void Initialize()
        {
            string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetBundlePath = Path.Combine(modDirectory, "better-placing/better-placing.unity3d");

            assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (assetBundle == null)
            {
                throw new FileNotFoundException("Could not load asset bundle from path '" + assetBundlePath + "'.");
            }

            foreach (var eachAssetName in assetBundle.GetAllAssetNames())
            {
                prefabNames.Add(GetAssetName(eachAssetName), eachAssetName);
            }
        }

        private static string GetAssetName(string assetPath)
        {
            string result = assetPath;

            int index = assetPath.LastIndexOf('/');
            if (index != -1)
            {
                result = result.Substring(index + 1);
            }

            index = result.LastIndexOf('.');
            if (index != -1)
            {
                result = result.Substring(0, index);
            }

            return result;
        }
    }
}