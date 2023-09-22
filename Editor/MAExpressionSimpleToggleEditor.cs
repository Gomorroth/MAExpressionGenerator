using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [CustomEditor(typeof(MAExpressionSimpleToggle))]
    internal sealed class MAExpressionSimpleToggleEditor : Editor
    {
        private static bool IsParameterSettingsShowing = false;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MAExpressionSimpleToggle.Target)));
            if (IsParameterSettingsShowing = EditorGUILayout.BeginFoldoutHeaderGroup(IsParameterSettingsShowing, "Parameter Settings"))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MAExpressionSimpleToggle.IsSave)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MAExpressionSimpleToggle.IsSynced)));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
