using System;
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
