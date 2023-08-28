using UnityEngine;
using VRC.SDKBase;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [DefaultExecutionOrder(-10000)]
    [ExecuteInEditMode]
    public abstract class MAExpressionBaseComponent : MonoBehaviour, IEditorOnly
    {
#if UNITY_EDITOR
        public void Awake()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RuntimeUtils.OnAwake?.Invoke(this);
            }
        }

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
