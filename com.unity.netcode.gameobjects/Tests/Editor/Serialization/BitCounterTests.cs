using NUnit.Framework;
using Unity.Multiplayer.Netcode;

namespace Unity.Netcode.EditorTests
{
    public class BitCounterTests
    {
        [Test]
        public void TestBitCounter64Bits()
        {
            ulong value = 0;
            // 0 is a special case. All values are considered at least 1 bit.
            Assert.AreEqual(1, BitCounter.GetUsedBitCount(value));

            for (int i = 0; i < 64; ++i)
            {
                value = 1UL << i;
                Assert.AreEqual(i + 1, BitCounter.GetUsedBitCount(value));
            }
        }

        [Test]
        public void TestBitCounter32Bits()
        {
            uint value = 0;
            // 0 is a special case. All values are considered at least 1 bit.
            Assert.AreEqual(1, BitCounter.GetUsedBitCount(value));

            for (int i = 0; i < 32; ++i)
            {
                value = 1U << i;
                Assert.AreEqual(i + 1, BitCounter.GetUsedBitCount(value));
            }
        }
        [Test]
        public void TestByteCounter64Bits()
        {
            ulong value = 0;
            // 0 is a special case. All values are considered at least 1 byte.
            Assert.AreEqual(1, BitCounter.GetUsedByteCount(value));

            for (int i = 0; i < 64; ++i)
            {
                value = 1UL << i;
                Assert.AreEqual(i / 8 + 1, BitCounter.GetUsedByteCount(value));
            }
        }

        [Test]
        public void TestByteCounter32Bits()
        {
            uint value = 0;
            // 0 is a special case. All values are considered at least 1 byte.
            Assert.AreEqual(1, BitCounter.GetUsedByteCount(value));

            for (int i = 0; i < 32; ++i)
            {
                value = 1U << i;
                Assert.AreEqual(i / 8 + 1, BitCounter.GetUsedByteCount(value));
            }
        }
    }
}
