using System;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.EditorTests
{
    public class NetworkSerializerTests
    {
        [Test]
        public void SerializeBool()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                bool outValueA = true;
                bool outValueB = false;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);

                // deserialize
                bool inValueA = default;
                bool inValueB = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                char outValueA = 'U';
                char outValueB = char.MinValue;
                char outValueC = char.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                char inValueA = default;
                char inValueB = default;
                char inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                sbyte outValueA = -123;
                sbyte outValueB = sbyte.MinValue;
                sbyte outValueC = sbyte.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                sbyte inValueA = default;
                sbyte inValueB = default;
                sbyte inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                byte outValueA = 123;
                byte outValueB = byte.MinValue;
                byte outValueC = byte.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                byte inValueA = default;
                byte inValueB = default;
                byte inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                short outValueA = 12345;
                short outValueB = short.MinValue;
                short outValueC = short.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                short inValueA = default;
                short inValueB = default;
                short inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                ushort outValueA = 12345;
                ushort outValueB = ushort.MinValue;
                ushort outValueC = ushort.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                ushort inValueA = default;
                ushort inValueB = default;
                ushort inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                int outValueA = 1234567890;
                int outValueB = int.MinValue;
                int outValueC = int.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                int inValueA = default;
                int inValueB = default;
                int inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                uint outValueA = 1234567890;
                uint outValueB = uint.MinValue;
                uint outValueC = uint.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                uint inValueA = default;
                uint inValueB = default;
                uint inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                long outValueA = 9876543210;
                long outValueB = long.MinValue;
                long outValueC = long.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                long inValueA = default;
                long inValueB = default;
                long inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                ulong outValueA = 9876543210;
                ulong outValueB = ulong.MinValue;
                ulong outValueC = ulong.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                ulong inValueA = default;
                ulong inValueB = default;
                ulong inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                float outValueA = 12345.6789f;
                float outValueB = float.MinValue;
                float outValueC = float.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                float inValueA = default;
                float inValueB = default;
                float inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                double outValueA = 12345.6789;
                double outValueB = double.MinValue;
                double outValueC = double.MaxValue;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                double inValueA = default;
                double inValueB = default;
                double inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                string outValueA = Guid.NewGuid().ToString("N");
                string outValueB = string.Empty;
                string outValueC = null;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                string inValueA = default;
                string inValueB = default;
                string inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Color outValueA = Color.black;
                Color outValueB = Color.white;
                Color outValueC = Color.red;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Color inValueA = default;
                Color inValueB = default;
                Color inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Color32 outValueA = new Color32(0, 0, 0, byte.MaxValue);
                Color32 outValueB = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
                Color32 outValueC = new Color32(Byte.MaxValue, 0, 0, byte.MaxValue);
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Color32 inValueA = default;
                Color32 inValueB = default;
                Color32 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Vector2 outValueA = Vector2.up;
                Vector2 outValueB = Vector2.negativeInfinity;
                Vector2 outValueC = Vector2.positiveInfinity;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Vector2 inValueA = default;
                Vector2 inValueB = default;
                Vector2 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Vector3 outValueA = Vector3.forward;
                Vector3 outValueB = Vector3.negativeInfinity;
                Vector3 outValueC = Vector3.positiveInfinity;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Vector3 inValueA = default;
                Vector3 inValueB = default;
                Vector3 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Vector4 outValueA = Vector4.one;
                Vector4 outValueB = Vector4.negativeInfinity;
                Vector4 outValueC = Vector4.positiveInfinity;
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Vector4 inValueA = default;
                Vector4 inValueB = default;
                Vector4 inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Quaternion outValueA = Quaternion.identity;
                Quaternion outValueB = Quaternion.Euler(new Vector3(30, 45, -60));
                Quaternion outValueC = Quaternion.Euler(new Vector3(90, -90, 180));
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Quaternion inValueA = default;
                Quaternion inValueB = default;
                Quaternion inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.Greater(Mathf.Abs(Quaternion.Dot(inValueA, outValueA)), 0.999f);
                Assert.Greater(Mathf.Abs(Quaternion.Dot(inValueB, outValueB)), 0.999f);
                Assert.Greater(Mathf.Abs(Quaternion.Dot(inValueC, outValueC)), 0.999f);
            }
        }

        [Test]
        public void SerializeRay()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Ray outValueA = new Ray(Vector3.zero, Vector3.forward);
                Ray outValueB = new Ray(Vector3.zero, Vector3.left);
                Ray outValueC = new Ray(Vector3.zero, Vector3.up);
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Ray inValueA = default;
                Ray inValueB = default;
                Ray inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
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
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Ray2D outValueA = new Ray2D(Vector2.zero, Vector2.up);
                Ray2D outValueB = new Ray2D(Vector2.zero, Vector2.left);
                Ray2D outValueC = new Ray2D(Vector2.zero, Vector2.right);
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outValueA);
                outSerializer.Serialize(ref outValueB);
                outSerializer.Serialize(ref outValueC);

                // deserialize
                Ray2D inValueA = default;
                Ray2D inValueB = default;
                Ray2D inValueC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inValueA);
                inSerializer.Serialize(ref inValueB);
                inSerializer.Serialize(ref inValueC);

                // validate
                Assert.AreEqual(inValueA, outValueA);
                Assert.AreEqual(inValueB, outValueB);
                Assert.AreEqual(inValueC, outValueC);
            }
        }

        private enum EnumA // int
        {
            A,
            B,
            C
        }

        private enum EnumB : byte
        {
            X,
            Y,
            Z
        }

        private enum EnumC : ushort
        {
            U,
            N,
            I,
            T,
            Y
        }

        private enum EnumD : ulong
        {
            N,
            E,
            T
        }

        [Test]
        public void SerializeEnum()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                EnumA outValueA = EnumA.C;
                EnumB outValueB = EnumB.X;
                EnumC outValueC = EnumC.N;
                EnumD outValueD = EnumD.T;
                EnumD outValueX = (EnumD)123;
                var outSerializer = new NetworkSerializer(outWriter);
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
                var inSerializer = new NetworkSerializer(inReader);
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

        [Test]
        public void SerializeBoolArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                bool[] outArrayA = null;
                bool[] outArrayB = new bool[0];
                bool[] outArrayC = { true, false, true };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                bool[] inArrayA = default;
                bool[] inArrayB = default;
                bool[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeCharArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                char[] outArrayA = null;
                char[] outArrayB = new char[0];
                char[] outArrayC = { 'U', 'N', 'I', 'T', 'Y', '\0' };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                char[] inArrayA = default;
                char[] inArrayB = default;
                char[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeSbyteArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                sbyte[] outArrayA = null;
                sbyte[] outArrayB = new sbyte[0];
                sbyte[] outArrayC = { -123, sbyte.MinValue, sbyte.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                sbyte[] inArrayA = default;
                sbyte[] inArrayB = default;
                sbyte[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeByteArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                byte[] outArrayA = null;
                byte[] outArrayB = new byte[0];
                byte[] outArrayC = { 123, byte.MinValue, byte.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                byte[] inArrayA = default;
                byte[] inArrayB = default;
                byte[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeShortArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                short[] outArrayA = null;
                short[] outArrayB = new short[0];
                short[] outArrayC = { 12345, short.MinValue, short.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                short[] inArrayA = default;
                short[] inArrayB = default;
                short[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeUshortArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                ushort[] outArrayA = null;
                ushort[] outArrayB = new ushort[0];
                ushort[] outArrayC = { 12345, ushort.MinValue, ushort.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                ushort[] inArrayA = default;
                ushort[] inArrayB = default;
                ushort[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeIntArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                int[] outArrayA = null;
                int[] outArrayB = new int[0];
                int[] outArrayC = { 1234567890, int.MinValue, int.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                int[] inArrayA = default;
                int[] inArrayB = default;
                int[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeUintArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                uint[] outArrayA = null;
                uint[] outArrayB = new uint[0];
                uint[] outArrayC = { 1234567890, uint.MinValue, uint.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                uint[] inArrayA = default;
                uint[] inArrayB = default;
                uint[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeLongArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                long[] outArrayA = null;
                long[] outArrayB = new long[0];
                long[] outArrayC = { 9876543210, long.MinValue, long.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                long[] inArrayA = default;
                long[] inArrayB = default;
                long[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeUlongArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                ulong[] outArrayA = null;
                ulong[] outArrayB = new ulong[0];
                ulong[] outArrayC = { 9876543210, ulong.MinValue, ulong.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                ulong[] inArrayA = default;
                ulong[] inArrayB = default;
                ulong[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeFloatArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                float[] outArrayA = null;
                float[] outArrayB = new float[0];
                float[] outArrayC = { 12345.6789f, float.MinValue, float.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                float[] inArrayA = default;
                float[] inArrayB = default;
                float[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeDoubleArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                double[] outArrayA = null;
                double[] outArrayB = new double[0];
                double[] outArrayC = { 12345.6789, double.MinValue, double.MaxValue };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                double[] inArrayA = default;
                double[] inArrayB = default;
                double[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeStringArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                string[] outArrayA = null;
                string[] outArrayB = new string[0];
                string[] outArrayC = { Guid.NewGuid().ToString("N"), String.Empty, null };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                string[] inArrayA = default;
                string[] inArrayB = default;
                string[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeColorArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Color[] outArrayA = null;
                Color[] outArrayB = new Color[0];
                Color[] outArrayC = { Color.black, Color.red, Color.white };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Color[] inArrayA = default;
                Color[] inArrayB = default;
                Color[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeColor32Array()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Color32[] outArrayA = null;
                Color32[] outArrayB = new Color32[0];
                Color32[] outArrayC =
                {
                    new Color32(0, 0, 0, byte.MaxValue),
                    new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue),
                    new Color32(Byte.MaxValue, 0, 0, byte.MaxValue)
                };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Color32[] inArrayA = default;
                Color32[] inArrayB = default;
                Color32[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeVector2Array()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Vector2[] outArrayA = null;
                Vector2[] outArrayB = new Vector2[0];
                Vector2[] outArrayC = { Vector2.up, Vector2.negativeInfinity, Vector2.positiveInfinity };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Vector2[] inArrayA = default;
                Vector2[] inArrayB = default;
                Vector2[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeVector3Array()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Vector3[] outArrayA = null;
                Vector3[] outArrayB = new Vector3[0];
                Vector3[] outArrayC = { Vector3.forward, Vector3.negativeInfinity, Vector3.positiveInfinity };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Vector3[] inArrayA = default;
                Vector3[] inArrayB = default;
                Vector3[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeVector4Array()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Vector4[] outArrayA = null;
                Vector4[] outArrayB = new Vector4[0];
                Vector4[] outArrayC = { Vector4.one, Vector4.negativeInfinity, Vector4.positiveInfinity };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Vector4[] inArrayA = default;
                Vector4[] inArrayB = default;
                Vector4[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeQuaternionArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Quaternion[] outArrayA = null;
                Quaternion[] outArrayB = new Quaternion[0];
                Quaternion[] outArrayC = { Quaternion.identity, Quaternion.Euler(new Vector3(30, 45, -60)), Quaternion.Euler(new Vector3(90, -90, 180)) };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Quaternion[] inArrayA = default;
                Quaternion[] inArrayB = default;
                Quaternion[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.Null(inArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                for (int i = 0; i < outArrayC.Length; ++i)
                {
                    Assert.Greater(Mathf.Abs(Quaternion.Dot(inArrayC[i], outArrayC[i])), 0.999f);
                }
            }
        }

        [Test]
        public void SerializeRayArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Ray[] outArrayA = null;
                Ray[] outArrayB = new Ray[0];
                Ray[] outArrayC =
                {
                    new Ray(Vector3.zero, Vector3.forward),
                    new Ray(Vector3.zero, Vector3.left),
                    new Ray(Vector3.zero, Vector3.up)
                };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Ray[] inArrayA = default;
                Ray[] inArrayB = default;
                Ray[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeRay2DArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                Ray2D[] outArrayA = null;
                Ray2D[] outArrayB = new Ray2D[0];
                Ray2D[] outArrayC =
                {
                    new Ray2D(Vector2.zero, Vector2.up),
                    new Ray2D(Vector2.zero, Vector2.left),
                    new Ray2D(Vector2.zero, Vector2.right)
                };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                Ray2D[] inArrayA = default;
                Ray2D[] inArrayB = default;
                Ray2D[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }

        [Test]
        public void SerializeEnumArray()
        {
            using (var outStream = PooledNetworkBuffer.Get())
            using (var outWriter = PooledNetworkWriter.Get(outStream))
            using (var inStream = PooledNetworkBuffer.Get())
            using (var inReader = PooledNetworkReader.Get(inStream))
            {
                // serialize
                EnumA[] outArrayA = null;
                EnumB[] outArrayB = new EnumB[0];
                EnumC[] outArrayC = { EnumC.U, EnumC.N, EnumC.I, EnumC.T, EnumC.Y, (EnumC)128 };
                var outSerializer = new NetworkSerializer(outWriter);
                outSerializer.Serialize(ref outArrayA);
                outSerializer.Serialize(ref outArrayB);
                outSerializer.Serialize(ref outArrayC);

                // deserialize
                EnumA[] inArrayA = default;
                EnumB[] inArrayB = default;
                EnumC[] inArrayC = default;
                inStream.Write(outStream.ToArray());
                inStream.Position = 0;
                var inSerializer = new NetworkSerializer(inReader);
                inSerializer.Serialize(ref inArrayA);
                inSerializer.Serialize(ref inArrayB);
                inSerializer.Serialize(ref inArrayC);

                // validate
                Assert.AreEqual(inArrayA, outArrayA);
                Assert.AreEqual(inArrayB, outArrayB);
                Assert.AreEqual(inArrayC, outArrayC);
            }
        }
    }
}