using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [InitializeOnLoad]
    internal static class AssetGenerator
    {
        private const string PrefabEditorPrefsKey = "gomoru.su.MAExpressionGenerator.generatedPrefabGUID";
        private const string PresetPrefabEditorPrefsKey = "gomoru.su.MAExpressionGenerator.PresetPrefabGUID";
        private const string ArtifactFolderEditorPrefsKey = "gomoru.su.MAExpressionGenerator.ArtifactFolderGUID";
        private const string PrefabPath = "Assets/MAExpressionGenerator/ExpressionGenerator.prefab";
        private const string PresetPrefabPath = "Assets/MAExpressionGenerator/ExpressionPreset.prefab";
        private const string ArtifactFolderPath = "Assets/MAExpressionGenerator/Artifact";

        static AssetGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                var guid = EditorPrefs.GetString(PrefabEditorPrefsKey, null);
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
                    EditorPrefs.SetString(PrefabEditorPrefsKey, AssetDatabase.AssetPathToGUID(PrefabPath));
                }

                guid = EditorPrefs.GetString(PresetPrefabEditorPrefsKey, null);
                if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    var directory = Path.GetDirectoryName(PresetPrefabPath);
                    if (!AssetDatabase.IsValidFolder(directory))
                    {
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(directory), Path.GetFileName(directory));
                    }
                    var prefab = new GameObject(Path.GetFileNameWithoutExtension(PresetPrefabPath)) { hideFlags = HideFlags.HideInHierarchy };
                    var generator = prefab.AddComponent<MAExpressionPreset>();

                    PrefabUtility.SaveAsPrefabAsset(prefab, PresetPrefabPath);
                    GameObject.DestroyImmediate(prefab);
                    EditorPrefs.SetString(PresetPrefabEditorPrefsKey, AssetDatabase.AssetPathToGUID(PrefabPath));
                }
            };
        }

        public static AnimatorController CreateArtifact(string prefix = null, bool useModularAvatarTemporaryFolder = false)
        {
            var fx = new AnimatorController();

            string path;
            if (useModularAvatarTemporaryFolder)
            {
                path = GetGeneratedAssetsFolder();
            }
            else
            {
                var guid = EditorPrefs.GetString(ArtifactFolderEditorPrefsKey, null);
                if (string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)) || !AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/MAExpressionGenerator"))
                        AssetDatabase.CreateFolder("Assets", "MAExpressionGenerator");
                    guid = AssetDatabase.CreateFolder("Assets/MAExpressionGenerator", "Artifact");
                    EditorPrefs.SetString(ArtifactFolderEditorPrefsKey, guid);
                }
                path = AssetDatabase.GUIDToAssetPath(guid);
            }


            AssetDatabase.CreateAsset(fx, Path.Combine(path, $"{prefix}{(string.IsNullOrEmpty(prefix) ? "" : "_")}{GUID.Generate()}.controller"));
            return fx;
        }

        private static MethodInfo _GetGeneratedAssetsFolder = typeof(nadena.dev.modular_avatar.core.editor.AvatarProcessor).Assembly.GetTypes().FirstOrDefault(x => x.Name == "Util")?.GetMethod(nameof(GetGeneratedAssetsFolder), BindingFlags.Static | BindingFlags.NonPublic);

        public static string GetGeneratedAssetsFolder()
        {
            var method = _GetGeneratedAssetsFolder;
            if (method != null)
                return method.Invoke(null, null) as string;

            return null;
        }
    }
}
