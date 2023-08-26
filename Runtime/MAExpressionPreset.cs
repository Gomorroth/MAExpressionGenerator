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
                    Targets.RemoveAll(x => !Generator.Targets.Any(y => x.Object == y.Object) || x.Object.IsEditorOnly());
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
