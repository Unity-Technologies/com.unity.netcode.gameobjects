using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Multiplayer.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public class FastBufferWriterTests
    {
        #region Test Types
        private enum ByteEnum : byte
        {
            A,
            B,
            C
        };
        private enum SByteEnum : sbyte
        {
            A,
            B,
            C
        };
        private enum ShortEnum : short
        {
            A,
            B,
            C
        };
        private enum UShortEnum : ushort
        {
            A,
            B,
            C
        };
        private enum IntEnum : int
        {
            A,
            B,
            C
        };
        private enum UIntEnum : uint
        {
            A,
            B,
            C
        };
        private enum LongEnum : long
        {
            A,
            B,
            C
        };
        private enum ULongEnum : ulong
        {
            A,
            B,
            C
        };
        
        private struct TestStruct
        {
            public byte a;
            public short b;
            public ushort c;
            public int d;
            public uint e;
            public long f;
            public ulong g;
            public bool h;
            public char i;
            public float j;
            public double k;
        }
        
        public enum WriteType
        {
            WriteDirect,
            WriteSafe,
            WriteAsObject
        }
        #endregion

        #region Common Checks
        private void WriteCheckBytes(ref FastBufferWriter writer, int writeSize, string failMessage="")
        {
            Assert.IsTrue(writer.VerifyCanWrite(2), "Writer denied write permission");
            writer.WriteValue((byte)0x80);
            Assert.AreEqual(writeSize+1, writer.Position, failMessage);
            Assert.AreEqual(writeSize+1, writer.Length, failMessage);
            writer.WriteValue((byte)0xFF);
            Assert.AreEqual(writeSize+2, writer.Position, failMessage);
            Assert.AreEqual(writeSize+2, writer.Length, failMessage);
        }

        private void VerifyCheckBytes(byte[] underlyingArray, int writeSize, string failMessage = "")
        {
            Assert.AreEqual(0x80, underlyingArray[writeSize], failMessage);
            Assert.AreEqual(0xFF, underlyingArray[writeSize+1], failMessage);
        }

        private unsafe void VerifyBytewiseEquality<T>(T value, byte[] underlyingArray, int valueOffset, int bufferOffset, int size, string failMessage = "") where T: unmanaged
        {
            byte* asBytePointer = (byte*) &value;
            for (var i = 0; i < size; ++i)
            {
                Assert.AreEqual(asBytePointer[i+valueOffset], underlyingArray[i+bufferOffset], failMessage);
            }
        }

        private unsafe void VerifyTypedEquality<T>(T value, byte* unsafePtr) where T: unmanaged
        {
            T* checkValue = (T*) unsafePtr;
            Assert.AreEqual(value, *checkValue);
        }

        private void VerifyPositionAndLength(ref FastBufferWriter writer, int position, string failMessage = "")
        {
            Assert.AreEqual(position, writer.Position, failMessage);
            Assert.AreEqual(position, writer.Length, failMessage);
        }

        private unsafe void CommonChecks<T>(ref FastBufferWriter writer, T valueToTest, int writeSize, string failMessage = "") where T: unmanaged
        {
            
            VerifyPositionAndLength(ref writer, writeSize, failMessage);
            
            WriteCheckBytes(ref writer, writeSize, failMessage);
            
            var underlyingArray = writer.ToArray();
            
            VerifyBytewiseEquality(valueToTest, underlyingArray, 0, 0, writeSize, failMessage);

            VerifyCheckBytes(underlyingArray, writeSize, failMessage);

            VerifyTypedEquality(valueToTest, writer.GetUnsafePtr());
        }
        #endregion
        
        #region Generic Checks
        private unsafe void RunTypeTest<T>(T valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            var alternateWriteSize = FastBufferWriter.GetWriteSize<T>();
            Assert.AreEqual(sizeof(T), writeSize);
            Assert.AreEqual(sizeof(T), alternateWriteSize);
            
            FastBufferWriter writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);

            using(writer)
            {

                Assert.IsTrue(writer.VerifyCanWrite(writeSize + 2), "Writer denied write permission");

                var failMessage = $"RunTypeTest failed with type {typeof(T)} and value {valueToTest}";

                writer.WriteValue(valueToTest);

                CommonChecks(ref writer, valueToTest, writeSize, failMessage);
            }
        }
        private unsafe void RunTypeTestSafe<T>(T valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            FastBufferWriter writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);

            using(writer)
            {
                Assert.AreEqual(sizeof(T), writeSize);

                var failMessage = $"RunTypeTest failed with type {typeof(T)} and value {valueToTest}";

                writer.WriteValueSafe(valueToTest);

                CommonChecks(ref writer, valueToTest, writeSize, failMessage);
            }
        }
        
        private unsafe void RunObjectTypeTest<T>(T valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            FastBufferWriter writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);

            using(writer)
            {
                Assert.AreEqual(sizeof(T), writeSize);

                var failMessage = $"RunObjectTypeTest failed with type {typeof(T)} and value {valueToTest}";
                writer.WriteObject(valueToTest);

                CommonChecks(ref writer, valueToTest, writeSize, failMessage);
            }
        }

        private unsafe void VerifyArrayEquality<T>(T[] value, byte* unsafePtr, int offset) where T: unmanaged
        {
            int* sizeValue = (int*)(unsafePtr + offset);
            Assert.AreEqual(value.Length, *sizeValue);

            fixed (T* asTPointer = value)
            {
                T* underlyingTArray = (T*) (unsafePtr + sizeof(int) + offset);
                for (var i = 0; i < value.Length; ++i)
                {
                    Assert.AreEqual(asTPointer[i], underlyingTArray[i]);
                }
            }
        }

        private unsafe void RunTypeArrayTest<T>(T[] valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            FastBufferWriter writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);
            using (writer)
            {

                Assert.AreEqual(sizeof(int) + sizeof(T) * valueToTest.Length, writeSize);
                Assert.IsTrue(writer.VerifyCanWrite(writeSize + 2), "Writer denied write permission");

                writer.WriteValue(valueToTest);
                VerifyPositionAndLength(ref writer, writeSize);

                WriteCheckBytes(ref writer, writeSize);

                VerifyArrayEquality(valueToTest, writer.GetUnsafePtr(), 0);

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, writeSize);
            }
        }

        private unsafe void RunTypeArrayTestSafe<T>(T[] valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            FastBufferWriter writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);
            using (writer)
            {

                Assert.AreEqual(sizeof(int) + sizeof(T) * valueToTest.Length, writeSize);

                writer.WriteValueSafe(valueToTest);
                VerifyPositionAndLength(ref writer, writeSize);

                WriteCheckBytes(ref writer, writeSize);

                VerifyArrayEquality(valueToTest, writer.GetUnsafePtr(), 0);

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, writeSize);
            }
        }

        private unsafe void RunObjectTypeArrayTest<T>(T[] valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            // Extra byte for WriteObject adding isNull flag
            FastBufferWriter writer = new FastBufferWriter(writeSize + 3, Allocator.Temp);
            using(writer)
            {
                
                Assert.AreEqual(sizeof(int) + sizeof(T) * valueToTest.Length, writeSize);

                writer.WriteObject(valueToTest);
                Assert.AreEqual(0, writer.ToArray()[0]);
                VerifyPositionAndLength(ref writer, writeSize + sizeof(byte));

                WriteCheckBytes(ref writer, writeSize + sizeof(byte));

                VerifyArrayEquality(valueToTest, writer.GetUnsafePtr(), sizeof(byte));

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, writeSize + sizeof(byte));
            }
        }
        #endregion

        #region Helpers
        private TestStruct GetTestStruct()
        {
            var random = new Random();

            var testStruct = new TestStruct
            {
                a = (byte) random.Next(),
                b = (short) random.Next(),
                c = (ushort) random.Next(),
                d = (int) random.Next(),
                e = (uint) random.Next(),
                f = ((long) random.Next() << 32) + random.Next(),
                g = ((ulong) random.Next() << 32) + (ulong) random.Next(),
                h = true,
                i = '\u263a',
                j = (float) random.NextDouble(),
                k = random.NextDouble(),
            };
            
            return testStruct;
        }
        #endregion

        #region Tests
        [Test]
        public void TestWritingBasicTypes(
            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3), typeof(Vector4),
                typeof(Quaternion), typeof(Color), typeof(Color32), typeof(Ray), typeof(Ray2D))]
            Type testType,
            [Values] WriteType writeType)
        {
            var random = new Random();

            if (testType == typeof(byte))
            {
                byte b = (byte) random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(b);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(b);
                }
                else
                {
                    RunObjectTypeTest(b);
                }
            }
            else if (testType == typeof(sbyte))
            {
                sbyte sb = (sbyte) random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(sb);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(sb);
                }
                else
                {
                    RunObjectTypeTest(sb);
                }
            }
            else if (testType == typeof(short))
            {
                short s = (short)random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(s);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(s);
                }
                else
                {
                    RunObjectTypeTest(s);
                }
            }
            else if (testType == typeof(ushort))
            {
                ushort us = (ushort)random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(us);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(us);
                }
                else
                {
                    RunObjectTypeTest(us);
                }
            }
            else if (testType == typeof(int))
            {
                int i = (int)random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(i);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(i);
                }
                else
                {
                    RunObjectTypeTest(i);
                }
            }
            else if (testType == typeof(uint))
            {
                uint ui = (uint)random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(ui);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(ui);
                }
                else
                {
                    RunObjectTypeTest(ui);
                }
            }
            else if (testType == typeof(long))
            {
                long l = ((long)random.Next() << 32) + random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(l);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(l);
                }
                else
                {
                    RunObjectTypeTest(l);
                }
            }
            else if (testType == typeof(ulong))
            {
                ulong ul = ((ulong)random.Next() << 32) + (ulong)random.Next();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(ul);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(ul);
                }
                else
                {
                    RunObjectTypeTest(ul);
                }
            }
            else if (testType == typeof(bool))
            {
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(true);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(true);
                }
                else
                {
                    RunObjectTypeTest(true);
                }
            }
            else if (testType == typeof(char))
            {
                char c = 'a';
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(c);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(c);
                }
                else
                {
                    RunObjectTypeTest(c);
                }

                c = '\u263a';
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(c);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(c);
                }
                else
                {
                    RunObjectTypeTest(c);
                }
            }
            else if (testType == typeof(float))
            {
                float f = (float)random.NextDouble();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(f);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(f);
                }
                else
                {
                    RunObjectTypeTest(f);
                }
            }
            else if (testType == typeof(double))
            {
                double d = random.NextDouble();
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(d);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(d);
                }
                else
                {
                    RunObjectTypeTest(d);
                }
            }
            else if (testType == typeof(ByteEnum))
            {
                ByteEnum e = ByteEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(SByteEnum))
            {
                SByteEnum e = SByteEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(ShortEnum))
            {
                ShortEnum e = ShortEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(UShortEnum))
            {
                UShortEnum e = UShortEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(IntEnum))
            {
                IntEnum e = IntEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(UIntEnum))
            {
                UIntEnum e = UIntEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(LongEnum))
            {
                LongEnum e = LongEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(ULongEnum))
            {
                ULongEnum e = ULongEnum.C;
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(e);
                }
                else
                {
                    RunObjectTypeTest(e);
                }
            }
            else if (testType == typeof(Vector2))
            {
                var v = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Vector3))
            {
                var v = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Vector4))
            {
                var v = new Vector4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Quaternion))
            {
                var v = new Quaternion((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Color))
            {
                var v = new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Color32))
            {
                var v = new Color32((byte)random.Next(), (byte)random.Next(), (byte)random.Next(), (byte)random.Next());
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Ray))
            {
                var v = new Ray(
                    new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), 
                    new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()));
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else if (testType == typeof(Ray2D))
            {
                var v = new Ray2D(
                    new Vector2((float)random.NextDouble(), (float)random.NextDouble()), 
                    new Vector2((float)random.NextDouble(), (float)random.NextDouble()));
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeTestSafe(v);
                }
                else
                {
                    RunObjectTypeTest(v);
                }
            }
            else
            {
                Assert.Fail("No type handler was provided for this type in the test!");
            }
        }

        [Test]
        public void TestWritingBasicArrays(
            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3), typeof(Vector4),
                typeof(Quaternion), typeof(Color), typeof(Color32), typeof(Ray), typeof(Ray2D))]
            Type testType,
            [Values] WriteType writeType)
        {
            var random = new Random();

            if (testType == typeof(byte))
            {
                byte[] b = {
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(b);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(b);
                }
                else
                {
                    RunObjectTypeArrayTest(b);
                }
            }
            else if (testType == typeof(sbyte))
            {
                sbyte[] sb = {
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(sb);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(sb);
                }
                else
                {
                    RunObjectTypeArrayTest(sb);
                }
            }
            else if (testType == typeof(short))
            {
                short[] s = {
                    (short) random.Next(),
                    (short) random.Next(),
                    (short) random.Next(),
                    (short) random.Next(),
                    (short) random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(s);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(s);
                }
                else
                {
                    RunObjectTypeArrayTest(s);
                }
            }
            else if (testType == typeof(ushort))
            {
                ushort[] us = {
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(us);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(us);
                }
                else
                {
                    RunObjectTypeArrayTest(us);
                }
            }
            else if (testType == typeof(int))
            {
                int[] i = {
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(i);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(i);
                }
                else
                {
                    RunObjectTypeArrayTest(i);
                }
            }
            else if (testType == typeof(uint))
            {
                uint[] ui = {
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(ui);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(ui);
                }
                else
                {
                    RunObjectTypeArrayTest(ui);
                }
            }
            else if (testType == typeof(long))
            {
                long[] l = {
                    ((long)random.Next() << 32) + (long)random.Next(),
                    ((long)random.Next() << 32) + (long)random.Next(),
                    ((long)random.Next() << 32) + (long)random.Next(),
                    ((long)random.Next() << 32) + (long)random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(l);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(l);
                }
                else
                {
                    RunObjectTypeArrayTest(l);
                }
            }
            else if (testType == typeof(ulong))
            {
                ulong[] ul = {
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(ul);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(ul);
                }
                else
                {
                    RunObjectTypeArrayTest(ul);
                }
            }
            else if (testType == typeof(bool))
            {
                bool[] b = {
                    true,
                    false,
                    true,
                    true,
                    false,
                    false,
                    true,
                    false,
                    true
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(b);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(b);
                }
                else
                {
                    RunObjectTypeArrayTest(b);
                }
            }
            else if (testType == typeof(char))
            {
                char[] c = {
                    'a',
                    '\u263a',
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(c);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(c);
                }
                else
                {
                    RunObjectTypeArrayTest(c);
                }
            }
            else if (testType == typeof(float))
            {
                float[] f = {
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(f);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(f);
                }
                else
                {
                    RunObjectTypeArrayTest(f);
                }
            }
            else if (testType == typeof(double))
            {
                double[] d = {
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble()
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(d);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(d);
                }
                else
                {
                    RunObjectTypeArrayTest(d);
                }
            }
            else if (testType == typeof(ByteEnum))
            {
                ByteEnum[] e = {
                    ByteEnum.C,
                    ByteEnum.A,
                    ByteEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(SByteEnum))
            {
                SByteEnum[] e = {
                    SByteEnum.C,
                    SByteEnum.A,
                    SByteEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(ShortEnum))
            {
                ShortEnum[] e = {
                    ShortEnum.C,
                    ShortEnum.A,
                    ShortEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(UShortEnum))
            {
                UShortEnum[] e = {
                    UShortEnum.C,
                    UShortEnum.A,
                    UShortEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(IntEnum))
            {
                IntEnum[] e = {
                    IntEnum.C,
                    IntEnum.A,
                    IntEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(UIntEnum))
            {
                UIntEnum[] e = {
                    UIntEnum.C,
                    UIntEnum.A,
                    UIntEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(LongEnum))
            {
                LongEnum[] e = {
                    LongEnum.C,
                    LongEnum.A,
                    LongEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(ULongEnum))
            {
                ULongEnum[] e = {
                    ULongEnum.C,
                    ULongEnum.A,
                    ULongEnum.B
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(e);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(e);
                }
                else
                {
                    RunObjectTypeArrayTest(e);
                }
            }
            else if (testType == typeof(Vector2))
            {
                var v = new[]
                {
                    new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Vector3))
            {
                var v = new[]
                {
                    new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble()),
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Vector4))
            {
                var v = new[]
                {
                    new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                };
                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Quaternion))
            {
                var v = new[]
                {
                    new Quaternion((float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble(), (float) random.NextDouble()),
                    new Quaternion((float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble(), (float) random.NextDouble()),
                    new Quaternion((float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble(), (float) random.NextDouble()),
                };

                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Color))
            {
                var v = new[]
                {
                    new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                };

                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Color32))
            {
                var v = new[]
                {
                    new Color32((byte) random.Next(), (byte) random.Next(), (byte) random.Next(), (byte) random.Next()),
                    new Color32((byte) random.Next(), (byte) random.Next(), (byte) random.Next(), (byte) random.Next()),
                    new Color32((byte) random.Next(), (byte) random.Next(), (byte) random.Next(), (byte) random.Next()),
                };

                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Ray))
            {
                var v = new[]
                {
                    new Ray(
                        new Vector3((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble()),
                        new Vector3((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble())),
                    new Ray(
                        new Vector3((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble()),
                        new Vector3((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble())),
                    new Ray(
                        new Vector3((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble()),
                        new Vector3((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble())),
                };

                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else if (testType == typeof(Ray2D))
            {
                var v = new[]
                {
                    new Ray2D(
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble())),
                    new Ray2D(
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble())),
                    new Ray2D(
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble())),
                };

                if (writeType == WriteType.WriteDirect)
                {
                    RunTypeArrayTest(v);
                }
                else if (writeType == WriteType.WriteSafe)
                {
                    RunTypeArrayTestSafe(v);
                }
                else
                {
                    RunObjectTypeArrayTest(v);
                }
            }
            else
            {
                Assert.Fail("No type handler was provided for this type in the test!");
            }
        }

        [Test]
        public void TestWritingStruct()
        {
            RunTypeTest(GetTestStruct());
        }

        [Test]
        public void TestWritingStructSafe()
        {
            RunTypeTestSafe(GetTestStruct());
        }

        [Test]
        public void TestWritingStructAsObjectWithRegisteredTypeTableSerializer()
        {
            SerializationTypeTable.Serializers[typeof(TestStruct)] = (ref FastBufferWriter writer, object obj) =>
            {
                writer.WriteValueSafe((TestStruct) obj);
            };
            try
            {
                RunObjectTypeTest(GetTestStruct());
            }
            finally
            {
                SerializationTypeTable.Serializers.Remove(typeof(TestStruct));
            }
        }

        [Test]
        public void TestWritingStructArray()
        {
            TestStruct[] arr = {
                GetTestStruct(),
                GetTestStruct(),
                GetTestStruct(),
            };
            RunTypeArrayTest(arr);
        }

        [Test]
        public void TestWritingStructArraySafe()
        {
            TestStruct[] arr = {
                GetTestStruct(),
                GetTestStruct(),
                GetTestStruct(),
            };
            RunTypeArrayTestSafe(arr);
        }

        [Test]
        public void TestWritingStructArrayAsObjectWithRegisteredTypeTableSerializer()
        {
            SerializationTypeTable.Serializers[typeof(TestStruct)] = (ref FastBufferWriter writer, object obj) =>
            {
                writer.WriteValueSafe((TestStruct) obj);
            };
            try
            {
                TestStruct[] arr = {
                    GetTestStruct(),
                    GetTestStruct(),
                    GetTestStruct(),
                };
                RunObjectTypeArrayTest(arr);
            }
            finally
            {
                SerializationTypeTable.Serializers.Remove(typeof(TestStruct));
            }
        }

        [Test]
        public unsafe void TestWritingString()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest);

            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {

                Assert.IsTrue(writer.VerifyCanWrite(serializedValueSize + 2), "Writer denied write permission");
                writer.WriteValue(valueToTest);

                VerifyPositionAndLength(ref writer, serializedValueSize);
                WriteCheckBytes(ref writer, serializedValueSize);

                int* sizeValue = (int*) writer.GetUnsafePtr();
                Assert.AreEqual(valueToTest.Length, *sizeValue);

                fixed (char* asCharPointer = valueToTest)
                {
                    char* underlyingCharArray = (char*) (writer.GetUnsafePtr() + sizeof(int));
                    for (var i = 0; i < valueToTest.Length; ++i)
                    {
                        Assert.AreEqual(asCharPointer[i], underlyingCharArray[i]);
                    }
                }

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, serializedValueSize);
            }
        }

        [Test]
        public unsafe void TestWritingStringSafe()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest);

            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {

                writer.WriteValueSafe(valueToTest);

                VerifyPositionAndLength(ref writer, serializedValueSize);
                WriteCheckBytes(ref writer, serializedValueSize);

                int* sizeValue = (int*) writer.GetUnsafePtr();
                Assert.AreEqual(valueToTest.Length, *sizeValue);

                fixed (char* asCharPointer = valueToTest)
                {
                    char* underlyingCharArray = (char*) ((byte*) writer.GetUnsafePtr() + sizeof(int));
                    for (var i = 0; i < valueToTest.Length; ++i)
                    {
                        Assert.AreEqual(asCharPointer[i], underlyingCharArray[i]);
                    }
                }

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, serializedValueSize);
            }
        }

        [Test]
        public unsafe void TestWritingStringAsObject()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest);

            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 3, Allocator.Temp);
            using(writer)
            {

                writer.WriteObject(valueToTest);

                VerifyPositionAndLength(ref writer, serializedValueSize + sizeof(byte));
                WriteCheckBytes(ref writer, serializedValueSize + sizeof(byte));

                int* sizeValue = (int*) (writer.GetUnsafePtr() + sizeof(byte));
                Assert.AreEqual(valueToTest.Length, *sizeValue);

                fixed (char* asCharPointer = valueToTest)
                {
                    char* underlyingCharArray = (char*) (writer.GetUnsafePtr() + sizeof(int) + sizeof(byte));
                    for (var i = 0; i < valueToTest.Length; ++i)
                    {
                        Assert.AreEqual(asCharPointer[i], underlyingCharArray[i]);
                    }
                }

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, serializedValueSize + sizeof(byte));
            }
        }

        [Test]
        public unsafe void TestWritingOneByteString()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest, true);
            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {

                Assert.IsTrue(writer.VerifyCanWrite(serializedValueSize + 2), "Writer denied write permission");
                writer.WriteValue(valueToTest, true);

                VerifyPositionAndLength(ref writer, serializedValueSize);
                WriteCheckBytes(ref writer, serializedValueSize);

                int* sizeValue = (int*) writer.GetUnsafePtr();
                Assert.AreEqual(valueToTest.Length, *sizeValue);

                fixed (char* asCharPointer = valueToTest)
                {
                    byte* underlyingByteArray = writer.GetUnsafePtr() + sizeof(int);
                    for (var i = 0; i < valueToTest.Length; ++i)
                    {
                        Assert.AreEqual((byte) asCharPointer[i], underlyingByteArray[i]);
                    }
                }

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, serializedValueSize);
            }
        }

        [Test]
        public unsafe void TestWritingOneByteStringSafe()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest, true);
            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {

                writer.WriteValueSafe(valueToTest, true);

                VerifyPositionAndLength(ref writer, serializedValueSize);
                WriteCheckBytes(ref writer, serializedValueSize);

                int* sizeValue = (int*) writer.GetUnsafePtr();
                Assert.AreEqual(valueToTest.Length, *sizeValue);

                fixed (char* asCharPointer = valueToTest)
                {
                    byte* underlyingByteArray = writer.GetUnsafePtr() + sizeof(int);
                    for (var i = 0; i < valueToTest.Length; ++i)
                    {
                        Assert.AreEqual((byte) asCharPointer[i], underlyingByteArray[i]);
                    }
                }

                var underlyingArray = writer.ToArray();
                VerifyCheckBytes(underlyingArray, serializedValueSize);
            }
        }

        [Test]
        public unsafe void TestWritingPartialValues([NUnit.Framework.Range(1, sizeof(ulong))] int count)
        {
            var random = new Random();
            var valueToTest = ((ulong) random.Next() << 32) + (ulong)random.Next();
            FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) + 2, Allocator.Temp);
            using (writer)
            {

                Assert.IsTrue(writer.VerifyCanWrite(count + 2), "Writer denied write permission");
                writer.WritePartialValue(valueToTest, count);

                var failMessage = $"TestWritingPartialValues failed with value {valueToTest}";
                VerifyPositionAndLength(ref writer, count, failMessage);
                WriteCheckBytes(ref writer, count, failMessage);
                var underlyingArray = writer.ToArray();
                VerifyBytewiseEquality(valueToTest, underlyingArray, 0, 0, count, failMessage);
                VerifyCheckBytes(underlyingArray, count, failMessage);

                ulong mask = 0;
                for (var i = 0; i < count; ++i)
                {
                    mask = (mask << 8) | 0b11111111;
                }

                ulong* checkValue = (ulong*) writer.GetUnsafePtr();
                Assert.AreEqual(valueToTest & mask, *checkValue & mask, failMessage);
            }
        }

        [Test]
        public unsafe void TestWritingPartialValuesWithOffsets([NUnit.Framework.Range(1, sizeof(ulong)-2)] int count)
        {
            var random = new Random();
            var valueToTest = ((ulong) random.Next() << 32) + (ulong)random.Next();
            FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) + 2, Allocator.Temp);
            
            using (writer)
            {

                Assert.IsTrue(writer.VerifyCanWrite(count + 2), "Writer denied write permission");
                writer.WritePartialValue(valueToTest, count, 2);
                var failMessage = $"TestWritingPartialValuesWithOffsets failed with value {valueToTest}";

                VerifyPositionAndLength(ref writer, count, failMessage);
                WriteCheckBytes(ref writer, count, failMessage);
                var underlyingArray = writer.ToArray();
                VerifyBytewiseEquality(valueToTest, underlyingArray, 2, 0, count, failMessage);
                VerifyCheckBytes(underlyingArray, count, failMessage);

                ulong mask = 0;
                for (var i = 0; i < count; ++i)
                {
                    mask = (mask << 8) | 0b11111111;
                }

                ulong* checkValue = (ulong*) writer.GetUnsafePtr();
                Assert.AreEqual((valueToTest >> 16) & mask, *checkValue & mask);
            }
        }

        [Test]
        public void TestToArray()
        {
            var testStruct = GetTestStruct();
            var requiredSize = FastBufferWriter.GetWriteSize(testStruct);
            var writer = new FastBufferWriter(requiredSize, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(requiredSize);
                writer.WriteValue(testStruct);
                var array = writer.ToArray();
                var underlyingArray = writer.ToArray();
                for(var i = 0; i < array.Length; ++i)
                {
                    Assert.AreEqual(array[i], underlyingArray[i]);
                }
            }
        }

        [Test]
        public void TestThrowingIfBoundsCheckingSkipped()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);

            using (writer)
            {
                Assert.Throws<OverflowException>(() => { writer.WriteByte(1); });
                var bytes = new byte[] {0, 1, 2};
                Assert.Throws<OverflowException>(() => { writer.WriteBytes(bytes, bytes.Length); });
                int i = 1;
                Assert.Throws<OverflowException>(() => { writer.WriteValue(i); });
                Assert.Throws<OverflowException>(() => { writer.WriteValue(bytes); });
                Assert.Throws<OverflowException>(() => { writer.WriteValue(""); });

                writer.VerifyCanWrite(sizeof(int) - 1);
                Assert.Throws<OverflowException>(() => { writer.WriteValue(i); });
                writer.WriteByte(1);
                writer.WriteByte(2);
                writer.WriteByte(3);
                Assert.Throws<OverflowException>(() => { writer.WriteByte(4); });
            }
        }

        [Test]
        public void TestThrowingIfDoingBytewiseWritesDuringBitwiseContext()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(100);
                var bytes = new byte[] {0, 1, 2};
                int i = 1;
                using (var context = writer.EnterBitwiseContext())
                {
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteByte(1); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteBytes(bytes, bytes.Length); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteValue(i); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteValue(bytes); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteValue(""); });
                    
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteByteSafe(1); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteBytesSafe(bytes, bytes.Length); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteValueSafe(i); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteValueSafe(bytes); });
                    Assert.Throws<InvalidOperationException>(() => { writer.WriteValueSafe(""); });
                }
                writer.WriteByte(1);
                writer.WriteBytes(bytes, bytes.Length);
                writer.WriteValue(i);
                writer.WriteValue(bytes);
                writer.WriteValue("");
            
                writer.WriteByteSafe(1);
                writer.WriteBytesSafe(bytes, bytes.Length);
                writer.WriteValueSafe(i);
                writer.WriteValueSafe(bytes);
                writer.WriteValueSafe("");
            }
        }

        [Test]
        public void TestVerifyCanWriteIsRelativeToPositionAndNotAllowedWritePosition()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                writer.VerifyCanWrite(100);
                writer.WriteByte(1);
                writer.VerifyCanWrite(1);
                writer.WriteByte(1);
                Assert.Throws<OverflowException>(() => { writer.WriteByte(1); });
            }
        }

        [Test]
        public void TestSeeking()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                writer.Seek(5);
                writer.WriteByteSafe(0);
                Assert.AreEqual(writer.Position, 6);
                Assert.AreEqual(writer.Length, 6);

                writer.Seek(0);
                writer.WriteByteSafe(1);
                Assert.AreEqual(writer.Position, 1);
                Assert.AreEqual(writer.Length, 6);

                writer.Seek(10);
                Assert.AreEqual(writer.Position, 10);
                Assert.AreEqual(writer.Length, 10);

                writer.Seek(2);
                writer.WriteByteSafe(2);

                writer.Seek(1);
                writer.WriteByteSafe(3);

                writer.Seek(4);
                writer.WriteByteSafe(4);

                writer.Seek(3);
                writer.WriteByteSafe(5);

                Assert.AreEqual(writer.Position, 4);
                Assert.AreEqual(writer.Length, 10);

                var expected = new byte[] {1, 3, 2, 5, 4, 0};
                var underlyingArray = writer.ToArray();
                for (var i = 0; i < expected.Length; ++i)
                {
                    Assert.AreEqual(expected[i], underlyingArray[i]);
                }
            }
        }

        [Test]
        public void TestTruncate()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                writer.Seek(10);
                Assert.AreEqual(writer.Position, 10);
                Assert.AreEqual(writer.Length, 10);

                writer.Seek(5);
                Assert.AreEqual(writer.Position, 5);
                Assert.AreEqual(writer.Length, 10);
                
                writer.Truncate(8);
                Assert.AreEqual(writer.Position, 5);
                Assert.AreEqual(writer.Length, 8);

                writer.Truncate();
                Assert.AreEqual(writer.Position, 5);
                Assert.AreEqual(writer.Length, 5);
            }
        }

        [Test]
        public void TestCapacity()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            Assert.AreEqual(100, writer.Capacity);
            writer.Dispose();
        }

        [Test]
        public void TestGrowth()
        {
            var writer = new FastBufferWriter(150, Allocator.Temp);
            var growingWriter = new FastBufferWriter(150, Allocator.Temp, 500);
            Assert.AreEqual(150, writer.Capacity);
            using (writer) 
            using (growingWriter)
            {
                var testStruct = GetTestStruct();
                writer.VerifyCanWriteValue(testStruct);
                writer.WriteValue(testStruct);
                growingWriter.VerifyCanWriteValue(testStruct);
                growingWriter.WriteValue(testStruct);
                
                // Seek to exactly where the write would cross the buffer boundary
                writer.Seek(150 - FastBufferWriter.GetWriteSize(testStruct) + 1);
                growingWriter.Seek(150 - FastBufferWriter.GetWriteSize(testStruct) + 1);
                var preGrowthLength = writer.Position;

                // First writer isn't allowed to grow because it didn't specify a maxSize
                Assert.IsFalse(writer.VerifyCanWriteValue(testStruct));
                Assert.Throws<OverflowException>(() => writer.WriteValue(testStruct));
                
                // Second writer is allowed to grow
                Assert.IsTrue(growingWriter.VerifyCanWriteValue(testStruct));
                
                // First writer shouldn't have grown
                Assert.AreEqual(150, writer.Capacity);
                Assert.AreEqual(preGrowthLength, writer.ToArray().Length);
                // First growth doubles the size
                Assert.AreEqual(300, growingWriter.Capacity);
                Assert.AreEqual(growingWriter.Position, growingWriter.ToArray().Length);
                growingWriter.WriteValue(testStruct);
                
                // Write right up to the very end of the buffer, verify it doesn't grow
                growingWriter.Seek(300 - FastBufferWriter.GetWriteSize(testStruct));
                Assert.IsTrue(growingWriter.VerifyCanWriteValue(testStruct));
                Assert.AreEqual(300, growingWriter.Capacity);
                Assert.AreEqual(growingWriter.Position, growingWriter.ToArray().Length);
                growingWriter.WriteValue(testStruct);
                
                // Go to the end of the buffer and grow again
                growingWriter.Seek(300);
                Assert.IsTrue(growingWriter.VerifyCanWriteValue(testStruct));
                growingWriter.WriteValue(testStruct);
                
                // Second growth caps it at maxSize
                Assert.AreEqual(500, growingWriter.Capacity);
                Assert.AreEqual(growingWriter.Position, growingWriter.ToArray().Length);
                
                VerifyBytewiseEquality(testStruct, writer.ToArray(), 0, 0, FastBufferWriter.GetWriteSize(testStruct));
                // Verify the growth properly copied the existing data
                VerifyBytewiseEquality(testStruct, growingWriter.ToArray(), 0, 0, FastBufferWriter.GetWriteSize(testStruct));
                VerifyBytewiseEquality(testStruct, growingWriter.ToArray(), 0, 150-FastBufferWriter.GetWriteSize(testStruct)+1, FastBufferWriter.GetWriteSize(testStruct));
                VerifyBytewiseEquality(testStruct, growingWriter.ToArray(), 0, 300-FastBufferWriter.GetWriteSize(testStruct), FastBufferWriter.GetWriteSize(testStruct));
                VerifyBytewiseEquality(testStruct, growingWriter.ToArray(), 0, 300, FastBufferWriter.GetWriteSize(testStruct));
            }
        }

        private delegate void GameObjectTestDelegate(GameObject obj, NetworkBehaviour networkBehaviour,
            NetworkObject networkObject);
        private void RunGameObjectTest(GameObjectTestDelegate testCode)
        {
            var obj = new GameObject("Object");
            var networkBehaviour = obj.AddComponent<NetworkObjectTests.EmptyNetworkBehaviour>();
            var networkObject = obj.AddComponent<NetworkObject>();
            // Create networkManager component
            var networkManager = obj.AddComponent<NetworkManager>();
            networkObject.NetworkManagerOwner = networkManager;

            // Set the NetworkConfig
            networkManager.NetworkConfig = new NetworkConfig()
            {
                // Set the current scene to prevent unexpected log messages which would trigger a failure
                RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name },
                // Set transport
                NetworkTransport = obj.AddComponent<DummyTransport>()
            };

            networkManager.StartHost();

            try
            {
                testCode(obj, networkBehaviour, networkObject);
            }
            finally
            {
                GameObject.DestroyImmediate(obj);
                networkManager.StopHost();
            }
        }
        
        [Test]
        public void TestNetworkBehaviour()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkBehaviour), Allocator.Temp);
                using (writer)
                {
                    Assert.IsTrue(writer.VerifyCanWrite(FastBufferWriter.GetWriteSize(networkBehaviour)));

                    writer.WriteValue(networkBehaviour);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(networkBehaviour), writer.Position);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 0, sizeof(ulong));
                    VerifyBytewiseEquality(networkBehaviour.NetworkBehaviourId, writer.ToArray(), 0,
                        sizeof(ulong), sizeof(ushort));
                }
            });
        }

        [Test]
        public void TestNetworkObject()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkObject), Allocator.Temp);
                using (writer)
                {
                    Assert.IsTrue(writer.VerifyCanWrite(FastBufferWriter.GetWriteSize(networkObject)));

                    writer.WriteValue(networkObject);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(networkObject), writer.Position);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 0, sizeof(ulong));
                }
            });
        }

        [Test]
        public void TestGameObject()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(obj), Allocator.Temp);
                using (writer)
                {
                    Assert.IsTrue(writer.VerifyCanWrite(FastBufferWriter.GetWriteSize(obj)));

                    writer.WriteValue(obj);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(obj), writer.Position);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 0, sizeof(ulong));
                }
            });
        }
        
        [Test]
        public void TestNetworkBehaviourSafe()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkBehaviour), Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(networkBehaviour);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(networkBehaviour), writer.Position);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 0, sizeof(ulong));
                    VerifyBytewiseEquality(networkBehaviour.NetworkBehaviourId, writer.ToArray(), 0,
                        sizeof(ulong), sizeof(ushort));
                }
            });
        }

        [Test]
        public void TestNetworkObjectSafe()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkObject), Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(networkObject);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(networkObject), writer.Position);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 0, sizeof(ulong));
                }
            });
        }

        [Test]
        public void TestGameObjectSafe()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(obj), Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(obj);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(obj), writer.Position);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 0, sizeof(ulong));
                }
            });
        }
        
        [Test]
        public void TestNetworkBehaviourAsObject()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                // +1 for extra isNull added by WriteObject
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkBehaviour) + 1, Allocator.Temp);
                using (writer)
                {
                    writer.WriteObject(networkBehaviour);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(networkBehaviour) + 1, writer.Position);
                    Assert.AreEqual(0, writer.ToArray()[0]);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 1, sizeof(ulong));
                    VerifyBytewiseEquality(networkBehaviour.NetworkBehaviourId, writer.ToArray(), 0,
                        sizeof(ulong)+1, sizeof(ushort));
                }
            });
        }

        [Test]
        public void TestNetworkObjectAsObject()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                // +1 for extra isNull added by WriteObject
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(networkObject) + 1, Allocator.Temp);
                using (writer)
                {
                    writer.WriteObject(networkObject);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(networkObject) + 1, writer.Position);
                    Assert.AreEqual(0, writer.ToArray()[0]);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 1, sizeof(ulong));
                }
            });
        }

        [Test]
        public void TestGameObjectAsObject()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
            {
                // +1 for extra isNull added by WriteObject
                var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(obj) + 1, Allocator.Temp);
                using (writer)
                {
                    writer.WriteObject(obj);

                    Assert.AreEqual(FastBufferWriter.GetWriteSize(obj) + 1, writer.Position);
                    Assert.AreEqual(0, writer.ToArray()[0]);
                    VerifyBytewiseEquality(networkObject.NetworkObjectId, writer.ToArray(), 0, 1, sizeof(ulong));
                }
            });
        }

        [Test]
        public void TestVerifyInternalDoesntReduceAllowedWritePoint()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                writer.VerifyCanWrite(25);
                writer.VerifyCanWriteInternal(5);
                Assert.AreEqual(writer.m_AllowedWriteMark, 25);
            }
        }
        #endregion
    }
}