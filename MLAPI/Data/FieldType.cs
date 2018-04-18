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
        ByteArray,
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
            else if (type == typeof(byte[]))
                return FieldType.ByteArray;
            else
                return FieldType.Invalid;
        }
    }
}
