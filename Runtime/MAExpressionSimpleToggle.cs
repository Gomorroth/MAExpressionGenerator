using System.Collections.Generic;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public sealed class MAExpressionSimpleToggle : MAExpressionObjectController
    {
        [SerializeField]
        public GameObject Target;

        [SerializeField]
        public bool IsSynced;

        [SerializeField]
        public bool IsSave;

        public override string DisplayName => GetTargetObject().name;

        public GameObject GetTargetObject() => Target != null ? Target : gameObject;

        public override IEnumerable<TargetObject> GetControlObjects()
        {
            var obj = GetTargetObject();
            yield return new TargetObject(obj, obj.activeInHierarchy);
        }

        public override string GetParameterPrefix() => GetTargetObject().transform.parent.GetRelativePath(GetTargetObject().GetRoot().transform);
    }
}
