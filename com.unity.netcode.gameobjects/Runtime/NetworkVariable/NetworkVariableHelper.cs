using System;
using UnityEngine;

namespace Unity.Netcode
{
    public class NetworkVariableHelper
    {
        // This is called by ILPP during module initialization for all unmanaged INetworkSerializable types
        // This sets up NetworkVariable so that it properly calls NetworkSerialize() when wrapping an INetworkSerializable value
        //
        // The reason this is done is to avoid runtime reflection and boxing in NetworkVariable - without this,
        // NetworkVariable would need to do a `var is INetworkSerializable` check, and then cast to INetworkSerializable,
        // *both* of which would cause a boxing allocation. Alternatively, NetworkVariable could have been split into
        // NetworkVariable and NetworkSerializableVariable or something like that, which would have caused a poor
        // user experience and an API that's easier to get wrong than right. This is a bit ugly on the implementation
        // side, but it gets the best achievable user experience and performance.
        //
        // RuntimeAccessModifiersILPP will make this `public`
        internal static void InitializeDelegatesNetworkSerializable<T>() where T : unmanaged, INetworkSerializable
        {
            NetworkVariableSerialization<T>.SetWriteDelegate(NetworkVariableSerialization<T>.WriteNetworkSerializable);
            NetworkVariableSerialization<T>.SetReadDelegate(NetworkVariableSerialization<T>.ReadNetworkSerializable);
        }
        internal static void InitializeDelegatesStruct<T>() where T : unmanaged, INetworkSerializeByMemcpy
        {
            NetworkVariableSerialization<T>.SetWriteDelegate(NetworkVariableSerialization<T>.WriteStruct);
            NetworkVariableSerialization<T>.SetReadDelegate(NetworkVariableSerialization<T>.ReadStruct);
        }
        internal static void InitializeDelegatesEnum<T>() where T : unmanaged, Enum
        {
            NetworkVariableSerialization<T>.SetWriteDelegate(NetworkVariableSerialization<T>.WriteEnum);
            NetworkVariableSerialization<T>.SetReadDelegate(NetworkVariableSerialization<T>.ReadEnum);
        }
        internal static void InitializeDelegatesPrimitive<T>() where T : unmanaged, IComparable, IConvertible, IComparable<T>, IEquatable<T>
        {
            NetworkVariableSerialization<T>.SetWriteDelegate(NetworkVariableSerialization<T>.WritePrimitive);
            NetworkVariableSerialization<T>.SetReadDelegate(NetworkVariableSerialization<T>.ReadPrimitive);
        }

        internal static void InitializeAllBaseDelegates()
        {
            // Built-in C# types, serialized through a generic method
            InitializeDelegatesPrimitive<bool>();
            InitializeDelegatesPrimitive<byte>();
            InitializeDelegatesPrimitive<sbyte>();
            InitializeDelegatesPrimitive<char>();
            InitializeDelegatesPrimitive<decimal>();
            InitializeDelegatesPrimitive<float>();
            InitializeDelegatesPrimitive<double>();
            InitializeDelegatesPrimitive<short>();
            InitializeDelegatesPrimitive<ushort>();
            InitializeDelegatesPrimitive<int>();
            InitializeDelegatesPrimitive<uint>();
            InitializeDelegatesPrimitive<long>();
            InitializeDelegatesPrimitive<ulong>();

            // Built-in Unity types, serialized with specific overloads because they're structs without ISerializeByMemcpy attached
            NetworkVariableSerialization<Vector2>.SetWriteDelegate((FastBufferWriter writer, in Vector2 value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Vector3>.SetWriteDelegate((FastBufferWriter writer, in Vector3 value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Vector4>.SetWriteDelegate((FastBufferWriter writer, in Vector4 value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Quaternion>.SetWriteDelegate((FastBufferWriter writer, in Quaternion value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Color>.SetWriteDelegate((FastBufferWriter writer, in Color value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Color32>.SetWriteDelegate((FastBufferWriter writer, in Color32 value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Ray>.SetWriteDelegate((FastBufferWriter writer, in Ray value) => { writer.WriteValueSafe(value); });
            NetworkVariableSerialization<Ray2D>.SetWriteDelegate((FastBufferWriter writer, in Ray2D value) => { writer.WriteValueSafe(value); });

            NetworkVariableSerialization<Vector2>.SetReadDelegate((FastBufferReader reader, out Vector2 value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Vector3>.SetReadDelegate((FastBufferReader reader, out Vector3 value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Vector4>.SetReadDelegate((FastBufferReader reader, out Vector4 value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Quaternion>.SetReadDelegate((FastBufferReader reader, out Quaternion value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Color>.SetReadDelegate((FastBufferReader reader, out Color value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Color32>.SetReadDelegate((FastBufferReader reader, out Color32 value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Ray>.SetReadDelegate((FastBufferReader reader, out Ray value) => { reader.ReadValueSafe(out value); });
            NetworkVariableSerialization<Ray2D>.SetReadDelegate((FastBufferReader reader, out Ray2D value) => { reader.ReadValueSafe(out value); });
        }
    }
}
