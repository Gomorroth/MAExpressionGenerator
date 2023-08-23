using UnityEngine;
using VRC.SDKBase;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
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
#endif
    }
}
