using System.IO;
using UnityEditor;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [InitializeOnLoad]
    internal static class AssetGenerator
    {
        private const string EditorPrefsKey = "gomoru.su.MAExpressionGenerator.generatedPrefabGUID";
        private const string PrefabPath = "Assets/MAExpressionGenerator/ExpressionGenerator.prefab";

        static AssetGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                var guid = EditorPrefs.GetString(EditorPrefsKey, null);
                if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    var directory = Path.GetDirectoryName(PrefabPath);
                    if (!AssetDatabase.IsValidFolder(directory))
                    {
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(directory), Path.GetFileName(directory));
                    }
                    var prefab = new GameObject(Path.GetFileNameWithoutExtension(PrefabPath)) { hideFlags = HideFlags.HideInHierarchy };
                    var generator = prefab.AddComponent<MAExpressionGenerator>();

                    PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
                    GameObject.DestroyImmediate(prefab);
                    EditorPrefs.SetString(EditorPrefsKey, AssetDatabase.AssetPathToGUID(PrefabPath));
                }
            };
        }
    }
}
