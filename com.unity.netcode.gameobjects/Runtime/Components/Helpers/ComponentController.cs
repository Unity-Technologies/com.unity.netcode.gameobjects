using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using System.Text.RegularExpressions;
#endif
using UnityEngine;
using Object = UnityEngine.Object;


namespace Unity.Netcode.Components
{
    /// <summary>
    /// Handles enabling or disabling commonly used components like <see cref="MonoBehaviour"/>, <see cref="MeshRenderer"/>, <see cref="Collider"/>, etc.<br />
    /// Anything that derives from <see cref="Component"/> and has an enabled property can be added to the list of objects.<br />
    /// NOTE: <see cref="NetworkBehaviour"/> derived components are not allowed and will be automatically removed.
    /// </summary>
    /// <remarks>
    /// This will synchronize the enabled or disabled state of the <see cref="Component"/>s with connected and late joining clients.<br />
    /// - Use <see cref="EnabledState"/> to determine the current synchronized enabled state.<br />
    /// - Use <see cref="SetEnabled(bool)"/> to change the enabled state and have the change applied to all components this <see cref="ComponentController"/> is synchronizing.<br />
    /// It is encouraged to create custom derived versions of this class to provide any additional functionality required for your project specific needs.
    /// </remarks>
    public class ComponentController : NetworkBehaviour
    {
        /// <summary>
        /// This is a serializable contianer class for <see cref="ComponentController"/> entries.
        /// </summary>
        [Serializable]
        internal class ComponentEntry
        {

            // Ignoring the naming convention in order to auto-assign element names
#pragma warning disable IDE1006
            /// <summary>
            /// Used for naming each element entry.
            /// </summary>
            [HideInInspector]
            public string name;
#pragma warning restore IDE1006

            /// <summary>
            /// When true, this component's enabled state will be the inverse of the value passed into <see cref="SetEnabled(bool)"/>.
            /// </summary>
            [Tooltip("When enabled, this component will inversely mirror the currently applied ComponentController's enabled state.")]
            public bool InvertEnabled;

            /// <summary>
            /// The amount of time to delay enabling this component when the <see cref="ComponentController"/> has just transitioned from a disabled to enabled state.
            /// </summary>
            /// <remarks>
            /// This can be useful under scenarios where you might want to prevent a component from being enabled too early prior to making any adjustments.<br />
            /// As an example, you might find that delaying the enabling of a <see cref="MeshRenderer"/> until at least the next frame will avoid any single frame
            /// rendering anomalies until the <see cref="Rigidbody"/> has updated the <see cref="Transform"/>.
            /// </remarks>
            [Range(0.0f, 2.0f)]
            [Tooltip("The amount of time to delay when transitioning this component from disabled to enabled. When 0, the change is immediate.")]
            public float EnableDelay;

            /// <summary>
            /// The amount of time to delay disabling this component when the <see cref="ComponentController"/> has just transitioned from an enabled to disabled state.
            /// </summary>
            /// <remarks>
            /// This can be useful under scenarios where you might want to prevent a component from being disabled too early prior to making any adjustments.<br />
            /// </remarks>
            [Tooltip("The amount of time to delay when transitioning this component from enabled to disabled. When 0, the change is immediate.")]
            [Range(0f, 2.0f)]
            public float DisableDelay;

            /// <summary>
            /// The component that will have its enabled property synchronized.
            /// </summary>
            /// <remarks>
            /// You can assign an entire <see cref="GameObject"/> to this property which will add all components attached to the <see cref="GameObject"/> and its children.
            /// </remarks>
            [Tooltip("The component that will have its enabled status synchonized. You can drop a GameObject onto this field and all valid components will be added to the list.")]
            public Object Component;
            internal PropertyInfo PropertyInfo;

            internal bool GetRelativeEnabled(bool enabled)
            {
                return InvertEnabled ? !enabled : enabled;
            }

            private List<PendingStateUpdate> m_PendingStateUpdates = new List<PendingStateUpdate>();

            /// <summary>
            /// Invoke prior to setting the state.
            /// </summary>
            internal bool QueueForDelay(bool enabled)
            {
                var relativeEnabled = GetRelativeEnabled(enabled);

                if (relativeEnabled ? EnableDelay > 0.0f : DisableDelay > 0.0f)
                {
                    // Start with no relative time offset
                    var relativeTimeOffset = 0.0f;
                    // If we have pending state updates, then get that time of the last state update
                    // and use that as the time to add this next state update.
                    if (m_PendingStateUpdates.Count > 0)
                    {
                        relativeTimeOffset = m_PendingStateUpdates[m_PendingStateUpdates.Count - 1].DelayTimeDelta;
                    }

                    // We process backwards, so insert new entries at the front
                    m_PendingStateUpdates.Insert(0, new PendingStateUpdate(this, enabled, relativeTimeOffset));
                    return true;
                }
                return false;
            }

