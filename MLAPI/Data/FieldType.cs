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
        internal static object ReadFieldType(BitReader reader, FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Bool:
                    return reader.ReadBool();
                case FieldType.Byte:
                    return reader.ReadByte();
                case FieldType.Double:
                    return reader.ReadDouble();
                case FieldType.Single:
                    return reader.ReadFloat();
                case FieldType.Int:
                    return reader.ReadInt();
                case FieldType.Long:
                    return reader.ReadLong();
                case FieldType.SByte:
                    return reader.ReadSByte();
                case FieldType.Short:
                    return reader.ReadShort();
                case FieldType.UInt:
                    return reader.ReadUInt();
                case FieldType.ULong:
                    return reader.ReadULong();
                case FieldType.UShort:
                    return reader.ReadUShort();
                case FieldType.String:
                    return reader.ReadString();
                case FieldType.Vector3:
                    Vector3 vector3 = Vector3.zero;
                    vector3.x = reader.ReadFloat();
                    vector3.y = reader.ReadFloat();
                    vector3.z = reader.ReadFloat();
                    return vector3;
                case FieldType.Vector2:
                    Vector2 vector2 = Vector2.zero;
                    vector2.x = reader.ReadFloat();
                    vector2.y = reader.ReadFloat();
                    return vector2;
                case FieldType.Quaternion:
                    Vector3 eulerAngle = Vector3.zero;
                    eulerAngle.x = reader.ReadFloat();
                    eulerAngle.y = reader.ReadFloat();
                    eulerAngle.z = reader.ReadFloat();
                    return Quaternion.Euler(eulerAngle);
                case FieldType.BoolArray:
                    ushort boolCount = reader.ReadUShort();
                    bool[] bools = new bool[boolCount];
                    for (int j = 0; j < boolCount; j++)
                        bools[j] =  reader.ReadBool();
                    return bools;
                case FieldType.ByteArray:
                    return reader.ReadByteArray();
                case FieldType.DoubleArray:
                    return reader.ReadDoubleArray();
                case FieldType.SingleArray:
                    return reader.ReadFloatArray();
                case FieldType.IntArray:
                    return reader.ReadIntArray();
                case FieldType.LongArray:
                    return reader.ReadLongArray();
                case FieldType.SByteArray:
                    return reader.ReadSByteArray();
                case FieldType.ShortArray:
                    return reader.ReadShortArray();
                case FieldType.UIntArray:
                    return reader.ReadUIntArray();
                case FieldType.ULongArray:
                    return reader.ReadULongArray();
                case FieldType.UShortArray:
                    return reader.ReadUShortArray();
                case FieldType.StringArray:
                    ushort stringCount = reader.ReadUShort();
                    string[] strings = new string[stringCount];
                    for (int j = 0; j < stringCount; j++)
                        strings[j] = reader.ReadString();
                    return strings;
                case FieldType.Vector3Array:
                    ushort vector3Count = reader.ReadUShort();
                    Vector3[] vector3s = new Vector3[vector3Count];
                    for (int j = 0; j < vector3Count; j++)
                    {
                        Vector3 vec3 = Vector3.zero;
                        vec3.x = reader.ReadFloat();
                        vec3.y = reader.ReadFloat();
                        vec3.z = reader.ReadFloat();
                        vector3s[j] = vec3;
                    }
                    return vector3s;
                case FieldType.Vector2Array:
                    ushort vector2Count = reader.ReadUShort();
                    Vector2[] vector2s = new Vector2[vector2Count];
                    for (int j = 0; j < vector2Count; j++)
                    {
                        Vector2 vec2 = Vector2.zero;
                        vec2.x = reader.ReadFloat();
                        vec2.y = reader.ReadFloat();
                        vector2s[j] = vec2;
                    }
                    return vector2s;
                case FieldType.QuaternionArray:
                    ushort quaternionCount = reader.ReadUShort();
                    Quaternion[] quaternions = new Quaternion[quaternionCount];
                    for (int j = 0; j < quaternionCount; j++)
                    {
                        Vector3 vec3 = Vector3.zero;
                        vec3.x = reader.ReadFloat();
                        vec3.y = reader.ReadFloat();
                        vec3.z = reader.ReadFloat();
                        quaternions[j] = Quaternion.Euler(vec3);
                    }
                    return quaternions;
                case FieldType.Invalid:
                    return null;
            }
            return null;
        }

        internal static void WriteFieldType(BitWriter writer, object value, FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Bool:
                    writer.WriteBool((bool)value);
                    break;
                case FieldType.Byte:
                    writer.WriteByte((byte)value);
                    break;
                case FieldType.Double:
                    writer.WriteDouble((double)value);
                    break;
                case FieldType.Single:
                    writer.WriteFloat((float)value);
                    break;
                case FieldType.Int:
                    writer.WriteInt((int)value);
                    break;
                case FieldType.Long:
                    writer.WriteLong((long)value);
                    break;
                case FieldType.SByte:
                    writer.WriteSByte((sbyte)value);
                    break;
                case FieldType.Short:
                    writer.WriteShort((short)value);
                    break;
                case FieldType.UInt:
                    writer.WriteUInt((uint)value);
                    break;
                case FieldType.ULong:
                    writer.WriteULong((ulong)value);
                    break;
                case FieldType.UShort:
                    writer.WriteUShort((ushort)value);
                    break;
                case FieldType.String:
                    writer.WriteString((string)value);
                    break;
                case FieldType.Vector3:
                    Vector3 vector3 = (Vector3)value;
                    writer.WriteFloat(vector3.x);
                    writer.WriteFloat(vector3.y);
                    writer.WriteFloat(vector3.z);
                    break;
                case FieldType.Vector2:
                    Vector2 vector2 = (Vector2)value;
                    writer.WriteFloat(vector2.x);
                    writer.WriteFloat(vector2.y);
                    break;
                case FieldType.Quaternion:
                    Vector3 euler = ((Quaternion)value).eulerAngles;
                    writer.WriteFloat(euler.x);
                    writer.WriteFloat(euler.y);
                    writer.WriteFloat(euler.z);
                    break;
                case FieldType.BoolArray:
                    bool[] bools = (bool[])value;
                    writer.WriteUShort((ushort)bools.Length);
                    for (int j = 0; j < bools.Length; j++)
                        writer.WriteBool(bools[j]);
                    break;
                case FieldType.ByteArray:
                    writer.WriteByteArray((byte[])value);
                    break;
                case FieldType.DoubleArray:
                    writer.WriteDoubleArray((double[])value);
                    break;
                case FieldType.SingleArray:
                    writer.WriteFloatArray((float[])value);
                    break;
                case FieldType.IntArray:
                    writer.WriteIntArray((int[])value);
                    break;
                case FieldType.LongArray:
                    writer.WriteLongArray((long[])value);
                    break;
                case FieldType.SByteArray:
                    writer.WriteSByteArray((sbyte[])value);
                    break;
                case FieldType.ShortArray:
                    writer.WriteShortArray((short[])value);
                    break;
                case FieldType.UIntArray:
                    writer.WriteUIntArray((uint[])value);
                    break;
                case FieldType.ULongArray:
                    writer.WriteULongArray((ulong[])value);
                    break;
                case FieldType.UShortArray:
                    writer.WriteUShortArray((ushort[])value);
                    break;
                case FieldType.StringArray:
                    string[] strings = (string[])value;
                    writer.WriteUShort((ushort)strings.Length);
                    for (int j = 0; j < strings.Length; j++)
                        writer.WriteString(strings[j]);
                    break;
                case FieldType.Vector3Array:
                    Vector3[] vector3s = (Vector3[])value;
                    writer.WriteUShort((ushort)vector3s.Length);
                    for (int j = 0; j < vector3s.Length; j++)
                    {
                        writer.WriteFloat(vector3s[j].x);
                        writer.WriteFloat(vector3s[j].y);
                        writer.WriteFloat(vector3s[j].z);
                    }
                    break;
                case FieldType.Vector2Array:
                    Vector2[] vector2s = (Vector2[])value;
                    writer.WriteUShort((ushort)vector2s.Length);
                    for (int j = 0; j < vector2s.Length; j++)
                    {
                        writer.WriteFloat(vector2s[j].x);
                        writer.WriteFloat(vector2s[j].y);
                    }
                    break;
                case FieldType.QuaternionArray:
                    Quaternion[] quaternions = (Quaternion[])value;
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

        internal static object[] ReadObjects(BitReader reader, byte paramCount)
        {
            object[] returnVal = new object[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                FieldType fieldType = (FieldType)reader.ReadBits(5);

                switch (fieldType)
                {
                    case FieldType.Bool:
                        returnVal[i] = reader.ReadBool();
                        break;
                    case FieldType.Byte:
                        returnVal[i] = reader.ReadByte();
                        break;
                    case FieldType.Double:
                        returnVal[i] = reader.ReadDouble();
                        break;
                    case FieldType.Single:
                        returnVal[i] = reader.ReadFloat();
                        break;
                    case FieldType.Int:
                        returnVal[i] = reader.ReadInt();
                        break;
                    case FieldType.Long:
                        returnVal[i] = reader.ReadLong();
                        break;
                    case FieldType.SByte:
                        returnVal[i] = reader.ReadSByte();
                        break;
                    case FieldType.Short:
                        returnVal[i] = reader.ReadShort();
                        break;
                    case FieldType.UInt:
                        returnVal[i] = reader.ReadUInt();
                        break;
                    case FieldType.ULong:
                        returnVal[i] = reader.ReadULong();
                        break;
                    case FieldType.UShort:
                        returnVal[i] = reader.ReadUShort();
                        break;
                    case FieldType.String:
                        returnVal[i] = reader.ReadString();
                        break;
                    case FieldType.Vector3:
                        Vector3 vector3 = Vector3.zero;
                        vector3.x = reader.ReadFloat();
                        vector3.y = reader.ReadFloat();
                        vector3.z = reader.ReadFloat();
                        returnVal[i] = vector3;
                        break;
                    case FieldType.Vector2:
                        Vector2 vector2 = Vector2.zero;
                        vector2.x = reader.ReadFloat();
                        vector2.y = reader.ReadFloat();
                        returnVal[i] = vector2;
                        break;
                    case FieldType.Quaternion:
                        Vector3 eulerAngle = Vector3.zero;
                        eulerAngle.x = reader.ReadFloat();
                        eulerAngle.y = reader.ReadFloat();
                        eulerAngle.z = reader.ReadFloat();
                        returnVal[i] = Quaternion.Euler(eulerAngle);
                        break;
                    case FieldType.BoolArray:
                        ushort boolCount = reader.ReadUShort();
                        bool[] bools = new bool[boolCount];
                        for (int j = 0; j < boolCount; j++)
                            bools[j] = reader.ReadBool();
                        returnVal[i] = bools;
                        break;
                    case FieldType.ByteArray:
                        returnVal[i] = reader.ReadByteArray();
                        break;
                    case FieldType.DoubleArray:
                        returnVal[i] = reader.ReadDoubleArray();
                        break;
                    case FieldType.SingleArray:
                        returnVal[i] = reader.ReadFloatArray();
                        break;
                    case FieldType.IntArray:
                        returnVal[i] = reader.ReadIntArray();
                        break;
                    case FieldType.LongArray:
                        returnVal[i] = reader.ReadLongArray();
                        break;
                    case FieldType.SByteArray:
                        returnVal[i] = reader.ReadSByteArray();
                        break;
                    case FieldType.ShortArray:
                        returnVal[i] = reader.ReadShortArray();
                        break;
                    case FieldType.UIntArray:
                        returnVal[i] = reader.ReadUIntArray();
                        break;
                    case FieldType.ULongArray:
                        returnVal[i] = reader.ReadULongArray();
                        break;
                    case FieldType.UShortArray:
                        returnVal[i] = reader.ReadUShortArray();
                        break;
                    case FieldType.StringArray:
                        ushort stringCount = reader.ReadUShort();
                        string[] strings = new string[stringCount];
                        for (int j = 0; j < stringCount; j++)
                            strings[j] = reader.ReadString();
                        returnVal[i] = strings;
                        break;
                    case FieldType.Vector3Array:
                        ushort vector3Count = reader.ReadUShort();
                        Vector3[] vector3s = new Vector3[vector3Count];
                        for (int j = 0; j < vector3Count; j++)
                        {
                            Vector3 vec3 = Vector3.zero;
                            vec3.x = reader.ReadFloat();
                            vec3.y = reader.ReadFloat();
                            vec3.z = reader.ReadFloat();
                            vector3s[j] = vec3;
                        }
                        returnVal[i] = vector3s;
                        break;
                    case FieldType.Vector2Array:
                        ushort vector2Count = reader.ReadUShort();
                        Vector2[] vector2s = new Vector2[vector2Count];
                        for (int j = 0; j < vector2Count; j++)
                        {
                            Vector2 vec2 = Vector2.zero;
                            vec2.x = reader.ReadFloat();
                            vec2.y = reader.ReadFloat();
                            vector2s[j] = vec2;
                        }
                        returnVal[i] = vector2s;
                        break;
                    case FieldType.QuaternionArray:
                        ushort quaternionCount = reader.ReadUShort();
                        Quaternion[] quaternions = new Quaternion[quaternionCount];
                        for (int j = 0; j < quaternionCount; j++)
                        {
                            Vector3 vec3 = Vector3.zero;
                            vec3.x = reader.ReadFloat();
                            vec3.y = reader.ReadFloat();
                            vec3.z = reader.ReadFloat();
                            quaternions[j] = Quaternion.Euler(vec3);
                        }
                        returnVal[i] = quaternions;
                        break;
                }
            }
            return returnVal;
        }
    }
}
