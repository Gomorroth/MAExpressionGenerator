using System;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [CustomEditor(typeof(MAExpressionPreset))]
    public sealed class MAExpressionPresetEditor : Editor
    {
        private SerializedProperty _name;
        private SerializedProperty _targets;

        private static bool _optionFoldout = false;
        private static bool[] _foldOuts;
        private static VRCExpressionsMenu _installTargetMenu;

        internal void OnEnable()
        {
            var gameObjects = new SerializedObject(
                serializedObject.targetObjects.Select(o =>
                    (UnityEngine.Object)(o as MAExpressionPreset).gameObject
                ).ToArray()
            );
            _name = gameObjects.FindProperty("m_Name");
            _targets = serializedObject.FindProperty(nameof(MAExpressionPreset.Targets));
        }

        public override void OnInspectorGUI()
        {
            var component = (target as MAExpressionPreset);
            component.RefreshTargets();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_name);
            if (EditorGUI.EndChangeCheck())
            {
                _name.serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Sync"))
            {
                serializedObject.ApplyModifiedProperties();
                (target as MAExpressionPreset).SyncObjectState();
                serializedObject.Update();
            }

            if (_optionFoldout = EditorGUILayout.Foldout(_optionFoldout, "Option"))
            {
                _installTargetMenu = EditorGUILayout.ObjectField("Install Target Menu", _installTargetMenu, typeof(VRCExpressionsMenu), true) as VRCExpressionsMenu;
            }

            EditorGUILayout.Separator();

            Array.Resize(ref _foldOuts, _targets.arraySize);

            for (int i = 0; i < _targets.arraySize; i++)
            {
                var x = _targets.GetArrayElementAtIndex(i);
                var list = x.FindPropertyRelative(nameof(MAExpressionPreset.Group.Targets));
                var generator = x.FindPropertyRelative(nameof(MAExpressionPreset.Group.Generator)).objectReferenceValue as MAExpressionGenerator;

                if (_foldOuts[i] = EditorGUILayout.BeginFoldoutHeaderGroup(_foldOuts[i], generator.name))
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
            }

            serializedObject.ApplyModifiedProperties();
        }

        internal static void GeneratePresets(GameObject avatarObject, MAExpressionPreset[] presets)
        {
            var fx = AssetGenerator.CreateArtifact(useModularAvatarTemporaryFolder: true);

            var obj = new GameObject("Preset");
            obj.SetActive(false);
            obj.transform.parent = avatarObject.transform;

            var layer = new AnimatorControllerLayer()
            {
                name = "Preset",
                defaultWeight = 1,
                stateMachine = new AnimatorStateMachine() { name = "Preset" }.HideInHierarchy().AddTo(fx),
            };

            var s = layer.stateMachine;

            var blank = new AnimationClip().HideInHierarchy().AddTo(fx);

            var states = new AnimatorState[presets.Length];

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                var state = new AnimatorState()
                {
                    name = preset.name,
                    writeDefaultValues = false,
                    motion = blank,
                }
                .HideInHierarchy().AddTo(fx);

                var d = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                var a = preset.Targets.Select(x => x.Targets.Select(y => (Name: MAExpressionGeneratorEditor.GetParameterName(x.Generator.ParamterPrefix, y.Object), y.Enable))).SelectMany(x => x);
                d.parameters.AddRange(a.Select(x => new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter() { name = x.Name, type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set, value = x.Enable ? 1 : 0 }));

                states[i] = state;
            }
            
            var idle = new AnimatorState()
            {
                name = "Idle",
                writeDefaultValues = false,
                motion = blank,
            }
            .HideInHierarchy().AddTo(fx);

            s.AddState(idle, s.entryPosition + new Vector3(200, 0));

            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                var t = new AnimatorStateTransition()
                {
                    destinationState = idle,
                    conditions = new AnimatorCondition[] { new AnimatorCondition() { parameter = "Preset", mode = AnimatorConditionMode.Equals, threshold = 0 } },
                    duration = 0,
                    hasExitTime = false,
                }.HideInHierarchy().AddTo(fx);

                state.AddTransition(t);

                t = new AnimatorStateTransition()
                {
                    destinationState = state,
                    conditions = new AnimatorCondition[] { new AnimatorCondition() { parameter = "Preset", mode = AnimatorConditionMode.Equals, threshold = i + 1  } },
                    duration = 0,
                    hasExitTime = false,
                }.HideInHierarchy().AddTo(fx);

                idle.AddTransition(t);

                for (int i2 = i + 1; i2 < states.Length; i2++)
                {
                    var state2 = states[i2];

                    state2.AddTransition(t);

                    t = new AnimatorStateTransition()
                    {
                        destinationState = state2,
                        conditions = new AnimatorCondition[] { new AnimatorCondition() { parameter = "Preset", mode = AnimatorConditionMode.Equals, threshold = i2 + 1 } },
                        duration = 0,
                        hasExitTime = false,
                    }.HideInHierarchy().AddTo(fx);

                    state.AddTransition(t);
                }

                s.AddState(state, s.entryPosition + new Vector3(200, 100 * (i + 1)));
            }

            fx.AddLayer(layer);

            fx.AddParameter("Preset", AnimatorControllerParameterType.Int);

            obj.GetOrAddComponent<ModularAvatarMergeAnimator>(x =>
            {
                x.pathMode = MergeAnimatorPathMode.Absolute;
                x.matchAvatarWriteDefaults = true;
                x.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                x.animator = fx;
            });

            obj.GetOrAddComponent<ModularAvatarParameters>(x =>
            {
                x.parameters.Add(new ParameterConfig() { saved = false, syncType = ParameterSyncType.NotSynced, nameOrPrefix = "Preset", localOnly = true, });
            });

            obj.GetOrAddComponent<ModularAvatarMenuInstaller>(x =>
            {
                x.installTargetMenu = _installTargetMenu;
            });

            obj.GetOrAddComponent<ModularAvatarMenuItem>(x =>
            {
                x.MenuSource = SubmenuSource.MenuAsset;
                x.Control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                x.Control.subMenu = new VRCExpressionsMenu()
                {
                    controls = presets.Select((y, i)=> 
                    new VRCExpressionsMenu.Control() 
                    { 
                        name = y.gameObject.name,
                        type = VRCExpressionsMenu.Control.ControlType.Button,
                        parameter = new VRCExpressionsMenu.Control.Parameter() { name = "Preset" },
                        value = i + 1,
                    }).ToList(),
                }
                .AddTo(fx);
            });

            AssetDatabase.SaveAssets();

            obj.SetActive(true);
        }
    }
}