            internal void SetValue(bool isEnabled)
            {
                // If invert enabled is true, then use the inverted value passed in.
                // Otherwise, directly apply the value passed in.
                PropertyInfo.SetValue(Component, GetRelativeEnabled(isEnabled));
            }

            internal bool HasPendingStateUpdates()
            {
                for (int i = m_PendingStateUpdates.Count - 1; i >= 0; i--)
                {
                    if (!m_PendingStateUpdates[i].CheckTimeDeltaDelay())
                    {
                        m_PendingStateUpdates.RemoveAt(i);
                        continue;
                    }
                }
                return m_PendingStateUpdates.Count > 0;
            }

            private class PendingStateUpdate
            {
                internal bool TimeDeltaDelayInProgress;
                internal bool PendingState;
                internal float DelayTimeDelta;

                internal ComponentEntry ComponentEntry;

                internal bool CheckTimeDeltaDelay()
                {
                    if (!TimeDeltaDelayInProgress)
                    {
                        return false;
                    }

                    var isDeltaDelayInProgress = DelayTimeDelta > Time.realtimeSinceStartup;

                    if (!isDeltaDelayInProgress)
                    {
                        ComponentEntry.SetValue(PendingState);
                    }
                    TimeDeltaDelayInProgress = isDeltaDelayInProgress;
                    return TimeDeltaDelayInProgress;
                }

                internal PendingStateUpdate(ComponentEntry componentControllerEntry, bool isEnabled, float relativeTimeOffset)
                {
                    ComponentEntry = componentControllerEntry;
                    // If there is a pending state, then add the delay to the end of the last pending state's.
                    var referenceTime = relativeTimeOffset > 0.0f ? relativeTimeOffset : Time.realtimeSinceStartup;

                    if (ComponentEntry.GetRelativeEnabled(isEnabled))
                    {
                        DelayTimeDelta = referenceTime + ComponentEntry.EnableDelay;
                    }
                    else
                    {
                        DelayTimeDelta = referenceTime + ComponentEntry.DisableDelay;
                    }
                    TimeDeltaDelayInProgress = true;
                    PendingState = isEnabled;
                }
            }
        }
        /// <summary>
        /// Determines whether the selected <see cref="Components"/>s will start enabled or disabled when spawned.
        /// </summary>
        [Tooltip("The initial state of the component controllers enabled status when instantiated.")]
        public bool StartEnabled = true;

        /// <summary>
        /// The list of <see cref="Components"/>s to be enabled and disabled.
        /// </summary>
        [Tooltip("The list of components to control. You can drag and drop an entire GameObject on this to include all components.")]
        [SerializeField]
        internal List<ComponentEntry> Components;

        /// <summary>
        /// Returns the current enabled state of the <see cref="ComponentController"/>.
        /// </summary>
        public bool EnabledState => m_IsEnabled;

        internal List<ComponentEntry> ValidComponents = new List<ComponentEntry>();
        private bool m_IsEnabled;

#if UNITY_EDITOR

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValidComponentType(Object component)
        {
            return !(component.GetType().IsSubclassOf(typeof(NetworkBehaviour)) || component.GetType() == typeof(NetworkObject) || component.GetType() == typeof(NetworkManager));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetComponentNameFormatted(Object component)
        {
            // Split the class name up based on capitalization
            var classNameDisplay = Regex.Replace(component.GetType().Name, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
            return $"{component.name} ({classNameDisplay})";
        }

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

            var gameObjectsToScan = new List<ComponentEntry>();
            // First pass is to verify all entries are valid and look for any GameObjects added as an entry to process next
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
                var objectType = Components[i].Component.GetType();
                if (objectType == typeof(GameObject))
                {
                    if (!gameObjectsToScan.Contains(Components[i]))
                    {
                        gameObjectsToScan.Add(Components[i]);
                    }
                    Components.RemoveAt(i);
                    continue;
                }

                if (!IsValidComponentType(Components[i].Component))
                {
                    Debug.LogWarning($"Removing {GetComponentNameFormatted(Components[i].Component)} since {Components[i].Component.GetType().Name} is not an allowed component type.");
                    Components.RemoveAt(i);
                    continue;
                }

                var propertyInfo = Components[i].Component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                if (propertyInfo == null || propertyInfo.PropertyType != typeof(bool))
                {
                    Debug.LogWarning($"{Components[i].Component.name} does not contain a public enabled property! (Removing)");
                    Components.RemoveAt(i);
                }
            }

            // Second pass is to process any GameObjects added.
            // Scan the GameObject and all of its children and add all valid components to the list.
            foreach (var entry in gameObjectsToScan)
            {
                var asGameObject = entry.Component as GameObject;
                var components = asGameObject.GetComponentsInChildren<Component>();
                foreach (var component in components)
                {
                    // Ignore any NetworkBehaviour derived, NetworkObject, or NetworkManager components
                    if (!IsValidComponentType(component))
                    {
                        continue;
                    }

                    var propertyInfo = component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
                    if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
                    {
                        var componentEntry = new ComponentEntry()
                        {
                            Component = component,
                            PropertyInfo = propertyInfo,
                        };
                        Components.Add(componentEntry);
                    }
                }
            }
            gameObjectsToScan.Clear();

            // Final (third) pass is to name each list element item as the component is normally viewed in the inspector view.
            foreach (var componentEntry in Components)
            {
                if (!componentEntry.Component)
                {
                    continue;
                }
                componentEntry.name = GetComponentNameFormatted(componentEntry.Component);
            }
        }
#endif

