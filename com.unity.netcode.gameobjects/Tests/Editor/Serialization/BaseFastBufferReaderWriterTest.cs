using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public abstract class BaseFastBufferReaderWriterTest
    {

        #region Test Types
        protected enum ByteEnum : byte
        {
            A,
            B,
            C
        };
        protected enum SByteEnum : sbyte
        {
            A,
            B,
            C
        };
        protected enum ShortEnum : short
        {
            A,
            B,
            C
        };
        protected enum UShortEnum : ushort
        {
            A,
            B,
            C
        };
        protected enum IntEnum : int
        {
            A,
            B,
            C
        };
        protected enum UIntEnum : uint
        {
            A,
            B,
            C
        };
        protected enum LongEnum : long
        {
            A,
            B,
            C
        };
        protected enum ULongEnum : ulong
        {
            A,
            B,
            C
        };

        protected struct TestStruct : INetworkSerializeByMemcpy
        {
            public byte A;
            public short B;
            public ushort C;
            public int D;
            public uint E;
            public long F;
            public ulong G;
            public bool H;
            public char I;
            public float J;
            public double K;
        }

        public enum WriteType
        {
            WriteDirect,
            WriteSafe
        }
        #endregion

        protected abstract void RunTypeTest<T>(T valueToTest) where T : unmanaged;

        protected abstract void RunTypeTestSafe<T>(T valueToTest) where T : unmanaged;

        protected abstract void RunTypeArrayTest<T>(T[] valueToTest) where T : unmanaged;

        protected abstract void RunTypeArrayTestSafe<T>(T[] valueToTest) where T : unmanaged;

        #region Helpers
        protected TestStruct GetTestStruct()
        {
            var random = new Random();

            var testStruct = new TestStruct
            {
                A = (byte)random.Next(),
                B = (short)random.Next(),
                C = (ushort)random.Next(),
                D = (int)random.Next(),
                E = (uint)random.Next(),
                F = ((long)random.Next() << 32) + random.Next(),
                G = ((ulong)random.Next() << 32) + (ulong)random.Next(),
                H = true,
                I = '\u263a',
                J = (float)random.NextDouble(),
                K = random.NextDouble(),
            };

            return testStruct;
        }

        #endregion


        private void RunTestWithWriteType<T>(T val, WriteType wt, FastBufferWriter.ForPrimitives _ = default) where T : unmanaged
        {
            switch (wt)
            {
                case WriteType.WriteDirect:
                    RunTypeTest(val);
                    break;
                case WriteType.WriteSafe:
                    RunTypeTestSafe(val);
                    break;
            }
        }

        public void BaseTypeTest(Type testType, WriteType writeType)
        {
            var random = new Random();

            if (testType == typeof(byte))
            {
                RunTestWithWriteType((byte)random.Next(), writeType);
            }
            else if (testType == typeof(sbyte))
            {
                RunTestWithWriteType((sbyte)random.Next(), writeType);
            }
            else if (testType == typeof(short))
            {
                RunTestWithWriteType((short)random.Next(), writeType);
            }
            else if (testType == typeof(ushort))
            {
                RunTestWithWriteType((ushort)random.Next(), writeType);
            }
            else if (testType == typeof(int))
            {
                RunTestWithWriteType((int)random.Next(), writeType);
            }
            else if (testType == typeof(uint))
            {
                RunTestWithWriteType((uint)random.Next(), writeType);
            }
            else if (testType == typeof(long))
            {
                RunTestWithWriteType(((long)random.Next() << 32) + random.Next(), writeType);
            }
            else if (testType == typeof(ulong))
            {
                RunTestWithWriteType(((ulong)random.Next() << 32) + (ulong)random.Next(), writeType);
            }
            else if (testType == typeof(bool))
            {
                RunTestWithWriteType(true, writeType);
            }
            else if (testType == typeof(char))
            {
                RunTestWithWriteType('a', writeType);
                RunTestWithWriteType('\u263a', writeType);
            }
            else if (testType == typeof(float))
            {
                RunTestWithWriteType((float)random.NextDouble(), writeType);
            }
            else if (testType == typeof(double))
            {
                RunTestWithWriteType(random.NextDouble(), writeType);
            }
            else if (testType == typeof(ByteEnum))
            {
                RunTestWithWriteType(ByteEnum.C, writeType);
            }
            else if (testType == typeof(SByteEnum))
            {
                RunTestWithWriteType(SByteEnum.C, writeType);
            }
            else if (testType == typeof(ShortEnum))
            {
                RunTestWithWriteType(ShortEnum.C, writeType);
            }
            else if (testType == typeof(UShortEnum))
            {
                RunTestWithWriteType(UShortEnum.C, writeType);
            }
            else if (testType == typeof(IntEnum))
            {
                RunTestWithWriteType(IntEnum.C, writeType);
            }
            else if (testType == typeof(UIntEnum))
            {
                RunTestWithWriteType(UIntEnum.C, writeType);
            }
            else if (testType == typeof(LongEnum))
            {
                RunTestWithWriteType(LongEnum.C, writeType);
            }
            else if (testType == typeof(ULongEnum))
            {
                RunTestWithWriteType(ULongEnum.C, writeType);
            }
            else if (testType == typeof(Vector2))
            {
                RunTestWithWriteType(new Vector2((float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Vector3))
            {
                RunTestWithWriteType(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Vector2Int))
            {
                RunTestWithWriteType(new Vector2Int((int)random.NextDouble(), (int)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Vector3Int))
            {
                RunTestWithWriteType(new Vector3Int((int)random.NextDouble(), (int)random.NextDouble(), (int)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Vector4))
            {
                RunTestWithWriteType(new Vector4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Quaternion))
            {
                RunTestWithWriteType(new Quaternion((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Color))
            {
                RunTestWithWriteType(new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Color32))
            {
                RunTestWithWriteType(new Color32((byte)random.Next(), (byte)random.Next(), (byte)random.Next(), (byte)random.Next()), writeType);
            }
            else if (testType == typeof(Ray))
            {
                RunTestWithWriteType(new Ray(
                    new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()),
                    new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble())), writeType);
            }
            else if (testType == typeof(Ray2D))
            {
                RunTestWithWriteType(new Ray2D(
                    new Vector2((float)random.NextDouble(), (float)random.NextDouble()),
                    new Vector2((float)random.NextDouble(), (float)random.NextDouble())), writeType);
            }
            else if (testType == typeof(TestStruct))
            {
                RunTestWithWriteType(GetTestStruct(), writeType);
            }
            else
            {
                Assert.Fail("No type handler was provided for this type in the test!");
            }
        }

        public void BaseArrayTypeTest(Type testType, WriteType writeType)
        {
            var random = new Random();
            void RunTypeTestLocal<T>(T[] val, WriteType wt) where T : unmanaged
            {
                switch (wt)
                {
                    case WriteType.WriteDirect:
                        RunTypeArrayTest(val);
                        break;
                    case WriteType.WriteSafe:
                        RunTypeArrayTestSafe(val);
                        break;
                }
            }

            if (testType == typeof(byte))
            {
                RunTypeTestLocal(new[]{
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next(),
                    (byte) random.Next()
                }, writeType);
            }
            else if (testType == typeof(sbyte))
            {
                RunTypeTestLocal(new[]{
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next(),
                    (sbyte) random.Next()
                }, writeType);
            }
            else if (testType == typeof(short))
            {
                RunTypeTestLocal(new[]{
                    (short) random.Next(),
                    (short) random.Next(),
                    (short) random.Next(),
                    (short) random.Next(),
                    (short) random.Next()
                }, writeType);
            }
            else if (testType == typeof(ushort))
            {
                RunTypeTestLocal(new[]{
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next(),
                    (ushort) random.Next()
                }, writeType);
            }
            else if (testType == typeof(int))
            {
                RunTypeTestLocal(new[]{
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next(),
                    random.Next()
                }, writeType);
            }
            else if (testType == typeof(uint))
            {
                RunTypeTestLocal(new[]{
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next(),
                    (uint) random.Next()
                }, writeType);
            }
            else if (testType == typeof(long))
            {
                RunTypeTestLocal(new[]{
                    ((long)random.Next() << 32) + (long)random.Next(),
                    ((long)random.Next() << 32) + (long)random.Next(),
                    ((long)random.Next() << 32) + (long)random.Next(),
                    ((long)random.Next() << 32) + (long)random.Next()
                }, writeType);
            }
            else if (testType == typeof(ulong))
            {
                RunTypeTestLocal(new[]{
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next(),
                    ((ulong)random.Next() << 32) + (ulong)random.Next()
                }, writeType);
            }
            else if (testType == typeof(bool))
            {
                RunTypeTestLocal(new[]{
                    true,
                    false,
                    true,
                    true,
                    false,
                    false,
                    true,
                    false,
                    true
                }, writeType);
            }
            else if (testType == typeof(char))
            {
                RunTypeTestLocal(new[]{
                    'a',
                    '\u263a'
                }, writeType);
            }
            else if (testType == typeof(float))
            {
                RunTypeTestLocal(new[]{
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble()
                }, writeType);
            }
            else if (testType == typeof(double))
            {
                RunTypeTestLocal(new[]{
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble(),
                    random.NextDouble()
                }, writeType);
            }
            else if (testType == typeof(ByteEnum))
            {
                RunTypeTestLocal(new[]{
                    ByteEnum.C,
                    ByteEnum.A,
                    ByteEnum.B
                }, writeType);
            }
            else if (testType == typeof(SByteEnum))
            {
                RunTypeTestLocal(new[]{
                    SByteEnum.C,
                    SByteEnum.A,
                    SByteEnum.B
                }, writeType);
            }
            else if (testType == typeof(ShortEnum))
            {
                RunTypeTestLocal(new[]{
                    ShortEnum.C,
                    ShortEnum.A,
                    ShortEnum.B
                }, writeType);
            }
            else if (testType == typeof(UShortEnum))
            {
                RunTypeTestLocal(new[]{
                    UShortEnum.C,
                    UShortEnum.A,
                    UShortEnum.B
                }, writeType);
            }
            else if (testType == typeof(IntEnum))
            {
                RunTypeTestLocal(new[]{
                    IntEnum.C,
                    IntEnum.A,
                    IntEnum.B
                }, writeType);
            }
            else if (testType == typeof(UIntEnum))
            {
                RunTypeTestLocal(new[]{
                    UIntEnum.C,
                    UIntEnum.A,
                    UIntEnum.B
                }, writeType);
            }
            else if (testType == typeof(LongEnum))
            {
                RunTypeTestLocal(new[]{
                    LongEnum.C,
                    LongEnum.A,
                    LongEnum.B
                }, writeType);
            }
            else if (testType == typeof(ULongEnum))
            {
                RunTypeTestLocal(new[]{
                    ULongEnum.C,
                    ULongEnum.A,
                    ULongEnum.B
                }, writeType);
            }
            else if (testType == typeof(Vector2))
            {
                RunTypeTestLocal(new[]{
                    new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Vector3))
            {
                RunTypeTestLocal(new[]{
                    new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble()),
                    new Vector3((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Vector2Int))
            {
                RunTypeTestLocal(new[]{
                    new Vector2Int((int) random.NextDouble(), (int) random.NextDouble()),
                    new Vector2Int((int) random.NextDouble(), (int) random.NextDouble()),
                    new Vector2Int((int) random.NextDouble(), (int) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Vector3Int))
            {
                RunTypeTestLocal(new[]{
                    new Vector3Int((int) random.NextDouble(), (int) random.NextDouble(), (int) random.NextDouble()),
                    new Vector3Int((int) random.NextDouble(), (int) random.NextDouble(), (int) random.NextDouble()),
                    new Vector3Int((int) random.NextDouble(), (int) random.NextDouble(), (int) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Vector4))
            {
                RunTypeTestLocal(new[]{
                    new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Vector4((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Quaternion))
            {
                RunTypeTestLocal(new[]{
                    new Quaternion((float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble(), (float) random.NextDouble()),
                    new Quaternion((float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble(), (float) random.NextDouble()),
                    new Quaternion((float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble(), (float) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Color))
            {
                RunTypeTestLocal(new[]{
                    new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                    new Color((float) random.NextDouble(), (float) random.NextDouble(), (float) random.NextDouble(),
                        (float) random.NextDouble()),
                }, writeType);
            }
            else if (testType == typeof(Color32))
            {
                RunTypeTestLocal(new[]{
                    new Color32((byte) random.Next(), (byte) random.Next(), (byte) random.Next(), (byte) random.Next()),
                    new Color32((byte) random.Next(), (byte) random.Next(), (byte) random.Next(), (byte) random.Next()),
                    new Color32((byte) random.Next(), (byte) random.Next(), (byte) random.Next(), (byte) random.Next()),
                }, writeType);
            }
            else if (testType == typeof(Ray))
            {
                RunTypeTestLocal(new[]{
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
                }, writeType);
            }
            else if (testType == typeof(Ray2D))
            {
                RunTypeTestLocal(new[]{
                    new Ray2D(
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble())),
                    new Ray2D(
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble())),
                    new Ray2D(
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble()),
                        new Vector2((float) random.NextDouble(), (float) random.NextDouble())),
                }, writeType);
            }
            else if (testType == typeof(TestStruct))
            {
                RunTypeTestLocal(new[] {
                    GetTestStruct(),
                    GetTestStruct(),
                    GetTestStruct(),
                }, writeType);
            }
            else
            {
                Assert.Fail("No type handler was provided for this type in the test!");
            }
        }
    }
}
