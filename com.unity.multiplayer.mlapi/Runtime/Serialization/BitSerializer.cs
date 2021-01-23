using System;
using MLAPI.Spawning;
using UnityEngine;

namespace MLAPI.Serialization
{
    public sealed class BitSerializer
    {
        private readonly BitReader m_Reader;
        private readonly BitWriter m_Writer;

        public bool IsReading { get; }

        internal BitSerializer(BitReader reader)
        {
            m_Reader = reader;
            IsReading = true;
        }

        internal BitSerializer(BitWriter writer)
        {
            m_Writer = writer;
            IsReading = false;
        }

        public void Serialize(ref bool value)
        {
            if (IsReading) value = m_Reader.ReadBool();
            else m_Writer.WriteBool(value);
        }

        public void Serialize(ref char value)
        {
            if (IsReading) value = m_Reader.ReadCharPacked();
            else m_Writer.WriteCharPacked(value);
        }

        public void Serialize(ref sbyte value)
        {
            if (IsReading) value = m_Reader.ReadSByte();
            else m_Writer.WriteSByte(value);
        }

        public void Serialize(ref byte value)
        {
            if (IsReading) value = m_Reader.ReadByteDirect();
            else m_Writer.WriteByte(value);
        }

        public void Serialize(ref short value)
        {
            if (IsReading) value = m_Reader.ReadInt16Packed();
            else m_Writer.WriteInt16Packed(value);
        }

        public void Serialize(ref ushort value)
        {
            if (IsReading) value = m_Reader.ReadUInt16Packed();
            else m_Writer.WriteUInt16Packed(value);
        }

        public void Serialize(ref int value)
        {
            if (IsReading) value = m_Reader.ReadInt32Packed();
            else m_Writer.WriteInt32Packed(value);
        }

        public void Serialize(ref uint value)
        {
            if (IsReading) value = m_Reader.ReadUInt32Packed();
            else m_Writer.WriteUInt32Packed(value);
        }

        public void Serialize(ref long value)
        {
            if (IsReading) value = m_Reader.ReadInt64Packed();
            else m_Writer.WriteInt64Packed(value);
        }

        public void Serialize(ref ulong value)
        {
            if (IsReading) value = m_Reader.ReadUInt64Packed();
            else m_Writer.WriteUInt64Packed(value);
        }

        public void Serialize(ref float value)
        {
            if (IsReading) value = m_Reader.ReadSinglePacked();
            else m_Writer.WriteSinglePacked(value);
        }

        public void Serialize(ref double value)
        {
            if (IsReading) value = m_Reader.ReadDoublePacked();
            else m_Writer.WriteDoublePacked(value);
        }

        public void Serialize(ref string value)
        {
            if (IsReading)
            {
                var isSet = m_Reader.ReadBool();
                value = isSet ? m_Reader.ReadStringPacked() : null;
            }
            else
            {
                var isSet = value != null;
                m_Writer.WriteBool(isSet);
                if (isSet)
                {
                    m_Writer.WriteStringPacked(value);
                }
            }
        }

        public void Serialize(ref Color value)
        {
            if (IsReading) value = m_Reader.ReadColorPacked();
            else m_Writer.WriteColorPacked(value);
        }

        public void Serialize(ref Color32 value)
        {
            if (IsReading) value = m_Reader.ReadColor32();
            else m_Writer.WriteColor32(value);
        }

        public void Serialize(ref Vector2 value)
        {
            if (IsReading) value = m_Reader.ReadVector2Packed();
            else m_Writer.WriteVector2Packed(value);
        }

        public void Serialize(ref Vector3 value)
        {
            if (IsReading) value = m_Reader.ReadVector3Packed();
            else m_Writer.WriteVector3Packed(value);
        }

        public void Serialize(ref Vector4 value)
        {
            if (IsReading) value = m_Reader.ReadVector4Packed();
            else m_Writer.WriteVector4Packed(value);
        }

        public void Serialize(ref Quaternion value)
        {
            if (IsReading) value = m_Reader.ReadRotationPacked();
            else m_Writer.WriteRotationPacked(value);
        }

        public void Serialize(ref Ray value)
        {
            if (IsReading) value = m_Reader.ReadRayPacked();
            else m_Writer.WriteRayPacked(value);
        }

        public void Serialize(ref Ray2D value)
        {
            if (IsReading) value = m_Reader.ReadRay2DPacked();
            else m_Writer.WriteRay2DPacked(value);
        }

        public unsafe void Serialize<TEnum>(ref TEnum value) where TEnum : unmanaged, Enum
        {
            if (sizeof(TEnum) == sizeof(int))
            {
                if (IsReading)
                {
                    int intValue = m_Reader.ReadInt32Packed();
                    value = *(TEnum*)(&intValue);
                }
                else
                {
                    TEnum enumValue = value;
                    m_Writer.WriteInt32Packed(*(int*)&enumValue);
                }
            }
            else if (sizeof(TEnum) == sizeof(byte))
            {
                if (IsReading)
                {
                    byte intValue = m_Reader.ReadByteDirect();
                    value = *(TEnum*)(&intValue);
                }
                else
                {
                    TEnum enumValue = value;
                    m_Writer.WriteByte(*(byte*)&enumValue);
                }
            }
            else if (sizeof(TEnum) == sizeof(short))
            {
                if (IsReading)
                {
                    short intValue = m_Reader.ReadInt16Packed();
                    value = *(TEnum*)(&intValue);
                }
                else
                {
                    TEnum enumValue = value;
                    m_Writer.WriteInt16Packed(*(short*)&enumValue);
                }
            }
            else if (sizeof(TEnum) == sizeof(long))
            {
                if (IsReading)
                {
                    long intValue = m_Reader.ReadInt64Packed();
                    value = *(TEnum*)(&intValue);
                }
                else
                {
                    TEnum enumValue = value;
                    m_Writer.WriteInt64Packed(*(long*)&enumValue);
                }
            }
            else if (IsReading)
            {
                value = default;
            }
        }

