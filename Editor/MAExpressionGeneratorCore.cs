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
using System.Reflection.Emit;
using Object = UnityEngine.Object;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public partial class MAExpressionGeneratorCore
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

            foreach (var item in items.Where(x => x.Enable))
            {
                var o = item.Object;
                var parameterName = GetParameterName(target.ParamterPrefix, o);
                var smr = o.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && useBoneToggle)
                {
                    var mesh = smr.sharedMesh;
                    var boneWeights = mesh.boneWeights;
                    foreach (var bone in boneWeights.Select(x => smr.bones[x.boneIndex0].gameObject).Where(x => !IsAvatarBone(x.name)))
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

        private static Type MenuInstallerTargetType;
        private static FieldInfo MenuInstallerTargetInstallerField;

        internal static string GetParameterName(string prefix, GameObject obj)
        {
            return RuntimeUtils.CombinePath(prefix, obj.transform.GetRelativePath(obj.GetParent()?.transform));
        }

        private static bool IsAvatarBone(string boneName) => HeuristicBoneMapper.IsHeuristicBone(boneName)
             || boneName.Contains("Breast", StringComparison.OrdinalIgnoreCase)
             || boneName.Contains("Bust", StringComparison.OrdinalIgnoreCase)
            ;

        private static class HeuristicBoneMapper
        {
            private static Func<string, bool> _isHeuristicBone;

            public static bool IsHeuristicBone(string name) => _isHeuristicBone.Invoke(name);

            static HeuristicBoneMapper()
            {
                var type = typeof(nadena.dev.modular_avatar.core.editor.AvatarProcessor).Assembly.GetType("nadena.dev.modular_avatar.core.editor.HeuristicBoneMapper");
                if (type == null)
                    throw new Exception("HeuristicBoneMapper not found.");

                var method = new DynamicMethod("", typeof(bool), new[] {typeof(string) }, type, true);
                var il = method.GetILGenerator();
                il.Emit(OpCodes.Ldsfld, type.GetField("NameToBoneMap", BindingFlags.NonPublic | BindingFlags.Static));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, type.GetMethod("NormalizeName", BindingFlags.NonPublic | BindingFlags.Static));
                il.Emit(OpCodes.Callvirt, typeof(ImmutableDictionary<string, HumanBodyBones>).GetMethod("ContainsKey"));
                il.Emit(OpCodes.Ret);

                _isHeuristicBone = method.CreateDelegate(typeof(Func<string, bool>)) as Func<string, bool>;
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
