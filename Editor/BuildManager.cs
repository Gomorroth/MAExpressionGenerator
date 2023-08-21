using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

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
                    var obj = sender.gameObject;
                    obj.SetActive(false);
                    Process(avatar.gameObject, sender);
                    GameObject.DestroyImmediate(sender);
                    obj.SetActive(true);
                }
            };
        }

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            var components = avatarGameObject.GetComponentsInChildren<MAExpressionBaseComponent>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    Process(avatarGameObject, component);
                    GameObject.DestroyImmediate(component);
                }
            }
            return true;
        }

        public static bool _isCreated = false;

        public static void Process(GameObject avatar, MAExpressionBaseComponent sender)
        {
            if (_isCreated)
                return;

            var presets = GameObject.FindObjectsOfType<MAExpressionPreset>();
            MAExpressionPresetEditor.GeneratePresets(avatar, presets);

            _isCreated = true;
            AssetDatabase.SaveAssets();
        }
    }
}
