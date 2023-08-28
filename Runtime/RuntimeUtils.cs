using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ComponentModel.Browsable(false)]
        public static Action<MAExpressionBaseComponent> OnAwake;

        internal static Span<T> AsSpan<T>(this List<T> list)
        {
            var o = Unsafe.As<ListObject<T>>(list);

            return new Span<T>(o.Array, 0, o.Count);
        }

        private sealed class ListObject<T>
        {
            public T[] Array;
            public int Count;
        }
    }
}
