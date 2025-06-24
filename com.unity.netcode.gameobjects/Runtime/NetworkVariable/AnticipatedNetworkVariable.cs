using System;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Defines how anticipated network variables handle authoritative updates that are older than the current anticipated state.
    /// </summary>
    public enum StaleDataHandling
    {
        /// <summary>
        /// Ignores authoritative updates that are older than the current anticipated state.
        /// The anticipated value will not be replaced until a newer authoritative update is received.
        /// </summary>
        Ignore,

        /// <summary>
        /// Applies authoritative updates even if they are older than the current anticipated state.
        /// This triggers reanticipation to calculate a new anticipated value based on the authoritative state.
        /// </summary>
        Reanticipate
    }

#pragma warning disable IDE0001
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// This version supports basic client anticipation - the client can set a value on the belief that the server
    /// will update it to reflect the same value in a future update (i.e., as the result of an RPC call).
    /// This value can then be adjusted as new updates from the server come in, in three basic modes:
    ///
    /// <list type="bullet">
    ///
    /// <item><description><b>Snap:</b> In this mode (with <see cref="StaleDataHandling"/> set to
    /// <see cref="Netcode.StaleDataHandling.Ignore"/> and no <see cref="NetworkBehaviour.OnReanticipate"/> callback),
    /// the moment a more up-to-date value is received from the authority, it will simply replace the anticipated value,
    /// resulting in a "snap" to the new value if it is different from the anticipated value.</description></item>
    ///
    /// <item><description><b>Smooth:</b> In this mode (with <see cref="StaleDataHandling"/> set to
    /// <see cref="Netcode.StaleDataHandling.Ignore"/> and an <see cref="NetworkBehaviour.OnReanticipate"/> callback that calls
    /// <see cref="Smooth"/> from the anticipated value to the authority value with an appropriate
    /// <see cref="Mathf.Lerp"/>-style smooth function), when a more up-to-date value is received from the authority,
    /// it will interpolate over time from an incorrect anticipated value to the correct authoritative value.</description></item>
    ///
    /// <item><description><b>Constant Reanticipation:</b> In this mode (with <see cref="StaleDataHandling"/> set to
    /// <see cref="Netcode.StaleDataHandling.Reanticipate"/> and an <see cref="NetworkBehaviour.OnReanticipate"/> that calculates a
    /// new anticipated value based on the current authoritative value), when a more up-to-date value is received from
    /// the authority, user code calculates a new anticipated value, possibly calling <see cref="Smooth"/> to interpolate
    /// between the previous anticipation and the new anticipation. This is useful for values that change frequently and
    /// need to constantly be re-evaluated, as opposed to values that change only in response to user action and simply
    /// need a one-time anticipation when the user performs that action.</description></item>
    ///
    /// </list>
    ///
    /// Note that these three modes may be combined. For example, if an <see cref="NetworkBehaviour.OnReanticipate"/> callback
    /// does not call either <see cref="Smooth"/> or <see cref="Anticipate"/>, the result will be a snap to the
    /// authoritative value, enabling for a callback that may conditionally call <see cref="Smooth"/> when the
    /// difference between the anticipated and authoritative values is within some threshold, but fall back to
    /// snap behavior if the difference is too large.
    /// </summary>
    /// <typeparam name="T">the unmanaged type for <see cref="NetworkVariable{T}"/> </typeparam>
