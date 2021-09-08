using System;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Two-way serializer wrapping FastBufferReader or FastBufferWriter.
    /// 
    /// Implemented as a ref struct for two reasons:
    /// 1. The BufferSerializer cannot outlive the FBR/FBW it wraps or using it will cause a crash
    /// 2. The BufferSerializer must always be passed by reference and can't be copied
    ///
    /// Ref structs help enforce both of those rules: they can't out live the stack context in which they were
    /// created, and they're always passed by reference no matter what.
    ///
    /// BufferSerializer doesn't wrapp FastBufferReader or FastBufferWriter directly because it can't.
    /// ref structs can't implement interfaces, and in order to be able to have two different implementations with
    /// the same interface (which allows us to avoid an "if(IsReader)" on every call), the thing directly wrapping
    /// the struct has to implement an interface. So IBufferSerializerImplementation exists as the interface,
    /// which is implemented by a normal struct, while the ref struct wraps the normal one to enforce the two above
    /// requirements. (Allowing direct access to the IBufferSerializerImplementation struct would allow dangerous
    /// things to happen because the struct's lifetime could outlive the Reader/Writer's.)
    /// </summary>
    /// <typeparam name="TImplementation">The implementation struct</typeparam>
    public ref struct BufferSerializer<TImplementation> where TImplementation : IBufferSerializerImplementation
    {
        private TImplementation m_Implementation;

        /// <summary>
        /// Check if the contained implementation is a reader
        /// </summary>
        public bool IsReader => m_Implementation.IsReader;

        /// <summary>
        /// Check if the contained implementation is a writer
        /// </summary>
        public bool IsWriter => m_Implementation.IsWriter;

        public BufferSerializer(TImplementation implementation)
        {
            m_Implementation = implementation;
        }

        /// <summary>
        /// Retrieves the FastBufferReader instance. Only valid if IsReader = true, throws
        /// InvalidOperationException otherwise.
        /// </summary>
        /// <returns>Reader instance</returns>
        public ref FastBufferReader GetFastBufferReader()
        {
            return ref m_Implementation.GetFastBufferReader();
        }

        /// <summary>
        /// Retrieves the FastBufferWriter instance. Only valid if IsWriter = true, throws
        /// InvalidOperationException otherwise.
        /// </summary>
        /// <returns>Writer instance</returns>
        public ref FastBufferWriter GetFastBufferWriter()
        {
            return ref m_Implementation.GetFastBufferWriter();
        }

        /// <summary>
        /// Serialize an object value.
        /// Note: Will ALWAYS cause allocations when reading.
        /// This function is also much slower than the others as it has to figure out how to serialize
        /// the object using runtime reflection.
        /// It's recommended not to use this unless you have no choice.
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        /// <param name="type">Type to deserialize to when reading</param>
        /// <param name="isNullable">
        /// If true, will force an isNull byte to be written.
        /// Some types will write this byte regardless.
        /// </param>
        public void SerializeValue(ref object value, Type type, bool isNullable = false)
        {
            m_Implementation.SerializeValue(ref value, type, isNullable);
        }

        /// <summary>
        /// Serialize an INetworkSerializable
        /// If your INetworkSerializable is implemented by a struct, as opposed to a class, use this
        /// function instead of SerializeValue. SerializeValue will incur a boxing allocation,
        /// SerializeNetworkSerializable will not.
        ///
        /// A definition of SerializeValue that doesn't allocate can't be created because C#
        /// doesn't allow overriding generics based solely on the constraint, so this would conflict
        /// with WriteValue&lt;T&gt;(ref T value) where T: unmanaged
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeNetworkSerializable<T>(ref T value) where T : INetworkSerializable
        {
            m_Implementation.SerializeNetworkSerializable(ref value);
        }

        /// <summary>
        /// Serialize an INetworkSerializable
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValue(ref INetworkSerializable value)
        {
            m_Implementation.SerializeValue(ref value);
        }

        /// <summary>
        /// Serialize a GameObject
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValue(ref GameObject value)
        {
            m_Implementation.SerializeValue(ref value);
        }

        /// <summary>
        /// Serialize a NetworkObject
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValue(ref NetworkObject value)
        {
            m_Implementation.SerializeValue(ref value);
        }

        /// <summary>
        /// Serialize a NetworkBehaviour
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValue(ref NetworkBehaviour value)
        {
            m_Implementation.SerializeValue(ref value);
        }

        /// <summary>
        /// Serialize a string.
        ///
        /// Note: Will ALWAYS allocate a new string when reading.
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="s">Value to serialize</param>
        /// <param name="oneByteChars">
        /// If true, will truncate each char to one byte.
        /// This is slower than two-byte chars, but uses less bandwidth.
        /// </param>
        public void SerializeValue(ref string s, bool oneByteChars = false)
        {
            m_Implementation.SerializeValue(ref s, oneByteChars);
        }

        /// <summary>
        /// Serialize an array value.
        ///
        /// Note: Will ALWAYS allocate a new array when reading.
        /// If you have a statically-sized array that you know is large enough, it's recommended to
        /// serialize the size yourself and iterate serializing array members.
        /// 
        /// (This is because C# doesn't allow setting an array's length value, so deserializing
        /// into an existing array of larger size would result in an array that doesn't have as many values
        /// as its Length indicates it should.)
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="array">Value to serialize</param>
        public void SerializeValue<T>(ref T[] array) where T : unmanaged
        {
            m_Implementation.SerializeValue(ref array);
        }

        /// <summary>
        /// Serialize a single byte
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValue(ref byte value)
        {
            m_Implementation.SerializeValue(ref value);
        }

        /// <summary>
        /// Serialize an unmanaged type. Supports basic value types as well as structs.
        /// The provided type will be copied to/from the buffer as it exists in memory.
        /// 
        /// Throws OverflowException if the end of the buffer has been reached.
        /// Write buffers will grow up to the maximum allowable message size before throwing OverflowException.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValue<T>(ref T value) where T : unmanaged
        {
            m_Implementation.SerializeValue(ref value);
        }

        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be writing multiple fields back-to-back and you know the total size,
        /// you can call PreCheck() once on the total size, and then follow it with calls to
        /// SerializeValuePreChecked() for faster serialization. Write buffers will grow during PreCheck()
        /// if needed.
        /// 
        /// PreChecked serialization operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using PreCheck(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using PreCheck is to avoid bounds checking in the following
        /// operations in release builds.
        ///
        /// To get the correct size to check for, use FastBufferWriter.GetWriteSize(value) or
        /// FastBufferWriter.GetWriteSize&lt;type&gt;()
        /// </summary>
        /// <param name="amount">Number of bytes you plan to read or write</param>
        /// <returns>True if the read/write can proceed, false otherwise.</returns>
        public bool PreCheck(int amount)
        {
            return m_Implementation.PreCheck(amount);
        }

        /// <summary>
        /// Serialize a GameObject.
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValuePreChecked(ref GameObject value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }

        /// <summary>
        /// Serialize a NetworkObject
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValuePreChecked(ref NetworkObject value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }

        /// <summary>
        /// Serialize a NetworkBehaviour
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValuePreChecked(ref NetworkBehaviour value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }

        /// <summary>
        /// Serialize a string.
        /// 
        /// Note: Will ALWAYS allocate a new string when reading.
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="s">Value to serialize</param>
        /// <param name="oneByteChars">
        /// If true, will truncate each char to one byte.
        /// This is slower than two-byte chars, but uses less bandwidth.
        /// </param>
        public void SerializeValuePreChecked(ref string s, bool oneByteChars = false)
        {
            m_Implementation.SerializeValuePreChecked(ref s, oneByteChars);
        }

        /// <summary>
        /// Serialize an array value.
        ///
        /// Note: Will ALWAYS allocate a new array when reading.
        /// If you have a statically-sized array that you know is large enough, it's recommended to
        /// serialize the size yourself and iterate serializing array members.
        /// 
        /// (This is because C# doesn't allow setting an array's length value, so deserializing
        /// into an existing array of larger size would result in an array that doesn't have as many values
        /// as its Length indicates it should.)
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="array">Value to serialize</param>
        public void SerializeValuePreChecked<T>(ref T[] array) where T : unmanaged
        {
            m_Implementation.SerializeValuePreChecked(ref array);
        }

        /// <summary>
        /// Serialize a single byte
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValuePreChecked(ref byte value)
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }

        /// <summary>
        /// Serialize an unmanaged type. Supports basic value types as well as structs.
        /// The provided type will be copied to/from the buffer as it exists in memory.
        ///
        /// Using the PreChecked versions of these functions requires calling PreCheck() ahead of time, and they should only
        /// be called if PreCheck() returns true. This is an efficiency option, as it allows you to PreCheck() multiple
        /// serialization operations in one function call instead of having to do bounds checking on every call.
        /// </summary>
        /// <param name="value">Value to serialize</param>
        public void SerializeValuePreChecked<T>(ref T value) where T : unmanaged
        {
            m_Implementation.SerializeValuePreChecked(ref value);
        }
    }
}
