#if NGO_FINDOBJECTS_NOSORTING
using System;
#endif
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Unity.Netcode
{
    /// <summary>
    /// Helper class to handle the variations of FindObjectsByType.
    /// </summary>
    /// <remarks>
    /// It is intentional that we do not include the UnityEngine namespace in order to avoid
    /// over-complicatd define wrapping between versions that do or don't support FindObjectsSortMode.
    /// </remarks>
    internal static class FindObjects
    {
        /// <summary>
        /// Replaces <see cref="Object.FindObjectsByType"/> to have one place where these changes are applied.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="includeInactive">When true, inactive objects will be included.</param>
        /// <param name="orderByIdentifier">When true, the array returned will be sorted by identifier.</param>
        /// <returns>Resulst as an <see cref="Array"/> of type T</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ByType<T>(bool includeInactive = false, bool orderByIdentifier = false) where T : Object
        {
            var inactive = includeInactive ? UnityEngine.FindObjectsInactive.Include : UnityEngine.FindObjectsInactive.Exclude;
#if NGO_FINDOBJECTS_NOSORTING
            var results = Object.FindObjectsByType<T>(inactive);
            if (orderByIdentifier)
            {
                Array.Sort(results, (a, b) => a.GetEntityId().CompareTo(b.GetEntityId()));
            }
#else
            var results = Object.FindObjectsByType<T>(inactive, orderByIdentifier ? UnityEngine.FindObjectsSortMode.InstanceID : UnityEngine.FindObjectsSortMode.None);
#endif
            return results;
        }
    }
}
