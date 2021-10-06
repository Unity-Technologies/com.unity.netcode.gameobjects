using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public class FastBufferReaderTests : BaseFastBufferReaderWriterTest
    {
        #region Common Checks
        private void WriteCheckBytes(FastBufferWriter writer, int writeSize, string failMessage = "")
        {
            Assert.IsTrue(writer.TryBeginWrite(2), "Writer denied write permission");
            writer.WriteValue((byte)0x80);
            Assert.AreEqual(writeSize + 1, writer.Position, failMessage);
            Assert.AreEqual(writeSize + 1, writer.Length, failMessage);
            writer.WriteValue((byte)0xFF);
            Assert.AreEqual(writeSize + 2, writer.Position, failMessage);
            Assert.AreEqual(writeSize + 2, writer.Length, failMessage);
        }

        private void VerifyCheckBytes(FastBufferReader reader, int checkPosition, string failMessage = "")
        {
            reader.Seek(checkPosition);
            reader.TryBeginRead(2);

            reader.ReadByte(out byte value);
            Assert.AreEqual(0x80, value, failMessage);
            reader.ReadByte(out value);
            Assert.AreEqual(0xFF, value, failMessage);
        }

        private void VerifyPositionAndLength(FastBufferReader reader, int length, string failMessage = "")
        {
            Assert.AreEqual(0, reader.Position, failMessage);
            Assert.AreEqual(length, reader.Length, failMessage);
        }

        private FastBufferReader CommonChecks<T>(FastBufferWriter writer, T valueToTest, int writeSize, string failMessage = "") where T : unmanaged
        {
            WriteCheckBytes(writer, writeSize, failMessage);

            var reader = new FastBufferReader(writer, Allocator.Temp);

            VerifyPositionAndLength(reader, writer.Length, failMessage);

            VerifyCheckBytes(reader, writeSize, failMessage);

            reader.Seek(0);

            return reader;
        }
        #endregion

        #region Generic Checks
        protected override unsafe void RunTypeTest<T>(T valueToTest)
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            Assert.AreEqual(sizeof(T), writeSize);
            var writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);

            using (writer)
            {
                Assert.IsTrue(writer.TryBeginWrite(writeSize + 2), "Writer denied write permission");

                var failMessage = $"RunTypeTest failed with type {typeof(T)} and value {valueToTest}";

                writer.WriteValue(valueToTest);

                var reader = CommonChecks(writer, valueToTest, writeSize, failMessage);

                using (reader)
                {
                    Assert.IsTrue(reader.TryBeginRead(FastBufferWriter.GetWriteSize<T>()));
                    reader.ReadValue(out T result);
                    Assert.AreEqual(valueToTest, result);
                }
            }
        }
        protected override unsafe void RunTypeTestSafe<T>(T valueToTest)
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            var writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);

            using (writer)
            {
                Assert.AreEqual(sizeof(T), writeSize);

                var failMessage = $"RunTypeTest failed with type {typeof(T)} and value {valueToTest}";

                writer.WriteValueSafe(valueToTest);


                var reader = CommonChecks(writer, valueToTest, writeSize, failMessage);

                using (reader)
                {
                    reader.ReadValueSafe(out T result);
                    Assert.AreEqual(valueToTest, result);
                }
            }
        }

        private void VerifyArrayEquality<T>(T[] value, T[] compareValue, int offset) where T : unmanaged
        {
            Assert.AreEqual(value.Length, compareValue.Length);

            for (var i = 0; i < value.Length; ++i)
            {
                Assert.AreEqual(value[i], compareValue[i]);
            }
        }

        protected override unsafe void RunTypeArrayTest<T>(T[] valueToTest)
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            var writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);
            using (writer)
            {
                Assert.AreEqual(sizeof(int) + sizeof(T) * valueToTest.Length, writeSize);
                Assert.IsTrue(writer.TryBeginWrite(writeSize + 2), "Writer denied write permission");

                writer.WriteValue(valueToTest);

                WriteCheckBytes(writer, writeSize);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(reader, writer.Length);

                    Assert.IsTrue(reader.TryBeginRead(writeSize));
                    reader.ReadValue(out T[] result);
                    VerifyArrayEquality(valueToTest, result, 0);

                    VerifyCheckBytes(reader, writeSize);
                }
            }
        }

        protected override unsafe void RunTypeArrayTestSafe<T>(T[] valueToTest)
        {
            var writeSize = FastBufferWriter.GetWriteSize(valueToTest);
            var writer = new FastBufferWriter(writeSize + 2, Allocator.Temp);
            using (writer)
            {
                Assert.AreEqual(sizeof(int) + sizeof(T) * valueToTest.Length, writeSize);

                writer.WriteValueSafe(valueToTest);

                WriteCheckBytes(writer, writeSize);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(reader, writer.Length);

                    reader.ReadValueSafe(out T[] result);
                    VerifyArrayEquality(valueToTest, result, 0);

                    VerifyCheckBytes(reader, writeSize);
                }
            }
        }

        #endregion

        #region Tests
        [Test]
        public void GivenFastBufferWriterContainingValue_WhenReadingUnmanagedType_ValueMatchesWhatWasWritten(
            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3), typeof(Vector4),
                typeof(Quaternion), typeof(Color), typeof(Color32), typeof(Ray), typeof(Ray2D), typeof(TestStruct))]
            Type testType,
            [Values] WriteType writeType)
        {
            BaseTypeTest(testType, writeType);
        }

        [Test]
        public void GivenFastBufferWriterContainingValue_WhenReadingArrayOfUnmanagedElementType_ValueMatchesWhatWasWritten(
            [Values(typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(bool), typeof(char), typeof(float), typeof(double),
                typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum), typeof(IntEnum),
                typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum), typeof(Vector2), typeof(Vector3), typeof(Vector4),
                typeof(Quaternion), typeof(Color), typeof(Color32), typeof(Ray), typeof(Ray2D), typeof(TestStruct))]
            Type testType,
            [Values] WriteType writeType)
        {
            BaseArrayTypeTest(testType, writeType);
        }

        [TestCase(false, WriteType.WriteDirect)]
        [TestCase(false, WriteType.WriteSafe)]
        [TestCase(true, WriteType.WriteDirect)]
        [TestCase(true, WriteType.WriteSafe)]
        public void GivenFastBufferWriterContainingValue_WhenReadingString_ValueMatchesWhatWasWritten(bool oneByteChars, WriteType writeType)
        {
            string valueToTest = "Hello, I am a test string!";

            var serializedValueSize = FastBufferWriter.GetWriteSize(valueToTest, oneByteChars);

            var writer = new FastBufferWriter(serializedValueSize + 3, Allocator.Temp);
            using (writer)
            {
                switch (writeType)
                {
                    case WriteType.WriteDirect:
                        Assert.IsTrue(writer.TryBeginWrite(serializedValueSize + 2), "Writer denied write permission");
                        writer.WriteValue(valueToTest, oneByteChars);
                        break;
                    case WriteType.WriteSafe:
                        writer.WriteValueSafe(valueToTest, oneByteChars);
                        break;
                }

                WriteCheckBytes(writer, serializedValueSize);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(reader, writer.Length);

                    string result = null;
                    switch (writeType)
                    {
                        case WriteType.WriteDirect:
                            Assert.IsTrue(reader.TryBeginRead(serializedValueSize + 2), "Reader denied read permission");
                            reader.ReadValue(out result, oneByteChars);
                            break;
                        case WriteType.WriteSafe:
                            reader.ReadValueSafe(out result, oneByteChars);
                            break;
                    }
                    Assert.AreEqual(valueToTest, result);

                    VerifyCheckBytes(reader, serializedValueSize);
                }
            }
        }


        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 0)]
        [TestCase(6, 0)]
        [TestCase(7, 0)]
        [TestCase(8, 0)]

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 1)]

        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 2)]
        [TestCase(6, 2)]

        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]

        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]

        [TestCase(1, 5)]
        [TestCase(2, 5)]
        [TestCase(3, 5)]

        [TestCase(1, 6)]
        [TestCase(2, 6)]

        [TestCase(1, 7)]
        public void GivenFastBufferWriterContainingValue_WhenReadingPartialValue_ValueMatchesWhatWasWritten(int count, int offset)
        {
            var random = new Random();
            var valueToTest = ((ulong)random.Next() << 32) + (ulong)random.Next();
            var writer = new FastBufferWriter(sizeof(ulong) + 2, Allocator.Temp);
            using (writer)
            {
                Assert.IsTrue(writer.TryBeginWrite(count + 2), "Writer denied write permission");
                writer.WritePartialValue(valueToTest, count, offset);

                var failMessage = $"TestReadingPartialValues failed with value {valueToTest}";
                WriteCheckBytes(writer, count, failMessage);

                var reader = new FastBufferReader(writer, Allocator.Temp);
                using (reader)
                {
                    VerifyPositionAndLength(reader, writer.Length, failMessage);
                    Assert.IsTrue(reader.TryBeginRead(count + 2), "Reader denied read permission");

                    ulong mask = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        mask = (mask << 8) | 0b11111111;
                    }

                    mask <<= (offset * 8);

                    reader.ReadPartialValue(out ulong result, count, offset);
                    Assert.AreEqual(valueToTest & mask, result & mask, failMessage);
                    VerifyCheckBytes(reader, count, failMessage);
                }
            }
        }


        [Test]
        public unsafe void GivenFastBufferReaderInitializedFromFastBufferWriterContainingValue_WhenCallingToArray_ReturnedArrayMatchesContentOfWriter()
        {
            var testStruct = GetTestStruct();
            var requiredSize = FastBufferWriter.GetWriteSize(testStruct);
            var writer = new FastBufferWriter(requiredSize, Allocator.Temp);

            using (writer)
            {
                writer.TryBeginWrite(requiredSize);
                writer.WriteValue(testStruct);

                var reader = new FastBufferReader(writer, Allocator.Temp);
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
        public void WhenCallingReadByteWithoutCallingTryBeingReadFirst_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                Assert.Throws<OverflowException>(() => { emptyReader.ReadByte(out byte b); });
            }
        }

        [Test]
        public void WhenCallingReadBytesWithoutCallingTryBeingReadFirst_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            byte[] b = { 0, 1, 2 };
            using (emptyReader)
            {
                Assert.Throws<OverflowException>(() => { emptyReader.ReadBytes(ref b, 3); });
            }
        }

        [Test]
        public void WhenCallingReadValueWithUnmanagedTypeWithoutCallingTryBeingReadFirst_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out int i); });
            }
        }

        [Test]
        public void WhenCallingReadValueWithByteArrayWithoutCallingTryBeingReadFirst_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out byte[] b); });
            }
        }

        [Test]
        public void WhenCallingReadValueWithStringWithoutCallingTryBeingReadFirst_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out string s); });
            }
        }

        [Test]
        public void WhenCallingReadValueAfterCallingTryBeginWriteWithTooFewBytes_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(sizeof(int) - 1);
                Assert.Throws<OverflowException>(() => { emptyReader.ReadValue(out int i); });
            }
        }

        [Test]
        public void WhenCallingReadBytePastBoundaryMarkedByTryBeginWrite_OverflowExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(sizeof(int) - 1);
                emptyReader.ReadByte(out byte b);
                emptyReader.ReadByte(out b);
                emptyReader.ReadByte(out b);
                Assert.Throws<OverflowException>(() => { emptyReader.ReadByte(out b); });
            }
        }

        [Test]
        public void WhenCallingReadByteDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadByte(out byte b); });
            }
        }

        [Test]
        public void WhenCallingReadBytesDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                byte[] b = { 0, 1, 2 };
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadBytes(ref b, 3); });
            }
        }

        [Test]
        public void WhenCallingReadValueWithUnmanagedTypeDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadValue(out int i); });
            }
        }

        [Test]
        public void WhenCallingReadValueWithByteArrayDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadValue(out byte[] b); });
            }
        }

        [Test]
        public void WhenCallingReadValueWithStringDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadValue(out string s); });
            }
        }

        [Test]
        public void WhenCallingReadByteSafeDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadByteSafe(out byte b); });
            }
        }

        [Test]
        public void WhenCallingReadBytesSafeDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                byte[] b = { 0, 1, 2 };
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadBytesSafe(ref b, 3); });
            }
        }

        [Test]
        public void WhenCallingReadValueSafeWithUnmanagedTypeDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadValueSafe(out int i); });
            }
        }

        [Test]
        public void WhenCallingReadValueSafeWithByteArrayDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadValueSafe(out byte[] b); });
            }
        }

        [Test]
        public void WhenCallingReadValueSafeWithStringDuringBitwiseContext_InvalidOperationExceptionIsThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                using var context = emptyReader.EnterBitwiseContext();
                Assert.Throws<InvalidOperationException>(() => { emptyReader.ReadValueSafe(out string s); });
            }
        }

        [Test]
        public void WhenCallingReadByteAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadByte(out byte theByte);
            }
        }

        [Test]
        public void WhenCallingReadBytesAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }

                byte[] theBytes = { 0, 1, 2 };
                emptyReader.ReadBytes(ref theBytes, 3);
            }
        }

        [Test]
        public void WhenCallingReadValueWithUnmanagedTypeAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadValue(out int i);
            }
        }

        [Test]
        public void WhenCallingReadValueWithByteArrayAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadValue(out byte[] theBytes);
            }
        }

        [Test]
        public void WhenCallingReadValueWithStringAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadValue(out string s);
            }
        }

        [Test]
        public void WhenCallingReadByteSafeAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadByteSafe(out byte theByte);
            }
        }

        [Test]
        public void WhenCallingReadBytesSafeAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }

                byte[] theBytes = { 0, 1, 2 };
                emptyReader.ReadBytesSafe(ref theBytes, 3);
            }
        }

        [Test]
        public void WhenCallingReadValueSafeWithUnmanagedTypeAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadValueSafe(out int i);
            }
        }

        [Test]
        public void WhenCallingReadValueSafeWithByteArrayAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadValueSafe(out byte[] theBytes);
            }
        }

        [Test]
        public void WhenCallingReadValueSafeWithStringAfterExitingBitwiseContext_InvalidOperationExceptionIsNotThrown()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                using (var context = emptyReader.EnterBitwiseContext())
                {
                    context.ReadBit(out bool theBit);
                }
                emptyReader.ReadValueSafe(out string s);
            }
        }

        [Test]
        public void WhenCallingTryBeginRead_TheAllowedReadPositionIsMarkedRelativeToCurrentPosition()
        {
            var nativeArray = new NativeArray<byte>(100, Allocator.Temp);
            var emptyReader = new FastBufferReader(nativeArray, Allocator.Temp);

            using (emptyReader)
            {
                emptyReader.TryBeginRead(100);
                emptyReader.ReadByte(out byte b);
                emptyReader.TryBeginRead(1);
                emptyReader.ReadByte(out b);
                Assert.Throws<OverflowException>(() => { emptyReader.ReadByte(out byte b); });
            }
        }

        [Test]
        public void WhenReadingAfterSeeking_TheNewReadComesFromTheCorrectPosition()
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

                var reader = new FastBufferReader(writer, Allocator.Temp);
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

        [Test]
        public unsafe void WhenCallingTryBeginReadInternal_AllowedReadPositionDoesNotMoveBackward()
        {
            var reader = new FastBufferReader(new NativeArray<byte>(100, Allocator.Temp), Allocator.Temp);
            using (reader)
            {
                reader.TryBeginRead(25);
                reader.TryBeginReadInternal(5);
                Assert.AreEqual(reader.Handle->AllowedReadMark, 25);
            }
        }

        #endregion
    }
}
