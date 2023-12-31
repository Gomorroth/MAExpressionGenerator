﻿using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [CustomEditor(typeof(MAExpressionPreset))]
    public sealed class MAExpressionPresetEditor : Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _targets;

        private static bool[] _foldOuts;

        internal void OnEnable()
        {
            var gameObjects = new SerializedObject(
                serializedObject.targetObjects.Select(o =>
                    (Object)(o as MAExpressionPreset).gameObject
                ).ToArray()
            );
            _name = gameObjects.FindProperty("m_Name");
            _targets = serializedObject.FindProperty(nameof(MAExpressionPreset.Targets));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_name);
            if (EditorGUI.EndChangeCheck())
            {
                _name.serializedObject.ApplyModifiedProperties();
            }

            if ((target as MAExpressionPreset)?.GetComponentInParent<VRCAvatarDescriptor>() == null)
            {
                EditorGUILayout.HelpBox("Preset is not placed inside the avatar", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Sync"))
            {
                //SyncObjectState();
                (target as MAExpressionPreset).HierarchyToPreset();
            }

            if (GUILayout.Button("Apply"))
            {
                //ApplyObjectState();
                (target as MAExpressionPreset).PresetToHierarchy();
            }

            EditorGUILayout.Separator();

            if (_foldOuts?.Length != _targets.arraySize)
            {
                Array.Resize(ref _foldOuts, _targets.arraySize);
                for (int i = 0; i < _foldOuts.Length; i++)
                {
                    _foldOuts[i] = true;
                }
            }

            for (int i = 0; i < _targets.arraySize; i++)
            {
                var x = _targets.GetArrayElementAtIndex(i);
                var list = x.FindPropertyRelative(nameof(MAExpressionPreset.Group.Targets));
                var generator = x.FindPropertyRelative(nameof(MAExpressionPreset.Group.Target)).objectReferenceValue as MAExpressionObjectController;

                if (_foldOuts[i] = EditorGUILayout.BeginFoldoutHeaderGroup(_foldOuts[i], generator?.DisplayName))
                {
                    new UnityEditorInternal.ReorderableList(serializedObject, list)
                    {
                        draggable = false,
                        displayAdd = false,
                        displayRemove = false,
                        headerHeight = 4,
                        footerHeight = 0,
                        drawHeaderCallback = rect => { },
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            var element = list.GetArrayElementAtIndex(index);
                            var target = element.FindPropertyRelative(nameof(TargetObject.Object));
                            var enable = element.FindPropertyRelative(nameof(TargetObject.Enable));

                            rect.height = EditorGUIUtility.singleLineHeight;
                            var rect2 = rect;
                            rect2.width = rect2.height;
                            EditorGUI.PropertyField(rect2, enable, GUIContent.none);

                            rect.x += rect.height;
                            rect.width -= rect.height;
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.PropertyField(rect, target, GUIContent.none);
                            EditorGUI.EndDisabledGroup();
                        },
                    }.DoLayoutList();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void SyncObjectState()
        {
            var count = _targets.arraySize;
            for (int i = 0; i < count; i++)
            {
                var group = _targets.GetArrayElementAtIndex(i);
                var targets = group.FindPropertyRelative(nameof(MAExpressionPreset.Group.Targets));
                var count2 = targets.arraySize;
                for (int i2 = 0; i2 < count2; i2++)
                {
                    var target = targets.GetArrayElementAtIndex(i2);
                    var o = target.FindPropertyRelative(nameof(TargetObject.Object));
                    var e = target.FindPropertyRelative(nameof(TargetObject.Enable));
                    e.boolValue = (o.objectReferenceValue as GameObject).activeInHierarchy;
                }
            }
        }

        public void ApplyObjectState()
        {
            var count = _targets.arraySize;
            for (int i = 0; i < count; i++)
            {
                var group = _targets.GetArrayElementAtIndex(i);
                var targets = group.FindPropertyRelative(nameof(MAExpressionPreset.Group.Targets));
                var count2 = targets.arraySize;
                for (int i2 = 0; i2 < count2; i2++)
                {
                    var target = targets.GetArrayElementAtIndex(i2);
                    var o = target.FindPropertyRelative(nameof(TargetObject.Object));
                    var e = target.FindPropertyRelative(nameof(TargetObject.Enable));
                    Undo.RecordObject(o.objectReferenceValue, "Apply Preset");
                    (o.objectReferenceValue as GameObject).SetActive(e.boolValue);
                }
            }
        }
    }
}
