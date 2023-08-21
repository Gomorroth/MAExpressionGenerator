﻿using System;
using UnityEngine;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [Serializable]
    public class TargetObject
    {
        [SerializeField]
        public GameObject Object;

        [SerializeField]
        public bool Enable;

        public TargetObject(GameObject obj, bool enable = true)
        {
            Object = obj;
            Enable = enable;
        }
    }
}
