using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    /// <summary>
    /// Registry for telling FastBufferWriter and FastBufferReader how to read types when passed to
    /// WriteObject and ReadObject, as well as telling BytePacker and ByteUnpacker how to do it when passed to
    /// WriteObjectPacked and ReadObjectPacked.
    ///
    /// These object-based serialization functions shouldn't be used if at all possible, but if they're required,
    /// and you need to serialize a type that's not natively supported, you can register it with the dictionaries here:
    ///
    /// Serializers and Deserializers for FastBufferWriter and FasteBufferReader
    /// SerializersPacked and DeserializersPacked for BytePacker and ByteUnpacker
    /// </summary>
    public static class SerializationTypeTable
    {
        public delegate void Serialize(ref FastBufferWriter writer, object value);
        public delegate void Deserialize(ref FastBufferReader reader, out object value);

        public static Dictionary<Type, Serialize> Serializers = new Dictionary<Type, Serialize>
        {
            [typeof(byte)] = (ref FastBufferWriter writer, object value) => writer.WriteByteSafe((byte)value),
            [typeof(sbyte)] = (ref FastBufferWriter writer, object value) => writer.WriteByteSafe((byte)(sbyte)value),

            [typeof(ushort)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((ushort)value),
            [typeof(short)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((short)value),
            [typeof(uint)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((uint)value),
            [typeof(int)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((int)value),
            [typeof(ulong)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((ulong)value),
            [typeof(long)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((long)value),

            [typeof(float)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((float)value),
            [typeof(double)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((double)value),

            [typeof(string)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((string)value),

            [typeof(Vector2)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Vector2)value),
            [typeof(Vector3)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Vector3)value),
            [typeof(Vector4)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Vector4)value),
            [typeof(Color)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Color)value),
            [typeof(Color32)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Color32)value),
            [typeof(Ray)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Ray)value),
            [typeof(Ray2D)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Ray2D)value),
            [typeof(Quaternion)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Quaternion)value),

            [typeof(char)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((char)value),

            [typeof(bool)] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((bool)value),


            [typeof(byte[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((byte[])value),
            [typeof(sbyte[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((sbyte[])value),

            [typeof(ushort[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((ushort[])value),
            [typeof(short[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((short[])value),
            [typeof(uint[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((uint[])value),
            [typeof(int[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((int[])value),
            [typeof(ulong[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((ulong[])value),
            [typeof(long[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((long[])value),

            [typeof(float[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((float[])value),
            [typeof(double[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((double[])value),

            [typeof(Vector2[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Vector2[])value),
            [typeof(Vector3[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Vector3[])value),
            [typeof(Vector4[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Vector4[])value),
            [typeof(Color[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Color[])value),
            [typeof(Color32[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Color32[])value),
            [typeof(Ray[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Ray[])value),
            [typeof(Ray2D[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Ray2D[])value),
            [typeof(Quaternion[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((Quaternion[])value),

            [typeof(char[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((char[])value),

            [typeof(bool[])] = (ref FastBufferWriter writer, object value) => writer.WriteValueSafe((bool[])value),
        };

        public static Dictionary<Type, Deserialize> Deserializers = new Dictionary<Type, Deserialize>
        {
            [typeof(byte)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadByteSafe(out byte tmp);
                value = tmp;
            },
            [typeof(sbyte)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadByteSafe(out byte tmp);
                value = (sbyte)tmp;
            },

            [typeof(ushort)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out ushort tmp);
                value = tmp;
            },
            [typeof(short)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out short tmp);
                value = tmp;
            },
            [typeof(uint)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out uint tmp);
                value = tmp;
            },
            [typeof(int)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out int tmp);
                value = tmp;
            },
            [typeof(ulong)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out ulong tmp);
                value = tmp;
            },
            [typeof(long)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out long tmp);
                value = tmp;
            },

            [typeof(float)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out float tmp);
                value = tmp;
            },
            [typeof(double)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out double tmp);
                value = tmp;
            },

            [typeof(string)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out string tmp);
                value = tmp;
            },

            [typeof(Vector2)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Vector2 tmp);
                value = tmp;
            },
            [typeof(Vector3)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Vector3 tmp);
                value = tmp;
            },
            [typeof(Vector4)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Vector4 tmp);
                value = tmp;
            },
            [typeof(Color)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Color tmp);
                value = tmp;
            },
            [typeof(Color32)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Color32 tmp);
                value = tmp;
            },
            [typeof(Ray)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Ray tmp);
                value = tmp;
            },
            [typeof(Ray2D)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Ray2D tmp);
                value = tmp;
            },
            [typeof(Quaternion)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Quaternion tmp);
                value = tmp;
            },

            [typeof(char)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out char tmp);
                value = tmp;
            },

            [typeof(bool)] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out bool tmp);
                value = tmp;
            },


            [typeof(byte[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out byte[] tmp);
                value = tmp;
            },
            [typeof(sbyte[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out sbyte[] tmp);
                value = tmp;
            },

            [typeof(ushort[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out ushort[] tmp);
                value = tmp;
            },
            [typeof(short[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out short[] tmp);
                value = tmp;
            },
            [typeof(uint[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out uint[] tmp);
                value = tmp;
            },
            [typeof(int[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out int[] tmp);
                value = tmp;
            },
            [typeof(ulong[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out ulong[] tmp);
                value = tmp;
            },
            [typeof(long[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out long[] tmp);
                value = tmp;
            },

            [typeof(float[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out float[] tmp);
                value = tmp;
            },
            [typeof(double[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out double[] tmp);
                value = tmp;
            },

            [typeof(Vector2[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Vector2[] tmp);
                value = tmp;
            },
            [typeof(Vector3[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Vector3[] tmp);
                value = tmp;
            },
            [typeof(Vector4[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Vector4[] tmp);
                value = tmp;
            },
            [typeof(Color[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Color[] tmp);
                value = tmp;
            },
            [typeof(Color32[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Color32[] tmp);
                value = tmp;
            },
            [typeof(Ray[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Ray[] tmp);
                value = tmp;
            },
            [typeof(Ray2D[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Ray2D[] tmp);
                value = tmp;
            },
            [typeof(Quaternion[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out Quaternion[] tmp);
                value = tmp;
            },

            [typeof(char[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out char[] tmp);
                value = tmp;
            },

            [typeof(bool[])] = (ref FastBufferReader reader, out object value) =>
            {
                reader.ReadValueSafe(out bool[] tmp);
                value = tmp;
            },
        };

        public static Dictionary<Type, Serialize> SerializersPacked = new Dictionary<Type, Serialize>
        {
            [typeof(byte)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (byte)value),
            [typeof(sbyte)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (byte)(sbyte)value),

            [typeof(ushort)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (ushort)value),
            [typeof(short)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (short)value),
            [typeof(uint)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (uint)value),
            [typeof(int)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (int)value),
            [typeof(ulong)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (ulong)value),
            [typeof(long)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (long)value),

            [typeof(float)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (float)value),
            [typeof(double)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (double)value),

            [typeof(string)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (string)value),

            [typeof(Vector2)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Vector2)value),
            [typeof(Vector3)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Vector3)value),
            [typeof(Vector4)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Vector4)value),
            [typeof(Color)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Color)value),
            [typeof(Color32)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Color32)value),
            [typeof(Ray)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Ray)value),
            [typeof(Ray2D)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Ray2D)value),
            [typeof(Quaternion)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (Quaternion)value),

            [typeof(char)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (char)value),

            [typeof(bool)] = (ref FastBufferWriter writer, object value) => BytePacker.WriteValuePacked(ref writer, (bool)value),
        };

        public static Dictionary<Type, Deserialize> DeserializersPacked = new Dictionary<Type, Deserialize>
        {
            [typeof(byte)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out byte tmp);
                value = tmp;
            },
            [typeof(sbyte)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out byte tmp);
                value = (sbyte)tmp;
            },

            [typeof(ushort)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out ushort tmp);
                value = tmp;
            },
            [typeof(short)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out short tmp);
                value = tmp;
            },
            [typeof(uint)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out uint tmp);
                value = tmp;
            },
            [typeof(int)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out int tmp);
                value = tmp;
            },
            [typeof(ulong)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out ulong tmp);
                value = tmp;
            },
            [typeof(long)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out long tmp);
                value = tmp;
            },

            [typeof(float)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out float tmp);
                value = tmp;
            },
            [typeof(double)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out double tmp);
                value = tmp;
            },

            [typeof(string)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out string tmp);
                value = tmp;
            },

            [typeof(Vector2)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Vector2 tmp);
                value = tmp;
            },
            [typeof(Vector3)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Vector3 tmp);
                value = tmp;
            },
            [typeof(Vector4)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Vector4 tmp);
                value = tmp;
            },
            [typeof(Color)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Color tmp);
                value = tmp;
            },
            [typeof(Color32)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Color32 tmp);
                value = tmp;
            },
            [typeof(Ray)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Ray tmp);
                value = tmp;
            },
            [typeof(Ray2D)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Ray2D tmp);
                value = tmp;
            },
            [typeof(Quaternion)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out Quaternion tmp);
                value = tmp;
            },

            [typeof(char)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out char tmp);
                value = tmp;
            },

            [typeof(bool)] = (ref FastBufferReader reader, out object value) =>
            {
                ByteUnpacker.ReadValuePacked(ref reader, out bool tmp);
                value = tmp;
            },
        };
    }
}
