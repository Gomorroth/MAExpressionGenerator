using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public sealed class MAExpressionPreset : MAExpressionBaseComponent
    {
        [SerializeField]
        public List<Group> Targets = new List<Group>();

#if UNITY_EDITOR

        protected override void OnUpdate()
        {
            var obj = gameObject;
            var avatar = obj.GetComponentInParent<VRCAvatarDescriptor>();

            if (avatar != null)
            {
                var generators = avatar.GetComponentsInChildren<MAExpressionObjectController>();
                bool isDirt = false;
                if (Targets != null && !Equals(generators))
                {
                    Targets.AddRange(generators.Where(x => !Targets.Any(y => x == y.Target)).Select(x => new Group(x)));
                    Targets.RemoveAll(x => x.Target == null || x.Targets.Any(y => y.Object.IsEditorOnly()));
                    isDirt = true;
                }
                foreach (var x in Targets)
                {
                    isDirt |= x.Refresh();
                }
                if (isDirt)
                {
                    EditorUtility.SetDirty(this);
                }
            }
        }

        public void HierarchyToPreset()
        {
            bool isDirt = false;
            foreach(var x in Targets.AsSpan())
            {
                foreach(var y in x.Targets.AsSpan())
                {
                    if (y.Enable != y.Object.activeInHierarchy)
                    {
                        isDirt = true;
                        Undo.RecordObject(this, "Sync Preset");
                    }

                    y.Enable = y.Object.activeInHierarchy;
                }
            }

            if (isDirt)
            {
                EditorUtility.SetDirty(this);
            }
        }

        public void PresetToHierarchy()
        {
            foreach (var x in Targets.AsSpan())
            {
                foreach (var y in x.Targets.AsSpan())
                {
                    if (y.Object.activeInHierarchy != y.Enable)
                    {
                        Undo.RecordObject(y.Object, "Apply Preset");
                        y.Object.SetActive(y.Enable);
                    }
                }
            }
        }

#endif

        private bool Equals(MAExpressionObjectController[] generators)
        {
            var targets = Targets.AsSpan();
            if (targets.Length != generators.Length)
                return false;

            for (int i = 0; i < targets.Length; i++)
            {
                var x = targets[i];
                bool exist = false;
                for(int i2 = 0; i2 < generators.Length; i2++)
                {
                    var y = generators[i];

                    if (x.Target == y)
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist)
                    return false;
            }

            return true;
        }

        [Serializable]
        public class Group
        {
            [SerializeField]
            public MAExpressionObjectController Target;

            [SerializeField]
            public List<TargetObject> Targets = new List<TargetObject>();

            public bool Refresh()
            {
                var targets = Target.GetControlObjects();
                if (targets.Count() != Targets.Count)
                {
                    Targets.AddRange(targets.Where(x => !Targets.Any(y => x.Object == y.Object)).Select(x => new TargetObject(x.Object, false)));
                    Targets.RemoveAll(x => !targets.Any(y => x.Object == y.Object) || x.Object.IsEditorOnly());
                    return true;
                }
                return false;
            }

            public Group(MAExpressionObjectController target)
            {
                Target = target;
                Targets = new List<TargetObject>(target.GetControlObjects());
            }
        }
    }
}