        /// <inheritdoc/>
        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            // Example of how to synchronize late joining clients when using an RPC to update
            // a local property's state.
            serializer.SerializeValue(ref m_IsEnabled);
            base.OnSynchronize(ref serializer);
        }

        /// <summary>
        /// Override this method in place of Awake. This method is invoked during Awake.
        /// </summary>
        /// <remarks>
        /// The <see cref="ComponentController"/>'s Awake method is protected to assure it is invoked in the correct order.
        /// </remarks>
        protected virtual void OnAwake()
        {
        }

        private void Awake()
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

            try
            {
                OnAwake();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If overriding this method, it is required that you invoke this base method.
        /// </remarks>
        public override void OnNetworkSpawn()
        {
            if (OnHasAuthority())
            {
                m_IsEnabled = StartEnabled;
            }
            base.OnNetworkSpawn();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If overriding this method, it is required that you invoke this base method.<br />
        /// Assures all instances subscribe to the internal <see cref="NetworkVariable{T}"/> of type
        /// <see cref="bool"/> that synchronizes all instances when <see cref="Object"/>s are enabled
        /// or disabled.
        /// </remarks>
        protected override void OnNetworkPostSpawn()
        {
            ApplyEnabled();
            base.OnNetworkPostSpawn();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// If overriding this method, it is required that you invoke this base method.
        /// </remarks>
        public override void OnDestroy()
        {
            if (m_CoroutineObject.IsRunning)
            {
                StopCoroutine(m_CoroutineObject.Coroutine);
                m_CoroutineObject.IsRunning = false;
            }
            base.OnDestroy();
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
        private void ApplyEnabled(bool ignoreDelays = false)
        {
            foreach (var entry in ValidComponents)
            {
                if (!ignoreDelays && entry.QueueForDelay(m_IsEnabled))
                {
                    if (!m_CoroutineObject.IsRunning)
                    {
                        m_CoroutineObject.Coroutine = StartCoroutine(PendingAppliedState());
                        m_CoroutineObject.IsRunning = true;
                    }
                }
                else
                {
                    entry.SetValue(m_IsEnabled);
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
            var continueProcessing = true;

            while (continueProcessing)
            {
                continueProcessing = false;
                foreach (var entry in ValidComponents)
                {
                    if (entry.HasPendingStateUpdates())
                    {
                        continueProcessing = true;
                    }
                }
                if (continueProcessing)
                {
                    yield return null;
                }
            }
            m_CoroutineObject.IsRunning = false;
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

            if (!OnHasAuthority())
            {
                Debug.Log($"[Client-{NetworkManager.LocalClientId}] Attempting to invoke {nameof(SetEnabled)} without authority!");
                return;
            }
            ChangeEnabled(isEnabled);
        }

        private void ChangeEnabled(bool isEnabled)
        {
            m_IsEnabled = isEnabled;
            ApplyEnabled();

            if (OnHasAuthority())
            {
                ToggleEnabledRpc(m_IsEnabled);
            }
        }

        internal void ForceChangeEnabled(bool isEnabled, bool ignoreDelays = false)
        {
            m_IsEnabled = isEnabled;
            ApplyEnabled(ignoreDelays);
        }

        /// <summary>
        /// Override this method to change how the instance determines the authority.<br />
        /// The default is to use the <see cref="NetworkObject.HasAuthority"/> method.
        /// </summary>
        /// <remarks>
        /// Useful when using a <see cref="NetworkTopologyTypes.ClientServer"/> network topology and you would like
        /// to have the owner be the authority of this <see cref="ComponentController"/> instance.
        /// </remarks>
        /// <returns>true = has authoriy | false = does not have authority</returns>
        protected virtual bool OnHasAuthority()
        {
            return HasAuthority;
        }

        [Rpc(SendTo.NotMe)]
        private void ToggleEnabledRpc(bool enabled, RpcParams rpcParams = default)
        {
            ChangeEnabled(enabled);
        }
    }
}