        public void Serialize(ref NetworkedObject netObject)
        {
            if (IsReading)
            {
                var isSet = m_Reader.ReadBool();
                if (isSet)
                {
                    var objectId = m_Reader.ReadUInt64Packed();
                    SpawnManager.SpawnedObjects.TryGetValue(objectId, out netObject);
                }
                else
                {
                    netObject = null;
                }
            }
            else
            {
                var isSet = netObject != null && netObject.IsSpawned;
                m_Writer.WriteBool(isSet);
                if (isSet)
                {
                    var objectId = netObject.NetworkId;
                    m_Writer.WriteUInt64Packed(objectId);
                }
            }
        }

        public void Serialize(ref NetworkedBehaviour netBehaviour)
        {
            if (IsReading)
            {
                var isSet = m_Reader.ReadBool();
                if (isSet)
                {
                    var objectId = m_Reader.ReadUInt64Packed();
                    var behaviourId = m_Reader.ReadUInt16Packed();
                    SpawnManager.SpawnedObjects.TryGetValue(objectId, out var netObject);
                    netBehaviour = netObject != null ? netObject.GetBehaviourAtOrderIndex(behaviourId) : null;
                }
                else
                {
                    netBehaviour = null;
                }
            }
            else
            {
                var isSet = netBehaviour != null && netBehaviour.HasNetworkedObject;
                m_Writer.WriteBool(isSet);
                if (isSet)
                {
                    var objectId = netBehaviour.NetworkedObject.NetworkId;
                    var behaviourId = netBehaviour.GetBehaviourId();
                    m_Writer.WriteUInt64Packed(objectId);
                    m_Writer.WriteUInt16Packed(behaviourId);
                }
            }
        }

        public void Serialize(ref bool[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new bool[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadBool();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteBool(array[i]);
                }
            }
        }

        public void Serialize(ref char[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new char[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadCharPacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteCharPacked(array[i]);
                }
            }
        }

        public void Serialize(ref sbyte[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new sbyte[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadSByte();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteSByte(array[i]);
                }
            }
        }

        public void Serialize(ref byte[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new byte[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadByteDirect();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteByte(array[i]);
                }
            }
        }

        public void Serialize(ref short[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new short[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadInt16Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteInt16Packed(array[i]);
                }
            }
        }

        public void Serialize(ref ushort[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new ushort[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadUInt16Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteUInt16Packed(array[i]);
                }
            }
        }

        public void Serialize(ref int[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new int[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadInt32Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteInt32Packed(array[i]);
                }
            }
        }

        public void Serialize(ref uint[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new uint[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadUInt32Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteUInt32Packed(array[i]);
                }
            }
        }

        public void Serialize(ref long[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new long[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadInt64Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteInt64Packed(array[i]);
                }
            }
        }

        public void Serialize(ref ulong[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new ulong[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadUInt64Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteUInt64Packed(array[i]);
                }
            }
        }

        public void Serialize(ref float[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new float[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadSinglePacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteSinglePacked(array[i]);
                }
            }
        }

        public void Serialize(ref double[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new double[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadDoublePacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteDoublePacked(array[i]);
                }
            }
        }

        public void Serialize(ref string[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new string[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadStringPacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteStringPacked(array[i]);
                }
            }
        }

        public void Serialize(ref Color[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Color[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadColorPacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteColorPacked(array[i]);
                }
            }
        }

        public void Serialize(ref Color32[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Color32[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadColor32();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteColor32(array[i]);
                }
            }
        }

        public void Serialize(ref Vector2[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Vector2[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadVector2Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteVector2Packed(array[i]);
                }
            }
        }

        public void Serialize(ref Vector3[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Vector3[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadVector3Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteVector3Packed(array[i]);
                }
            }
        }

        public void Serialize(ref Vector4[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Vector4[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadVector4Packed();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteVector4Packed(array[i]);
                }
            }
        }

        public void Serialize(ref Quaternion[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Quaternion[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadRotationPacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteRotationPacked(array[i]);
                }
            }
        }

        public void Serialize(ref Ray[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Ray[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadRayPacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteRayPacked(array[i]);
                }
            }
        }

        public void Serialize(ref Ray2D[] array)
        {
            if (IsReading)
            {
                var length = m_Reader.ReadInt32Packed();
                array = length > -1 ? new Ray2D[length] : null;
                for (var i = 0; i < length; ++i)
                {
                    array[i] = m_Reader.ReadRay2DPacked();
                }
            }
            else
            {
                var length = array?.Length ?? -1;
                m_Writer.WriteInt32Packed(length);
                for (var i = 0; i < length; ++i)
                {
                    m_Writer.WriteRay2DPacked(array[i]);
                }
            }
        }

        public void Serialize<TEnum>(ref TEnum[] array) where TEnum : Enum
        {
            var enumType = typeof(TEnum);
            var intType = Enum.GetUnderlyingType(enumType);

            // todo
        }

        public void Serialize(ref NetworkedObject[] array)
        {
            // todo
        }

        public void Serialize(ref NetworkedBehaviour[] array)
        {
            // todo
        }
    }
}