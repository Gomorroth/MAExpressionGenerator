using System;
using System.IO;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [InitializeOnLoad]
    internal static class AssetGenerator
    {
        private const string PrefabEditorPrefsKey = "gomoru.su.MAExpressionGenerator.generatedPrefabGUID";
        private const string PresetPrefabEditorPrefsKey = "gomoru.su.MAExpressionGenerator.PresetPrefabGUID";
        private const string PresetManagerPrefabEditorPrefsKey = "gomoru.su.MAExpressionGenerator.ManagerPrefabGUID";
        private const string ArtifactFolderEditorPrefsKey = "gomoru.su.MAExpressionGenerator.ArtifactFolderGUID";
        private const string PrefabPath = "Assets/MAExpressionGenerator/Generator.prefab";
        private const string PresetPrefabPath = "Assets/MAExpressionGenerator/Preset.prefab";
        private const string ManagerPrefabPath = "Assets/MAExpressionGenerator/PresetManager.prefab";
        private const string ArtifactFolderPath = "Assets/MAExpressionGenerator/Artifact";

        static AssetGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                bool flag = false;

                flag |= CreatePrefab(PrefabEditorPrefsKey, PrefabPath, x => x.AddComponent<MAExpressionGenerator>());
                flag |= CreatePrefab(PresetPrefabEditorPrefsKey, PresetPrefabPath, x => x.AddComponent<MAExpressionPreset>());
                flag |= CreatePrefab(PresetManagerPrefabEditorPrefsKey, ManagerPrefabPath, x =>
                {
                    x.AddComponent<MAExpressionPresetManager>();
                    x.AddComponent<ModularAvatarMenuInstaller>();
                });

                if (flag)
                    AssetDatabase.SaveAssets();
            };
        }

        private static bool CreatePrefab(string prefsKey, string path, Action<GameObject> initialization)
        {
            var guid = EditorPrefs.GetString(prefsKey, null);
            if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
            {
                var directory = Path.GetDirectoryName(path);
                CreateDirectoryRecursive(directory);

                var prefab = new GameObject(Path.GetFileNameWithoutExtension(path)) { hideFlags = HideFlags.HideInHierarchy };
                initialization(prefab);

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                GameObject.DestroyImmediate(prefab);
                EditorPrefs.SetString(prefsKey, AssetDatabase.AssetPathToGUID(path));
                return true;
            }
            return false;
        }

        private static void CreateDirectoryRecursive(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path);
                CreateDirectoryRecursive(parent);
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }

        public static Object CreateAssetContainer(string prefix = null, string subDir = null)
        {
            var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.GeneratedAssets>();

            var guid = EditorPrefs.GetString(ArtifactFolderEditorPrefsKey, null);
            if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) || !AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)))
            {
                if (!AssetDatabase.IsValidFolder("Assets/MAExpressionGenerator"))
                    AssetDatabase.CreateFolder("Assets", "MAExpressionGenerator");
                guid = AssetDatabase.CreateFolder("Assets/MAExpressionGenerator", "Artifact");
                EditorPrefs.SetString(ArtifactFolderEditorPrefsKey, guid);
            }
            string path = AssetDatabase.GUIDToAssetPath(guid);

            var fileName = $"{prefix}{(string.IsNullOrEmpty(prefix) ? "" : "_")}{GUID.Generate()}.asset";

            if (!string.IsNullOrEmpty(subDir))
                path = Path.Combine(path, subDir, fileName);
            else 
                path = Path.Combine(path, fileName);

            CreateDirectoryRecursive(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(container, path);

            return container;
        }
    }
}
