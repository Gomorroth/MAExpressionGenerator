﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [DisallowMultipleComponent]
    public sealed class MAExpressionGenerator : MAExpressionObjectController
    {
        [SerializeField]
        public List<TargetObject> Targets;

        [SerializeField]
        public TargetObject[] GeneratedTargets;

        [SerializeField]
        public bool GenerateBoneToggle = true;

        [SerializeField]
        public string ParamterPrefix = InitialParameterPrefix;

        public override string DisplayName => gameObject.name;

        public override IEnumerable<TargetObject> GetControlObjects() => GeneratedTargets;

        public override string GetParameterPrefix() => ParamterPrefix;

#if UNITY_EDITOR
        protected override void OnUpdate()
        {
            var parent = transform.parent;
            if (parent != null)
            {
                if (ParamterPrefix == InitialParameterPrefix)
                {
                    ParamterPrefix = parent.name;
                }
                var targets = parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                if (Targets == null)
                {
                    Targets = targets.Select(x => new TargetObject(x.gameObject)).ToList();
                    EditorUtility.SetDirty(this);
                }
                else
                {
                    var count = Targets.Count;
                    if (count != targets.Count())
                    {
                        Targets.AddRange(targets.Where(x => !Targets.Any(y => y.Object == x.gameObject)).Select(x => new TargetObject(x.gameObject, !x.IsEditorOnly())));
                        Targets.RemoveAll(x => x.Object == null || !x.Object.IsIn(parent.gameObject));
                        Targets.Sort((x, y) => x.Object.transform.GetSiblingIndex() - y.Object.transform.GetSiblingIndex());
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif

    }
}
