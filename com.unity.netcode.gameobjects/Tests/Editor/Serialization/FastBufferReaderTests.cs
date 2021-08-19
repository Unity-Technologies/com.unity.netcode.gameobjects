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
    public class FastBufferReaderTests
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

        private void VerifyCheckBytes(ref FastBufferReader reader, int checkPosition, string failMessage = "")
        {
            reader.Seek(checkPosition);
            reader.VerifyCanRead(2);
            
            reader.ReadByte(out byte value);
            Assert.AreEqual(0x80, value, failMessage);
            reader.ReadByte(out value);
            Assert.AreEqual(0xFF, value, failMessage);
        }
        
        private void VerifyPositionAndLength(ref FastBufferReader reader, int length, string failMessage = "")
        {
            Assert.AreEqual(0, reader.Position, failMessage);
            Assert.AreEqual(length, reader.Length, failMessage);
        }

        private FastBufferReader CommonChecks<T>(ref FastBufferWriter writer, T valueToTest, int writeSize, string failMessage = "") where T: unmanaged
        {
            WriteCheckBytes(ref writer, writeSize, failMessage);
            
            FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
            
            VerifyPositionAndLength(ref reader, writer.Length, failMessage);

            VerifyCheckBytes(ref reader, writeSize, failMessage);
            
            reader.Seek(0);

            return reader;
        }
        #endregion
        
        #region Generic Checks
        private unsafe void RunTypeTest<T>(T valueToTest) where T : unmanaged
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            Assert.AreEqual(sizeof(T), writeSize);
            FastBufferWriter writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);

            using(writer)
            {
                Assert.IsTrue(writer.VerifyCanWrite(writeSize + 2), "Writer denied write permission");

                var failMessage = $"RunTypeTest failed with type {typeof(T)} and value {valueToTest}";

                writer.WriteValue(valueToTest);

                var reader = CommonChecks(ref writer, valueToTest, writeSize, failMessage);

                using (reader)
                {
                    Assert.IsTrue(reader.VerifyCanRead(FastBufferWriter.GetWriteSize<T>()));
                    reader.ReadValue(out T result);
                    Assert.AreEqual(valueToTest, result);
                }
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


                var reader = CommonChecks(ref writer, valueToTest, writeSize, failMessage);

                using (reader)
                {
                    reader.ReadValueSafe(out T result);
                    Assert.AreEqual(valueToTest, result);
                }
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

                var reader = CommonChecks(ref writer, valueToTest, writeSize, failMessage);

                using (reader)
                {
                    reader.ReadObject(out object result, typeof(T));
                    Assert.AreEqual(valueToTest, result);
                }
            }
        }

        private void VerifyArrayEquality<T>(T[] value, T[] compareValue, int offset) where T: unmanaged
        {
            Assert.AreEqual(value.Length, compareValue.Length);

            for (var i = 0; i < value.Length; ++i)
            {
                Assert.AreEqual(value[i], compareValue[i]);
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

                WriteCheckBytes(ref writer, writeSize);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    Assert.IsTrue(reader.VerifyCanRead(writeSize));
                    reader.ReadValue(out T[] result);
                    VerifyArrayEquality(valueToTest, result, 0);

                    VerifyCheckBytes(ref reader, writeSize);
                }
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

                WriteCheckBytes(ref writer, writeSize);
                
                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    reader.ReadValueSafe(out T[] result);
                    VerifyArrayEquality(valueToTest, result, 0);

                    VerifyCheckBytes(ref reader, writeSize);
                }
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

                WriteCheckBytes(ref writer, writeSize + 1);
                
                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    reader.ReadObject(out object result, typeof(T[]));
                    VerifyArrayEquality(valueToTest, (T[]) result, 0);

                    VerifyCheckBytes(ref reader, writeSize + 1);
                }
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
        public void TestReadingBasicTypes(
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
        public void TestReadingBasicArrays(
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
        public void TestReadingStruct()
        {
            RunTypeTest(GetTestStruct());
        }

        [Test]
        public void TestReadingStructSafe()
        {
            RunTypeTestSafe(GetTestStruct());
        }

        [Test]
        public void TestReadingStructAsObjectWithRegisteredTypeTableSerializer()
        {
            SerializationTypeTable.Serializers[typeof(TestStruct)] = (ref FastBufferWriter writer, object obj) =>
            {
                writer.WriteValueSafe((TestStruct) obj);
            };
            SerializationTypeTable.Deserializers[typeof(TestStruct)] = (ref FastBufferReader reader, out object obj) =>
            {
                reader.ReadValueSafe(out TestStruct value);
                obj = value;
            };
            try
            {
                RunObjectTypeTest(GetTestStruct());
            }
            finally
            {
                SerializationTypeTable.Serializers.Remove(typeof(TestStruct));
                SerializationTypeTable.Deserializers.Remove(typeof(TestStruct));
            }
        }

        [Test]
        public void TestReadingStructArray()
        {
            TestStruct[] arr = {
                GetTestStruct(),
                GetTestStruct(),
                GetTestStruct(),
            };
            RunTypeArrayTest(arr);
        }

        [Test]
        public void TestReadingStructArraySafe()
        {
            TestStruct[] arr = {
                GetTestStruct(),
                GetTestStruct(),
                GetTestStruct(),
            };
            RunTypeArrayTestSafe(arr);
        }

        [Test]
        public void TestReadingStructArrayAsObjectWithRegisteredTypeTableSerializer()
        {
            SerializationTypeTable.Serializers[typeof(TestStruct)] = (ref FastBufferWriter writer, object obj) =>
            {
                writer.WriteValueSafe((TestStruct) obj);
            };
            SerializationTypeTable.Deserializers[typeof(TestStruct)] = (ref FastBufferReader reader, out object obj) =>
            {
                reader.ReadValueSafe(out TestStruct value);
                obj = value;
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
                SerializationTypeTable.Deserializers.Remove(typeof(TestStruct));
            }
        }

        [Test]
        public void TestReadingString()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest);

            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {
                Assert.IsTrue(writer.VerifyCanWrite(serializedValueSize + 2), "Writer denied write permission");
                writer.WriteValue(valueToTest);

                WriteCheckBytes(ref writer, serializedValueSize);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    Assert.IsTrue(reader.VerifyCanRead(serializedValueSize + 2), "Reader denied read permission");
                    reader.ReadValue(out string result);
                    Assert.AreEqual(valueToTest, result);

                    VerifyCheckBytes(ref reader, serializedValueSize);
                }
            }
        }

        [Test]
        public void TestReadingStringSafe()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest);

            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {
                writer.WriteValueSafe(valueToTest);

                WriteCheckBytes(ref writer, serializedValueSize);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    reader.ReadValueSafe(out string result);
                    Assert.AreEqual(valueToTest, result);

                    VerifyCheckBytes(ref reader, serializedValueSize);
                }
            }
        }

        [Test]
        public void TestReadingStringAsObject()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest);

            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 3, Allocator.Temp);
            using(writer)
            {
                writer.WriteObject(valueToTest);

                WriteCheckBytes(ref writer, serializedValueSize+1);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    reader.ReadObject(out object result, typeof(string));
                    Assert.AreEqual(valueToTest, result);

                    VerifyCheckBytes(ref reader, serializedValueSize + 1);
                }
            }
        }

        [Test]
        public void TestReadingOneByteString()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest, true);
            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {
                Assert.IsTrue(writer.VerifyCanWrite(serializedValueSize + 2), "Writer denied write permission");
                writer.WriteValue(valueToTest, true);

                WriteCheckBytes(ref writer, serializedValueSize);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    Assert.IsTrue(reader.VerifyCanRead(serializedValueSize + 2), "Reader denied read permission");
                    reader.ReadValue(out string result, true);
                    Assert.AreEqual(valueToTest, result);

                    VerifyCheckBytes(ref reader, serializedValueSize);
                }
            }
        }

        [Test]
        public void TestReadingOneByteStringSafe()
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest, true);
            FastBufferWriter writer = new FastBufferWriter(serializedValueSize + 2, Allocator.Temp);
            using(writer)
            {
                writer.WriteValueSafe(valueToTest, true);

                WriteCheckBytes(ref writer, serializedValueSize);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length);

                    reader.ReadValueSafe(out string result, true);
                    Assert.AreEqual(valueToTest, result);

                    VerifyCheckBytes(ref reader, serializedValueSize);
                }
            }
        }

        [Test]
        public unsafe void TestReadingPartialValues([NUnit.Framework.Range(1, sizeof(ulong))] int count)
        {
            var random = new Random();
            var valueToTest = ((ulong) random.Next() << 32) + (ulong)random.Next();
            FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) + 2, Allocator.Temp);
            using (writer)
            {
                Assert.IsTrue(writer.VerifyCanWrite(count + 2), "Writer denied write permission");
                writer.WritePartialValue(valueToTest, count);

                var failMessage = $"TestReadingPartialValues failed with value {valueToTest}";
                WriteCheckBytes(ref writer, count, failMessage);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length, failMessage);
                    Assert.IsTrue(reader.VerifyCanRead(count + 2), "Reader denied read permission");

                    ulong mask = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        mask = (mask << 8) | 0b11111111;
                    }

                    reader.ReadPartialValue(out ulong result, count);
                    Assert.AreEqual(valueToTest & mask, result & mask, failMessage);
                    VerifyCheckBytes(ref reader, count, failMessage);
                }
            }
        }

        [Test]
        public void TestReadingPartialValuesWithOffsets([NUnit.Framework.Range(1, sizeof(ulong)-2)] int count)
        {
            var random = new Random();
            var valueToTest = ((ulong) random.Next() << 32) + (ulong)random.Next();
            FastBufferWriter writer = new FastBufferWriter(sizeof(ulong) + 2, Allocator.Temp);
            
            using (writer)
            {
                Assert.IsTrue(writer.VerifyCanWrite(count + 2), "Writer denied write permission");
                writer.WritePartialValue(valueToTest, count, 2);
                var failMessage = $"TestReadingPartialValuesWithOffsets failed with value {valueToTest}";
                WriteCheckBytes(ref writer, count, failMessage);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(ref reader, writer.Length, failMessage);
                    Assert.IsTrue(reader.VerifyCanRead(count + 2), "Reader denied read permission");

                    ulong mask = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        mask = (mask << 8) | 0b11111111;
                    }

                    mask <<= 16;

                    reader.ReadPartialValue(out ulong result, count, 2);
                    Assert.AreEqual(valueToTest & mask, result & mask, failMessage);
                    VerifyCheckBytes(ref reader, count, failMessage);
                }
            }
        }

        [Test]
        public unsafe void TestToArray()
        {
            var testStruct = GetTestStruct();
            var requiredSize = FastBufferWriter.GetWriteSize(testStruct);
            var writer = new FastBufferWriter(requiredSize, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(requiredSize);
                writer.WriteValue(testStruct);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    var array = reader.ToArray();
                    var underlyingArray = writer.GetUnsafePtr();
                    for (var i = 0; i < array.Length; ++i)
                    {
                        Assert.AreEqual(array[i], underlyingArray[i]);
                    }
                }
            }
        }

        [Test]
        public void TestThrowingIfBoundsCheckingSkipped()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp, 0);
            nativeArray.Dispose();

            using (emptyReader)
            using (writer)
            {
                Assert.Throws<OverflowException>(() => { emptyReader.ReadByte(out byte b); });
                var bytes = new byte[] {0, 1, 2};
                Assert.Throws<OverflowException>(() => { emptyReader.ReadBytes(ref bytes, bytes.Length); });
                int i = 1;
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out int i); });
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out bytes); });
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out string s); });

                writer.VerifyCanWrite(sizeof(int) - 1);
                writer.WriteByte(1);
                writer.WriteByte(2);
                writer.WriteByte(3);
                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    Assert.Throws<OverflowException>(() => { reader.ReadValue(out int i); });
                    Assert.Throws<OverflowException>(() => { reader.ReadValue(out byte b); });
                    Assert.IsTrue(reader.VerifyCanRead(3));
                    reader.ReadByte(out byte b);
                    reader.ReadByte(out b);
                    reader.ReadByte(out b);
                    Assert.Throws<OverflowException>(() => { reader.ReadValue(out byte b); });
                }
            }
        }

        [Test]
        public void TestThrowingIfDoingBytewiseReadsDuringBitwiseContext()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);

            using (writer)
            {
                writer.VerifyCanWrite(100);
                var bytes = new byte[] {0, 1, 2};
                int i = 1;
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

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    Assert.IsTrue(reader.VerifyCanRead(writer.Length));
                    using (var context = reader.EnterBitwiseContext())
                    {
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadByte(out byte b); });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadBytes(ref bytes, bytes.Length); });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadValue(out i); });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadValue(out bytes); });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadValue(out string s); });

                        Assert.Throws<InvalidOperationException>(() => { reader.ReadByteSafe(out byte b); });
                        Assert.Throws<InvalidOperationException>(() =>
                        {
                            reader.ReadBytesSafe(ref bytes, bytes.Length);
                        });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadValueSafe(out i); });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadValueSafe(out bytes); });
                        Assert.Throws<InvalidOperationException>(() => { reader.ReadValueSafe(out string s); });
                    }
                }
            }
        }

        [Test]
        public void TestVerifyCanReadIsRelativeToPositionAndNotAllowedReadPosition()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var reader = new FastBufferReader(nativeArray, Allocator.Temp, 100);
            nativeArray.Dispose();
            using (reader)
            {
                reader.VerifyCanRead(100);
                reader.ReadByte(out byte b);
                reader.VerifyCanRead(1);
                reader.ReadByte(out b);
                Assert.Throws<OverflowException>(() => { reader.ReadByte(out b); });
            }
        }

        [Test]
        public void TestSeeking()
        {
            var writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                writer.WriteByteSafe(1);
                writer.WriteByteSafe(3);
                writer.WriteByteSafe(2);
                writer.WriteByteSafe(5);
                writer.WriteByteSafe(4);
                writer.WriteByteSafe(0);

                var reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    reader.Seek(5);
                    reader.ReadByteSafe(out byte b);
                    Assert.AreEqual(reader.Position, 6);
                    Assert.AreEqual(reader.Length, writer.Length);
                    Assert.AreEqual(0, b);

                    reader.Seek(0);
                    reader.ReadByteSafe(out b);
                    Assert.AreEqual(reader.Position, 1);
                    Assert.AreEqual(reader.Length, writer.Length);
                    Assert.AreEqual(1, b);

                    reader.Seek(10);
                    Assert.AreEqual(reader.Position, writer.Length);
                    Assert.AreEqual(reader.Length, writer.Length);

                    reader.Seek(2);
                    reader.ReadByteSafe(out b);
                    Assert.AreEqual(2, b);

                    reader.Seek(1);
                    reader.ReadByteSafe(out b);
                    Assert.AreEqual(3, b);

                    reader.Seek(4);
                    reader.ReadByteSafe(out b);
                    Assert.AreEqual(4, b);

                    reader.Seek(3);
                    reader.ReadByteSafe(out b);
                    Assert.AreEqual(5, b);

                    Assert.AreEqual(reader.Position, 4);
                    Assert.AreEqual(reader.Length, writer.Length);
                }
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
            networkManager.SetSingleton();
            networkObject.NetworkManagerOwner = networkManager;

            // Set the NetworkConfig
            networkManager.NetworkConfig = new NetworkConfig()
            {
                // Set the current scene to prevent unexpected log messages which would trigger a failure
                RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name },
                // Set transport
                NetworkTransport = obj.AddComponent<DummyTransport>()
            };

            networkManager.StartServer();

            try
            {
                testCode(obj, networkBehaviour, networkObject);
            }
            finally
            {
                GameObject.DestroyImmediate(obj);
                networkManager.StopServer();
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp);
                    using (reader)
                    {
                        Assert.IsTrue(reader.VerifyCanRead(FastBufferWriter.GetNetworkBehaviourWriteSize()));
                        reader.ReadValue(out NetworkBehaviour result);
                        Assert.AreSame(result, networkBehaviour);
                    }

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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp);
                    using (reader)
                    {
                        Assert.IsTrue(reader.VerifyCanRead(FastBufferWriter.GetNetworkObjectWriteSize()));
                        reader.ReadValue(out NetworkObject result);
                        Assert.AreSame(result, networkObject);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp);
                    using (reader)
                    {
                        Assert.IsTrue(reader.VerifyCanRead(FastBufferWriter.GetGameObjectWriteSize()));
                        reader.ReadValue(out GameObject result);
                        Assert.AreSame(result, obj);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp);
                    using (reader)
                    {
                        reader.ReadValueSafe(out NetworkBehaviour result);
                        Assert.AreSame(result, networkBehaviour);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp);
                    using (reader)
                    {
                        reader.ReadValueSafe(out NetworkObject result);
                        Assert.AreSame(result, networkObject);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp);
                    using (reader)
                    {
                        reader.ReadValueSafe(out GameObject result);
                        Assert.AreSame(result, obj);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp, writer.Length);
                    using (reader)
                    {
                        reader.ReadObject(out object result, typeof(NetworkBehaviour));
                        Assert.AreSame(result, networkBehaviour);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp, writer.Length);
                    using (reader)
                    {
                        reader.ReadObject(out object result, typeof(NetworkObject));
                        Assert.AreSame(result, networkObject);
                    }
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

                    var reader = new FastBufferReader(ref writer, Allocator.Temp, writer.Length);
                    using (reader)
                    {
                        reader.ReadObject(out object result, typeof(GameObject));
                        Assert.AreSame(result, obj);
                    }
                }
            });
        }

        [Test]
        public void TestVerifyInternalDoesntReduceAllowedWritePoint()
        {
            var reader = new FastBufferReader(new NativeArray<byte>(100, Allocator.Temp), Allocator.Temp);
            using (reader)
            {
                reader.VerifyCanRead(25);
                reader.VerifyCanReadInternal(5);
                Assert.AreEqual(reader.m_AllowedReadMark, 25);
            }
        }

        #endregion
    }
}