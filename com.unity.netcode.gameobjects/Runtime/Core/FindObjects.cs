using UnityEngine;

namespace Unity.Netcode
{
    internal static class FindObjects
    {
        public static T[] FindObjectsByType<T>() where T : Object
        {
#if NGO_FINDOBJECTS_NOSORTING
            var results = Object.FindObjectsByType<T>();
#else
#if UNITY_2023_1_OR_NEWER
            var results = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            var results = Object.FindObjectsOfType<T>();
#endif
#endif
            return results;
        }

    }
}
