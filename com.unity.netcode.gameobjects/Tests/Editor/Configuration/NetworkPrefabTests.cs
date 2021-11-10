using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class NetworkPrefabTests
    {
        [Test]
        public void OnAfterDeserialize_PrefabOverride_SetsPrefabField()
        {
            const string prefabName = "Correct Prefab";
            var np = new NetworkPrefab
            {
                Prefab = new GameObject { name = "Prefab to overwrite" },
#pragma warning disable 618
                SourcePrefabToOverride = new GameObject { name = prefabName },
#pragma warning restore 618
                Override = NetworkPrefabOverride.Prefab
            };

            np.OnAfterDeserialize();

            Assert.AreEqual(prefabName, np.Prefab.name);
        }

        // Should return false because if you don't have an override you need the base prefab set
        [Test]
        public void IsValid_NoPrefabSet_ReturnsFalse()
        {
            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = null
            };

            Assert.False(np.Validate());
        }

        // Should return false because network prefabs must be network objects
        [Test]
        public void IsValid_PrefabNotNetworkObject_ReturnsFalse()
        {
            var prefab = new GameObject();
            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = prefab
            };

            Assert.False(np.Validate());
        }

        // Should return false because we don't support child-network objects
        [Test]
        public void IsValid_PrefabHasChildNetworkObjects_ReturnsFalse()
        {
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>();
            var child = new GameObject();
            child.transform.parent = prefab.transform;
            child.AddComponent<NetworkObject>();

            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = prefab
            };

            Assert.False(np.Validate());
        }

        // No override, normal Network Object prefab
        [Test]
        public void IsValid_PrefabIsNetworkObject_ReturnsTrue()
        {
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>();

            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = prefab
            };

            Assert.True(np.Validate());
        }

        [Test]
        public void IsValid_HashOverrideSourceIsZero_ReturnsFalse()
        {
            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Hash,
                SourceHashToOverride = 0
            };

            Assert.False(np.Validate());
        }

        [Test]
        public void IsValid_PrefabOverrideSourceIsNotNetworkObject_ReturnsFalse()
        {
            GameObject prefab = new GameObject();
            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                Prefab = prefab
            };

            Assert.False(np.Validate());
        }

        [Test]
        public void IsValid_HashOverrideTargetIsNull_ReturnsFalse()
        {
            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Hash,
                SourceHashToOverride = 1,
                OverridingTargetPrefab = null
            };

            Assert.False(np.Validate());
        }

        [Test]
        public void IsValid_PrefabOverrideTargetIsNull_ReturnsFalse()
        {
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>();

            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                Prefab = prefab,
                OverridingTargetPrefab = null
            };

            Assert.False(np.Validate());
        }

        [Test]
        public void IsValid_HashOverrideTargetIsNotNetworkObject_ReturnsFalse()
        {
            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Hash,
                SourceHashToOverride = 1,
                OverridingTargetPrefab = new GameObject()
            };

            Assert.False(np.Validate());
        }

        [Test]
        public void IsValid_PrefabOverrideTargetIsNotNetworkObject_ReturnsFalse()
        {
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>();

            NetworkPrefab np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                Prefab = prefab,
                OverridingTargetPrefab = new GameObject()
            };

            Assert.False(np.Validate());
        }

        [Test]
        public void GetSourcePrefabHash_ReturnsGlobalIdHash()
        {
            const int hash = 5;
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>().GlobalObjectIdHash = hash;

            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = prefab
            };

            Assert.AreEqual(hash, np.GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_NoPrefabSet_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => new NetworkPrefab().GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_PrefabNotNetworkObject_ThrowsInvalidOperation()
        {
            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = new GameObject()
            };
            Assert.Throws<InvalidOperationException>(() => np.GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_PrefabOverride_ReturnsGlobalIdHash()
        {
            const int hash = 5;
            var prefab = new GameObject();
            prefab.AddComponent<NetworkObject>().GlobalObjectIdHash = hash;

            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                Prefab = prefab
            };

            Assert.AreEqual(hash, np.GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_PrefabOverrideSourceIsNull_ThrowsInvalidOperation()
        {
            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                Prefab = null
            };
            Assert.Throws<InvalidOperationException>(() => np.GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_PrefabOverrideSourceNotNetworkObject_ThrowsInvalidOperation()
        {
            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                Prefab = new GameObject()
            };
            Assert.Throws<InvalidOperationException>(() => np.GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_HashOverride_ReturnsGlobalIdHash()
        {
            const int hash = 5;

            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Hash,
                SourceHashToOverride = hash
            };

            Assert.AreEqual(hash, np.GetSourcePrefabHash());
        }

        [Test]
        public void GetSourcePrefabHash_HashOverrideIsZero_ThrowsInvalidOperation()
        {
            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Hash,
                SourceHashToOverride = 0
            };
            Assert.Throws<InvalidOperationException>(() => np.GetSourcePrefabHash());
        }

        [Test]
        public void GetTargetPrefab_NoOverride_ReturnsPrefab()
        {
            var prefab = new GameObject();

            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = prefab
            };

            Assert.AreEqual(prefab, np.GetTargetPrefab());
        }

        [Test]
        public void GetTargetPrefab_PrefabOverride_ReturnsTargetPrefab()
        {
            var prefab = new GameObject();

            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Prefab,
                OverridingTargetPrefab = prefab
            };

            Assert.AreEqual(prefab, np.GetTargetPrefab());
        }

        [Test]
        public void GetTargetPrefab_HashOverride_ReturnsTargetPrefab()
        {
            var prefab = new GameObject();

            var np = new NetworkPrefab
            {
                Override = NetworkPrefabOverride.Hash,
                OverridingTargetPrefab = prefab
            };

            Assert.AreEqual(prefab, np.GetTargetPrefab());
        }
    }
}
