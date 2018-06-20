using System;

namespace MLAPI.Tests.NetworkingManagerComponents.Cryptography
{
    using MLAPI.NetworkingManagerComponents.Cryptography;
    using NUnit.Framework;
    using System.Security.Cryptography;

    [TestFixture]
    public class MessageDigestTest
    {
        [Test]
        public void CompareWithNative()
        {
            Random dataRnd = new Random(0);
            Random lengthRnd = new Random(0);
            for (int i = 0; i < 10; i++)
            {
                using (SHA1Managed sha = new SHA1Managed())
                {
                    byte[] data = new byte[lengthRnd.Next(100)];
                    dataRnd.NextBytes(data);
                    byte[] managed = sha.ComputeHash(data);
                    byte[] recursive = MessageDigest.SHA1_Opt(data).ToArray();
                    Assert.That(managed, Is.EquivalentTo(recursive));
                }
            }
        }
    }
}
