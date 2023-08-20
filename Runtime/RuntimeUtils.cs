﻿using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    public static class RuntimeUtils
    {
        private const string EditorOnlyTag = "EditorOnly";
        public static bool IsEditorOnly(this GameObject obj) => obj.tag == EditorOnlyTag;
        public static bool IsEditorOnly(this Component component) => component.tag == EditorOnlyTag;

        public static bool IsIn(this GameObject obj, GameObject target)
        {
            var p = obj.transform;
            while (p != null)
            {
                if (p == target.transform)
                    return true;

                p = p.parent;
            }
            return false;
        }
    }
}