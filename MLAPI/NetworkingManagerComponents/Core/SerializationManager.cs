using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MLAPI.Logging;
using MLAPI.Serialization;

namespace MLAPI.Serialization
{
    /// <summary>
    /// Helper class to add custom serialization for arbitrary types that are hidden behind the 3rd party wall.
    /// Useful for doing things like serializing .NET types.
    /// </summary>
    public static class SerializationManager
    {   
        private static readonly Dictionary<Type, BoxedSerializationDelegate> cachedExternalSerializers = new Dictionary<Type, BoxedSerializationDelegate>();
        private static readonly Dictionary<Type, BoxedDeserializationDelegate> cachedExternalDeserializers = new Dictionary<Type, BoxedDeserializationDelegate>();

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
        /// <param name="onSerialize">The delegate to invoke to serialize the type</param>
        /// <param name="onDeserialize">The delegate to invoke to deserialize the type</param>
        /// <typeparam name="T">The type to register</typeparam>
        public static void RegisterSerializationHandlers<T>(CustomSerializationDelegate<T> onSerialize, CustomDeserializationDelegate<T> onDeserialize)
        {
            cachedExternalSerializers[typeof(T)] = delegate(Stream stream, object instance) { onSerialize(stream, (T)instance); };
            cachedExternalDeserializers[typeof(T)] = delegate(Stream stream) { return onDeserialize(stream); };
        }

        internal static bool TrySerialize(Stream stream, object obj)
        {
            if (cachedExternalSerializers.ContainsKey(obj.GetType()))
            {
                cachedExternalSerializers[obj.GetType()](stream, obj);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool TryDeserialize(Stream stream, Type type, out object obj)
        {
            if (cachedExternalDeserializers.ContainsKey(type))
            {
                obj = cachedExternalDeserializers[type](stream);
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }
    }
}