﻿using System.Collections.Generic;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public sealed class MAExpressionGenerator : MAExpressionBaseComponent
    {
        [SerializeField]
        public List<TargetObject> Targets;

        [SerializeField]
        public string ParamterPrefix = "\u200B";
    }
}
