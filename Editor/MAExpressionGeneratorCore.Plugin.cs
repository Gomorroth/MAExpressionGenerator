using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

[assembly: ExportsPlugin(typeof(gomoru.su.ModularAvatarExpressionGenerator.MAExpressionGeneratorCore))]

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    partial class MAExpressionGeneratorCore : Plugin<MAExpressionGeneratorCore>
    {
        public override string DisplayName => "MA Expression Generator";
        public override string QualifiedName => "gomoru.su.modular-avatar-expression-generator";

        protected override void Configure() => 
            InPhase(BuildPhase.Transforming)
            .Run(new GenerateInstallTargetsPass()).Then
            .Run(new GenerateSimpleTogglePass()).Then
            .Run(new GeneratePresetsPass()).Then
            .Run("Finalize MA Expression Generator", context =>
            {
                foreach(var x in context.AvatarRootObject.GetComponentsInChildren<MAExpressionBaseComponent>(true))
                    GameObject.DestroyImmediate(x);
            });

        private sealed class GenerateInstallTargetsPass : Pass<GenerateInstallTargetsPass>
        {
            protected override void Execute(BuildContext context) => GenerateInstallTargets(context.AvatarRootObject);

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
        }

        private sealed class GenerateSimpleTogglePass : Pass<GenerateSimpleTogglePass>
        {
            protected override void Execute(BuildContext context) => GenerateSimpleToggle(context);

            public static void GenerateSimpleToggle(BuildContext context)
            {
                var root = new GameObject();
                root.transform.parent = context.AvatarRootObject.transform;
                root.SetActive(false);

                var parameters = root.AddComponent<ModularAvatarParameters>();

                var fx = new AnimatorController() { name = "SimpleToggle" }.AddTo(context.AssetContainer);
                foreach (var component in context.AvatarRootObject.GetComponentsInChildren<MAExpressionSimpleToggle>())
                {
                    var obj = component.GetTargetObject();
                    var layer = fx.AddLayer(obj.name, context.AssetContainer);
                    var stateMachine = layer.stateMachine;
                    var state = stateMachine.AddState("ONOFF", Vector3.zero);
                    state.writeDefaultValues = false;
                    state.timeParameter = GetParameterName(component.GetParameterPrefix(), obj);
                    state.timeParameterActive = true;

                    var anim = new AnimationClip() { name = $"{obj.name} ONOFF" }.AddTo(context.AssetContainer);

                    var path = obj.transform.GetRelativePath(context.AvatarRootObject.transform);
                    anim.SetCurve(path, typeof(GameObject), "m_IsActive", AnimationCurve.Linear(0, 0, 1 / 60f, 1));

                    state.motion = anim;

                    fx.AddParameter(new AnimatorControllerParameter() { name = state.timeParameter, type = AnimatorControllerParameterType.Float, defaultFloat = obj.activeInHierarchy ? 1 : 0 });
                    parameters.parameters.Add(new ParameterConfig() { nameOrPrefix = state.timeParameter, syncType = ParameterSyncType.Bool, defaultValue = obj.activeInHierarchy ? 1 : 0, saved = component.IsSave, localOnly = !component.IsSynced });

                    var menuInstaller = root.AddComponent<ModularAvatarMenuInstaller>();
                    var menu = menuInstaller.menuToAppend = ScriptableObject.CreateInstance<VRCExpressionsMenu>().AddTo(context.AssetContainer);
                    menu.controls.Add(new VRCExpressionsMenu.Control() { name = component.DisplayName, parameter = new VRCExpressionsMenu.Control.Parameter() { name = state.timeParameter }, type = VRCExpressionsMenu.Control.ControlType.Toggle });
                }

                root.GetOrAddComponent<ModularAvatarMergeAnimator>(x =>
                {
                    x.animator = fx;
                    x.layerType = VRCAvatarDescriptor.AnimLayerType.FX;
                    x.matchAvatarWriteDefaults = true;
                    x.pathMode = MergeAnimatorPathMode.Absolute;
                });
            }
        }

        private sealed class GeneratePresetsPass : Pass<GeneratePresetsPass>
        {
            protected override void Execute(BuildContext context) => GeneratePresets(context);

            public static void GeneratePresets(BuildContext context)
            {
                var presets = context.AvatarRootObject.GetComponentsInChildren<MAExpressionPreset>();

                if (presets.Length == 0)
                    return;

                var fx = new AnimatorController() { name = "Preset" }.AddTo(context.AssetContainer);

                var manager = context.AvatarRootObject.GetComponentInChildren<MAExpressionPresetManager>();
                GameObject obj;
                if (manager != null)
                {
                    obj = manager.gameObject;
                    obj.SetActive(false);
                }
                else
                {
                    obj = new GameObject("Preset");
                    obj.transform.parent = context.AvatarRootObject.transform;
                    obj.SetActive(false);
                    obj.AddComponent<ModularAvatarMenuInstaller>();
                }

                var layer = new AnimatorControllerLayer()
                {
                    name = "Preset",
                    defaultWeight = 1,
                    stateMachine = new AnimatorStateMachine() { name = "Preset" }.HideInHierarchy().AddTo(context.AssetContainer),
                };

                var s = layer.stateMachine;

                var blank = new AnimationClip().HideInHierarchy().AddTo(context.AssetContainer);

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
                    .HideInHierarchy().AddTo(context.AssetContainer);

                    var d = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
                    var a = preset.Targets.Select(x => x.Targets.Select(y => (Name: GetParameterName(x.Target.GetParameterPrefix(), y.Object), y.Enable))).SelectMany(x => x);
                    d.parameters.AddRange(a.Select(x => new VRC_AvatarParameterDriver.Parameter() { name = x.Name, type = VRC_AvatarParameterDriver.ChangeType.Set, value = x.Enable ? 1 : 0 }));

                    states[i] = state;
                }

                var idle = new AnimatorState()
                {
                    name = "Idle",
                    writeDefaultValues = false,
                    motion = blank,
                }
                .HideInHierarchy().AddTo(context.AssetContainer);

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
                    }.HideInHierarchy().AddTo(context.AssetContainer);

                    state.AddTransition(t);

                    t = new AnimatorStateTransition()
                    {
                        destinationState = state,
                        conditions = new AnimatorCondition[] { new AnimatorCondition() { parameter = "Preset", mode = AnimatorConditionMode.Equals, threshold = i + 1 } },
                        duration = 0,
                        hasExitTime = false,
                    }.HideInHierarchy().AddTo(context.AssetContainer);

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
                        }.HideInHierarchy().AddTo(context.AssetContainer);

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

                var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>().AddTo(context.AssetContainer);
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
        }
    }
}
