using System;
namespace MLAPI_Tests.Serialization
{
    using MLAPI.Serialization;
    using NUnit.Framework;



    [TestFixture]
    public class ArithmeticTest
    {

        [Test]
        public void testCeil()
        {
            Assert.That(Arithmetic.CeilingExact(10, 5), Is.EqualTo(2));
            Assert.That(Arithmetic.CeilingExact(11, 5), Is.EqualTo(3));
            Assert.That(Arithmetic.CeilingExact(0, 5), Is.EqualTo(0));
            Assert.That(Arithmetic.CeilingExact(1, 5), Is.EqualTo(1));
            Assert.That(Arithmetic.CeilingExact(2, 5), Is.EqualTo(1));
            Assert.That(Arithmetic.CeilingExact(3, 5), Is.EqualTo(1));
            Assert.That(Arithmetic.CeilingExact(4, 5), Is.EqualTo(1));
            Assert.That(Arithmetic.CeilingExact(5, 5), Is.EqualTo(1));
            Assert.That(Arithmetic.CeilingExact(6, 5), Is.EqualTo(2));

        }

        [Test]
        public void testZigZag()
        {
            Assert.That(Arithmetic.ZigZagDecode(Arithmetic.ZigZagEncode(1234)), Is.EqualTo(1234));
            Assert.That(Arithmetic.ZigZagDecode(Arithmetic.ZigZagEncode(-1)), Is.EqualTo(-1));
            Assert.That(Arithmetic.ZigZagDecode(Arithmetic.ZigZagEncode(0)), Is.EqualTo(0));
            Assert.That(Arithmetic.ZigZagDecode(Arithmetic.ZigZagEncode(Int64.MaxValue)), Is.EqualTo(Int64.MaxValue));
            Assert.That(Arithmetic.ZigZagDecode(Arithmetic.ZigZagEncode(Int64.MinValue)), Is.EqualTo(Int64.MinValue));
        }
    }
}
