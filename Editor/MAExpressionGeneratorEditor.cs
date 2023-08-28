using UnityEditor;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [CustomEditor(typeof(MAExpressionGenerator))]
    public sealed class MAExpressionGeneratorEditor : Editor
    {
        private SerializedProperty _targets;
        private SerializedProperty _parameterPrefix;
        private UnityEditorInternal.ReorderableList _targetsList;

        private static bool _isOptionFoldout = false;

        public void OnEnable()
        {
            _targets = serializedObject.FindProperty(nameof(MAExpressionGenerator.Targets));
            _parameterPrefix = serializedObject.FindProperty(nameof(MAExpressionGenerator.ParamterPrefix));

            _targetsList = new UnityEditorInternal.ReorderableList(serializedObject, _targets)
            {
                draggable = false,
                displayAdd = false,
                displayRemove = false,
                footerHeight = 0,
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Target"),
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = _targets.GetArrayElementAtIndex(index);
                    var target = element.FindPropertyRelative(nameof(TargetObject.Object));
                    var enable = element.FindPropertyRelative(nameof(TargetObject.Enable));

                    rect.height = EditorGUIUtility.singleLineHeight;
                    var rect2 = rect;
                    rect2.width = rect2.height;
                    EditorGUI.PropertyField(rect2, enable, GUIContent.none);

                    rect.x += rect.height;
                    rect.width -= rect.height;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField (rect, target, GUIContent.none);
                    EditorGUI.EndDisabledGroup();
                },
            };

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _targetsList.DoLayoutList();

            if (_isOptionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_isOptionFoldout, "Option"))
            {
                EditorGUILayout.PropertyField(_parameterPrefix);
            }

            EditorGUILayout.Separator();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Run"))
            {
                Processor.GenerateExpressions(target as MAExpressionGenerator);
            }
        }
    }
}
