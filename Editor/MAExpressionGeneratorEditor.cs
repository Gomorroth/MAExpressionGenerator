using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

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
        }

        public override void OnInspectorGUI()
        {
            (target as MAExpressionGenerator).RefreshTargets();
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
                Generate();
            }
        }

        private void Generate()
        {
            var component = target as MAExpressionGenerator;
            if (component == null)
                return;

            var obj = component.gameObject;
            var parent = obj.transform.parent.gameObject;
            var fx = AssetGenerator.CreateArtifact(prefix: $"{parent.name}_{DateTime.Now:yyyyMMdd_HHmmss}");

            var items = component.Targets;

            var parameter = obj.GetOrAddComponent<ModularAvatarParameters>(x =>
            {
                x.parameters.Clear();
            });

            var boneList = new List<(string Parameter, GameObject Bone)>();
            bool useBoneToggle = true;

            var menu = new VRCExpressionsMenu() { name = "Menu" }.AddTo(fx);

            foreach (var item in items)
            {
                var o = item.Object;
                var parameterName = GetParameterName(component.ParamterPrefix, o);// EditorUtils.CombinePath(component.ParamterPrefix, o.transform.GetRelativePath(parent.transform));
                var smr = o.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && useBoneToggle)
                {
                    var mesh = smr.sharedMesh;
                    var boneWeights = mesh.boneWeights;
                    foreach (var bone in boneWeights.Select(x => smr.bones[x.boneIndex0].gameObject).Where(x => !IsHumanoidBone(x.name)))
                    {
                        if (!bone.IsIn(parent))
                        {
                            useBoneToggle = false;
                            break;
                        }
                        boneList.Add((parameterName, bone));
                    }
                }
                var anim = new AnimationClip() { name = $"{o.name} ONOFF" }.AddTo(fx);

                var path = o.transform.GetRelativePath(o.GetRoot()?.transform);
                anim.SetCurve(path, typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0, 0, 1 / 60f, 1));

                parameter.parameters.Add(new ParameterConfig() { nameOrPrefix = parameterName, syncType = ParameterSyncType.Bool, saved = true, defaultValue = o.activeInHierarchy ? 1 : 0 });
                fx.AddParameter(new AnimatorControllerParameter() { name = parameterName, defaultFloat = o.activeInHierarchy ? 1 : 0, type = AnimatorControllerParameterType.Float });

                var layer = new AnimatorControllerLayer()
                {
                    name = anim.name,
                    defaultWeight = 1,
                    stateMachine = new AnimatorStateMachine() { name = anim.name }.HideInHierarchy().AddTo(fx),
                };

                var s = layer.stateMachine;
                var state = new AnimatorState() 
                {
                    name = o.name,
                    writeDefaultValues = false,
                    motion = anim,
                    timeParameter = parameterName,
                    timeParameterActive = true,
                }
                .HideInHierarchy().AddTo(fx);
                
                s.AddState(state, s.entryPosition + new Vector3(200, 0));

                fx.AddLayer(layer);

                menu.controls.Add(new VRCExpressionsMenu.Control() { name = o.name, type = VRCExpressionsMenu.Control.ControlType.Toggle, parameter = new VRCExpressionsMenu.Control.Parameter() { name = parameterName } });
            }

            foreach (var boneGroup in boneList.GroupBy(x => x.Bone).ToLookup(x => x.Select(x2 => x2.Parameter).Distinct(), x => x.Key).GroupBy(x => x.Key, x => x as IEnumerable<GameObject>, new Comparer()).OrderBy(x => x.Key.Count()))
            {
                var name = $"Bone/{string.Join(", ", boneGroup.Key.Select(x => x.Substring(x.LastIndexOf("/") + 1).Replace(" ONOFF", "")))}";
                
                var off = new AnimationClip() { name = $"{name} OFF"}.AddTo(fx);
                var on = new AnimationClip() { name = $"{name} ON" }.AddTo(fx);

                foreach (var x in boneGroup)
                {
                    foreach(var x2 in x)
                    {
                        var path = x2.transform.GetRelativePath(x2.GetRoot()?.transform);
                        off.SetCurve(path, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 0));
                        on.SetCurve(path, typeof(GameObject), "m_IsActive", AnimationCurve.Constant(0, 0, 1));
                    }
                }

                var layer = new AnimatorControllerLayer()
                {
                    name = name,
                    defaultWeight = 1,
                    stateMachine = new AnimatorStateMachine() { name = name }.HideInHierarchy().AddTo(fx),
                };
                var s = layer.stateMachine;

                var state_off = new AnimatorState()
                {
                    name = name,
                    writeDefaultValues = false,
                    motion = off,
                }
                .HideInHierarchy().AddTo(fx);

                var state_on = new AnimatorState()
                {
                    name = name,
                    writeDefaultValues = false,
                    motion = on,
                }
                .HideInHierarchy().AddTo(fx);

                foreach (var x in boneGroup.Key)
                {
                    state_off.AddTransition(new AnimatorStateTransition
                    {
                        hideFlags = HideFlags.HideInHierarchy,
                        hasExitTime = false,
                        duration = 0,
                        conditions = new[] { new AnimatorCondition() { parameter = x, mode = AnimatorConditionMode.Greater, threshold = 0.5f } },
                        destinationState = state_on
                    }.HideInHierarchy().AddTo(fx));
                }

                state_on.AddTransition(new AnimatorStateTransition
                {
                    hideFlags = HideFlags.HideInHierarchy,
                    hasExitTime = false,
                    duration = 0,
                    conditions = boneGroup.Key.Select(x => new AnimatorCondition() { parameter = x, mode = AnimatorConditionMode.Less, threshold = 0.5f }).ToArray(),
                    destinationState = state_off
                }.HideInHierarchy().AddTo(fx));

                s.AddState(state_off, s.entryPosition + new Vector3(200, 0));
                s.AddState(state_on, s.entryPosition + new Vector3(200, 100));

                fx.AddLayer(layer);
            }

            obj.GetOrAddComponent<ModularAvatarMenuInstaller>(x =>
            {

            });

            obj.GetOrAddComponent<ModularAvatarMenuItem>(x =>
            {
                x.Control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                x.MenuSource = SubmenuSource.MenuAsset;
                x.Control.subMenu = menu;
            });

            obj.GetOrAddComponent<ModularAvatarMergeAnimator>(x =>
            {
                x.animator = fx;
                x.deleteAttachedAnimator = true;
                x.layerType = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX;
                x.matchAvatarWriteDefaults = true;
                x.pathMode = MergeAnimatorPathMode.Absolute;
            });

            obj.GetOrAddComponent<Animator>(x =>
            {
                x.runtimeAnimatorController = fx;
            });

            obj.name = parent.name;

            AssetDatabase.SaveAssets();
        }

        internal static string GetParameterName(string prefix, GameObject obj)
        {
            return EditorUtils.CombinePath(prefix, obj.transform.GetRelativePath(obj.GetParent()?.transform));
        }

        private static bool IsHumanoidBone(string boneName) => HeuristicBoneMapper.NameToBoneMap.ContainsKey(HeuristicBoneMapper.NormalizeName(boneName)) || boneName.IndexOf("Breast", StringComparison.OrdinalIgnoreCase) != -1;

        private static class HeuristicBoneMapper
        {
            private static FieldInfo _fieldInfo = typeof(nadena.dev.modular_avatar.core.editor.AvatarProcessor).Assembly.GetType("nadena.dev.modular_avatar.core.editor.HeuristicBoneMapper").GetField("NameToBoneMap", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new NullReferenceException();
            private static ImmutableDictionary<string, HumanBodyBones> _cache;
            public static ImmutableDictionary<string, HumanBodyBones> NameToBoneMap
            {
                get
                {
                    if (_cache == null)
                        _cache = _fieldInfo.GetValue(null) as ImmutableDictionary<string, HumanBodyBones> ?? throw new InvalidOperationException();
                    return _cache;
                }
            }

            public static string NormalizeName(string name)
            {
                return name.ToLowerInvariant()
                    .Replace("_", "")
                    .Replace(".", "")
                    .Replace(" ", "");
            }
        }

        private sealed class Comparer : IEqualityComparer<IEnumerable<string>>
        {
            public bool Equals(IEnumerable<string> x, IEnumerable<string> y)
            {
                if (x.Count() != y.Count())
                    return false;
                return !x.Zip(y, (a, b) => (a == b)).Any(x2 => !x2);
            }

            public int GetHashCode(IEnumerable<string> obj)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var x in obj)
                    sb.Append(x);
                return sb.ToString().GetHashCode();
            }
        }
    }
}
