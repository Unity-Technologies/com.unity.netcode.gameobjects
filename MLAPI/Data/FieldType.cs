using MLAPI.NetworkingManagerComponents.Binary;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace MLAPI.Data
{
    internal static class FieldTypeHelper
    {
        internal static bool ObjectEqual(object o1, object o2)
        {
            if (o1 == null && o2 == null)
                return true;
            if (o1 == null && o2 != null || o1 != null && o2 == null)
                return false;
            if (o1.GetType() != o2.GetType())
                return false;
            if (o1.GetType().IsArray != o2.GetType().IsArray)
                return false;
            if (o1.GetType().IsArray && o1.GetType().GetElementType() != o2.GetType().GetElementType())
                return false;

            if (o1.GetType().IsArray)
            {
                Array ar1 = (Array)o1;
                Array ar2 = (Array)o2;
                if (ar1.Length != ar2.Length)
                    return false;

                int i = 0;
                foreach (object item in ar1)
                {
                    if (item != ar2.GetValue(i))
                        return false;
                    i++;
                }
                return true;
            }
            return o1.Equals(o2);
        }

        //TODO: Better description, method name is not very descriptive.
        internal static object GetReferenceArrayValue(object newValue, object currentValue)
        {
            if (newValue.GetType().IsArray)
            {
                Array newArray = (Array)newValue;
                Array currentArray = (Array)currentValue;
                if (currentValue != null && newArray.Length == currentArray.Length)
                {
                    for (int i = 0; i < newArray.Length; i++) newArray.SetValue(currentArray.GetValue(i), i); //Copy the old array values
                    return newArray;
                }
                else
                {
                    //Create a new instance.
                    Array newArr = Array.CreateInstance(newValue.GetType().GetElementType(), newArray.Length);
                    for (int i = 0; i < newArray.Length; i++) newArr.SetValue(newArray.GetValue(i), i);
                    return newArr;
                }
            }
            return newValue;
        }

        internal static void WriteFieldType(BitWriter writer, object newValue, object oldValue)
        {
            Type type = newValue.GetType();
            if (type.IsArray)
            {
                Array newArray = (Array)newValue;
                Array oldArray = (Array)oldValue;
                if (oldValue == null || newArray.Length != oldArray.Length)
                {
                    writer.WriteBool(false); //False = not a diff
                    //Send the full array.
                    ushort arrayLength = (ushort)newArray.Length;
                    writer.WriteUShort(arrayLength);
                    foreach (object element in newArray) WriteFieldType(writer, element);
                }
                else
                {
                    writer.WriteBool(true); //True = diff
                    //Send diff
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        bool changed = newArray.GetValue(i) != oldArray.GetValue(i);
                        writer.WriteBool(changed);
                        if (changed) WriteFieldType(writer, newArray.GetValue(i));
                    }
                }
            }
            else
            {
                if (newValue is bool)
                    writer.WriteBool((bool)newValue);
                else if (newValue is byte)
                    writer.WriteByte((byte)newValue);
                else if (newValue is double)
                    writer.WriteDouble((double)newValue);
                else if (newValue is float)
                    writer.WriteFloat((float)newValue);
                else if (newValue is int)
                    writer.WriteInt((int)newValue);
                else if (newValue is long)
                    writer.WriteLong((long)newValue);
                else if (newValue is sbyte)
                    writer.WriteSByte((sbyte)newValue);
                else if (newValue is short)
                    writer.WriteShort((short)newValue);
                else if (newValue is uint)
                    writer.WriteUInt((uint)newValue);
                else if (newValue is ulong)
                    writer.WriteULong((ulong)newValue);
                else if (newValue is ushort)
                    writer.WriteUShort((ushort)newValue);
                else if (newValue is string)
                    writer.WriteString((string)newValue);
                else if (newValue is Vector3)
                {
                    Vector3 vector3 = (Vector3)newValue;
                    writer.WriteFloat(vector3.x);
                    writer.WriteFloat(vector3.y);
                    writer.WriteFloat(vector3.z);
                }
                else if (newValue is Vector2)
                {
                    Vector2 vector2 = (Vector2)newValue;
                    writer.WriteFloat(vector2.x);
                    writer.WriteFloat(vector2.y);
                }
                else if (newValue is Quaternion)
                {
                    Vector3 euler = ((Quaternion)newValue).eulerAngles;
                    writer.WriteFloat(euler.x);
                    writer.WriteFloat(euler.y);
                    writer.WriteFloat(euler.z);
                }
                else
                {
                    BinarySerializer.Serialize(newValue, writer);
                }
            }
        }

        internal static void WriteFieldType(BitWriter writer, object value)
        {
            Type type = value.GetType();
            if (type.IsArray)
            {
                Array array = (Array)value;
                ushort arrayLength = (ushort)array.Length;
                writer.WriteUShort(arrayLength);
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

        internal static object ReadFieldType(BitReader reader, Type type, ref object oldValueRef)
        {
            if (type.IsArray)
            {
                bool diffMode = reader.ReadBool();
                if (diffMode)
                {
                    Array arr = (Array)oldValueRef;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (!reader.ReadBool()) //If it's not changed
                            continue;

                        arr.SetValue(ReadFieldType(reader, type.GetElementType()), i);
                    }
                    return arr;
                }
                else
                {
                    ushort arrayLength = reader.ReadUShort();
                    Type elementType = type.GetElementType();
                    Array array = Array.CreateInstance(elementType, arrayLength);
                    for (int i = 0; i < arrayLength; i++) array.SetValue(ReadFieldType(reader, elementType), i);
                    return array;
                }
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
