using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [CustomEditor(typeof(MAExpressionPresetManager))]
    internal sealed class MAExpressionPresetManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var avatar = (target as MAExpressionPresetManager).GetComponentInParent<VRCAvatarDescriptor>();
            if (avatar != null)
            {
                var presets = avatar.GetComponentsInChildren<MAExpressionPreset>(true);
                if (presets != null && presets.Length != 0)
                {
                    EditorGUILayout.BeginFoldoutHeaderGroup(true, "Presets");
                    EditorGUI.BeginDisabledGroup(true);
                    foreach (var preset in presets)
                    {
                        EditorGUILayout.ObjectField(preset, typeof(MAExpressionPreset), true);
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("Preset is not found", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Preset Manager is not placed inside the avatar", MessageType.Warning);
            }
        }
    }
}
