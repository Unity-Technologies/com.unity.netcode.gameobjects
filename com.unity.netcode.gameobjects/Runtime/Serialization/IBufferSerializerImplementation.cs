using System;
using UnityEngine;

namespace Unity.Netcode
{
    public interface IBufferSerializerImplementation
    {
        bool IsReader { get; }
        bool IsWriter { get; }

        ref FastBufferReader GetFastBufferReader();
        ref FastBufferWriter GetFastBufferWriter();

        void SerializeValue(ref object value, Type type, bool isNullable = false);
        void SerializeValue(ref GameObject value);
        void SerializeValue(ref NetworkObject value);
        void SerializeValue(ref NetworkBehaviour value);
        void SerializeValue(ref string s, bool oneByteChars = false);
        void SerializeValue<T>(ref T[] array) where T : unmanaged;
        void SerializeValue(ref byte value);
        void SerializeValue<T>(ref T value) where T : unmanaged;

        // Has to have a different name to avoid conflicting with "where T: unmananged"
        // Using SerializeValue(INetworkSerializable) will result in boxing on struct INetworkSerializables
        // So this is provided as an alternative to avoid boxing allocations.
        void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable, new();

        bool PreCheck(int amount);
        void SerializeValuePreChecked(ref GameObject value);
        void SerializeValuePreChecked(ref NetworkObject value);
        void SerializeValuePreChecked(ref NetworkBehaviour value);
        void SerializeValuePreChecked(ref string s, bool oneByteChars = false);
        void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged;
        void SerializeValuePreChecked(ref byte value);
        void SerializeValuePreChecked<T>(ref T value) where T : unmanaged;
    }
}
