using MLAPI.Attributes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MLAPI.NetworkingManagerComponents
{
    /// <summary>
    /// Helper class for serializing classes to binary
    /// </summary>
    public static class BinarySerializer
    {
        private static Dictionary<string, FieldInfo[]> cachedFields = new Dictionary<string, FieldInfo[]>();

        /// <summary>
        /// Clears the cache of the serializer
        /// </summary>
        public static void ClearCache()
        {
            cachedFields.Clear();
        }

        /// <summary>
        /// Serializes a class instance to binary
        /// </summary>
        /// <typeparam name="T">The class type to serialize</typeparam>
        /// <param name="instance">The instance to serialize</param>
        /// <returns>Binary serialized version of the instance</returns>
        public static byte[] Serialize<T>(T instance)
        {
            FieldInfo[] sortedFields;

            if (cachedFields.ContainsKey(instance.GetType().FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            int outputSize = 0;
            //Calculate output size
            for (int i = 0; i < sortedFields.Length; i++)
            {
                if (sortedFields[i].FieldType == typeof(bool))
                    outputSize += 1;
                else if (sortedFields[i].FieldType == typeof(byte))
                    outputSize += 1;
                else if (sortedFields[i].FieldType == typeof(char))
                    outputSize += 2;
                else if (sortedFields[i].FieldType == typeof(double))
                    outputSize += 8;
                else if (sortedFields[i].FieldType == typeof(float))
                    outputSize += 4;
                else if (sortedFields[i].FieldType == typeof(decimal))
                    outputSize += 16;
                else if (sortedFields[i].FieldType == typeof(int))
                    outputSize += 4;
                else if (sortedFields[i].FieldType == typeof(long))
                    outputSize += 8;
                else if (sortedFields[i].FieldType == typeof(sbyte))
                    outputSize += 1;
                else if (sortedFields[i].FieldType == typeof(short))
                    outputSize += 2;
                else if (sortedFields[i].FieldType == typeof(uint))
                    outputSize += 4;
                else if (sortedFields[i].FieldType == typeof(ulong))
                    outputSize += 8;
                else if (sortedFields[i].FieldType == typeof(ushort))
                    outputSize += 2;
                else if (sortedFields[i].FieldType == typeof(string))
                    outputSize += Encoding.UTF8.GetByteCount((string)sortedFields[i].GetValue(instance)) + 2;
                else if (sortedFields[i].FieldType == typeof(byte[]))
                    outputSize += ((byte[])sortedFields[i].GetValue(instance)).Length + 2; //Two bytes to specify the size
            }

            //Write data
            using (MemoryStream stream = new MemoryStream(outputSize))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    for (int i = 0; i < sortedFields.Length; i++)
                    {
                        if (sortedFields[i].FieldType == typeof(bool))
                            writer.Write((bool)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(byte))
                            writer.Write((byte)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(char))
                            writer.Write((char)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(double))
                            writer.Write((double)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(float))
                            writer.Write((float)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(decimal))
                            writer.Write((decimal)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(int))
                            writer.Write((int)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(long))
                            writer.Write((long)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(sbyte))
                            writer.Write((sbyte)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(short))
                            writer.Write((short)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(uint))
                            writer.Write((uint)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(ulong))
                            writer.Write((ulong)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(ushort))
                            writer.Write((ushort)sortedFields[i].GetValue(instance));
                        else if (sortedFields[i].FieldType == typeof(string))
                        {
                            writer.Write((ushort)Encoding.UTF8.GetByteCount((string)sortedFields[i].GetValue(instance))); //Size of string in bytes
                            writer.Write(Encoding.UTF8.GetBytes((string)sortedFields[i].GetValue(instance)));
                        }
                        else if (sortedFields[i].FieldType == typeof(byte[]))
                        {
                            writer.Write((ushort)((byte[])sortedFields[i].GetValue(instance)).Length); //Size of byte array
                            writer.Write((byte[])sortedFields[i].GetValue(instance));
                        }
                    }
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes binary and turns it back into the original class
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="binary">The binary to deserialize</param>
        /// <returns>An instance of T</returns>
        public static T Deserialize<T>(byte[] binary) where T : new()
        {
            T instance = new T();

            FieldInfo[] sortedFields;

            if (cachedFields.ContainsKey(instance.GetType().FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            using (MemoryStream stream = new MemoryStream(binary))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    for (int i = 0; i < sortedFields.Length; i++)
                    {
                        if (sortedFields[i].FieldType == typeof(bool))
                            sortedFields[i].SetValue(instance, reader.ReadBoolean());
                        else if (sortedFields[i].FieldType == typeof(byte))
                            sortedFields[i].SetValue(instance, reader.ReadByte());
                        else if (sortedFields[i].FieldType == typeof(char))
                            sortedFields[i].SetValue(instance, reader.ReadChar());
                        else if (sortedFields[i].FieldType == typeof(double))
                            sortedFields[i].SetValue(instance, reader.ReadDouble());
                        else if (sortedFields[i].FieldType == typeof(float))
                            sortedFields[i].SetValue(instance, reader.ReadSingle());
                        else if (sortedFields[i].FieldType == typeof(decimal))
                            sortedFields[i].SetValue(instance, reader.ReadDecimal());
                        else if (sortedFields[i].FieldType == typeof(int))
                            sortedFields[i].SetValue(instance, reader.ReadInt32());
                        else if (sortedFields[i].FieldType == typeof(long))
                            sortedFields[i].SetValue(instance, reader.ReadInt64());
                        else if (sortedFields[i].FieldType == typeof(sbyte))
                            sortedFields[i].SetValue(instance, reader.ReadSByte());
                        else if (sortedFields[i].FieldType == typeof(short))
                            sortedFields[i].SetValue(instance, reader.ReadInt16());
                        else if (sortedFields[i].FieldType == typeof(uint))
                            sortedFields[i].SetValue(instance, reader.ReadUInt32());
                        else if (sortedFields[i].FieldType == typeof(ulong))
                            sortedFields[i].SetValue(instance, reader.ReadUInt64());
                        else if (sortedFields[i].FieldType == typeof(ushort))
                            sortedFields[i].SetValue(instance, reader.ReadUInt64());
                        else if (sortedFields[i].FieldType == typeof(string))
                        {
                            ushort size = reader.ReadUInt16();
                            sortedFields[i].SetValue(instance, Encoding.UTF8.GetString(reader.ReadBytes(size)));
                        }
                        else if (sortedFields[i].FieldType == typeof(byte[]))
                        {
                            ushort size = reader.ReadUInt16();
                            sortedFields[i].SetValue(instance, reader.ReadBytes(size));
                        }
                    }
                }
            }
            return instance;
        }
    }
}
