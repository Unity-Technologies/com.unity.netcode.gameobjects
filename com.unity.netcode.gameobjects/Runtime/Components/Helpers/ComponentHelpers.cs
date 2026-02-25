#if UNIFIED_NETCODE
using System.Reflection;
using UnityEngine;

namespace Unity.Netcode
{
    internal static class ComponentHelpers
    {
        /// <summary>
        /// Copies the properties and fields of a source component to a target component
        /// </summary>
        /// <typeparam name="T">Type of the component being copied.</typeparam>
        /// <param name="target">The copy to target.</param>
        /// <param name="source">The copy from source.</param>
        /// <returns></returns>
        internal static T Copy<T>(this Component target, T source) where T : Component
        {
            var targetType = target.GetType();
            var sourceType = source.GetType();
            if (targetType != sourceType)
            {
                Debug.LogError($"[ComponentHelpers][GetCopyOf<{targetType.Name}>][Mismatched target & source] Source: {sourceType.Name}!");
                return null; 
            }

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;

            // Copy properties
            foreach (var property in targetType.GetProperties(bindingFlags))
            {
                if (property.CanWrite)
                {
                    try { property.SetValue(target, property.GetValue(source, null), null); }
                    catch { } // Handle exceptions for unsupported properties
                }
            }

            // Copy fields
            foreach (var field in targetType.GetFields(bindingFlags))
            {
                field.SetValue(target, field.GetValue(source));
            }

            return target as T;
        }

        /// <summary>
        /// Add a component of Type T and copy the source somponent's properties and fields.
        /// </summary>
        /// <typeparam name="T">Component type to add.</typeparam>
        /// <param name="gameObject">The target GameObject the component will be added to.</param>
        /// <param name="sourceComponent">The source component (must be the same Type of T).</param>
        /// <returns></returns>
        internal static T AddAndCopy<T>(this GameObject gameObject, T sourceComponent) where T : Component
        {
            return gameObject.AddComponent<T>().Copy(sourceComponent);
        }
    }
}
#endif
