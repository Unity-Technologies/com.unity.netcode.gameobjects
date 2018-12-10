using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MLAPI.NetworkedVar;
using MLAPI.Serialization;
using System;

namespace MLAPI
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    [Serializable]
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
            if (Settings.SendTickrate == 0) return true;
            if (Settings.SendTickrate < 0) return false;
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
        public void ReadDelta(Stream stream, bool keepDirtyDelta)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                T previousValue = InternalValue;
                InternalValue = (T)reader.ReadObjectPacked((typeof(T)));

                if (keepDirtyDelta) isDirty = true;

                if (OnValueChanged != null)
                    OnValueChanged(previousValue, InternalValue);
            }
        }

        /// <inheritdoc />
        public void SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        /// <inheritdoc />
        public void ReadField(Stream stream)
        {
            ReadDelta(stream, false);
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
    
    // These support serialization
    [Serializable]
    public class NetworkedVarString : NetworkedVar<string>
    {
        public NetworkedVarString() { }
        public NetworkedVarString(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarString(string value) : base(value) { }
        public NetworkedVarString(NetworkedVarSettings settings, string value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarBool : NetworkedVar<bool>
    {
        public NetworkedVarBool() { }
        public NetworkedVarBool(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarBool(bool value) : base(value) { }
        public NetworkedVarBool(NetworkedVarSettings settings, bool value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarByte : NetworkedVar<byte>
    {
        public NetworkedVarByte() { }
        public NetworkedVarByte(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarByte(byte value) : base(value) { }
        public NetworkedVarByte(NetworkedVarSettings settings, byte value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarSByte : NetworkedVar<sbyte>
    {
        public NetworkedVarSByte() { }
        public NetworkedVarSByte(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarSByte(sbyte value) : base(value) { }
        public NetworkedVarSByte(NetworkedVarSettings settings, sbyte value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarUShort : NetworkedVar<ushort>
    {
        public NetworkedVarUShort() { }
        public NetworkedVarUShort(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarUShort(ushort value) : base(value) { }
        public NetworkedVarUShort(NetworkedVarSettings settings, ushort value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarShort : NetworkedVar<short>
    {
        public NetworkedVarShort() { }
        public NetworkedVarShort(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarShort(short value) : base(value) { }
        public NetworkedVarShort(NetworkedVarSettings settings, short value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarUInt : NetworkedVar<uint>
    {
        public NetworkedVarUInt() { }
        public NetworkedVarUInt(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarUInt(uint value) : base(value) { }
        public NetworkedVarUInt(NetworkedVarSettings settings, uint value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarInt : NetworkedVar<int>
    {
        public NetworkedVarInt() { }
        public NetworkedVarInt(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarInt(int value) : base(value) { }
        public NetworkedVarInt(NetworkedVarSettings settings, int value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarULong : NetworkedVar<ulong>
    {
        public NetworkedVarULong() { }
        public NetworkedVarULong(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarULong(ulong value) : base(value) { }
        public NetworkedVarULong(NetworkedVarSettings settings, ulong value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarLong : NetworkedVar<long>
    {
        public NetworkedVarLong() { }
        public NetworkedVarLong(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarLong(long value) : base(value) { }
        public NetworkedVarLong(NetworkedVarSettings settings, long value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarFloat : NetworkedVar<float>
    {
        public NetworkedVarFloat() { }
        public NetworkedVarFloat(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarFloat(float value) : base(value) { }
        public NetworkedVarFloat(NetworkedVarSettings settings, float value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarDouble : NetworkedVar<double>
    {
        public NetworkedVarDouble() { }
        public NetworkedVarDouble(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarDouble(double value) : base(value) { }
        public NetworkedVarDouble(NetworkedVarSettings settings, double value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarVector2 : NetworkedVar<Vector2>
    {
        public NetworkedVarVector2() { }
        public NetworkedVarVector2(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarVector2(Vector2 value) : base(value) { }
        public NetworkedVarVector2(NetworkedVarSettings settings, Vector2 value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarVector3 : NetworkedVar<Vector3>
    {
        public NetworkedVarVector3() { }
        public NetworkedVarVector3(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarVector3(Vector3 value) : base(value) { }
        public NetworkedVarVector3(NetworkedVarSettings settings, Vector3 value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarVector4 : NetworkedVar<Vector4>
    {
        public NetworkedVarVector4() { }
        public NetworkedVarVector4(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarVector4(Vector4 value) : base(value) { }
        public NetworkedVarVector4(NetworkedVarSettings settings, Vector4 value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarColor : NetworkedVar<Color>
    {
        public NetworkedVarColor() { }
        public NetworkedVarColor(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarColor(Color value) : base(value) { }
        public NetworkedVarColor(NetworkedVarSettings settings, Color value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarColor32 : NetworkedVar<Color32>
    {
        public NetworkedVarColor32() { }
        public NetworkedVarColor32(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarColor32(Color32 value) : base(value) { }
        public NetworkedVarColor32(NetworkedVarSettings settings, Color32 value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarRay : NetworkedVar<Ray>
    {
        public NetworkedVarRay() { }
        public NetworkedVarRay(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarRay(Ray value) : base(value) { }
        public NetworkedVarRay(NetworkedVarSettings settings, Ray value) : base(settings, value) { }
    }

    [Serializable]
    public class NetworkedVarQuaternion : NetworkedVar<Quaternion>
    {
        public NetworkedVarQuaternion() { }
        public NetworkedVarQuaternion(NetworkedVarSettings settings) : base(settings) { }
        public NetworkedVarQuaternion(Quaternion value) : base(value) { }
        public NetworkedVarQuaternion(NetworkedVarSettings settings, Quaternion value) : base(settings, value) { }
    }
}
