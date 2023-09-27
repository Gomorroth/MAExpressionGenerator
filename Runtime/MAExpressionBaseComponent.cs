using UnityEngine;
using VRC.SDKBase;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [ExecuteInEditMode]
    public abstract class MAExpressionBaseComponent : MonoBehaviour, IEditorOnly
    {
        protected internal const string InitialParameterPrefix = "\u200B";

#if UNITY_EDITOR
        private void Update()
        {
            if (_flag && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                OnUpdate();
            }
        }

        private bool _flag = true;

        protected virtual void OnUpdate()
        {
            _flag = false;
        }
#endif
    }
}
