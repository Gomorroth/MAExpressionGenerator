using UnityEngine;
using VRC.SDKBase;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public abstract class MAExpressionBaseComponent : MonoBehaviour, IEditorOnly
    {
        public void Start()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RuntimeUtils.OnAwake?.Invoke(this);
            }
#endif
        }
    }
}
