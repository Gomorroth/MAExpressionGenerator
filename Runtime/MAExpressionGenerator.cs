using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public sealed class MAExpressionGenerator : MAExpressionBaseComponent
    {
        [SerializeField]
        public List<TargetObject> Targets;

        [SerializeField]
        public string ParamterPrefix = null;

        [SerializeField]
        public bool IsParameterPrefixInitialized = false;

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

                if (!IsParameterPrefixInitialized)
                {
                    ParamterPrefix = parent.name;
                    IsParameterPrefixInitialized = true;
                }
            }
        }
    }
}
