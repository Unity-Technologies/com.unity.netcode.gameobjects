using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace Unity.Netcode
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

        protected struct TestStruct
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

        public void BaseTypeTest(Type testType, WriteType writeType)
        {
            var random = new Random();

            void RunTypeTestLocal<T>(T val, WriteType wt) where T : unmanaged
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

            if (testType == typeof(byte))
            {
                RunTypeTestLocal((byte)random.Next(), writeType);
            }
            else if (testType == typeof(sbyte))
            {
                RunTypeTestLocal((sbyte)random.Next(), writeType);
            }
            else if (testType == typeof(short))
            {
                RunTypeTestLocal((short)random.Next(), writeType);
            }
            else if (testType == typeof(ushort))
            {
                RunTypeTestLocal((ushort)random.Next(), writeType);
            }
            else if (testType == typeof(int))
            {
                RunTypeTestLocal((int)random.Next(), writeType);
            }
            else if (testType == typeof(uint))
            {
                RunTypeTestLocal((uint)random.Next(), writeType);
            }
            else if (testType == typeof(long))
            {
                RunTypeTestLocal(((long)random.Next() << 32) + random.Next(), writeType);
            }
            else if (testType == typeof(ulong))
            {
                RunTypeTestLocal(((ulong)random.Next() << 32) + (ulong)random.Next(), writeType);
            }
            else if (testType == typeof(bool))
            {
                RunTypeTestLocal(true, writeType);
            }
            else if (testType == typeof(char))
            {
                RunTypeTestLocal('a', writeType);
                RunTypeTestLocal('\u263a', writeType);
            }
            else if (testType == typeof(float))
            {
                RunTypeTestLocal((float)random.NextDouble(), writeType);
            }
            else if (testType == typeof(double))
            {
                RunTypeTestLocal(random.NextDouble(), writeType);
            }
            else if (testType == typeof(ByteEnum))
            {
                RunTypeTestLocal(ByteEnum.C, writeType);
            }
            else if (testType == typeof(SByteEnum))
            {
                RunTypeTestLocal(SByteEnum.C, writeType);
            }
            else if (testType == typeof(ShortEnum))
            {
                RunTypeTestLocal(ShortEnum.C, writeType);
            }
            else if (testType == typeof(UShortEnum))
            {
                RunTypeTestLocal(UShortEnum.C, writeType);
            }
            else if (testType == typeof(IntEnum))
            {
                RunTypeTestLocal(IntEnum.C, writeType);
            }
            else if (testType == typeof(UIntEnum))
            {
                RunTypeTestLocal(UIntEnum.C, writeType);
            }
            else if (testType == typeof(LongEnum))
            {
                RunTypeTestLocal(LongEnum.C, writeType);
            }
            else if (testType == typeof(ULongEnum))
            {
                RunTypeTestLocal(ULongEnum.C, writeType);
            }
            else if (testType == typeof(Vector2))
            {
                RunTypeTestLocal(new Vector2((float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Vector3))
            {
                RunTypeTestLocal(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Vector4))
            {
                RunTypeTestLocal(new Vector4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Quaternion))
            {
                RunTypeTestLocal(new Quaternion((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Color))
            {
                RunTypeTestLocal(new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()), writeType);
            }
            else if (testType == typeof(Color32))
            {
                RunTypeTestLocal(new Color32((byte)random.Next(), (byte)random.Next(), (byte)random.Next(), (byte)random.Next()), writeType);
            }
            else if (testType == typeof(Ray))
            {
                RunTypeTestLocal(new Ray(
                    new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()),
                    new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble())), writeType);
            }
            else if (testType == typeof(Ray2D))
            {
                RunTypeTestLocal(new Ray2D(
                    new Vector2((float)random.NextDouble(), (float)random.NextDouble()),
                    new Vector2((float)random.NextDouble(), (float)random.NextDouble())), writeType);
            }
            else if (testType == typeof(TestStruct))
            {
                RunTypeTestLocal(GetTestStruct(), writeType);
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
