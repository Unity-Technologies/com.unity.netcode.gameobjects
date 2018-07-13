using MLAPI.Attributes;
using MLAPI.Data;
using MLAPI.NetworkingManagerComponents.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MLAPI.NetworkingManagerComponents.Binary
{
    /// <summary>
    /// Helper class for serializing classes to binary
    /// </summary>
    public static class BinarySerializer
    {
        private static Dictionary<string, FieldInfo[]> cachedFields = new Dictionary<string, FieldInfo[]>();
        private static Dictionary<string, MethodInfo> preSerialize = new Dictionary<string, MethodInfo>();
        private static Dictionary<string, MethodInfo> postDeserialize = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Clears the cache of the serializer
        /// </summary>
        public static void ClearCache()
        {
            cachedFields.Clear();
            preSerialize.Clear();
            postDeserialize.Clear();
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
            MethodInfo preMethod;

            if (cachedFields.ContainsKey(instance.GetType().FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            if (preSerialize.ContainsKey(instance.GetType().FullName))
                preMethod = preSerialize[instance.GetType().FullName];
            else
            {
                preMethod = instance.GetType().GetMethod("PreSerialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                preSerialize.Add(instance.GetType().FullName, preMethod);
            }

            if (preMethod != null)
                preMethod.Invoke(instance, null);

            using (BitWriterDeprecated writer = BitWriterDeprecated.Get())
            {
                for (int i = 0; i < sortedFields.Length; i++)
                {
                    FieldTypeHelper.WriteFieldType(writer, sortedFields[i].GetValue(instance));
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
            MethodInfo postMethod;

            if (cachedFields.ContainsKey(instance.GetType().FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            if (postDeserialize.ContainsKey(instance.GetType().FullName))
                postMethod = postDeserialize[instance.GetType().FullName];
            else
            {
                postMethod = instance.GetType().GetMethod("PostDeserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                postDeserialize.Add(instance.GetType().FullName, postMethod);
            }

            using (BitReaderDeprecated reader = BitReaderDeprecated.Get(binary))
            {
                for (int i = 0; i < sortedFields.Length; i++)
                    sortedFields[i].SetValue(instance, FieldTypeHelper.ReadFieldType(reader, sortedFields[i].FieldType));

                if (postMethod != null)
                    postMethod.Invoke(instance, null);
                
                return instance;
            }
        }

        /// <summary>
        /// Deserializes binary and turns it back into the original class
        /// </summary>
        /// <typeparam name="T">The type to return</typeparam>
        /// <param name="reader">The reader to deserialize</param>
        /// <returns>An instance of T</returns>
        public static T Deserialize<T>(BitReaderDeprecated reader) where T : new()
        {
            T instance = new T();

            FieldInfo[] sortedFields;
            MethodInfo postMethod;

            if (cachedFields.ContainsKey(instance.GetType().FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            if (postDeserialize.ContainsKey(instance.GetType().FullName))
                postMethod = postDeserialize[instance.GetType().FullName];
            else
            {
                postMethod = instance.GetType().GetMethod("PostDeserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                postDeserialize.Add(instance.GetType().FullName, postMethod);
            }

            for (int i = 0; i < sortedFields.Length; i++)
            {
                sortedFields[i].SetValue(instance, FieldTypeHelper.ReadFieldType(reader, sortedFields[i].FieldType));
            }

            if (postMethod != null)
                postMethod.Invoke(instance, null);
            
            return instance;
        }

        internal static void Serialize(object instance, BitWriterDeprecated writer)
        {
            FieldInfo[] sortedFields;
            MethodInfo preMethod;

            if (cachedFields.ContainsKey(instance.GetType().FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            if (preSerialize.ContainsKey(instance.GetType().FullName))
                preMethod = preSerialize[instance.GetType().FullName];
            else
            {
                preMethod = instance.GetType().GetMethod("PreSerialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                preSerialize.Add(instance.GetType().FullName, preMethod);
            }

            if (preMethod != null)
                preMethod.Invoke(instance, null);
            
            for (int i = 0; i < sortedFields.Length; i++)
                FieldTypeHelper.WriteFieldType(writer, sortedFields[i].GetValue(instance));
        }

        internal static object Deserialize(BitReaderDeprecated reader, Type type)
        {
            object instance = Activator.CreateInstance(type);
            FieldInfo[] sortedFields;
            MethodInfo postMethod;

            if (cachedFields.ContainsKey(type.FullName))
                sortedFields = cachedFields[instance.GetType().FullName];
            else
            {
                sortedFields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(x => x.Name).Where(x => !x.IsDefined(typeof(BinaryIgnore), true)).ToArray();
                cachedFields.Add(instance.GetType().FullName, sortedFields);
            }

            if (postDeserialize.ContainsKey(instance.GetType().FullName))
                postMethod = postDeserialize[instance.GetType().FullName];
            else
            {
                postMethod = instance.GetType().GetMethod("PostDeserialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                postDeserialize.Add(instance.GetType().FullName, postMethod);
            }

            for (int i = 0; i < sortedFields.Length; i++)
            {
                sortedFields[i].SetValue(instance, FieldTypeHelper.ReadFieldType(reader, sortedFields[i].FieldType));
            }

            if (postMethod != null)
                postMethod.Invoke(instance, null);

            return instance;
        }
    }
}
