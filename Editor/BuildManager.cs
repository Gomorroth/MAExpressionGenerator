using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
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

            var context = new Processor.RuntimeBuildContext(avatar);

            Processor.GenerateInstallTargets(avatar);
            Processor.GenerateSimpleToggle(context);
            Processor.GeneratePresets(context);

            _isCreated = true;
        }
    }
}
