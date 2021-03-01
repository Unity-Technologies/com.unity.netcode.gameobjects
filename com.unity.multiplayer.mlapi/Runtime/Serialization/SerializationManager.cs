using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MLAPI.Reflection;
using UnityEngine;

namespace MLAPI.Serialization
{
    /// <summary>
    /// Helper class to manage the MLAPI serialization.
    /// </summary>
    public static class SerializationManager
    {
        private static readonly Dictionary<Type, FieldInfo[]> k_FieldCache = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, BoxedSerializationDelegate> k_CachedExternalSerializers = new Dictionary<Type, BoxedSerializationDelegate>();
        private static readonly Dictionary<Type, BoxedDeserializationDelegate> k_CachedExternalDeserializers = new Dictionary<Type, BoxedDeserializationDelegate>();

        /// <summary>
        /// The delegate used when registering custom deserialization for a type.
        /// </summary>
        /// <param name="stream">The stream to read the data required to construct the type.</param>
        /// <typeparam name="T">The type to deserialize.</typeparam>
        public delegate T CustomDeserializationDelegate<T>(Stream stream);

        /// <summary>
        /// The delegate used when registering custom serialization for a type.
        /// </summary>
        /// <param name="stream">The stream to write data to that is required to reconstruct the type in the deserialization delegate.</param>
        /// <param name="instance">The instance to serialize to the stream.</param>
        /// <typeparam name="T">The type to serialize.</typeparam>
        public delegate void CustomSerializationDelegate<T>(Stream stream, T instance);

        // These two are what we use internally. They box the value.
        private delegate void BoxedSerializationDelegate(Stream stream, object instance);

        private delegate object BoxedDeserializationDelegate(Stream stream);

        /// <summary>
        /// Registers a custom serialization and deserialization pair for a object.
        /// This is useful for writing objects that are behind the third party wall. Such as .NET types.
        /// </summary>
        /// <param name="onSerialize">The delegate to invoke to serialize the type.</param>
        /// <param name="onDeserialize">The delegate to invoke to deserialize the type.</param>
        /// <typeparam name="T">The type to register.</typeparam>
        public static void RegisterSerializationHandlers<T>(CustomSerializationDelegate<T> onSerialize, CustomDeserializationDelegate<T> onDeserialize)
        {
            k_CachedExternalSerializers[typeof(T)] = (stream, instance) => onSerialize(stream, (T)instance);
            k_CachedExternalDeserializers[typeof(T)] = stream => onDeserialize(stream);
        }

        /// <summary>
        /// Removes a serialization handler that was registered previously for a specific type.
        /// This will remove both the serialization and deserialization handler.
        /// </summary>
        /// <typeparam name="T">The type for the serialization handlers to remove.</typeparam>
        /// <returns>Whether or not either the serialization or deserialization handlers for the type was removed.</returns>
        public static bool RemoveSerializationHandlers<T>()
        {
            bool serializationRemoval = k_CachedExternalSerializers.Remove(typeof(T));
            bool deserializationRemoval = k_CachedExternalDeserializers.Remove(typeof(T));

            return serializationRemoval || deserializationRemoval;
        }

        internal static bool TrySerialize(Stream stream, object obj)
        {
            if (k_CachedExternalSerializers.ContainsKey(obj.GetType()))
            {
                k_CachedExternalSerializers[obj.GetType()](stream, obj);
                return true;
            }

            return false;
        }

        internal static bool TryDeserialize(Stream stream, Type type, out object obj)
        {
            if (k_CachedExternalDeserializers.ContainsKey(type))
            {
                obj = k_CachedExternalDeserializers[type](stream);
                return true;
            }

            obj = null;
            return false;
        }

        internal static FieldInfo[] GetFieldsForType(Type type)
        {
            if (k_FieldCache.ContainsKey(type)) return k_FieldCache[type];

            FieldInfo[] fields = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => (x.IsPublic || x.GetCustomAttributes(typeof(SerializeField), true).Length > 0) && IsTypeSupported(x.FieldType))
                .OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();

            k_FieldCache.Add(type, fields);

            return fields;
        }

        private static readonly HashSet<Type> k_SupportedTypes = new HashSet<Type>()
        {
            typeof(byte),
            typeof(byte),
            typeof(sbyte),
            typeof(ushort),
            typeof(short),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
            typeof(bool),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Color),
            typeof(Color32),
            typeof(Ray),
            typeof(Quaternion),
            typeof(char),
            typeof(GameObject),
            typeof(NetworkObject),
            typeof(NetworkBehaviour)
        };

        /// <summary>
        /// Returns if a type is supported for serialization
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>Whether or not the type is supported</returns>
        public static bool IsTypeSupported(Type type)
        {
            return type.IsEnum || k_SupportedTypes.Contains(type) || type.HasInterface(typeof(INetworkSerializable)) ||
                   (k_CachedExternalSerializers.ContainsKey(type) && k_CachedExternalDeserializers.ContainsKey(type)) ||
                   (type.IsArray && type.HasElementType && IsTypeSupported(type.GetElementType()));
        }
    }
}