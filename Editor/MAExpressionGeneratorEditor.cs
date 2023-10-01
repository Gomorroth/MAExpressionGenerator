using UnityEditor;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [CustomEditor(typeof(MAExpressionGenerator))]
    public sealed class MAExpressionGeneratorEditor : Editor
    {
        private SerializedProperty _targets;
        private SerializedProperty _parameterPrefix;
        private SerializedProperty _generateBoneToggle;
        private UnityEditorInternal.ReorderableList _targetsList;

        private static bool _isOptionFoldout = false;

        public void OnEnable()
        {
            _targets = serializedObject.FindProperty(nameof(MAExpressionGenerator.Targets));
            _parameterPrefix = serializedObject.FindProperty(nameof(MAExpressionGenerator.ParamterPrefix));
            _generateBoneToggle = serializedObject.FindProperty(nameof(MAExpressionGenerator.GenerateBoneToggle));

            _targetsList = new UnityEditorInternal.ReorderableList(serializedObject, _targets)
            {
                draggable = false,
                displayAdd = false,
                displayRemove = false,
                footerHeight = 0,
                drawHeaderCallback = rect =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;

                    var enableRect = rect;
                    enableRect.width = rect.height * "Include".Length / 2;

                    var activeRect = rect;
                    activeRect.width = rect.height * "Active".Length / 2;
                    activeRect.x += enableRect.width;

                    var targetRect = rect;
                    targetRect.width -= enableRect.width + activeRect.width;
                    targetRect.x += enableRect.width + activeRect.width;


                    EditorGUI.LabelField(enableRect, "Include");
                    EditorGUI.LabelField(activeRect, "Enable");
                    EditorGUI.LabelField(targetRect, "Target");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = _targets.GetArrayElementAtIndex(index);
                    var target = element.FindPropertyRelative(nameof(TargetObject.Object));
                    var include = element.FindPropertyRelative(nameof(TargetObject.Include));
                    var enable = element.FindPropertyRelative(nameof(TargetObject.Enable));

                    rect.height = EditorGUIUtility.singleLineHeight;

                    var includeRect = rect;
                    includeRect.width = rect.height * "Include".Length / 2;
                    includeRect.x += includeRect.width / 4;

                    var enableRect = rect;
                    enableRect.width = rect.height * "Enable".Length / 2;
                    enableRect.x += includeRect.width + enableRect.width / 4;

                    var targetRect = rect;
                    targetRect.width -= includeRect.width + enableRect.width;
                    targetRect.x += includeRect.width + enableRect.width;

                    EditorGUI.PropertyField(includeRect, include, GUIContent.none);
                    EditorGUI.PropertyField(enableRect, enable, GUIContent.none);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField (targetRect, target, GUIContent.none);
                    EditorGUI.EndDisabledGroup();
                },
            };

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _targetsList.DoLayoutList();

            EditorGUILayout.PropertyField(_generateBoneToggle);

            if (_isOptionFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_isOptionFoldout, "Option"))
            {
                EditorGUILayout.PropertyField(_parameterPrefix);
            }

            EditorGUILayout.Separator();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Run"))
            {
                MAExpressionGeneratorCore.GenerateExpressions(target as MAExpressionGenerator);
            }
        }
    }
}
