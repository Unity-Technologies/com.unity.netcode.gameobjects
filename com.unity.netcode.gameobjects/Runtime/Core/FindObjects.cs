using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    internal static class FindObjects
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindObjectsByType<T>() where T : Object
        {
#if NGO_FINDOBJECTS_NOSORTING
            var results = Object.FindObjectsByType<T>();
#elif UNITY_2023_1_OR_NEWER
            var results = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            var results = Object.FindObjectsOfType<T>();
#endif
            return results;
        }

    }
}
