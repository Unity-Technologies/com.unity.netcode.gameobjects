using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Handles enabling or disabling commonly used components, behaviours, RenderMeshes, etc.<br />
    /// Anything that derives from <see cref="Object"/> and has an enabled property can be added
    /// to the list of objects.<br />
    /// This also synchronizes the enabling or disabling of the objects with connected and late 
    /// joining clients.
    /// </summary>
    public class ObjectController : NetworkBehaviour
    {
        /// <summary>
        /// Determines whether the selected <see cref="Object"/>s will start out enabled or disabled.
        /// </summary>
        public bool InitialState;

        /// <summary>
        /// The list of <see cref="Object"/>s to be enabled and disabled.
        /// </summary>
        public List<Object> Objects;

        private Dictionary<Object, PropertyInfo> m_ValidObjects = new Dictionary<Object, PropertyInfo>();        
        private NetworkVariable<bool> m_IsEnabled = new NetworkVariable<bool>();

        /// <inheritdoc/>
        protected virtual void Awake()
        {
            var emptyEntries = 0;
            foreach (var someObject in Objects)
            {
                if (someObject == null)
                {
                    emptyEntries++;
                    continue;
                }
                var propertyInfo = someObject.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
                {
                    m_ValidObjects.Add(someObject, propertyInfo);
                }
                else
                {
                    Debug.LogWarning($"{name} does not contain a public enable property! (Ignoring)");
                }
            }
            if (emptyEntries > 0)
            {
                Debug.LogWarning($"{name} has {emptyEntries} emtpy(null) entries in the Objects list!");
            }
            else
            {
                Debug.Log($"{name} has {m_ValidObjects.Count} valid object entries.");
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
            foreach (var entry in m_ValidObjects)
            {
                entry.Value.SetValue(entry.Key, enabled);
            }
        }

        /// <summary>
        /// Invoke on the authority side to enable or disable the <see cref="Objects"/>.
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
