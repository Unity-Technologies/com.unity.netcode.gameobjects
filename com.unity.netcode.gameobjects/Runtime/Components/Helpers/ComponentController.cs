using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// This is a serializable contianer class for <see cref="ComponentController"/> entries.
    /// </summary>
    [Serializable]
    public class ComponentControllerEntry
    {
        /// <summary>
        /// When true, this component's enabled state will be the inverse of
        /// the value passed into <see cref="ComponentController.SetEnabled(bool)"/>.
        /// </summary>
        [Tooltip("When enabled, this component will inversely mirror the currently applied enable or disable state.")]
        public bool InvertEnabled;

        /// <summary>
        /// The amount of time to delay enabling this component when the <see cref="ComponentController"/> has just transitioned from a disabled to enabled state.
        /// </summary>
        public float EnableDelay;

        /// <summary>
        /// The amount of time to delay disabling this component when the <see cref="ComponentController"/> has just transitioned from an enabled to disabled state.
        /// </summary>
        public float DisableDelay;

        /// <summary>
        /// The component to control.
        /// </summary>
        /// <remarks>
        /// You can assign an entire <see cref="GameObject"/> to this property which will
        /// add all components attached to the <see cref="GameObject"/>. The <see cref="StartEnabled"/>
        /// and <see cref="InvertEnabled"/> properties will be applied to all components found on the <see cref="GameObject"/>.
        /// </remarks>
        public Object Component;
        internal PropertyInfo PropertyInfo;


        internal bool TimeDeltaDelayInProgress;
        internal bool PendingState;
        internal float EnableDelayTimeDelta;
        internal float DisableDelayTimeDelta;

        private bool GetRelativeEnabled(bool enabled)
        {
            return InvertEnabled ? !enabled : enabled;
        }

        internal bool HasDelay(bool enabled)
        {
            return GetRelativeEnabled(enabled) ? EnableDelay > 0.0f : DisableDelay > 0.0f;
        }

        internal void StartTimeDeltaTracking(bool isEnabled)
        {
            if (GetRelativeEnabled(isEnabled))
            {
                EnableDelayTimeDelta = Time.realtimeSinceStartup + EnableDelay;
            }
            else
            {
                DisableDelayTimeDelta = Time.realtimeSinceStartup + DisableDelay;
            }
            TimeDeltaDelayInProgress = true;
            PendingState = isEnabled;
        }


        internal void SetValue(bool isEnabled)
        {
            // If invert enabled is true, then use the inverted value passed in.
            // Otherwise, directly apply the value passed in.
            PropertyInfo.SetValue(Component, GetRelativeEnabled(isEnabled));
        }

        internal bool CheckTimeDeltaDelay()
        {
            if (!TimeDeltaDelayInProgress)
            {
                return false;
            }

            var isDeltaDelayInProgress = ((EnableDelayTimeDelta > Time.realtimeSinceStartup)
                || (DisableDelayTimeDelta > Time.realtimeSinceStartup));

            if (!isDeltaDelayInProgress)
            {
                SetValue(PendingState);
            }
            TimeDeltaDelayInProgress = isDeltaDelayInProgress;
            return TimeDeltaDelayInProgress;
        }
    }

    /// <summary>
    /// Handles enabling or disabling commonly used components, behaviours, RenderMeshes, etc.<br />
    /// Anything that derives from <see cref="Component"/> and has an enabled property can be added
    /// to the list of objects.<br />
    /// <see cref="NetworkBehaviour"/> derived components are not allowed and will be automatically removed.
    /// </summary>
    /// <remarks>
    /// This will synchronize the enabled or disabled state of the <see cref="Component"/>s with
    /// connected and late joining clients.<br />
    /// This class provides the basic functionality to synchronizing components' enabled state.<br />
    /// It is encouraged to create custom derived versions of this class to provide any additional
    /// functionality required for your project specific needs.
    /// </remarks>
    public class ComponentController : NetworkBehaviour
    {
        /// <summary>
        /// Determines whether the selected <see cref="Components"/>s will start enabled or disabled when spawned.
        /// </summary>
        [Tooltip("The initial state of the component controllers enabled status when instnatiated.")]
        public bool StartEnabled = true;

        /// <summary>
        /// The coroutine yield time used to check on any pending delayed <see cref="ComponentControllerEntry"/> state transitions.
        /// </summary>
        /// <remarks>
        /// If there are any <see cref="ComponentControllerEntry"/> delays (enable, disable, or both), then upon changing the state of a <see cref="ComponentController"/> a coroutine will be started
        /// (if not already started) that will monitor any pending delayed transitions (<see cref="EnableDelay"/> or <see cref="DisableDelay"/>) and will
        /// handle changing each delayed <see cref="ComponentControllerEntry"/> until all instances have transitioned their value to the pending state change.
        /// When there are no more pending delayed <see cref="ComponentControllerEntry"/>s, the coroutine will stop.
        /// </remarks>
        public float PendingDelayYieldTime = 0.032f;

        /// <summary>
        /// The list of <see cref="Components"/>s to be enabled and disabled.
        /// </summary>
        [Tooltip("The list of components to control. You can drag and drop an entire GameObject on this to include all components.")]
        public List<ComponentControllerEntry> Components;

        /// <summary>
        /// Returns the current enabled state of the <see cref="ComponentController"/>.
        /// </summary>
        public bool EnabledState => m_IsEnabled.Value;

        internal List<ComponentControllerEntry> ValidComponents = new List<ComponentControllerEntry>();
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

            var gameObjectsToScan = new List<ComponentControllerEntry>();
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                if (Components[i] == null)
                {
                    continue;
                }

                if (Components[i].Component == null)
                {
                    continue;
                }
                var componentType = Components[i].Component.GetType();
                if (componentType == typeof(GameObject))
                {
                    gameObjectsToScan.Add(Components[i]);
                    Components.RemoveAt(i);
                    continue;
                }

                if (componentType.IsSubclassOf(typeof(NetworkBehaviour)))
                {
                    Debug.LogWarning($"Removing {Components[i].Component.name} since {nameof(NetworkBehaviour)}s are not allowed to be controlled by this component.");
                    Components.RemoveAt(i);
                    continue;
                }

                var propertyInfo = Components[i].Component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo == null && propertyInfo.PropertyType != typeof(bool))
                {
                    Debug.LogWarning($"{Components[i].Component.name} does not contain a public enabled property! (Removing)");
                    Components.RemoveAt(i);
                }
            }

            foreach (var entry in gameObjectsToScan)
            {
                var asGameObject = entry.Component as GameObject;
                var components = asGameObject.GetComponents<Component>();
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
                        var componentEntry = new ComponentControllerEntry()
                        {
                            Component = component,
                            PropertyInfo = propertyInfo,
                        };
                        Components.Add(componentEntry);
                    }
                }
            }
            gameObjectsToScan.Clear();
        }
