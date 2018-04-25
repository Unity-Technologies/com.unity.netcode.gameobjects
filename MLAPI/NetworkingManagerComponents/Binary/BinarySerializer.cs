using MLAPI.Attributes;
using MLAPI.Data;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents.Binary
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

            using (BitWriter writer = BitWriter.Get())
            {
                for (int i = 0; i < sortedFields.Length; i++)
                {
                    FieldType fieldType = FieldTypeHelper.GetFieldType(sortedFields[i].FieldType);
                    if (fieldType == FieldType.Invalid)
                    {
                        Debug.LogWarning("MLAPI: The field " + sortedFields[i].Name + " will not be serialized as it's not of a supported type. Add the BinaryIgnore attribute to prevent this message from shwoing up.");
                        continue;
                    }
                    FieldTypeHelper.WriteFieldType(writer, sortedFields[i].GetValue(instance), fieldType);
                }
                return writer.Finalize();
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

            BitReader reader = new BitReader(binary);
            for (int i = 0; i < sortedFields.Length; i++)
            {
                FieldType fieldType = FieldTypeHelper.GetFieldType(sortedFields[i].FieldType);
                if (fieldType == FieldType.Invalid)
                {
                    Debug.LogWarning("MLAPI: The field " + sortedFields[i].Name + " will not be deserialized as it's not of a supported type. Add the BinaryIgnore attribute to prevent this message from shwoing up.");
                    continue;
                }
                sortedFields[i].SetValue(instance, FieldTypeHelper.ReadFieldType(reader, fieldType));
            }
            return instance;
        }
    }
}
