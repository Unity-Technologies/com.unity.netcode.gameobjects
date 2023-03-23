using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Random = System.Random;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class BufferSerializerTests
    {
        [Test]
        public void TestIsReaderIsWriter()
        {
            var writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                Assert.IsFalse(serializer.IsReader);
                Assert.IsTrue(serializer.IsWriter);
            }
            byte[] readBuffer = new byte[4];
            var reader = new FastBufferReader(readBuffer, Allocator.Temp);
            using (reader)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                Assert.IsTrue(serializer.IsReader);
                Assert.IsFalse(serializer.IsWriter);
            }
        }
        [Test]
        public unsafe void TestGetUnderlyingStructs()
        {
            var writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                FastBufferWriter underlyingWriter = serializer.GetFastBufferWriter();
                Assert.IsTrue(underlyingWriter.Handle == writer.Handle);
                // Can't use Assert.Throws() because ref structs can't be passed into lambdas.
                try
                {
                    serializer.GetFastBufferReader();
                }
                catch (InvalidOperationException)
                {
                    // pass
                }

            }
            byte[] readBuffer = new byte[4];
            var reader = new FastBufferReader(readBuffer, Allocator.Temp);
            using (reader)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                FastBufferReader underlyingReader = serializer.GetFastBufferReader();
                Assert.IsTrue(underlyingReader.Handle == reader.Handle);
                // Can't use Assert.Throws() because ref structs can't be passed into lambdas.
                try
                {
                    serializer.GetFastBufferWriter();
                }
                catch (InvalidOperationException)
                {
                    // pass
                }
            }
        }

        [Test]
        public void TestSerializingValues()
        {
            var random = new Random();
            int value = random.Next();

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                serializer.SerializeValue(ref value);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    int readValue = 0;
                    deserializer.SerializeValue(ref readValue);

                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingBytes()
        {
            var random = new Random();
            byte value = (byte)random.Next();

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                serializer.SerializeValue(ref value);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    byte readValue = 0;
                    deserializer.SerializeValue(ref readValue);

                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingArrays()
        {
            var random = new Random();
            int[] value = { random.Next(), random.Next(), random.Next() };

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                serializer.SerializeValue(ref value);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    int[] readValue = null;
                    deserializer.SerializeValue(ref readValue);

                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingStrings([Values] bool oneBytChars)
        {
            string value = "I am a test string";

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                serializer.SerializeValue(ref value, oneBytChars);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    string readValue = null;
                    deserializer.SerializeValue(ref readValue, oneBytChars);

                    Assert.AreEqual(value, readValue);
                }
            }
        }


        [Test]
        public void TestSerializingValuesPreChecked()
        {
            var random = new Random();
            int value = random.Next();

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value);
                }
                catch (OverflowException)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                serializer.SerializeValuePreChecked(ref value);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    int readValue = 0;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue);
                    }
                    catch (OverflowException)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                    deserializer.SerializeValuePreChecked(ref readValue);

                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingBytesPreChecked()
        {
            var random = new Random();
            byte value = (byte)random.Next();

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value);
                }
                catch (OverflowException)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                serializer.SerializeValuePreChecked(ref value);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    byte readValue = 0;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue);
                    }
                    catch (OverflowException)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                    deserializer.SerializeValuePreChecked(ref readValue);

                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingArraysPreChecked()
        {
            var random = new Random();
            int[] value = { random.Next(), random.Next(), random.Next() };

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value);
                }
                catch (OverflowException)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                serializer.SerializeValuePreChecked(ref value);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    int[] readValue = null;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue);
                    }
                    catch (OverflowException)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                    deserializer.SerializeValuePreChecked(ref readValue);

                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingStringsPreChecked([Values] bool oneBytChars)
        {
            string value = "I am a test string";

            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                var serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value, oneBytChars);
                }
                catch (OverflowException)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value, oneBytChars)));
                serializer.SerializeValuePreChecked(ref value, oneBytChars);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    var deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                    string readValue = null;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue, oneBytChars);
                    }
                    catch (OverflowException)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value, oneBytChars)));
                    deserializer.SerializeValuePreChecked(ref readValue, oneBytChars);

                    Assert.AreEqual(value, readValue);
                }
            }
        }

        private struct MathematicsTypeTestStruct : INetworkSerializable
        {
            public int SerializedWriteSize;
            public int SerializedReadSize;
            public int CalculatedSize;

            private FastBufferReader m_Reader;
            private FastBufferWriter m_Writer;

            private bool m_IsWriting;

            private int m_LastPosition;

            private void VerifyTypeSize(Type valueType)
            {
                var typeSize = UnsafeUtility.SizeOf(valueType);
                var bytesSerialized = GetBytesSerialized();
                if (typeSize != bytesSerialized)
                {
                    Debug.Log($"Type {valueType.Name} serialized {bytesSerialized} bytes but is calculated to be {typeSize} size");
                }
            }

            private int GetBytesSerialized()
            {
                var bytesSerialized = 0;
                if (m_IsWriting)
                {
                    bytesSerialized = m_Writer.Position - m_LastPosition;
                }
                else
                {
                    bytesSerialized = m_Reader.Position - m_LastPosition;
                }
                return bytesSerialized;
            }
            private void SetLastPosition()
            {
                if (m_IsWriting)
                {
                    m_LastPosition = m_Writer.Position;
                }
                else
                {
                    m_LastPosition = m_Reader.Position;
                }
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                var typesToSerialize = new Type[] {typeof(bool2), typeof(bool2x2), typeof(bool2x3),
                    typeof(bool2x4), typeof(bool3), typeof(bool3x2), typeof(bool3x3), typeof(bool3x4), typeof(bool4),
                    typeof(bool4x2), typeof(bool4x3), typeof(bool4x4), typeof(double2), typeof(double2x2), typeof(double2x3),
                    typeof(double2x4), typeof(double3), typeof(double3x2), typeof(double3x3), typeof(double3x4), typeof(double4),
                    typeof(double4x2), typeof(double4x3), typeof(double4x4), typeof(float2), typeof(float2x2), typeof(float2x3),
                    typeof(float2x4), typeof(float3), typeof(float3x2), typeof(float3x3), typeof(float3x4), typeof(float4),
                    typeof(float4x2), typeof(float4x3), typeof(float4x4), typeof(half), typeof(half2), typeof(half3), typeof(half4),
                    typeof(int2), typeof(int2x2), typeof(int2x3), typeof(int2x4), typeof(int3), typeof(int3x2), typeof(int3x3),
                    typeof(int3x4), typeof(int4), typeof(int4x2), typeof(int4x3), typeof(int4x4), typeof(quaternion), typeof(uint2),
                    typeof(uint2x2), typeof(uint2x3), typeof(uint2x4), typeof(uint3), typeof(uint3x2), typeof(uint3x3), typeof(uint3x4),
                    typeof(uint4), typeof(uint4x2), typeof(uint4x3), typeof(uint4x4) };

                var random = new Random();
                var positionStart = 0;
                m_IsWriting = serializer.IsWriter;
                if (m_IsWriting)
                {
                    m_Writer = serializer.GetFastBufferWriter();
                    positionStart = m_Writer.Position;
                }
                else
                {
                    m_Reader = serializer.GetFastBufferReader();
                    positionStart = m_Reader.Position;
                }
                m_LastPosition = positionStart;
                foreach (var testType in typesToSerialize)
                {
                    SetLastPosition();
                    if (testType == typeof(bool2))
                    {
                        var valueType = math.bool2(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool2x2))
                    {
                        var valueType = math.bool2x2(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool2x3))
                    {
                        var valueType = math.bool2x3(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool2x4))
                    {
                        var valueType = math.bool2x4(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool3))
                    {
                        var valueType = math.bool3(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool3x2))
                    {
                        var valueType = math.bool3x2(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool3x3))
                    {
                        var valueType = math.bool3x3(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool3x4))
                    {
                        var valueType = math.bool3x4(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool4))
                    {
                        var valueType = math.bool4(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool4x2))
                    {
                        var valueType = math.bool4x2(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool4x3))
                    {
                        var valueType = math.bool4x3(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(bool4x4))
                    {
                        var valueType = math.bool4x4(Time.realtimeSinceStartup % 2 == 1);
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double2))
                    {
                        var valueType = math.double2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double2x2))
                    {
                        var valueType = math.double2x2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double2x3))
                    {
                        var valueType = math.double2x3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double2x4))
                    {
                        var valueType = math.double2x4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double3))
                    {
                        var valueType = math.double3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double3x2))
                    {
                        var valueType = math.double3x2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double3x3))
                    {
                        var valueType = math.double3x3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double3x4))
                    {
                        var valueType = math.double3x4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double4))
                    {
                        var valueType = math.double4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double4x2))
                    {
                        var valueType = math.double4x2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double4x3))
                    {
                        var valueType = math.double4x3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(double4x4))
                    {
                        var valueType = math.double4x4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float2))
                    {
                        var valueType = math.float2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float2x2))
                    {
                        var valueType = math.float2x2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float2x3))
                    {
                        var valueType = math.float2x3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float2x4))
                    {
                        var valueType = math.float2x4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float3))
                    {
                        var valueType = math.float3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float3x2))
                    {
                        var valueType = math.float3x2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float3x3))
                    {
                        var valueType = math.float3x3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float3x4))
                    {
                        var valueType = math.float3x4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float4))
                    {
                        var valueType = math.float4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float4x2))
                    {
                        var valueType = math.float4x2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float4x3))
                    {
                        var valueType = math.float4x3((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(float4x4))
                    {
                        var valueType = math.float4x4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(half))
                    {
                        var valueType = math.half((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(half2))
                    {
                        var valueType = math.half2((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(half3))
                    {
                        var valueType = math.half4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(half4))
                    {
                        var valueType = math.half4((float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int2))
                    {
                        var valueType = math.int2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int2x2))
                    {
                        var valueType = math.int2x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int2x3))
                    {
                        var valueType = math.int2x3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int2x4))
                    {
                        var valueType = math.int2x4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int3))
                    {
                        var valueType = math.int3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int3x2))
                    {
                        var valueType = math.int3x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int3x3))
                    {
                        var valueType = math.int3x3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int3x4))
                    {
                        var valueType = math.int3x4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int4))
                    {
                        var valueType = math.int4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int4x2))
                    {
                        var valueType = math.int4x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int4x3))
                    {
                        var valueType = math.int4x3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(int4x4))
                    {
                        var valueType = math.uint2x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(quaternion))
                    {
                        var valueType = math.quaternion((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint2))
                    {
                        var valueType = math.uint2x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint2x2))
                    {
                        var valueType = math.uint2x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint2x3))
                    {
                        var valueType = math.uint2x3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint2x4))
                    {
                        var valueType = math.uint2x4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint3))
                    {
                        var valueType = math.uint3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint3x2))
                    {
                        var valueType = math.uint3x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint3x3))
                    {
                        var valueType = math.uint3x3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint3x4))
                    {
                        var valueType = math.uint3x4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint4))
                    {
                        var valueType = math.uint4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint4x2))
                    {
                        var valueType = math.uint4x2(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint4x3))
                    {
                        var valueType = math.uint4x3(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }
                    else if (testType == typeof(uint4x4))
                    {
                        var valueType = math.uint4x4(random.Next());
                        serializer.SerializeValue(ref valueType);
                    }

                    VerifyTypeSize(testType);
                }
                if (!m_IsWriting)
                {
                    SerializedReadSize = m_Reader.Position - positionStart;
                    serializer.SerializeValue(ref SerializedWriteSize);

                    foreach (var testType in typesToSerialize)
                    {
                        var typeSize = UnsafeUtility.SizeOf(testType);
                        CalculatedSize += typeSize;
                    }
                }
                else
                {
                    SerializedWriteSize = m_Writer.Position - positionStart;
                    serializer.SerializeValue(ref SerializedWriteSize);
                }
            }
        }

        /// <summary>
        /// Validates that all Unity.Mathematics types can be serialized
        /// with a very basic check at the end that both writing and reading
        /// sizes match. (values are not compared as that is done in FastBufferReader
        /// and FastBufferWriter tests. This just verifies basic serialization.)
        /// </summary>
        [Test]
        public void TestSerilizationOfMathematicsTypes()
        {
            var mathematicsStructWrite = new MathematicsTypeTestStruct();
            var mathematicsStructRead = new MathematicsTypeTestStruct();
            var writer = new FastBufferWriter(8192, Allocator.Temp);
            using (writer)
            {
                var serializer = new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(writer));
                serializer.SerializeNetworkSerializable(ref mathematicsStructWrite);
            }
            var reader = new FastBufferReader(writer, Allocator.Temp);
            using (reader)
            {
                var deserializer = new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(reader));
                deserializer.SerializeNetworkSerializable(ref mathematicsStructRead);
            }
            Assert.AreEqual(mathematicsStructRead.SerializedReadSize, mathematicsStructRead.SerializedWriteSize);

            Debug.Log($"Calculated Size {mathematicsStructRead.CalculatedSize} | Serialized Size {mathematicsStructRead.SerializedReadSize}");
            //Assert.AreEqual(mathematicsStructRead.CalculatedSize, mathematicsStructRead.SerializedReadSize);
        }
    }
}
