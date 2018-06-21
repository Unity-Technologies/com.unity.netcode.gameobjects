using System;
namespace MLAPI.Tests.Data
{
    using MLAPI.Data;
    using NUnit.Framework;


    [TestFixture]
    public class StringExtensionTest
    {

        [Test]
        public static void CheckHash32()
        {
            string str1 = "string 1";
            string str2 = "string 2";

            Assert.That(str1.GetStableHash32(), Is.Not.EqualTo(str2.GetStableHash32()));
        }

        [Test]
        public static void CheckHash64()
        {
            string str1 = "string 1";
            string str2 = "string 2";

            Assert.That(str1.GetStableHash64(), Is.Not.EqualTo(str2.GetStableHash64()));
        }
    }
}
