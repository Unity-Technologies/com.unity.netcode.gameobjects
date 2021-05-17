using NUnit.Framework;

namespace MLAPI.EditorTests
{
    public class FixedAllocatorTest
    {
        [Test]
        public void TestAllocator()
        {
            int pos;

            IndexAllocator allocator = new IndexAllocator(20000);
            allocator.DebugDisplay();

            // allocate 20 bytes
            Assert.IsTrue(allocator.Allocate(0, 20, out pos));
            allocator.DebugDisplay();
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

            Assert.IsTrue(allocator.Deallocate(0));
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            allocator.Deallocate(1);
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            allocator.Deallocate(2);
            allocator.DebugDisplay();
            Assert.IsTrue(allocator.Verify());

            Assert.IsTrue(true);
        }
    }
}
