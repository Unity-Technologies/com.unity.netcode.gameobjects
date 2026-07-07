using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Netcode.GameObjects.EditorTests
{
    internal class AddressableSceneRegistryTests
    {
        [Test]
        public void RegisterReturnsStableHash()
        {
            // Passing null for the NetworkSceneManager skips the build-settings collision check,
            // which is the only thing the registry uses the manager for.
            var registry = new AddressableSceneRegistry(null);
            const string address = "Assets/Scenes/AddressableScene.unity";

            var hash = registry.Register(address);

            Assert.That(hash, Is.Not.EqualTo(0));
            // Hash must match the XXHash of the address so the value is identical across peers.
            Assert.That(hash, Is.EqualTo(AddressableSceneRegistry.HashFromAddress(address)));
            // Registering the same address again returns the same hash (idempotent).
            Assert.That(registry.Register(address), Is.EqualTo(hash));
        }

        [Test]
        public void RoundTripAddressAndHash()
        {
            var registry = new AddressableSceneRegistry(null);
            const string address = "MyAddressableSceneKey";

            var hash = registry.Register(address);

            Assert.That(registry.IsAddressableScene(hash), Is.True);
            Assert.That(registry.IsAddressableScene(address), Is.True);

            Assert.That(registry.TryGetAddress(hash, out var resolvedAddress), Is.True);
            Assert.That(resolvedAddress, Is.EqualTo(address));

            Assert.That(registry.TryGetHash(address, out var resolvedHash), Is.True);
            Assert.That(resolvedHash, Is.EqualTo(hash));
        }

        [Test]
        public void UnregisteredHashIsNotAddressable()
        {
            var registry = new AddressableSceneRegistry(null);
            Assert.That(registry.IsAddressableScene(12345u), Is.False);
            Assert.That(registry.TryGetAddress(12345u, out _), Is.False);
        }

        [Test]
        public void NullOrEmptyAddressDoesNotRegister()
        {
            var registry = new AddressableSceneRegistry(null);
            LogAssert.ignoreFailingMessages = true;
            Assert.That(registry.Register(string.Empty), Is.EqualTo(0));
            Assert.That(registry.IsAddressableScene(string.Empty), Is.False);
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void ClearRemovesAllEntries()
        {
            var registry = new AddressableSceneRegistry(null);
            var hash = registry.Register("SomeScene");
            Assert.That(registry.IsAddressableScene(hash), Is.True);

            registry.Clear();
            Assert.That(registry.IsAddressableScene(hash), Is.False);
        }
    }
}