#pragma warning restore IDE0001
    [Serializable]
    [GenerateSerializationForGenericParameter(0)]
    public class AnticipatedNetworkVariable<T> : NetworkVariableBase
    {
        [SerializeField]
        private NetworkVariable<T> m_AuthoritativeValue;
        private T m_AnticipatedValue;
        private T m_PreviousAnticipatedValue;
        private ulong m_LastAuthorityUpdateCounter = 0;
        private ulong m_LastAnticipationCounter = 0;
        private bool m_IsDisposed = false;
        private bool m_SettingAuthoritativeValue = false;

        private T m_SmoothFrom;
        private T m_SmoothTo;
        private float m_SmoothDuration;
        private float m_CurrentSmoothTime;
        private bool m_HasSmoothValues;

#pragma warning disable IDE0001
        /// <summary>
        /// Defines what the behavior should be if we receive a value from the server with an earlier associated
        /// time value than the anticipation time value.
        /// <br/><br/>
        /// If this is <see cref="Netcode.StaleDataHandling.Ignore"/>, the stale data will be ignored and the authoritative
        /// value will not replace the anticipated value until the anticipation time is reached. <see cref="OnAuthoritativeValueChanged"/>
        /// and <see cref="NetworkBehaviour.OnReanticipate"/> will also not be invoked for this stale data.
        /// <br/><br/>
        /// If this is <see cref="Netcode.StaleDataHandling.Reanticipate"/>, the stale data will replace the anticipated data and
        /// <see cref="OnAuthoritativeValueChanged"/> and <see cref="NetworkBehaviour.OnReanticipate"/> will be invoked.
        /// In this case, the authoritativeTime value passed to <see cref="NetworkBehaviour.OnReanticipate"/> will be lower than
        /// the anticipationTime value, and that callback can be used to calculate a new anticipated value.
        /// </summary>
#pragma warning restore IDE0001
        public StaleDataHandling StaleDataHandling;

        /// <summary>
        /// Delegate for handling changes in the authoritative value.
        /// </summary>
        /// <param name="variable">The network variable that changed.</param>
        /// <param name="previousValue">The previous value before the change.</param>
        /// <param name="newValue">The new value after the change.</param>
        public delegate void OnAuthoritativeValueChangedDelegate(AnticipatedNetworkVariable<T> variable, in T previousValue, in T newValue);

        /// <summary>
        /// Invoked any time the authoritative value changes, even when the data is stale or has been changed locally.
        /// </summary>
        public OnAuthoritativeValueChangedDelegate OnAuthoritativeValueChanged = null;

        /// <summary>
        /// Determines if the difference between the last serialized value and the current value is large enough
        /// to serialize it again.
        /// </summary>
        public event NetworkVariable<T>.CheckExceedsDirtinessThresholdDelegate CheckExceedsDirtinessThreshold
        {
            add => m_AuthoritativeValue.CheckExceedsDirtinessThreshold += value;
            remove => m_AuthoritativeValue.CheckExceedsDirtinessThreshold -= value;
        }

        private class AnticipatedObject : IAnticipatedObject
        {
            public AnticipatedNetworkVariable<T> Variable;

            public void Update()
            {
                Variable.Update();
            }

            public void ResetAnticipation()
            {
                Variable.ShouldReanticipate = false;
            }

            public NetworkObject OwnerObject => Variable.m_NetworkBehaviour.NetworkObject;
        }

        private AnticipatedObject m_AnticipatedObject;

        /// <summary>
        /// Initializes the network variable, setting up initial values and registering with the anticipation system.
        /// </summary>
        public override void OnInitialize()
        {
            m_AuthoritativeValue.Initialize(m_NetworkBehaviour);
            NetworkVariableSerialization<T>.Duplicate(m_AuthoritativeValue.Value, ref m_AnticipatedValue);
            NetworkVariableSerialization<T>.Duplicate(m_AnticipatedValue, ref m_PreviousAnticipatedValue);
            if (m_NetworkBehaviour != null && m_NetworkBehaviour.NetworkManager != null && m_NetworkBehaviour.NetworkManager.AnticipationSystem != null)
            {
                m_AnticipatedObject = new AnticipatedObject { Variable = this };
                m_NetworkBehaviour.NetworkManager.AnticipationSystem.AllAnticipatedObjects.Add(m_AnticipatedObject);
            }
        }

        /// <summary>
        /// Checks if the current value has changed enough from its last synchronized value to warrant a new network update.
        /// </summary>
        /// <returns>True if the value should be synchronized or false if it should not.</returns>
        public override bool ExceedsDirtinessThreshold()
        {
            return m_AuthoritativeValue.ExceedsDirtinessThreshold();
        }

        /// <summary>
        /// Retrieves the current value for the variable.
        /// This is the "display value" for this variable, and is affected by <see cref="Anticipate"/> and
        /// <see cref="Smooth"/>, as well as by updates from the authority, depending on <see cref="StaleDataHandling"/>
        /// and the behavior of any <see cref="NetworkBehaviour.OnReanticipate"/> callbacks.
        /// <br /><br />
        /// When a server update arrives, this value will be overwritten
        /// by the new server value (unless stale data handling is set
        /// to "Ignore" and the update is determined to be stale).
        /// This value will be duplicated in
        /// <see cref="PreviousAnticipatedValue"/>, which
        /// will NOT be overwritten in server updates.
        /// </summary>
        public T Value => m_AnticipatedValue;

        /// <summary>
        /// Indicates whether this variable currently needs
        /// reanticipation. If this is true, the anticipated value
        /// has been overwritten by the authoritative value from the
        /// server; the previous anticipated value is stored in <see cref="PreviousAnticipatedState"/>.
        /// </summary>
        public bool ShouldReanticipate
        {
            get;
            private set;
        }

        /// <summary>
        /// Holds the most recent anticipated value, whatever was
        /// most recently set using <see cref="Anticipate"/>. Unlike
        /// <see cref="Value"/>, this does not get overwritten
        /// when a server update arrives.
        /// </summary>
        public T PreviousAnticipatedValue => m_PreviousAnticipatedValue;

        /// <summary>
        /// Sets the current value of the variable on the expectation that the authority will set the variable
        /// to the same value within one network round trip (i.e., in response to an RPC).
        /// </summary>
        /// <param name="value">The anticipated value that is expected to be confirmed by the authority.</param>
        public void Anticipate(T value)
        {
            if (m_NetworkBehaviour.NetworkManager.ShutdownInProgress || !m_NetworkBehaviour.NetworkManager.IsListening)
            {
                return;
            }
            m_SmoothDuration = 0;
            m_CurrentSmoothTime = 0;
            m_LastAnticipationCounter = m_NetworkBehaviour.NetworkManager.AnticipationSystem.AnticipationCounter;
            m_AnticipatedValue = value;
            NetworkVariableSerialization<T>.Duplicate(m_AnticipatedValue, ref m_PreviousAnticipatedValue);
            if (CanWrite)
            {
                AuthoritativeValue = value;
            }
        }

#pragma warning disable IDE0001
        /// <summary>
        /// Retrieves or sets the underlying authoritative value.<br />
        /// Note that only a client or server with write permissions to this variable may set this value.<br />
        /// When this variable has been anticipated, this value will alawys return the most recent authoritative
        /// state, which is updated even if <see cref="StaleDataHandling"/> is <see cref="Netcode.StaleDataHandling.Ignore"/>.
        /// </summary>
#pragma warning restore IDE0001
        public T AuthoritativeValue
        {
            get => m_AuthoritativeValue.Value;
            set
            {
                m_SettingAuthoritativeValue = true;
                try
                {
                    m_AuthoritativeValue.Value = value;
                    m_AnticipatedValue = value;
                    NetworkVariableSerialization<T>.Duplicate(m_AnticipatedValue, ref m_PreviousAnticipatedValue);
                }
                finally
                {
                    m_SettingAuthoritativeValue = false;
                }
            }
        }

        /// <summary>
        /// A function to interpolate between two values based on a percentage.
        /// See <see cref="Mathf.Lerp"/>, <see cref="Vector3.Lerp"/>, <see cref="Vector3.Slerp"/>, and so on
        /// for examples.
        /// </summary>
        /// <param name="authoritativeValue">The authoritative value to interpolate from.</param>
        /// <param name="anticipatedValue">The anticipated value to interpolate to.</param>
        /// <param name="amount">The interpolation factor between 0 and 1.</param>
        /// <returns>The interpolated value.</returns>
        public delegate T SmoothDelegate(T authoritativeValue, T anticipatedValue, float amount);

        private SmoothDelegate m_SmoothDelegate = null;

        /// <summary>
        /// Initializes a new instance of the AnticipatedNetworkVariable class.
        /// </summary>
        /// <param name="value">The initial value for the network variable. Defaults to the type's default value if not specified.</param>
        /// <param name="staleDataHandling">Determines how the variable handles authoritative updates that are older than the current anticipated state. Defaults to StaleDataHandling.Ignore.</param>
        public AnticipatedNetworkVariable(T value = default,
            StaleDataHandling staleDataHandling = StaleDataHandling.Ignore)
            : base()
        {
            StaleDataHandling = staleDataHandling;
            m_AuthoritativeValue = new NetworkVariable<T>(value)
            {
                OnValueChanged = OnValueChangedInternal
            };
        }

        /// <summary>
        /// Updates the smooth interpolation state if active.
        /// </summary>
        public void Update()
        {
            if (m_CurrentSmoothTime < m_SmoothDuration)
            {
                m_CurrentSmoothTime += m_NetworkBehaviour.NetworkManager.RealTimeProvider.DeltaTime;
                var pct = math.min(m_CurrentSmoothTime / m_SmoothDuration, 1f);
                m_AnticipatedValue = m_SmoothDelegate(m_SmoothFrom, m_SmoothTo, pct);
                NetworkVariableSerialization<T>.Duplicate(m_AnticipatedValue, ref m_PreviousAnticipatedValue);
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            if (m_NetworkBehaviour != null && m_NetworkBehaviour.NetworkManager != null && m_NetworkBehaviour.NetworkManager.AnticipationSystem != null)
            {
                if (m_AnticipatedObject != null)
                {
                    m_NetworkBehaviour.NetworkManager.AnticipationSystem.AllAnticipatedObjects.Remove(m_AnticipatedObject);
                    m_NetworkBehaviour.NetworkManager.AnticipationSystem.ObjectsToReanticipate.Remove(m_AnticipatedObject);
                    m_AnticipatedObject = null;
                }
            }

            m_IsDisposed = true;

            m_AuthoritativeValue.Dispose();
            if (m_AnticipatedValue is IDisposable anticipatedValueDisposable)
            {
                anticipatedValueDisposable.Dispose();
            }

            m_AnticipatedValue = default;
            if (m_PreviousAnticipatedValue is IDisposable previousValueDisposable)
            {
                previousValueDisposable.Dispose();
                m_PreviousAnticipatedValue = default;
            }

            if (m_HasSmoothValues)
            {
                if (m_SmoothFrom is IDisposable smoothFromDisposable)
                {
                    smoothFromDisposable.Dispose();
                    m_SmoothFrom = default;
                }
                if (m_SmoothTo is IDisposable smoothToDisposable)
                {
                    smoothToDisposable.Dispose();
                    m_SmoothTo = default;
                }

                m_HasSmoothValues = false;
            }
        }

        /// <summary>
        /// Finalizer that ensures proper cleanup of network variable resources.
        /// </summary>
        ~AnticipatedNetworkVariable()
        {
            Dispose();
        }

        private void OnValueChangedInternal(T previousValue, T newValue)
        {
            if (!m_SettingAuthoritativeValue)
            {
                m_LastAuthorityUpdateCounter = m_NetworkBehaviour.NetworkManager.AnticipationSystem.LastAnticipationAck;
                if (StaleDataHandling == StaleDataHandling.Ignore && m_LastAnticipationCounter > m_LastAuthorityUpdateCounter)
                {
                    // Keep the anticipated value unchanged because it is more recent than the authoritative one.
                    return;
                }


                ShouldReanticipate = true;
                m_NetworkBehaviour.NetworkManager.AnticipationSystem.ObjectsToReanticipate.Add(m_AnticipatedObject);
            }

            NetworkVariableSerialization<T>.Duplicate(AuthoritativeValue, ref m_AnticipatedValue);

            m_SmoothDuration = 0;
            m_CurrentSmoothTime = 0;
            OnAuthoritativeValueChanged?.Invoke(this, previousValue, newValue);
        }

        /// <summary>
        /// Interpolate this variable from <see cref="from"/> to <see cref="to"/> over <see cref="durationSeconds"/> of
        /// real time. The duration uses <see cref="Time.deltaTime"/>, so it is affected by <see cref="Time.timeScale"/>.
        /// </summary>
        /// <param name="from">The starting value for the interpolation.</param>
        /// <param name="to">The target value to interpolate towards.</param>
        /// <param name="durationSeconds">The duration of the interpolation in seconds.</param>
        /// <param name="how">A delegate that defines how the interpolation should be performed between the two values. It provides a function to interpolate between two values based on a percentage.</param>
        public void Smooth(in T from, in T to, float durationSeconds, SmoothDelegate how)
        {
            if (durationSeconds <= 0)
            {
                NetworkVariableSerialization<T>.Duplicate(to, ref m_AnticipatedValue);
                m_SmoothDuration = 0;
                m_CurrentSmoothTime = 0;
                m_SmoothDelegate = null;
                return;
            }
            NetworkVariableSerialization<T>.Duplicate(from, ref m_AnticipatedValue);
            NetworkVariableSerialization<T>.Duplicate(from, ref m_SmoothFrom);
            NetworkVariableSerialization<T>.Duplicate(to, ref m_SmoothTo);
            m_SmoothDuration = durationSeconds;
            m_CurrentSmoothTime = 0;
            m_SmoothDelegate = how;
            m_HasSmoothValues = true;
        }

        /// <inheritdoc/>
        public override bool IsDirty()
        {
            return m_AuthoritativeValue.IsDirty();
        }

        /// <inheritdoc/>
        public override void ResetDirty()
        {
            m_AuthoritativeValue.ResetDirty();
        }

        /// <inheritdoc/>
        public override void WriteDelta(FastBufferWriter writer)
        {
            m_AuthoritativeValue.WriteDelta(writer);
        }

        /// <inheritdoc/>
        public override void WriteField(FastBufferWriter writer)
        {
            m_AuthoritativeValue.WriteField(writer);
        }

        /// <inheritdoc/>
        public override void ReadField(FastBufferReader reader)
        {
            m_AuthoritativeValue.ReadField(reader);
            NetworkVariableSerialization<T>.Duplicate(m_AuthoritativeValue.Value, ref m_AnticipatedValue);
            NetworkVariableSerialization<T>.Duplicate(m_AnticipatedValue, ref m_PreviousAnticipatedValue);
        }

        /// <inheritdoc/>
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            m_AuthoritativeValue.ReadDelta(reader, keepDirtyDelta);
            // Assure that the post delta read is invoked in order to update
            // previous value.
            m_AuthoritativeValue.PostDeltaRead();
        }
    }
}
