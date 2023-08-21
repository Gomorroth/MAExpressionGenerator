using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace gomoru.su.ModularAvatarExpressionGenerator
{
    internal static class EditorUtils
    {
        public static T GetOrAddComponent<T>(this GameObject obj, Action<T> action) where T : Component
        {
            var component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            action(component);
            return component;
        }

        public static T AddTo<T>(this T obj, Object asset) where T : Object
        {
            AssetDatabase.AddObjectToAsset(obj, asset);
            return obj;
        }

        public static T HideInHierarchy<T>(this T obj) where T : Object
        {
            obj.hideFlags |= HideFlags.HideInHierarchy;
            return obj;
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
    }
}
