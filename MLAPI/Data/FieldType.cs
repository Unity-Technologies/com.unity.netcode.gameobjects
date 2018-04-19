using MLAPI.NetworkingManagerComponents.Binary;
using System;
using System.Reflection;
using UnityEngine;

namespace MLAPI.Data
{
    /// <summary>
    /// The datatype used to classify SyncedVars
    /// </summary>
    internal enum FieldType
    {
        Bool,
        Byte,
        Double,
        Single,
        Int,
        Long,
        SByte,
        Short,
        UInt,
        ULong,
        UShort,
        String,
        Vector3,
        Vector2,
        Quaternion,
        BoolArray,
        ByteArray,
        DoubleArray,
        SingleArray,
        IntArray,
        LongArray,
        SByteArray,
        ShortArray,
        UIntArray,
        ULongArray,
        UShortArray,
        StringArray,
        Vector3Array,
        Vector2Array,
        QuaternionArray,
        Invalid
    }

    internal static class FieldTypeHelper
    {
        internal static void WriteFieldType(BitWriter writer, FieldInfo field, object fieldInstance, FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Bool:
                    writer.WriteBool((bool)field.GetValue(fieldInstance));
                    break;
                case FieldType.Byte:
                    writer.WriteByte((byte)field.GetValue(fieldInstance));
                    break;
                case FieldType.Double:
                    writer.WriteDouble((double)field.GetValue(fieldInstance));
                    break;
                case FieldType.Single:
                    writer.WriteFloat((float)field.GetValue(fieldInstance));
                    break;
                case FieldType.Int:
                    writer.WriteInt((int)field.GetValue(fieldInstance));
                    break;
                case FieldType.Long:
                    writer.WriteLong((long)field.GetValue(fieldInstance));
                    break;
                case FieldType.SByte:
                    writer.WriteSByte((sbyte)field.GetValue(fieldInstance));
                    break;
                case FieldType.Short:
                    writer.WriteShort((short)field.GetValue(fieldInstance));
                    break;
                case FieldType.UInt:
                    writer.WriteUInt((uint)field.GetValue(fieldInstance));
                    break;
                case FieldType.ULong:
                    writer.WriteULong((ulong)field.GetValue(fieldInstance));
                    break;
                case FieldType.UShort:
                    writer.WriteUShort((ushort)field.GetValue(fieldInstance));
                    break;
                case FieldType.String:
                    writer.WriteString((string)field.GetValue(fieldInstance));
                    break;
                case FieldType.Vector3:
                    Vector3 vector3 = (Vector3)field.GetValue(fieldInstance);
                    writer.WriteFloat(vector3.x);
                    writer.WriteFloat(vector3.y);
                    writer.WriteFloat(vector3.z);
                    break;
                case FieldType.Vector2:
                    Vector2 vector2 = (Vector2)field.GetValue(fieldInstance);
                    writer.WriteFloat(vector2.x);
                    writer.WriteFloat(vector2.y);
                    break;
                case FieldType.Quaternion:
                    Vector3 euler = ((Quaternion)field.GetValue(fieldInstance)).eulerAngles;
                    writer.WriteFloat(euler.x);
                    writer.WriteFloat(euler.y);
                    writer.WriteFloat(euler.z);
                    break;
                case FieldType.BoolArray:
                    bool[] bools = (bool[])field.GetValue(fieldInstance);
                    writer.WriteUShort((ushort)bools.Length);
                    for (int j = 0; j < bools.Length; j++)
                        writer.WriteBool(bools[j]);
                    break;
                case FieldType.ByteArray:
                    writer.WriteByteArray((byte[])field.GetValue(fieldInstance));
                    break;
                case FieldType.DoubleArray:
                    writer.WriteDoubleArray((double[])field.GetValue(fieldInstance));
                    break;
                case FieldType.SingleArray:
                    writer.WriteFloatArray((float[])field.GetValue(fieldInstance));
                    break;
                case FieldType.IntArray:
                    writer.WriteIntArray((int[])field.GetValue(fieldInstance));
                    break;
                case FieldType.LongArray:
                    writer.WriteLongArray((long[])field.GetValue(fieldInstance));
                    break;
                case FieldType.SByteArray:
                    writer.WriteSByteArray((sbyte[])field.GetValue(fieldInstance));
                    break;
                case FieldType.ShortArray:
                    writer.WriteShortArray((short[])field.GetValue(fieldInstance));
                    break;
                case FieldType.UIntArray:
                    writer.WriteUIntArray((uint[])field.GetValue(fieldInstance));
                    break;
                case FieldType.ULongArray:
                    writer.WriteULongArray((ulong[])field.GetValue(fieldInstance));
                    break;
                case FieldType.UShortArray:
                    writer.WriteUShortArray((ushort[])field.GetValue(fieldInstance));
                    break;
                case FieldType.StringArray:
                    string[] strings = (string[])field.GetValue(fieldInstance);
                    writer.WriteUShort((ushort)strings.Length);
                    for (int j = 0; j < strings.Length; j++)
                        writer.WriteString(strings[j]);
                    break;
                case FieldType.Vector3Array:
                    Vector3[] vector3s = (Vector3[])field.GetValue(fieldInstance);
                    writer.WriteUShort((ushort)vector3s.Length);
                    for (int j = 0; j < vector3s.Length; j++)
                    {
                        writer.WriteFloat(vector3s[j].x);
                        writer.WriteFloat(vector3s[j].y);
                        writer.WriteFloat(vector3s[j].z);
                    }
                    break;
                case FieldType.Vector2Array:
                    Vector2[] vector2s = (Vector2[])field.GetValue(fieldInstance);
                    writer.WriteUShort((ushort)vector2s.Length);
                    for (int j = 0; j < vector2s.Length; j++)
                    {
                        writer.WriteFloat(vector2s[j].x);
                        writer.WriteFloat(vector2s[j].y);
                    }
                    break;
                case FieldType.QuaternionArray:
                    Quaternion[] quaternions = (Quaternion[])field.GetValue(fieldInstance);
                    writer.WriteUShort((ushort)quaternions.Length);
                    for (int j = 0; j < quaternions.Length; j++)
                    {
                        writer.WriteFloat(quaternions[j].eulerAngles.x);
                        writer.WriteFloat(quaternions[j].eulerAngles.y);
                        writer.WriteFloat(quaternions[j].eulerAngles.z);
                    }
                    break;
            }
        }

