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
        public static bool IsEditorOnly(this GameObject obj) => obj.CompareTag(EditorOnlyTag);
        public static bool IsEditorOnly(this Component component) => component.CompareTag(EditorOnlyTag);

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


        public static string GetRelativePath(this Transform transform, Transform root, bool includeRelativeTo = false)
        {
            var buffer = _relativePathBuffer;
            if (buffer is null)
            {
                buffer = _relativePathBuffer = new string[128];
            }

            var t = transform;
            int idx = buffer.Length;
            while (t != null && t != root)
            {
                buffer[--idx] = t.name;
                t = t.parent;
            }
            if (includeRelativeTo && t != null && t == root)
            {
                buffer[--idx] = t.name;
            }

            return string.Join("/", buffer, idx, buffer.Length - idx);
        }

        private static string[] _relativePathBuffer;


        public static GameObject GetParent(this GameObject obj) => obj?.transform.parent?.gameObject;

        public static GameObject GetRoot(this GameObject obj)
        {
            var t = obj?.transform;
            while (t?.parent != null)
            {
                t = t.parent;
            }
            return t?.gameObject;
        }

        public static string CombinePath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }
            else if (left.EndsWith("/"))
            {
                return $"{left}{right}";
            }
            else
            {
                return $"{left}/{right}";
            }
        }

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