#endif

        /// <summary>
        /// This checks to make sure that all <see cref="Component"/> entries are valid and will create a final
        /// <see cref="ComponentControllerEntry"/> list of valid entries.
        /// </summary>
        protected virtual void Awake()
        {
            ValidComponents.Clear();

            // If no components then don't try to initialize.
            if (Components == null)
            {
                return;
            }

            var emptyEntries = 0;

            foreach (var entry in Components)
            {
                if (entry == null)
                {
                    emptyEntries++;
                    continue;
                }
                var propertyInfo = entry.Component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
                {
                    entry.PropertyInfo = propertyInfo;
                    ValidComponents.Add(entry);
                }
                else
                {
                    NetworkLog.LogWarning($"{name} does not contain a public enable property! (Ignoring)");
                }
            }
            if (emptyEntries > 0)
            {
                NetworkLog.LogWarning($"{name} has {emptyEntries} emtpy(null) entries in the {nameof(Components)} list!");
            }

            // Apply the initial state of all components this instance is controlling.
            InitializeComponents();
        }

        /// <inheritdoc/>
        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                m_IsEnabled.Value = StartEnabled;
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

        /// <inheritdoc/>
        public override void OnDestroy()
        {
            if (m_CoroutineObject.IsRunning)
            {
                StopCoroutine(m_CoroutineObject.Coroutine);
                m_CoroutineObject.IsRunning = false;
            }
            base.OnDestroy();
        }

        private void OnEnabledChanged(bool previous, bool current)
        {
            ApplyEnabled(current);
        }

        /// <summary>
        /// Initializes each component entry to its initial state.
        /// </summary>
        private void InitializeComponents()
        {
            foreach (var entry in ValidComponents)
            {
                // If invert enabled is true, then use the inverted value passed in.
                // Otherwise, directly apply the value passed in.
                var isEnabled = entry.InvertEnabled ? !StartEnabled : StartEnabled;
                entry.PropertyInfo.SetValue(entry.Component, isEnabled);
            }
        }

        /// <summary>
        /// Applies states changes to all components being controlled by this instance.
        /// </summary>
        /// <param name="enabled">the state update to apply</param>
        private void ApplyEnabled(bool enabled)
        {

            foreach (var entry in ValidComponents)
            {
                if (entry.HasDelay(enabled))
                {
                    if (!m_CoroutineObject.IsRunning)
                    {
                        m_CoroutineObject.Coroutine = StartCoroutine(PendingAppliedState());
                        m_CoroutineObject.IsRunning = true;
                    }
                    entry.StartTimeDeltaTracking(enabled);
                }
                else
                {
                    entry.SetValue(enabled);
                }
            }
        }

        private class CoroutineObject
        {
            public Coroutine Coroutine;
            public bool IsRunning;
        }

        private CoroutineObject m_CoroutineObject = new CoroutineObject();


        private IEnumerator PendingAppliedState()
        {
            var checkPendingDeltaDelays = true;
            var waitTime = new WaitForSeconds(PendingDelayYieldTime);

            while (checkPendingDeltaDelays)
            {
                yield return waitTime;
                checkPendingDeltaDelays = false;
                foreach (var entry in ValidComponents)
                {
                    if (!entry.CheckTimeDeltaDelay())
                    {
                        continue;
                    }
                    checkPendingDeltaDelays = true;
                }
            }
            m_CoroutineObject.IsRunning = false;
            yield break;
        }

        /// <summary>
        /// Invoke on the authority side to enable or disable components assigned to this instance.
        /// </summary>
        /// <remarks>
        /// If any component entry has the <see cref="ComponentControllerEntry.InvertEnabled"/> set to true,
        /// then the inverse of the isEnabled property passed in will be used. If the component entry has the
        /// <see cref="ComponentControllerEntry.InvertEnabled"/> set to false (default), then the value of the
        /// isEnabled property will be applied.
        /// </remarks>
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
