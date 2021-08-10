using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class FixedAllocatorTest
    {
        [Test]
        public void SimpleTest()
        {
            int pos;

            var allocator = new IndexAllocator(20000, 200);
            allocator.DebugDisplay();

            // allocate 20 bytes
            Assert.IsTrue(allocator.Allocate(0, 20, out pos));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // can't ask for negative amount of memory
            Assert.IsFalse(allocator.Allocate(1, -20, out pos));
            Assert.IsTrue(allocator.Verify());

            // can't ask for deallocation of negative index
            Assert.IsFalse(allocator.Deallocate(-1));
            Assert.IsTrue(allocator.Verify());

            // can't ask for the same index twice
            Assert.IsFalse(allocator.Allocate(0, 20, out pos));
            Assert.IsTrue(allocator.Verify());

            // allocate another 20 bytes
            Assert.IsTrue(allocator.Allocate(1, 20, out pos));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // allocate a third 20 bytes
            Assert.IsTrue(allocator.Allocate(2, 20, out pos));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // deallocate 0
            Assert.IsTrue(allocator.Deallocate(0));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // deallocate 1
            allocator.Deallocate(1);
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // deallocate 2
            allocator.Deallocate(2);
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // allocate 50 bytes
            Assert.IsTrue(allocator.Allocate(0, 50, out pos));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // allocate another 50 bytes
            Assert.IsTrue(allocator.Allocate(1, 50, out pos));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // allocate a third 50 bytes
            Assert.IsTrue(allocator.Allocate(2, 50, out pos));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // deallocate 1, a block in the middle this time
            allocator.Deallocate(1);
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            // allocate a smaller one in its place
            allocator.Allocate(1, 25, out pos);
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());
        }

        [Test]
        public void ReuseTest()
        {
            int count = 100;
            bool[] used = new bool[count];
            int[] pos = new int[count];
            int iterations = 10000;

            var allocator = new IndexAllocator(20000, 200);

            for (int i = 0; i < iterations; i++)
            {
                int index = Random.Range(0, count);
                if (used[index])
                {
                    Assert.IsTrue(allocator.Deallocate(index));
                    used[index] = false;
                }
                else
                {
                    int position;
                    int length = 10 * Random.Range(1, 10);
                    Assert.IsTrue(allocator.Allocate(index, length, out position));
                    pos[index] = position;
                    used[index] = true;
                }
                Assert.IsTrue(allocator.Verify());
            }
            allocator.DebugDisplay();
        }

    }
}
