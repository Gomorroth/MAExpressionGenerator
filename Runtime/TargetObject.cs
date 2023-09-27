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
        
        [SerializeField] 
        public bool Active;

        public TargetObject(GameObject obj, bool enable = true, bool? active = null)
        {
            Object = obj;
            Enable = enable;
            Active = active ?? obj.activeInHierarchy;
        }
    }
}
