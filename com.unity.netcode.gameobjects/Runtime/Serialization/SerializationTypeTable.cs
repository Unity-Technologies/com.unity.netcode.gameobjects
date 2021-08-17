using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public static class SerializationTypeTable
    {
        public delegate void Serialize(ref FastBufferWriter writer, object value);

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
    }
}