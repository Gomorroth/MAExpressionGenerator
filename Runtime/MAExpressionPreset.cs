using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public sealed class MAExpressionPreset : MAExpressionBaseComponent
    {
        [SerializeField]
        public List<Group> Targets = new List<Group>();

        public void Refresh()
        {
            var avatar = gameObject.GetComponentInParent<VRCAvatarDescriptor>();
            if (avatar != null)
            {
                var generators = avatar.GetComponentsInChildren<MAExpressionGenerator>();
                if (Targets != null && generators.Length != Targets.Count)
                {
                    Targets.AddRange(generators.Where(x => !Targets.Any(y => x == y.Generator)).Select(x => new Group(x)));
                    Targets.RemoveAll(x => x.Generator == null);
                }
                foreach(var x in Targets)
                {
                    x.Refresh();
                }
            }
        }

        [Serializable]
        public class Group
        {
            [SerializeField]
            public MAExpressionGenerator Generator;

            [SerializeField]
            public List<TargetObject> Targets = new List<TargetObject>();

            public void Refresh()
            {
                if (Generator?.Targets?.Count != Targets.Count)
                {
                    Targets.AddRange(Generator.Targets.Where(x => !Targets.Any(y => x.Object == y.Object)).Select(x => new TargetObject(x.Object, false)));
                    Targets.RemoveAll(x => !Generator.Targets.Any(y => x.Object == y.Object));
                }
            }

            public Group(MAExpressionGenerator generator)
            {
                Generator = generator;
                Targets = new List<TargetObject>(generator.Targets.Select(x => new TargetObject(x.Object, false)));
            }
        }
    }
}
