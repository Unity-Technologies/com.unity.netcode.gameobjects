using System;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    /// <typeparam name="T">the unmanaged type for <see cref="NetworkVariable{T}"/> </typeparam>
    [Serializable]
    [GenerateSerializationForGenericParameter(0)]
    public class NetworkVariable<T> : NetworkVariableBase
    {
        /// <summary>
        /// Delegate type for value changed event
        /// </summary>
        /// <param name="previousValue">The value before the change</param>
        /// <param name="newValue">The new value</param>
        public delegate void OnValueChangedDelegate(T previousValue, T newValue);
        /// <summary>
        /// The callback to be invoked when the value gets changed
        /// </summary>
        public OnValueChangedDelegate OnValueChanged;

        /// <summary>
        /// Delegate that determines if the difference between two values exceeds a threshold for network synchronization
        /// </summary>
        /// <param name="previousValue">The previous value to compare against</param>
        /// <param name="newValue">The new value to compare</param>
        /// <returns>True if the difference exceeds the threshold and should be synchronized, false otherwise</returns>
        public delegate bool CheckExceedsDirtinessThresholdDelegate(in T previousValue, in T newValue);

        /// <summary>
        /// Delegate instance for checking if value changes exceed the dirtiness threshold
        /// </summary>
        public CheckExceedsDirtinessThresholdDelegate CheckExceedsDirtinessThreshold;

        /// <summary>
        /// Determines if the current value has changed enough from its previous value to warrant network synchronization
        /// </summary>
        /// <returns>True if the value should be synchronized, false otherwise</returns>
        public override bool ExceedsDirtinessThreshold()
        {
            if (CheckExceedsDirtinessThreshold != null && m_HasPreviousValue)
            {
                return CheckExceedsDirtinessThreshold(m_PreviousValue, m_InternalValue);
            }

            return true;
        }

        /// <summary>
        /// Initializes the NetworkVariable by setting up initial and previous values
        /// </summary>
        public override void OnInitialize()
        {
            base.OnInitialize();

            m_HasPreviousValue = true;
            NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
            NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_PreviousValue);
        }

        /// <summary>
        /// Constructor for <see cref="NetworkVariable{T}"/>
        /// </summary>
        /// <param name="value">initial value set that is of type T</param>
        /// <param name="readPerm">the <see cref="NetworkVariableReadPermission"/> for this <see cref="NetworkVariable{T}"/></param>
        /// <param name="writePerm">the <see cref="NetworkVariableWritePermission"/> for this <see cref="NetworkVariable{T}"/></param>
        public NetworkVariable(T value = default,
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
            : base(readPerm, writePerm)
        {
            m_InternalValue = value;
            m_LastInternalValue = default;
            // Since we start with IsDirty = true, this doesn't need to be duplicated
            // right away. It won't get read until after ResetDirty() is called, and
            // the duplicate will be made there. Avoiding calling
            // NetworkVariableSerialization<T>.Duplicate() is important because calling
            // it in the constructor might not give users enough time to set the
            // DuplicateValue callback if they're using UserNetworkVariableSerialization
            m_PreviousValue = default;
        }

        /// <summary>
        /// Resets the NetworkVariable when the associated NetworkObject is not spawned
        /// </summary>
        /// <param name="value">the value to reset the NetworkVariable to (if none specified it resets to the default)</param>
        public void Reset(T value = default)
        {
            if (m_NetworkBehaviour == null || m_NetworkBehaviour != null && !m_NetworkBehaviour.NetworkObject.IsSpawned)
            {
                m_InternalValue = value;
                NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
                m_PreviousValue = default;
            }
        }

        /// <summary>
        /// The current internal value of the NetworkVariable.
        /// </summary>
        /// <remarks>
        /// When using collections, this InternalValue can be updated directly without going through the <see cref="NetworkVariable{T}.Value"/> setter.
        /// </remarks>
        [SerializeField]
        private protected T m_InternalValue;

        /// <summary>
        /// The last valid/authorized value of the network variable.
        /// </summary>
        /// <remarks>
        /// The introduction of standard .NET collections caused an issue with permissions since there is no way to detect changes in the
        /// collection without doing a full comparison. While this approach does consume more memory per collection instance, it is the
        /// lowest risk approach to resolving the issue where a client with no write permissions could make changes to a collection locally
        /// which can cause a myriad of issues.
        /// </remarks>
        private protected T m_LastInternalValue;

        /// <summary>
        /// The most recent value that was synchronized over the network.
        /// Synchronized over the network at the end of the frame in which the <see cref="NetworkVariable{T}"/> was marked dirty.
        /// </summary>
        /// <remarks>
        /// Only contains the value synchronized over the network at the end of the last frame.
        /// All in-between changes on the authority are tracked by <see cref="m_LastInternalValue"/>.
        /// </remarks>
        private protected T m_PreviousValue;

        /// <summary>
        /// Whether this network variable has had changes synchronized over the network.
        /// Indicates whether <see cref="m_PreviousValue"/> is populated and valid.
        /// </summary>
        private bool m_HasPreviousValue;
        private bool m_IsDisposed;

        /// <summary>
        /// The value of the NetworkVariable container
        /// </summary>
        /// <remarks>
        /// When assigning collections to <see cref="Value"/>, unless it is a completely new collection this will not
        /// detect any deltas with most managed collection classes since assignment of one collection value to another
        /// is actually just a reference to the collection itself. <br />
        /// To detect deltas in a collection, you should invoke <see cref="CheckDirtyState"/> after making modifications to the collection.
        /// </remarks>
        public virtual T Value
        {
            get => m_InternalValue;
            set
            {
                if (CannotWrite)
                {
                    LogWritePermissionError();
                    return;
                }

                // Compare the Value being applied to the current value
                if (!NetworkVariableSerialization<T>.AreEqual(ref m_InternalValue, ref value))
                {
                    T previousValue = m_InternalValue;
                    m_InternalValue = value;
                    NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
                    SetDirty(true);
                    m_IsDisposed = false;
                    OnValueChanged?.Invoke(previousValue, m_InternalValue);
                }
            }
        }

        /// <summary>
        /// Invoke this method to check if a collection's items are dirty.
        /// The default behavior is to exit early if the <see cref="NetworkVariable{T}"/> is already dirty.
        /// </summary>
        /// <param name="forceCheck"> when true, this check will force a full item collection check even if the NetworkVariable is already dirty</param>
        /// <returns>True if the variable is dirty and needs synchronization, false if clean or client lacks write permissions</returns>
        /// <remarks>
        /// This is to be used as a way to check if a <see cref="NetworkVariable{T}"/> containing a managed collection has any changees to the collection items.<br />
        /// If you invoked this when a collection is dirty, it will not trigger the <see cref="OnValueChanged"/> unless you set forceCheck param to true. <br />
        /// </remarks>
        public bool CheckDirtyState(bool forceCheck = false)
        {
            var isDirty = base.IsDirty();

            // A client without permissions invoking this method should only check to assure the current value is equal to the last known current value
            if (CannotWrite)
            {
                // If modifications are detected, then revert back to the last known current value
                if (!NetworkVariableSerialization<T>.AreEqual(ref m_InternalValue, ref m_LastInternalValue))
                {
                    NetworkVariableSerialization<T>.Duplicate(m_LastInternalValue, ref m_InternalValue);
                }
                return false;
            }

            // Compare the last internal value with the current value if not dirty or forcing a check.
            if ((!isDirty || forceCheck) && !NetworkVariableSerialization<T>.AreEqual(ref m_LastInternalValue, ref m_InternalValue))
            {
                SetDirty(true);
                OnValueChanged?.Invoke(m_LastInternalValue, m_InternalValue);
                m_IsDisposed = false;
                isDirty = true;
                NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
            }
            return isDirty;
        }

        /// <inheritdoc/>
        internal override void OnCheckIsDirtyState()
        {
            CheckDirtyState();
            base.OnCheckIsDirtyState();
        }

        internal ref T RefValue()
        {
            return ref m_InternalValue;
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_IsDisposed = true;
            if (m_InternalValue is IDisposable internalValueDisposable)
            {
                internalValueDisposable.Dispose();
            }

            m_InternalValue = default;

            if (m_LastInternalValue is IDisposable internalOriginalValueDisposable)
            {
                internalOriginalValueDisposable.Dispose();
            }
            m_LastInternalValue = default;

            if (m_HasPreviousValue && m_PreviousValue is IDisposable previousValueDisposable)
            {
                m_HasPreviousValue = false;
                previousValueDisposable.Dispose();
            }

            m_PreviousValue = default;
        }

        /// <summary>
        /// Finalizer that ensures proper cleanup of resources
        /// </summary>
        ~NetworkVariable()
        {
            Dispose();
        }

        /// <summary>
        /// Gets Whether or not the container is dirty
        /// </summary>
        /// <returns>Whether or not the container is dirty</returns>
        public override bool IsDirty()
        {
            // If the client does not have write permissions but the internal value is determined to be locally modified and we are applying updates, then we should revert
            // to the original collection value prior to applying updates (primarily for collections).
            if (!NetworkUpdaterCheck && CannotWrite && !NetworkVariableSerialization<T>.AreEqual(ref m_InternalValue, ref m_LastInternalValue))
            {
                NetworkVariableSerialization<T>.Duplicate(m_LastInternalValue, ref m_InternalValue);
                return true;
            }

            // For most cases we can use the dirty flag.
            // This doesn't work for cases where we're wrapping more complex types
            // like INetworkSerializable, NativeList, NativeArray, etc.
            // Changes to the values in those types don't call the Value.set method,
            // so we can't catch those changes and need to compare the current value
            // against the previous one.
            if (base.IsDirty())
            {
                return true;
            }

            var dirty = !NetworkVariableSerialization<T>.AreEqual(ref m_PreviousValue, ref m_InternalValue);

            // Cache the dirty value so we don't perform this again if we already know we're dirty
            // Unfortunately we can't cache the NOT dirty state, because that might change
            // in between to checks... but the DIRTY state won't change until ResetDirty()
            // is called.
            SetDirty(dirty);
            return dirty;
        }

        /// <summary>
        /// Resets the dirty state and marks the variable as synced / clean
        /// </summary>
        public override void ResetDirty()
        {
            // Resetting the dirty value declares that the current value is not dirty
            // Therefore, we set the m_PreviousValue field to a duplicate of the current
            // field, so that our next dirty check is made against the current "not dirty"
            // value.
            if (IsDirty())
            {
                m_HasPreviousValue = true;
                NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_PreviousValue);
                // Once updated, assure the original current value is updated for future comparison purposes
                NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
            }
            base.ResetDirty();
        }

        /// <summary>
        /// Sets the <see cref="Value"/>, marks the <see cref="NetworkVariable{T}"/> dirty, and invokes the <see cref="OnValueChanged"/> callback
        /// if there are subscribers to that event.
        /// </summary>
        /// <param name="value">the new value of type `T` to be set/></param>
        private protected void Set(T value)
        {
            SetDirty(true);
            m_InternalValue = value;
            OnValueChanged?.Invoke(m_PreviousValue, m_InternalValue);
        }

        /// <summary>
        /// Writes the variable to the writer
        /// </summary>
        /// <param name="writer">The stream to write the value to</param>
        public override void WriteDelta(FastBufferWriter writer)
        {
            NetworkVariableSerialization<T>.WriteDelta(writer, ref m_InternalValue, ref m_PreviousValue);
        }

        /// <summary>
        /// Reads value from the reader and applies it
        /// </summary>
        /// <param name="reader">The stream to read the value from</param>
        /// <param name="keepDirtyDelta">Whether or not the container should keep the dirty delta, or mark the delta as consumed</param>
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            // If the client does not have write permissions but the internal value is determined to be locally modified and we are applying updates, then we should revert
            // to the original collection value prior to applying updates (primarily for collections).
            if (CannotWrite && !NetworkVariableSerialization<T>.AreEqual(ref m_LastInternalValue, ref m_InternalValue))
            {
                NetworkVariableSerialization<T>.Duplicate(m_LastInternalValue, ref m_InternalValue);
            }

            NetworkVariableSerialization<T>.ReadDelta(reader, ref m_InternalValue);

            // keepDirtyDelta marks a variable received as dirty and causes the server to send the value to clients
            // In a prefect world, whether a variable was A) modified locally or B) received and needs retransmit
            // would be stored in different fields
            // LEGACY NOTE: This is only to handle NetworkVariableDeltaMessage Version 0 connections. The updated
            // NetworkVariableDeltaMessage no longer uses this approach.
            if (keepDirtyDelta)
            {
                SetDirty(true);
            }
            OnValueChanged?.Invoke(m_PreviousValue, m_InternalValue);
        }

        /// <summary>
        /// This should be always invoked (client & server) to assure the previous values are set
        /// !! IMPORTANT !!
        /// When a server forwards delta updates to connected clients, it needs to preserve the previous dirty value(s)
        /// until it is done serializing all valid NetworkVariable field deltas (relative to each client). This is invoked
        /// after it is done forwarding the deltas at the end of the <see cref="NetworkVariableDeltaMessage.Handle(ref NetworkContext)"/> method.
        /// </summary>
        internal override void PostDeltaRead()
        {
            // In order to get managed collections to properly have a previous and current value, we have to
            // duplicate the collection at this point before making any modifications to the current.
            m_HasPreviousValue = true;
            NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_PreviousValue);
            // Once updated, assure the original current value is updated for future comparison purposes
            NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader)
        {
            // If the client does not have write permissions but the internal value is determined to be locally modified and we are applying updates, then we should revert
            // to the original collection value prior to applying updates (primarily for collections).
            if (CannotWrite && !NetworkVariableSerialization<T>.AreEqual(ref m_LastInternalValue, ref m_InternalValue))
            {
                NetworkVariableSerialization<T>.Duplicate(m_LastInternalValue, ref m_InternalValue);
            }

            NetworkVariableSerialization<T>.Read(reader, ref m_InternalValue);
            // In order to get managed collections to properly have a previous and current value, we have to
            // duplicate the collection at this point before making any modifications to the current.
            // We duplicate the final value after the read (for ReadField ONLY) so the previous value is at par
            // with the current value (since this is only invoked when initially synchronizing).
            m_HasPreviousValue = true;
            NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_PreviousValue);

            // Once updated, assure the original current value is updated for future comparison purposes
            NetworkVariableSerialization<T>.Duplicate(m_InternalValue, ref m_LastInternalValue);
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer)
        {
            NetworkVariableSerialization<T>.Write(writer, ref m_InternalValue);
        }

        internal override void WriteFieldSynchronization(FastBufferWriter writer)
        {
            // If we have a pending update, then synchronize the client with the previously known
            // value since the updated version will be sent on the next tick or next time it is
            // set to be updated
            if (base.IsDirty() && m_HasPreviousValue)
            {
                NetworkVariableSerialization<T>.Write(writer, ref m_PreviousValue);
            }
            else
            {
                base.WriteFieldSynchronization(writer);
            }
        }
    }
}