        internal static FieldType GetFieldType(Type type)
        {
            if (type == typeof(bool))
                return FieldType.Bool;
            else if (type == typeof(byte))
                return FieldType.Byte;
            else if (type == typeof(double))
                return FieldType.Double;
            else if (type == typeof(float))
                return FieldType.Single;
            else if (type == typeof(int))
                return FieldType.Int;
            else if (type == typeof(long))
                return FieldType.Long;
            else if (type == typeof(sbyte))
                return FieldType.SByte;
            else if (type == typeof(short))
                return FieldType.Short;
            else if (type == typeof(uint))
                return FieldType.UInt;
            else if (type == typeof(ulong))
                return FieldType.ULong;
            else if (type == typeof(ushort))
                return FieldType.UShort;
            else if (type == typeof(string))
                return FieldType.String;
            else if (type == typeof(Vector3))
                return FieldType.Vector3;
            else if (type == typeof(Vector2))
                return FieldType.Vector2;
            else if (type == typeof(Quaternion))
                return FieldType.Quaternion;
            else if (type == typeof(bool[]))
                return FieldType.BoolArray;
            else if (type == typeof(byte[]))
                return FieldType.ByteArray;
            else if (type == typeof(double[]))
                return FieldType.DoubleArray;
            else if (type == typeof(float[]))
                return FieldType.SingleArray;
            else if (type == typeof(int[]))
                return FieldType.IntArray;
            else if (type == typeof(long[]))
                return FieldType.LongArray;
            else if (type == typeof(sbyte[]))
                return FieldType.SByteArray;
            else if (type == typeof(short[]))
                return FieldType.ShortArray;
            else if (type == typeof(uint[]))
                return FieldType.UIntArray;
            else if (type == typeof(ulong[]))
                return FieldType.ULongArray;
            else if (type == typeof(ushort[]))
                return FieldType.UShortArray;
            else if (type == typeof(string[]))
                return FieldType.StringArray;
            else if (type == typeof(Vector3[]))
                return FieldType.Vector3Array;
            else if (type == typeof(Vector2[]))
                return FieldType.Vector2Array;
            else if (type == typeof(Quaternion[]))
                return FieldType.QuaternionArray;
            else
                return FieldType.Invalid;
        }
    }
}
