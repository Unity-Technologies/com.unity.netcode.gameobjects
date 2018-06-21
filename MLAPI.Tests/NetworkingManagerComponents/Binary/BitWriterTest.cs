using System;

namespace MLAPI.Tests.NetworkingManagerComponents.Binary
{
    using MLAPI.NetworkingManagerComponents.Binary;
    using NUnit.Framework;


    [TestFixture]
    public class BitWriterTest
    {
        [Test]
        public void TestWritingTrue()
        {
            BitWriter bitWriter = BitWriter.Get();

            bitWriter.WriteBool(true);

            byte[] result = bitWriter.Finalize();

            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(1));
        }
    }
}
