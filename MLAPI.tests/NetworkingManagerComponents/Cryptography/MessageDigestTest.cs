using System;

namespace MLAPI.tests.NetworkingManagerComponents.Cryptography
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
            for (int i = 0; i < 500; i++)
            {
                using (SHA256Managed sha = new SHA256Managed())
                {
                    byte[] data = new byte[lengthRnd.Next(500)];
                    dataRnd.NextBytes(data);
                    byte[] managed = sha.ComputeHash(data);
                    byte[] recursive = MessageDigest.SHA1_Opt(data).ToArray();
                    Assert.That(managed, Is.EquivalentTo(recursive));
                }
            }
        }
    }
}
