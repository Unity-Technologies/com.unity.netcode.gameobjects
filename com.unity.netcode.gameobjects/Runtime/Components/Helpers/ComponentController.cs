using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Handles enabling or disabling commonly used components, behaviours, RenderMeshes, etc.<br />
    /// Anything that derives from <see cref="Component"/> and has an enabled property can be added
    /// to the list of objects.<br />
    /// <see cref="NetworkBehaviour"/> derived components are not allowed and will be automatically removed.
    /// </summary>
    /// <remarks>
    /// This will synchronize the enabled or disabled state of the <see cref="Component"/>s with
    /// connected and late joining clients.
    /// </remarks>
    public class ComponentController : NetworkBehaviour
    {
        /// <summary>
        /// Determines whether the selected <see cref="Components"/>s will start enabled or disabled when spawned.
        /// </summary>
        [Tooltip("The initial state of the components when spawned.")]
        public bool InitialState = true;

        /// <summary>
        /// The list of <see cref="Components"/>s to be enabled and disabled.
        /// </summary>
        [Tooltip("The list of components to control. You can drag and drop an entire GameObject on this to include all components.")]
        public List<Object> Components;

        private Dictionary<Component, PropertyInfo> m_ValidComponents = new Dictionary<Component, PropertyInfo>();
        private NetworkVariable<bool> m_IsEnabled = new NetworkVariable<bool>();

#if UNITY_EDITOR
        /// <inheritdoc/>
        /// <remarks>
        /// Checks for invalid <see cref="Object"/> entries.
        /// </remarks>
        protected virtual void OnValidate()
        {
            if (Components == null || Components.Count == 0)
            {
                return;
            }

            var gameObjectsToScan = new List<GameObject>();
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                if (Components[i] == null)
                {
                    continue;
                }
                var componentType = Components[i].GetType();
                if (componentType == typeof(GameObject))
                {
                    gameObjectsToScan.Add(Components[i] as GameObject);
                    Components.RemoveAt(i);
                    continue;
                }

                if (componentType.IsSubclassOf(typeof(NetworkBehaviour)))
                {
                    Debug.LogWarning($"Removing {Components[i].name} since {nameof(NetworkBehaviour)}s are not allowed to be controlled by this component.");
                    Components.RemoveAt(i);
                    continue;
                }

                var propertyInfo = Components[i].GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo == null && propertyInfo.PropertyType != typeof(bool))
                {
                    Debug.LogWarning($"{Components[i].name} does not contain a public enabled property! (Removing)");
                    Components.RemoveAt(i);
                }
            }

            foreach (var entry in gameObjectsToScan)
            {
                var components = entry.GetComponents<Component>();
                foreach (var component in components)
                {
                    // Ignore any NetworkBehaviour derived components
                    if (component.GetType().IsSubclassOf(typeof(NetworkBehaviour)))
                    {
                        continue;
                    }

                    var propertyInfo = component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                    if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
                    {
                        Components.Add(component);
                    }
                }
            }
            gameObjectsToScan.Clear();
        }
#endif

        /// <inheritdoc/>
        /// <remarks>
        /// Also checks to assure all <see cref="Component"/> entries are valid and creates a final table of
        /// <see cref="Component"/>s paired to their <see cref="PropertyInfo"/>.
        /// </remarks>
        protected virtual void Awake()
        {
            var emptyEntries = 0;
            foreach (var someObject in Components)
            {
                if (someObject == null)
                {
                    emptyEntries++;
                    continue;
                }
                var propertyInfo = someObject.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
                {
                    m_ValidComponents.Add(someObject as Component, propertyInfo);
                }
                else
                {
                    Debug.LogWarning($"{name} does not contain a public enable property! (Ignoring)");
                }
            }
            if (emptyEntries > 0)
            {
                Debug.LogWarning($"{name} has {emptyEntries} emtpy(null) entries in the {nameof(Components)} list!");
            }
            else
            {
                Debug.Log($"{name} has {m_ValidComponents.Count} valid {nameof(Component)} entries.");
            }
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                m_IsEnabled.Value = InitialState;
            }
            base.OnNetworkSpawn();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Assures all instances subscribe to the internal <see cref="NetworkVariable{T}"/> of type
        /// <see cref="bool"/> that synchronizes all instances when <see cref="Object"/>s are enabled
        /// or disabled.
        /// </remarks>
        protected override void OnNetworkPostSpawn()
        {
            m_IsEnabled.OnValueChanged += OnEnabledChanged;
            ApplyEnabled(m_IsEnabled.Value);
            base.OnNetworkPostSpawn();
        }

        /// <inheritdoc/>
        public override void OnNetworkDespawn()
        {
            m_IsEnabled.OnValueChanged -= OnEnabledChanged;
            base.OnNetworkDespawn();
        }

        private void OnEnabledChanged(bool previous, bool current)
        {
            ApplyEnabled(current);
        }

        private void ApplyEnabled(bool enabled)
        {
            foreach (var entry in m_ValidComponents)
            {
                entry.Value.SetValue(entry.Key, enabled);
            }
        }

        /// <summary>
        /// Invoke on the authority side to enable or disable the <see cref="Object"/>s.
        /// </summary>
        /// <param name="isEnabled">true = enabled | false = disabled</param>
        public void SetEnabled(bool isEnabled)
        {
            if (!IsSpawned)
            {
                Debug.Log($"[{name}] Must be spawned to use {nameof(SetEnabled)}!");
                return;
            }

            if (!HasAuthority)
            {
                Debug.Log($"[Client-{NetworkManager.LocalClientId}] Attempting to invoke {nameof(SetEnabled)} without authority!");
                return;
            }
            m_IsEnabled.Value = isEnabled;
        }
    }
}
