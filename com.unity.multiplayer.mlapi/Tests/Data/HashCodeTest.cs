using System;
namespace MLAPI_Tests.Data
{
    using MLAPI.Hashing;
    using NUnit.Framework;


    [TestFixture]
    public class HashCodeTest
    {

        [Test]
        public static void CheckStringHash16()
        {
            string str1 = "string 1";
            string str2 = "string 2";

            Assert.That(str1.GetStableHash16(), Is.Not.EqualTo(str2.GetStableHash16()));
        }

        [Test]
        public static void CheckStringHash32()
        {
            string str1 = "string 1";
            string str2 = "string 2";

            Assert.That(str1.GetStableHash32(), Is.Not.EqualTo(str2.GetStableHash32()));
        }

        [Test]
        public static void CheckStringHash64()
        {
            string str1 = "string 1";
            string str2 = "string 2";

            Assert.That(str1.GetStableHash64(), Is.Not.EqualTo(str2.GetStableHash64()));
        }

        [Test]
        public static void CheckByteHash16()
        {
            byte[] str1 = { 1, 2, 3, 5 };
            byte[] str2 = { 1, 2, 3, 4 };

            Assert.That(str1.GetStableHash16(), Is.Not.EqualTo(str2.GetStableHash16()));
        }

        [Test]
        public static void CheckByteHash32()
        {
            byte[] str1 = { 1, 2, 3, 5 };
            byte[] str2 = { 1, 2, 3, 4 };

            Assert.That(str1.GetStableHash32(), Is.Not.EqualTo(str2.GetStableHash32()));
        }

        [Test]
        public static void CheckByteHash64()
        {
            byte[] str1 = { 1, 2, 3, 5 };
            byte[] str2 = { 1, 2, 3, 4 };

            Assert.That(str1.GetStableHash64(), Is.Not.EqualTo(str2.GetStableHash64()));
        }


    }
}
