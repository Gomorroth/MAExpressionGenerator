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
                    EditorGUI.LabelField(activeRect, "Active");
                    EditorGUI.LabelField(targetRect, "Target");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = _targets.GetArrayElementAtIndex(index);
                    var target = element.FindPropertyRelative(nameof(TargetObject.Object));
                    var enable = element.FindPropertyRelative(nameof(TargetObject.Enable));
                    var active = element.FindPropertyRelative(nameof(TargetObject.Active));

                    rect.height = EditorGUIUtility.singleLineHeight;

                    var enableRect = rect;
                    enableRect.width = rect.height * "Include".Length / 2;
                    enableRect.x += enableRect.width / 4;

                    var activeRect = rect;
                    activeRect.width = rect.height * "Active".Length / 2;
                    activeRect.x += enableRect.width + activeRect.width / 4;

                    var targetRect = rect;
                    targetRect.width -= enableRect.width + activeRect.width;
                    targetRect.x += enableRect.width + activeRect.width;

                    EditorGUI.PropertyField(enableRect, enable, GUIContent.none);
                    EditorGUI.PropertyField(activeRect, active, GUIContent.none);

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
                Processor.GenerateExpressions(target as MAExpressionGenerator);
            }
        }
    }
}
