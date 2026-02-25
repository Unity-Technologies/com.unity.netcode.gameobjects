#if UNIFIED_NETCODE
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Can be used to remove and add back a component that is being tracked.
    /// Requires invoking <see cref="Initialize"/> before a component is marked to be tracked.
    /// </summary>
    [HideInInspector]
    internal class ComponentMarker : MonoBehaviour
    {
        internal static Dictionary<NetworkManager, Dictionary<GameObject, HashSet<ComponentMarker>>> RegisteredMarkers = new Dictionary<NetworkManager, Dictionary<GameObject, HashSet<ComponentMarker>>>();

        private static void AddInstance(NetworkManager networkManager, ComponentMarker instance)
        {
            if (!RegisteredMarkers.ContainsKey(networkManager))
            {
                RegisteredMarkers.Add(networkManager, new Dictionary<GameObject, HashSet<ComponentMarker>>());
            }

            if (!RegisteredMarkers[networkManager].ContainsKey(instance.gameObject))
            {
                RegisteredMarkers[networkManager].Add(instance.gameObject, new HashSet<ComponentMarker>());
            }
            RegisteredMarkers[networkManager][instance.gameObject].Add(instance);
        }

        private static void RemoveInstance(NetworkManager networkManager, ComponentMarker instance)
        {
            if (!RegisteredMarkers.ContainsKey(networkManager))
            {
                return;
            }

            if (!RegisteredMarkers[networkManager].ContainsKey(instance.gameObject))
            {
                return;
            }
            RegisteredMarkers[networkManager][instance.gameObject].Remove(instance);

            if (RegisteredMarkers[networkManager][instance.gameObject].Count == 0)
            {
                RegisteredMarkers[networkManager].Remove(instance.gameObject);
            }

            if (RegisteredMarkers[networkManager].Count == 0)
            {
                RegisteredMarkers.Remove(networkManager);
            }
        }

        internal NetworkManager NetworkManager { get; private set; }

        internal Component PrefabInstance { get; private set; }
        internal Component CurrentInstance { get; private set; }

        internal void Add<T>() where T : Component
        {
            if (CurrentInstance)
            {
                return;
            }
            var instanceAsType = (T)PrefabInstance;
            CurrentInstance = ComponentHelpers.AddAndCopy(gameObject, instanceAsType);
        }

        internal void Remove<T>() where T : Component
        {
            if (!CurrentInstance)
            {
                return;
            }
            Destroy(CurrentInstance);
            CurrentInstance = null;
        }

        /// <summary>
        /// Initializes this marker to track the current component instance paired with the prefab's instance of the component.
        /// </summary>
        /// <typeparam name="T">The type of component being marked.</typeparam>
        /// <param name="networkManager">To help with integration testing (tracking which NetworkManager instance a registered marker belongs to.</param>
        /// <param name="currentInstance">The current active comonent instance.</param>
        /// <param name="prefabInstance">The prefab's instance of the component (used to replicate the setttings when adding back).</param>
        internal void Initialize<T>(NetworkManager networkManager, T currentInstance, T prefabInstance) where T : Component
        {
            CurrentInstance = currentInstance;
            PrefabInstance = prefabInstance;
            NetworkManager = networkManager;
            AddInstance(networkManager, this);
        }

        internal void OnDestroy()
        {
            RemoveInstance(NetworkManager, this);
        }
    }
}
#endif
