using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Covers <see cref="TransformHandleAllocator"/>, in particular that a freed handle is held long enough
    /// that a state update still in flight for the instance that owned it cannot be applied to whichever
    /// instance picks it up next.
    /// </summary>
    // These tests do not need to run against the Rust server.
    [IgnoreIfServiceEnvironmentVariableSet]
    internal class TransformHandleAllocatorTests
    {
        /// <summary>
        /// Has to match the allocator's hold duration.
        /// </summary>
        private const double k_RecycleDelaySeconds = 5.0;

        [Test]
        public void AllocatesDenselyAndSkipsTheInvalidHandle()
        {
            var allocator = new TransformHandleAllocator();

            for (ushort expected = 1; expected <= 100; expected++)
            {
                var handle = allocator.Allocate(0.0);
                Assert.AreNotEqual(TransformHandleAllocator.InvalidHandle, handle, "Allocated the reserved invalid handle!");
                Assert.AreEqual(expected, handle, "Handles were not allocated densely!");
            }
        }

        [Test]
        public void ReleasedHandleIsNotReissuedBeforeItsHoldExpires()
        {
            var allocator = new TransformHandleAllocator();

            var first = allocator.Allocate(0.0);
            var second = allocator.Allocate(0.0);
            allocator.Release(first, 0.0);

            // Anything allocated before the hold expires has to be a fresh handle, not the released one.
            for (double time = 0.0; time < k_RecycleDelaySeconds; time += 1.0)
            {
                var handle = allocator.Allocate(time);
                Assert.AreNotEqual(first, handle,
                    $"Released handle {first} was reissued at time {time}, before its hold expired!");
                Assert.AreNotEqual(second, handle, "Reissued a handle that was never released!");
            }
        }

        [Test]
        public void ReleasedHandleIsReissuedOnceItsHoldExpires()
        {
            var allocator = new TransformHandleAllocator();

            var first = allocator.Allocate(0.0);
            allocator.Release(first, 0.0);

            var reissued = allocator.Allocate(k_RecycleDelaySeconds);
            Assert.AreEqual(first, reissued, "A handle held past its delay was not reused, which would leak the handle space!");
        }

        [Test]
        public void ReleasedHandlesAreReissuedInReleaseOrder()
        {
            var allocator = new TransformHandleAllocator();

            var first = allocator.Allocate(0.0);
            var second = allocator.Allocate(0.0);
            var third = allocator.Allocate(0.0);

            // Released at increasing times, so they become reusable in the same order.
            allocator.Release(first, 0.0);
            allocator.Release(second, 1.0);
            allocator.Release(third, 2.0);

            Assert.AreEqual(first, allocator.Allocate(k_RecycleDelaySeconds), "Oldest released handle was not reissued first!");
            Assert.AreEqual(second, allocator.Allocate(k_RecycleDelaySeconds + 1.0), "Handles were not reissued in release order!");
            Assert.AreEqual(third, allocator.Allocate(k_RecycleDelaySeconds + 2.0), "Handles were not reissued in release order!");
        }

        [Test]
        public void ReleasingTheInvalidHandleIsIgnored()
        {
            var allocator = new TransformHandleAllocator();

            allocator.Release(TransformHandleAllocator.InvalidHandle, 0.0);

            // If the invalid handle had been queued it would come back out here.
            Assert.AreEqual(1, allocator.Allocate(k_RecycleDelaySeconds * 2.0), "The reserved invalid handle entered the recycle queue!");
        }

        [Test]
        public void RegisteredHandleResolvesBackToItsInstance()
        {
            var allocator = new TransformHandleAllocator();
            var handle = allocator.Allocate(0.0);

            // The association is what the receiving side uses to route a batched state update. A null instance
            // is enough to prove the table behavior without needing a spawned NetworkObject.
            allocator.Register(handle, null);
            Assert.IsTrue(allocator.TryGet(handle, out _), "A registered handle did not resolve!");
            Assert.AreEqual(1, allocator.GetRegisteredCount());

            allocator.Unregister(handle);
            Assert.IsFalse(allocator.TryGet(handle, out _), "An unregistered handle still resolved!");
            Assert.AreEqual(0, allocator.GetRegisteredCount());
        }

        [Test]
        public void ReleaseAlsoDropsTheRegistration()
        {
            var allocator = new TransformHandleAllocator();
            var handle = allocator.Allocate(0.0);
            allocator.Register(handle, null);

            allocator.Release(handle, 0.0);

            Assert.IsFalse(allocator.TryGet(handle, out _),
                "A released handle still resolved, which would route state updates to a despawned instance!");
        }

        [Test]
        public void ClearResetsTheAllocator()
        {
            var allocator = new TransformHandleAllocator();
            allocator.Allocate(0.0);
            allocator.Allocate(0.0);
            var third = allocator.Allocate(0.0);
            allocator.Register(third, null);

            allocator.Clear();

            Assert.AreEqual(0, allocator.GetRegisteredCount(), "Clear left registrations behind!");
            Assert.AreEqual(1, allocator.Allocate(0.0), "Clear did not reset the handle sequence, so a new session would not start from the beginning!");
        }
    }
}
