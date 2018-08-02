using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;

namespace MLAPI
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    [System.Serializable]
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
     
        /// <summary>
        /// Creates a NetworkedVar with the default value and settings
        /// </summary>
        public NetworkedVar()
        {
            
        }

        /// <summary>
        /// Creates a NetworkedVar with the default value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkedVar</param>
        public NetworkedVar(NetworkedVarSettings settings)
        {
            this.Settings = settings;
        }

        /// <summary>
        /// Creates a NetworkedVar with a custom value and custom settings
        /// </summary>
        /// <param name="settings">The settings to use for the NetworkedVar</param>
        /// <param name="value">The initial value to use for the NetworkedVar</param>
        public NetworkedVar(NetworkedVarSettings settings, T value)
        {
            this.Settings = settings;
            this.InternalValue = value;
        }

        /// <summary>
        /// Creates a NetworkedVar with a custom value and the default settings
        /// </summary>
        /// <param name="value">The initial value to use for the NetworkedVar</param>
        public NetworkedVar(T value)
        {
            this.InternalValue = value;
        }

        [SerializeField]
        private T InternalValue = default(T);
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
                    T previousValue = InternalValue;
                    InternalValue = value;
                    if (OnValueChanged != null)
                        OnValueChanged(previousValue, InternalValue);
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
            if (Settings.SendTickrate <= 0) return true;
            if (NetworkingManager.singleton.NetworkTime - LastSyncedTime >= (1f / Settings.SendTickrate)) return true;
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
        /// <param name="stream">The stream to write the value to</param>
        public void WriteDelta(Stream stream) => WriteField(stream); //The NetworkedVar is built for simple data types and has no delta.

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
        /// <param name="stream">The stream to read the value from</param>
        public void ReadDelta(Stream stream) => ReadField(stream); //The NetworkedVar is built for simple data types and has no delta.

        /// <inheritdoc />
        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        /// <inheritdoc />
        public void ReadField(Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                T previousValue = InternalValue;
                InternalValue = (T)reader.ReadObjectPacked((typeof(T)));
                if (OnValueChanged != null)
                    OnValueChanged(previousValue, InternalValue);
            }
        }
        
        /// <inheritdoc />
        public void WriteField(Stream stream)
        {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream))
            {
                writer.WriteObjectPacked(InternalValue); //BOX
            }
        }

        /// <inheritdoc />
        public string GetChannel()
        {
            return Settings.SendChannel;
        }
    }
}
