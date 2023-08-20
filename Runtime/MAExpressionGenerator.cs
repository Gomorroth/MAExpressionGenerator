using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDKBase;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed class MAExpressionGenerator : MonoBehaviour, IEditorOnly
    {
        [SerializeField]
        public List<TargetObject> Targets;

        public string ParamterPrefix = "Costume";

        public void Start()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DestroyImmediate(gameObject);
            }
#endif
        }

        public void Update()
        {
            RefreshTargets();
        }

        public void RefreshTargets()
        {
            var parent = transform.parent;
            if (parent != null)
            {
                var targets = parent.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(x => !x.IsEditorOnly());
                if (Targets == null)
                {
                    Targets = targets.Select(x => new TargetObject(x.gameObject)).ToList();
                }
                else
                {
                    var count = Targets.Count;
                    Targets.AddRange(targets.Where(x => !Targets.Any(y => y.Object == x.gameObject)).Select(x => new TargetObject(x.gameObject)));
                    Targets.RemoveAll(x => x.Object == null || x.Object.IsEditorOnly() || !x.Object.IsIn(parent.gameObject));
                    if (count != Targets.Count)
                        Targets.Sort((x, y) => x.Object.transform.GetSiblingIndex() - y.Object.transform.GetSiblingIndex());
                }
            }
        }

        [Serializable]
        public class TargetObject
        {
            [SerializeField]
            public GameObject Object;

            [SerializeField]
            public bool Enable;

            public TargetObject(GameObject obj)
            {
                Object = obj;
                Enable = true;
            }
        }
    }
}
