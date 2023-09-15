using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Collections.Immutable;
using VRC.SDKBase;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public static class Processor
    {
        public static void GenerateExpressions(MAExpressionGenerator target)
        {
            var obj = target.gameObject;
            var parent = obj.transform.parent.gameObject;
            var avatar = obj.GetComponentInParent<VRCAvatarDescriptor>();
            string avatarName = avatar == null ? null : string.IsNullOrEmpty(avatar.Name) ? avatar.gameObject.name : avatar.Name;
            var container = AssetGenerator.CreateAssetContainer(subDir: $"{avatar.gameObject.scene.name}/{avatarName}{(avatar.gameObject != parent ? $"/{parent.name}" : "")}");
            var fx = new AnimatorController().AddTo(container);

            var items = target.Targets;

            var parameter = obj.GetOrAddComponent<ModularAvatarParameters>(x =>
            {
                x.parameters.Clear();
            });

            var boneList = new List<(string Parameter, GameObject Bone)>();
            bool useBoneToggle = true;

            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = "Menu";
            menu.AddTo(container);

            foreach (var item in items)
            {
                var o = item.Object;
                var parameterName = GetParameterName(target.ParamterPrefix, o);
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
                var anim = new AnimationClip() { name = $"{o.name} ONOFF" }.AddTo(container);

                var path = o.transform.GetRelativePath(o.GetRoot()?.transform);
                anim.SetCurve(path, typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0, 0, 1 / 60f, 1));

                parameter.parameters.Add(new ParameterConfig() { nameOrPrefix = parameterName, syncType = ParameterSyncType.Bool, saved = true, defaultValue = item.Active ? 1 : 0 });
                fx.AddParameter(new AnimatorControllerParameter() { name = parameterName, defaultFloat = item.Active ? 1 : 0, type = AnimatorControllerParameterType.Float });

                var layer = fx.AddLayer(anim.name, container);
                var state = layer.stateMachine.AddState("ONOFF", layer.stateMachine.entryPosition + new Vector3(200, 0));
                state.writeDefaultValues = false;
                state.motion = anim;
                state.timeParameterActive = true;
                state.timeParameter = parameterName;

                menu.controls.Add(new VRCExpressionsMenu.Control() { name = o.name, type = VRCExpressionsMenu.Control.ControlType.Toggle, parameter = new VRCExpressionsMenu.Control.Parameter() { name = parameterName } });
            }

            if (target.GenerateBoneToggle)
            {
                foreach (var boneGroup in boneList.GroupBy(x => x.Bone).ToLookup(x => x.Select(x2 => x2.Parameter).Distinct(), x => x.Key).GroupBy(x => x.Key, x => x as IEnumerable<GameObject>, new Comparer()).OrderBy(x => x.Key.Count()))
                {
                    var name = $"Bone/{string.Join(", ", boneGroup.Key.Select(x => x.Substring(x.LastIndexOf("/") + 1).Replace(" ONOFF", "")))}";
                    name = EditorUtils.MakeAnimatorSafeName(name);

                    var off = new AnimationClip() { name = $"{name} OFF" }.AddTo(container);
                    var on = new AnimationClip() { name = $"{name} ON" }.AddTo(container);

                    foreach (var x in boneGroup)
                    {
                        foreach (var x2 in x)
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
                        stateMachine = new AnimatorStateMachine() { name = name }.HideInHierarchy().AddTo(container),
                    };
                    var s = layer.stateMachine;

                    var state_off = new AnimatorState()
                    {
                        name = "OFF",
                        writeDefaultValues = false,
                        motion = off,
                    }
                    .HideInHierarchy().AddTo(container);

                    var state_on = new AnimatorState()
                    {
                        name = "ON",
                        writeDefaultValues = false,
                        motion = on,
                    }
                    .HideInHierarchy().AddTo(container);

                    foreach (var x in boneGroup.Key)
                    {
                        state_off.AddTransition(new AnimatorStateTransition
                        {
                            hideFlags = HideFlags.HideInHierarchy,
                            hasExitTime = false,
                            duration = 0,
                            conditions = new[] { new AnimatorCondition() { parameter = x, mode = AnimatorConditionMode.Greater, threshold = 0.5f } },
                            destinationState = state_on
                        }.HideInHierarchy().AddTo(container));
                    }

                    state_on.AddTransition(new AnimatorStateTransition
                    {
                        hideFlags = HideFlags.HideInHierarchy,
                        hasExitTime = false,
                        duration = 0,
                        conditions = boneGroup.Key.Select(x => new AnimatorCondition() { parameter = x, mode = AnimatorConditionMode.Less, threshold = 0.5f }).ToArray(),
                        destinationState = state_off
                    }.HideInHierarchy().AddTo(container));

                    s.AddState(state_off, s.entryPosition + new Vector3(200, 0));
                    s.AddState(state_on, s.entryPosition + new Vector3(200, 100));

                    fx.AddLayer(layer);
                }
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
                x.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                x.matchAvatarWriteDefaults = true;
                x.pathMode = MergeAnimatorPathMode.Absolute;
            });

            obj.name = parent.name;

            AssetDatabase.SaveAssets();
        }

        public static void GeneratePresets(GameObject avatarObject)
        {
            var presets = avatarObject.GetComponentsInChildren<MAExpressionPreset>();

            if (presets.Length == 0)
                return;

            var container = AssetGenerator.CreateAssetContainer(useModularAvatarTemporaryFolder: true);
            var fx = new AnimatorController().AddTo(container);

            var manager = avatarObject.GetComponentInChildren<MAExpressionPresetManager>();
            GameObject obj;
            if (manager != null)
            {
                obj = manager.gameObject;
            }
            else
            {
                obj = new GameObject("Preset");
                obj.transform.parent = avatarObject.transform;
            }
            obj.SetActive(false);

            var layer = new AnimatorControllerLayer()
            {
                name = "Preset",
                defaultWeight = 1,
                stateMachine = new AnimatorStateMachine() { name = "Preset" }.HideInHierarchy().AddTo(container),
            };

            var s = layer.stateMachine;

            var blank = new AnimationClip().HideInHierarchy().AddTo(container);

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
                .HideInHierarchy().AddTo(container);

                var d = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                var a = preset.Targets.Select(x => x.Targets.Select(y => (Name: GetParameterName(x.Generator.ParamterPrefix, y.Object), y.Enable))).SelectMany(x => x);
                d.parameters.AddRange(a.Select(x => new VRC_AvatarParameterDriver.Parameter() { name = x.Name, type = VRC_AvatarParameterDriver.ChangeType.Set, value = x.Enable ? 1 : 0 }));

                states[i] = state;
            }

            var idle = new AnimatorState()
            {
                name = "Idle",
                writeDefaultValues = false,
                motion = blank,
            }
            .HideInHierarchy().AddTo(container);

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
                }.HideInHierarchy().AddTo(container);

                state.AddTransition(t);

                t = new AnimatorStateTransition()
                {
                    destinationState = state,
                    conditions = new AnimatorCondition[] { new AnimatorCondition() { parameter = "Preset", mode = AnimatorConditionMode.Equals, threshold = i + 1 } },
                    duration = 0,
                    hasExitTime = false,
                }.HideInHierarchy().AddTo(container);

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
                    }.HideInHierarchy().AddTo(container);

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
                x.parameters.Add(new ParameterConfig() { saved = false, syncType = ParameterSyncType.Int, nameOrPrefix = "Preset", localOnly = true });
            });

            var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>().AddTo(container); ;
            menu.controls = presets.Select((y, i) =>
                new VRCExpressionsMenu.Control()
                {
                    name = y.gameObject.name,
                    type = VRCExpressionsMenu.Control.ControlType.Button,
                    parameter = new VRCExpressionsMenu.Control.Parameter() { name = "Preset" },
                    value = i + 1,
                }).ToList();

            obj.GetOrAddComponent<ModularAvatarMenuItem>(x =>
            {
                x.MenuSource = SubmenuSource.MenuAsset;
                x.Control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                x.Control.subMenu = menu;
            });

            AssetDatabase.SaveAssets();
        }

        public static void GenerateInstallTargets(GameObject avatarObject)
        {
            var installTarget = avatarObject.GetComponentInChildren<MAExpressionGeneratorMenuInstallTarget>();
            if (installTarget == null)
                return;

            var obj = installTarget.gameObject;

            var generators = avatarObject.GetComponentsInChildren<MAExpressionGenerator>();

            if (MenuInstallerTargetType == null)
            {
                var type = MenuInstallerTargetType = typeof(ModularAvatarMenuInstaller).Module.GetTypes().First(y => y.Name == "ModularAvatarMenuInstallTarget");
                MenuInstallerTargetInstallerField = type.GetField("installer");
            }

            foreach (var generator in generators)
            {
                var installer = generator.GetComponent<ModularAvatarMenuInstaller>();
                if (installer != null)
                {
                    var o = new GameObject();
                    o.SetActive(false);
                    o.transform.parent = obj.transform.parent;
                    var c = o.AddComponent(MenuInstallerTargetType);
                    MenuInstallerTargetInstallerField.SetValue(c, installer);
                }
            }
        }

        private static Type MenuInstallerTargetType;
        private static FieldInfo MenuInstallerTargetInstallerField;

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
                const int Prime1 = 1117;
                const int Prime2 = 1777;

                int hash = Prime1;
                foreach (var x in obj)
                {
                    hash = hash * Prime2 + x.GetHashCode();
                }
                return hash;
            }
        }
    }
}
