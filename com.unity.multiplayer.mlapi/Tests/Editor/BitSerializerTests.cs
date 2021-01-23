using System;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.Tests
{
    public class BitSerializerTests
    {
        [Test]
        public void SerializeBool()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                bool outValueA = true;
                bool outValueB = false;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);

                // deserialize
                bool inValueA = default;
                bool inValueB = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
            }
        }

        [Test]
        public void SerializeChar()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                char outValueA = 'U';
                char outValueB = char.MinValue;
                char outValueC = char.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                char inValueA = default;
                char inValueB = default;
                char inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeSbyte()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                sbyte outValueA = 123;
                sbyte outValueB = sbyte.MinValue;
                sbyte outValueC = sbyte.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                sbyte inValueA = default;
                sbyte inValueB = default;
                sbyte inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeByte()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                byte outValueA = 123;
                byte outValueB = byte.MinValue;
                byte outValueC = byte.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                byte inValueA = default;
                byte inValueB = default;
                byte inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeShort()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                short outValueA = 12345;
                short outValueB = short.MinValue;
                short outValueC = short.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                short inValueA = default;
                short inValueB = default;
                short inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeUshort()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                ushort outValueA = 12345;
                ushort outValueB = ushort.MinValue;
                ushort outValueC = ushort.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                ushort inValueA = default;
                ushort inValueB = default;
                ushort inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeInt()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                int outValueA = 1234567890;
                int outValueB = int.MinValue;
                int outValueC = int.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                int inValueA = default;
                int inValueB = default;
                int inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeUint()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                uint outValueA = 1234567890;
                uint outValueB = uint.MinValue;
                uint outValueC = uint.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                uint inValueA = default;
                uint inValueB = default;
                uint inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeLong()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                long outValueA = 9876543210;
                long outValueB = long.MinValue;
                long outValueC = long.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                long inValueA = default;
                long inValueB = default;
                long inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeUlong()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                ulong outValueA = 9876543210;
                ulong outValueB = ulong.MinValue;
                ulong outValueC = ulong.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                ulong inValueA = default;
                ulong inValueB = default;
                ulong inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeFloat()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                float outValueA = 12345.6789f;
                float outValueB = float.MinValue;
                float outValueC = float.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                float inValueA = default;
                float inValueB = default;
                float inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeDouble()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                double outValueA = 12345.6789;
                double outValueB = double.MinValue;
                double outValueC = double.MaxValue;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                double inValueA = default;
                double inValueB = default;
                double inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeString()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                string outValueA = Guid.NewGuid().ToString("N");
                string outValueB = string.Empty;
                string outValueC = null;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                string inValueA = default;
                string inValueB = default;
                string inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeColor()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Color outValueA = Color.black;
                Color outValueB = Color.white;
                Color outValueC = Color.red;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Color inValueA = default;
                Color inValueB = default;
                Color inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeColor32()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Color32 outValueA = new Color32(0, 0, 0, byte.MaxValue);
                Color32 outValueB = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
                Color32 outValueC = new Color32(Byte.MaxValue, 0, 0, byte.MaxValue);
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Color32 inValueA = default;
                Color32 inValueB = default;
                Color32 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeVector2()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Vector2 outValueA = Vector2.up;
                Vector2 outValueB = Vector2.negativeInfinity;
                Vector2 outValueC = Vector2.positiveInfinity;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Vector2 inValueA = default;
                Vector2 inValueB = default;
                Vector2 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeVector3()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Vector3 outValueA = Vector3.forward;
                Vector3 outValueB = Vector3.negativeInfinity;
                Vector3 outValueC = Vector3.positiveInfinity;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Vector3 inValueA = default;
                Vector3 inValueB = default;
                Vector3 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeVector4()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Vector4 outValueA = Vector4.one;
                Vector4 outValueB = Vector4.negativeInfinity;
                Vector4 outValueC = Vector4.positiveInfinity;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Vector4 inValueA = default;
                Vector4 inValueB = default;
                Vector4 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeQuaternion()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Quaternion outValueA = Quaternion.identity;
                Quaternion outValueB = Quaternion.Euler(Vector3.negativeInfinity);
                Quaternion outValueC = Quaternion.Euler(Vector3.positiveInfinity);
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Quaternion inValueA = default;
                Quaternion inValueB = default;
                Quaternion inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeRay()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Ray outValueA = new Ray(Vector3.zero, Vector3.forward);
                Ray outValueB = new Ray(Vector3.zero, Vector3.negativeInfinity);
                Ray outValueC = new Ray(Vector3.zero, Vector3.positiveInfinity);
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Ray inValueA = default;
                Ray inValueB = default;
                Ray inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        [Test]
        public void SerializeRay2D()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                Ray2D outValueA = new Ray2D(Vector2.zero, Vector2.up);
                Ray2D outValueB = new Ray2D(Vector2.zero, Vector2.negativeInfinity);
                Ray2D outValueC = new Ray2D(Vector2.zero, Vector2.positiveInfinity);
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Ray2D inValueA = default;
                Ray2D inValueB = default;
                Ray2D inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        enum EnumA // int
        {
            A,
            B,
            C
        }

        enum EnumB : byte
        {
            X,
            Y,
            Z
        }

        enum EnumC : ushort
        {
            U,
            N,
            I,
            T,
            Y
        }

        enum EnumD : ulong
        {
            N,
            E,
            T
        }

        [Test]
        public void SerializeEnum()
        {
            using (var outStream = PooledBitStream.Get())
            using (var outWriter = PooledBitWriter.Get(outStream))
            using (var inStream = PooledBitStream.Get())
            using (var inReader = PooledBitReader.Get(inStream))
            {
                // serialize
                EnumA outValueA = EnumA.C;
                EnumB outValueB = EnumB.X;
                EnumC outValueC = EnumC.N;
                EnumD outValueD = EnumD.T;
                EnumD outValueX = (EnumD)123;
                var outSerializer = new BitSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);
                outSerializer.Serialize(ref outValueD);
                outSerializer.Serialize(ref outValueX);

                // deserialize
                EnumA inValueA = default;
                EnumB inValueB = default;
                EnumC inValueC = default;
                EnumD inValueD = default;
                EnumD inValueX = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new BitSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);
                inSerializer.Serialize(ref inValueD);
                inSerializer.Serialize(ref inValueX);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
                Assert.AreEqual(inValueD, outValueD);
                Assert.AreEqual(inValueX, outValueX);
            }
        }
    }
}