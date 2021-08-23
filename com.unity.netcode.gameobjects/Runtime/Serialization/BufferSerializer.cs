using System;
using Unity.Multiplayer.Netcode;
using UnityEngine;

namespace Unity.Netcode.Serialization
{
    public ref struct BufferSerializer<T> where T : IBufferSerializerImplementation
    {
        private T m_Implementation;
        
        public bool IsReader => m_Implementation.IsReader;
        public bool IsWriter => m_Implementation.IsWriter;

        public BufferSerializer(T implementation)
        {
            m_Implementation = implementation;
        }

        public ref FastBufferReader GetFastBufferReader()
        {
            return ref m_Implementation.GetFastBufferReader();
        }
        public ref FastBufferWriter GetFastBufferWriter()
        {
            return ref m_Implementation.GetFastBufferWriter();
        }

        public void SerializeValue(ref object value, Type type, bool isNullable = false)
        {
            m_Implementation.SerializeValue(ref value, type, isNullable);
        }
        public void SerializeValue(ref INetworkSerializable value)
        {
            m_Implementation.SerializeValue(ref value);
        }
        public void SerializeValue(ref GameObject value)
        {
            m_Implementation.SerializeValue(ref value);
        }
        public void SerializeValue(ref NetworkObject value)
        {
            m_Implementation.SerializeValue(ref value);
        }
        public void SerializeValue(ref NetworkBehaviour value)
        {
            m_Implementation.SerializeValue(ref value);
        }
        public void SerializeValue(ref string s, bool oneByteChars = false)
        {
            m_Implementation.SerializeValue(ref s, oneByteChars);
        }
        public void SerializeValue<T>(ref T[] array) where T : unmanaged
        {
            m_Implementation.SerializeValue(ref array);
        }
        public void SerializeValue(ref byte value)
        {
            m_Implementation.SerializeValue(ref value);
        }
        public void SerializeValue<T>(ref T value) where T : unmanaged
        {
            m_Implementation.SerializeValue(ref value);
        }
        
        // Has to have a different name to avoid conflicting with "where T: unmananged"
        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable
        {
            m_Implementation.SerializeNetworkSerializable(ref value);
        }

        public bool PreCheck(int amount)
        {
            return m_Implementation.PreCheck(amount);
        }

        public void SerializeValuePreChecked(ref GameObject value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }
        public void SerializeValuePreChecked(ref NetworkObject value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }
        public void SerializeValuePreChecked(ref NetworkBehaviour value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }
        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false)
        {
            m_Implementation.SerializeValuePreChecked(ref s, oneByteChars);
        }
        public void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged
        {
            m_Implementation.SerializeValuePreChecked(ref array);
        }
        public void SerializeValuePreChecked(ref byte value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }
        public void SerializeValuePreChecked<T>(ref T value) where T : unmanaged
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }
    }
}