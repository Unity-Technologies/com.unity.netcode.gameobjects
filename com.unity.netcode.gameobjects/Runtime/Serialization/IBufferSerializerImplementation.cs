using System;
using Unity.Multiplayer.Netcode;
using UnityEngine;

namespace Unity.Netcode.Serialization
{
    public interface IBufferSerializerImplementation
    {
        public bool IsReader { get; }
        public bool IsWriter { get; }

        public ref FastBufferReader GetFastBufferReader();
        public ref FastBufferWriter GetFastBufferWriter();

        public void SerializeValue(ref object value, Type type, bool isNullable = false);
        public void SerializeValue(ref INetworkSerializable value);
        public void SerializeValue(ref GameObject value);
        public void SerializeValue(ref NetworkObject value);
        public void SerializeValue(ref NetworkBehaviour value);
        public void SerializeValue(ref string s, bool oneByteChars = false);
        public void SerializeValue<T>(ref T[] array) where T : unmanaged;
        public void SerializeValue(ref byte value);
        public void SerializeValue<T>(ref T value) where T : unmanaged;

        // Has to have a different name to avoid conflicting with "where T: unmananged"
        // Using SerializeValue(INetworkSerializable) will result in boxing on struct INetworkSerializables
        // So this is provided as an alternative to avoid boxing allocations.
        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable;

        public bool PreCheck(int amount);
        public void SerializeValuePreChecked(ref GameObject value);
        public void SerializeValuePreChecked(ref NetworkObject value);
        public void SerializeValuePreChecked(ref NetworkBehaviour value);
        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false);
        public void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged;
        public void SerializeValuePreChecked(ref byte value);
        public void SerializeValuePreChecked<T>(ref T value) where T : unmanaged;
    }
}
