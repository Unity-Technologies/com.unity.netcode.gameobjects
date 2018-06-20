using System;

namespace MLAPI.Tests.Data
{
    using MLAPI.Data;
    using NUnit.Framework;


    [TestFixture]
    public class FieldTypeTests
    {
        [Test]
        public void TestForReferenceTypes()
        {
            object o = (int)2; //Boxed to ref type. But the type of the value is a value type.
            Assert.That(FieldTypeHelper.IsRefType(o.GetType()), Is.False);
            int i = 3; //Value type
            Assert.That(FieldTypeHelper.IsRefType(i.GetType()), Is.False);
            string s = "123"; //Actually ref type. But it's immutable so the method should return true.
            Assert.That(FieldTypeHelper.IsRefType(s.GetType()), Is.False);  
            byte[] bs = new byte[5];
            Assert.That(FieldTypeHelper.IsRefType(bs.GetType()), Is.True);
        }

        [Test]
        public void TestSequenceEquals()
        {
            Random rnd = new Random(0);
            byte[] byteArray = new byte[50];
            rnd.NextBytes(byteArray);
            Assert.That(FieldTypeHelper.SequenceEquals(byteArray, null), Is.False);
            Assert.That(FieldTypeHelper.SequenceEquals(null, byteArray), Is.False);

            Random rnd1 = new Random(0);
            for (int i = 0; i < 500; i++)
            {
                rnd = new Random(0);
                int length = rnd1.Next(100);
                if (length == 50) length++;
                byte[] diffLengthArray = new byte[length];
                rnd.NextBytes(diffLengthArray);
                Assert.That(FieldTypeHelper.SequenceEquals(diffLengthArray, byteArray), Is.False);
                Assert.That(FieldTypeHelper.SequenceEquals(byteArray, diffLengthArray), Is.False);
            }

            rnd1 = new Random(0);
            for (int i = 0; i < 500; i++)
            {
                rnd = new Random(0);
                byte[] sameLengthDiffValues = new byte[50];
                rnd.NextBytes(byteArray);

                sameLengthDiffValues[rnd1.Next(50)] = (byte)rnd.Next(255);
                Assert.That(FieldTypeHelper.SequenceEquals(sameLengthDiffValues, byteArray), Is.False);
                Assert.That(FieldTypeHelper.SequenceEquals(byteArray, sameLengthDiffValues), Is.False);
            }

            rnd = new Random(0);
            byte[] copyOfTheArray = new byte[50];
            rnd.NextBytes(copyOfTheArray);

            Assert.That(FieldTypeHelper.SequenceEquals(copyOfTheArray, byteArray), Is.True);
            Assert.That(FieldTypeHelper.SequenceEquals(byteArray, copyOfTheArray), Is.True);
        }
    }
}
