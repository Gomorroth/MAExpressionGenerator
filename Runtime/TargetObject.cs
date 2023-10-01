using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    [Serializable]
    public class TargetObject
    {
        [SerializeField]
        public GameObject Object;

        [SerializeField]
        public bool Include;

        [SerializeField]
        public bool Enable;
        

        public TargetObject(GameObject obj, bool? enable = null, bool? include = null)
        {
            Object = obj;
            Enable = enable ?? obj.activeInHierarchy;
            Include = include ?? !obj.IsEditorOnly();
        }
    }
}
