using System;
using System.Linq;
using System.Reflection;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase.Editor.BuildPipeline;
using Object = UnityEngine.Object;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [InitializeOnLoad]
    internal sealed class BuildManager : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => new nadena.dev.modular_avatar.core.editor.AvatarProcessor().callbackOrder - 100;

        static BuildManager()
        {
            RuntimeUtils.OnAwake = sender =>
            {
                var avatar = sender.GetComponentInParent<VRCAvatarDescriptor>();
                if (avatar != null)
                {
                    Process(avatar.gameObject);
                    var components = avatar.GetComponentsInChildren<MAExpressionBaseComponent>();
                    if (components != null)
                    {
                        foreach (var component in components)
                        {
                            Object.DestroyImmediate(component);
                        }
                    }
                }
            };
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            _isCreated = false;
            Process(avatarGameObject);
            var components = avatarGameObject.GetComponentsInChildren<MAExpressionBaseComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    Object.DestroyImmediate(component);
                }
            }
            return true;
        }

        public static bool _isCreated = false;

        public static void Process(GameObject avatar)
        {
            if (_isCreated)
                return;

            GenerateInstallTargets(avatar);
            GeneratePresets(avatar);

            _isCreated = true;
        }

        internal static void GeneratePresets(GameObject avatarObject)
        {
            var fx = AssetGenerator.CreateArtifact(useModularAvatarTemporaryFolder: true);

            var presets = avatarObject.GetComponentsInChildren<MAExpressionPreset>();

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
                    conditions = new AnimatorCondition[] { new AnimatorCondition() { parameter = "Preset", mode = AnimatorConditionMode.Equals, threshold = i + 1 } },
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
                x.parameters.Add(new ParameterConfig() { saved = false, syncType = ParameterSyncType.Int, nameOrPrefix = "Preset", localOnly = true });
            });

            obj.GetOrAddComponent<ModularAvatarMenuInstaller>(x =>
            {
                if (x.installTargetMenu == null)
                {
                    x.installTargetMenu = avatarObject.GetComponent<VRCAvatarDescriptor>().expressionsMenu;
                }
            });

            obj.GetOrAddComponent<ModularAvatarMenuItem>(x =>
            {
                x.MenuSource = SubmenuSource.MenuAsset;
                x.Control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                x.Control.subMenu = new VRCExpressionsMenu()
                {
                    controls = presets.Select((y, i) =>
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

            //obj.SetActive(true);
        }

        private static Type MenuInstallerTargetType;
        private static FieldInfo MenuInstallerTargetInstallerField;

        internal static void GenerateInstallTargets(GameObject avatarObject)
        {
            var installTarget = avatarObject.GetComponentInChildren<MAExpressionGeneratorMenuInstallTarget>();
            if (installTarget == null)
                return;

            var obj = installTarget.gameObject;
            obj.SetActive(false);

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
                    var c = obj.AddComponent(MenuInstallerTargetType);
                    MenuInstallerTargetInstallerField.SetValue(c, installer);
                }
            }
        }
    }
}
