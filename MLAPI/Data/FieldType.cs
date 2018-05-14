using MLAPI.NetworkingManagerComponents.Binary;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MLAPI.Data
{
    internal static class FieldTypeHelper
    {
        internal static void WriteFieldType(BitWriter writer, object value)
        {
            Type type = value.GetType();
            if (type.IsArray)
            {
                ushort arrayLength = (ushort)((Array)value).Length;
                writer.WriteUShort(arrayLength);
                IEnumerable<object> array = (IEnumerable<object>)value;
                foreach (object element in array) WriteFieldType(writer, element);
            }
            else
            {
                if (value is bool)
                    writer.WriteBool((bool)value);
                else if (value is byte)
                    writer.WriteByte((byte)value);
                else if (value is double)
                    writer.WriteDouble((double)value);
                else if (value is float)
                    writer.WriteFloat((float)value);
                else if (value is int)
                    writer.WriteInt((int)value);
                else if (value is long)
                    writer.WriteLong((long)value);
                else if (value is sbyte)
                    writer.WriteSByte((sbyte)value);
                else if (value is short)
                    writer.WriteShort((short)value);
                else if (value is uint)
                    writer.WriteUInt((uint)value);
                else if (value is ulong)
                    writer.WriteULong((ulong)value);
                else if (value is ushort)
                    writer.WriteUShort((ushort)value);
                else if (value is string)
                    writer.WriteString((string)value);
                else if (value is Vector3)
                {
                    Vector3 vector3 = (Vector3)value;
                    writer.WriteFloat(vector3.x);
                    writer.WriteFloat(vector3.y);
                    writer.WriteFloat(vector3.z);
                }
                else if (value is Vector2)
                {
                    Vector2 vector2 = (Vector2)value;
                    writer.WriteFloat(vector2.x);
                    writer.WriteFloat(vector2.y);
                }
                else if (value is Quaternion)
                {
                    Vector3 euler = ((Quaternion)value).eulerAngles;
                    writer.WriteFloat(euler.x);
                    writer.WriteFloat(euler.y);
                    writer.WriteFloat(euler.z);
                }
                else
                {
                    BinarySerializer.Serialize(value, writer);
                }
            }
        }

        internal static object ReadFieldType(BitReader reader, Type type)
        {
            if (type.IsArray)
            {
                ushort arrayLength = reader.ReadUShort();
                Type elementType = type.GetElementType();
                Array array = Array.CreateInstance(elementType, arrayLength);
                for (int i = 0; i < arrayLength; i++) array.SetValue(ReadFieldType(reader, elementType), i);
                return array;
            }
            else
            {
                if (type == typeof(bool))
                    return reader.ReadBool();
                else if (type == typeof(byte))
                    return reader.ReadByte();
                else if (type == typeof(double))
                    return reader.ReadDouble();
                else if (type == typeof(float))
                    return reader.ReadFloat();
                else if (type == typeof(int))
                    return reader.ReadInt();
                else if (type == typeof(long))
                    return reader.ReadLong();
                else if (type == typeof(sbyte))
                    return reader.ReadSByte();
                else if (type == typeof(short))
                    return reader.ReadShort();
                else if (type == typeof(uint))
                    return reader.ReadUInt();
                else if (type == typeof(ulong))
                    return reader.ReadULong();
                else if (type == typeof(ushort))
                    return reader.ReadUShort();
                else if (type == typeof(string))
                    return reader.ReadString();
                else if (type == typeof(Vector3))
                {
                    Vector3 vector3 = new Vector3();
                    vector3.x = reader.ReadFloat();
                    vector3.x = reader.ReadFloat();
                    vector3.y = reader.ReadFloat();
                    return vector3;
                }
                else if (type == typeof(Vector2))
                {
                    Vector2 vector2 = new Vector2();
                    vector2.x = reader.ReadFloat();
                    vector2.x = reader.ReadFloat();
                    return vector2;
                }
                else if (type == typeof(Quaternion))
                {
                    Vector3 euler = new Vector3();
                    euler.x = reader.ReadFloat();
                    euler.x = reader.ReadFloat();
                    euler.y = reader.ReadFloat();
                    return Quaternion.Euler(euler);
                }
                else
                {
                    return BinarySerializer.Deserialize(reader, type);
                }
            }
        }
    }
}
