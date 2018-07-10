using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Core;
using System;
using System.Collections.Generic;

namespace MLAPI.Data
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    public class NetworkedVar<T> : INetworkedVar
    {
        /// <summary>
        /// Gets or sets wheter or not the variable needs to be delta synced
        /// </summary>
        public bool isDirty { get; set; }
        /// <summary>
        /// The settings for this var
        /// </summary>
        public readonly NetworkedVarSettings Settings = new NetworkedVarSettings();
        /// <summary>
        /// Gets the last time the variable was synced
        /// </summary>
        public float LastSyncedTime { get; internal set; }        
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
        private NetworkedBehaviour networkedBehaviour;

        internal NetworkedVar()
        {
        }

        private T InternalValue;
        /// <summary>
        /// The value of the NetworkedVar container
        /// </summary>
        public T Value
        {
            get
            {
                return InternalValue;
            }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(InternalValue, value))
                {
                    isDirty = true;
                    InternalValue = value;
                }
            }
        }

        /// <inheritdoc />
        public void ResetDirty()
        {
            isDirty = false;
            LastSyncedTime = NetworkingManager.singleton.NetworkTime;
        }

        /// <inheritdoc />
        public bool IsDirty()
        {
            if (!isDirty) return false;
            if (NetworkingManager.singleton.NetworkTime - LastSyncedTime >= Settings.SendTickrate) return true;
            return false;
        }

        /// <inheritdoc />
        public bool CanClientRead(uint clientId)
        {
            switch (Settings.ReadPermission)
            {
                case NetworkedVarPermission.Everyone:
                    return true;
                case NetworkedVarPermission.ServerOnly:
                    return false;
                case NetworkedVarPermission.OwnerOnly:
                    return networkedBehaviour.OwnerClientId == clientId;
                case NetworkedVarPermission.Custom:
                {
                    if (Settings.ReadPermissionCallback == null) return false;
                    return Settings.ReadPermissionCallback(clientId);
                }
            }
            return true;
        }

        /// <summary>
        /// Writes the variable to the writer
        /// </summary>
        /// <param name="writer">The writer to write the value to</param>
        public void WriteDeltaToWriter(BitWriter writer) => WriteFieldToWriter(writer); //The NetworkedVar is built for simple data types and has no delta.

        /// <inheritdoc />
        public bool CanClientWrite(uint clientId)
        {
            switch (Settings.WritePermission)
            {
                case NetworkedVarPermission.Everyone:
                    return true;
                case NetworkedVarPermission.ServerOnly:
                    return false;
                case NetworkedVarPermission.OwnerOnly:
                    return networkedBehaviour.OwnerClientId == clientId;
                case NetworkedVarPermission.Custom:
                {
                    if (Settings.WritePermissionCallback == null) return false;
                    return Settings.WritePermissionCallback(clientId);
                }
            }

            return true;
        }

        /// <summary>
        /// Reads value from the reader and applies it
        /// </summary>
        /// <param name="reader">The reader to read the value from</param>
        public void SetDeltaFromReader(BitReader reader) => SetFieldFromReader(reader); //The NetworkedVar is built for simple data types and has no delta.

        /// <inheritdoc />
        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        /// <inheritdoc />
        public void SetFieldFromReader(BitReader reader)
        {
            // TODO TwoTen - Boxing sucks
            T previousValue = InternalValue;
            InternalValue = (T)FieldTypeHelper.ReadFieldType(reader, typeof(T), (object)InternalValue);
            if (OnValueChanged != null)
                OnValueChanged(previousValue, InternalValue);
        }
        
        /// <inheritdoc />
        public void WriteFieldToWriter(BitWriter writer)
        {
            //TODO: Write field
        }

        /// <inheritdoc />
        public string GetChannel()
        {
            return Settings.SendChannel;
        }
    }

    /// <summary>
    /// Interface for networked value containers
    /// </summary>
    public interface INetworkedVar
    {
        /// <summary>
        /// Returns the name of the channel to be used for syncing
        /// </summary>
        /// <returns>The name of the channel to be used for syncing</returns>
        string GetChannel();
        /// <summary>
        /// Resets the dirty state and marks the variable as synced / clean
        /// </summary>
        void ResetDirty();
        /// <summary>
        /// Gets wheter or not the container is dirty
        /// </summary>
        /// <returns>Wheter or not the container is dirty</returns>
        bool IsDirty();
        /// <summary>
        /// Gets wheter or not a specific client can write to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Wheter or not the client can write to the variable</returns>
        bool CanClientWrite(uint clientId);
        /// <summary>
        /// Gets wheter or not a specific client can read to the varaible
        /// </summary>
        /// <param name="clientId">The clientId of the remote client</param>
        /// <returns>Wheter or not the client can read to the variable</returns>
        bool CanClientRead(uint clientId);
        /// <summary>
        /// Writes the dirty changes, that is, the changes since the variable was last dirty, to the writer
        /// </summary>
        /// <param name="writer">The writer to write the dirty changes to</param>
        void WriteDeltaToWriter(BitWriter writer);
        /// <summary>
        /// Writes the complete state of the variable to the writer
        /// </summary>
        /// <param name="writer">The writer to write the state to</param>
        void WriteFieldToWriter(BitWriter writer);
        /// <summary>
        /// Reads the complete state from the reader and applies it
        /// </summary>
        /// <param name="reader">The reader to read the state from</param>
        void SetFieldFromReader(BitReader reader);
        /// <summary>
        /// Reads delta from the reader and applies them to the internal value
        /// </summary>
        /// <param name="reader">The reader to read the delta from</param>
        void SetDeltaFromReader(BitReader reader);
        /// <summary>
        /// Sets NetworkedBehaviour the container belongs to.
        /// </summary>
        /// <param name="behaviour">The behaviour the container behaves to</param>
        void SetNetworkedBehaviour(NetworkedBehaviour behaviour);
    }
}
