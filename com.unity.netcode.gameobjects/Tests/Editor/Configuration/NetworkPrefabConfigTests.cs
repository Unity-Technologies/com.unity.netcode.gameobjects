using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class NetworkPrefabConfigTests
    {
        [SetUp]
        public void Setup()
        {
            Debug.unityLogger.logEnabled = false;
        }

        [TearDown]
        public void Teardown()
        {
            Debug.unityLogger.logEnabled = true;
        }

        // Verify both source->target and target->source overrides are registered. Golden Path.
        [Test]
        public void InitializeOverrides_AddsHashLookups()
        {
            const int firstSourceHash = 10;
            const int firstTargetHash = 1;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = firstTargetHash;
            const int secondSourceHash = 20;
            const int secondTargetHash = 2;
            var go2 = new GameObject();
            go2.AddComponent<NetworkObject>().GlobalObjectIdHash = secondTargetHash;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = firstSourceHash,
                        OverridingTargetPrefab = go
                    },
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = secondSourceHash,
                        OverridingTargetPrefab = go2
                    },
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);
            npc.TryGetSourcePrefabHash(firstTargetHash, out var firstOrigHash);
            npc.TryGetSourcePrefabHash(secondTargetHash, out var secondOrigHash);

            Assert.AreEqual(firstSourceHash, firstOrigHash);
            Assert.AreEqual(secondSourceHash, secondOrigHash);
        }

        [Test]
        public void InitializeOverrides_NullNetworkPrefabEntry_DontThrow()
        {
            var go = new GameObject();
            go.AddComponent<NetworkObject>();
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    null,
                    new() { Prefab = go}
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, false);
        }

        [Test]
        public void InitializeOverrides_DuplicateSourceHashes_RemovesDupe()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = 1;
            var go2 = new GameObject();
            go2.AddComponent<NetworkObject>().GlobalObjectIdHash = 2;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = 1,
                        OverridingTargetPrefab = go
                    },
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = 1,
                        OverridingTargetPrefab = go2
                    },
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);

            Assert.That(networkConfig.NetworkPrefabs, Has.Count.EqualTo(1));
            Assert.That(networkConfig.NetworkPrefabs[0].OverridingTargetPrefab, Is.SameAs(go));
        }

        [Test]
        public void InitializeOverrides_NoOverride_RegisterPrefabHash()
        {
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = 1;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.None,
                        Prefab = go
                    },
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);

            CollectionAssert.Contains(npc.GetRegisteredPrefabHashCodes(), 1);
        }

        [Test]
        public void InitializeOverrides_MultipleEntriesSameTarget_RemovesDupe()
        {
            LogAssert.ignoreFailingMessages = true;

            const int targetHash = 1;
            const int firstSourceHash = 2;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = targetHash;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = firstSourceHash,
                        OverridingTargetPrefab = go
                    },
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = 3,
                        OverridingTargetPrefab = go
                    },
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);
            bool foundSource = npc.TryGetSourcePrefabHash(targetHash, out var orig);

            Assert.That(networkConfig.NetworkPrefabs, Has.Count.EqualTo(1));
            Assert.IsTrue(foundSource);
            Assert.AreEqual(firstSourceHash, orig);
        }

        [Test]
        public void InitializeOverrides_RemoveBadEntriesDisabled_DoesntRemove()
        {
            LogAssert.ignoreFailingMessages = true;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = 1;
            var go2 = new GameObject();
            go2.AddComponent<NetworkObject>().GlobalObjectIdHash = 2;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = 1,
                        OverridingTargetPrefab = go
                    },
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = 1,
                        OverridingTargetPrefab = go2
                    },
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, false);

            Assert.That(networkConfig.NetworkPrefabs, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetPrefab_NullParam_ThrowsArgumentNull()
        {
            var npc = new NetworkPrefabConfig();

            Assert.Throws<ArgumentNullException>(() => npc.GetPrefab(null));
        }

        [Test]
        public void GetPrefab_NotANetworkPrefab_ThrowsInvalidOperation()
        {
            var npc = new NetworkPrefabConfig();

            Assert.Throws<InvalidOperationException>(() => npc.GetPrefab(new GameObject()));
        }

        [Test]
        public void GetPrefab_ObjectNotRegistered_ThrowsInvalidOperation()
        {
            var go = new GameObject();
            go.AddComponent<NetworkObject>();
            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(new NetworkConfig(), false);

            Assert.Throws<InvalidOperationException>(() => npc.GetPrefab(go));
        }

        [Test]
        public void TryGetPrefab_NoOverride_ReturnsOriginalPrefab()
        {
            const int prefabHash = 1;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = prefabHash;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.None,
                        Prefab = go
                    },
                }
            };
            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);

            bool found = npc.TryGetPrefab(prefabHash, out GameObject prefab);

            Assert.IsTrue(found);
            Assert.AreEqual(prefabHash, prefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        [Test]
        public void TryGetPrefab_HashOverride_ReturnsTargetPrefab()
        {
            const int sourceHash = 1;
            const int overrideHash = 2;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = overrideHash;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = sourceHash,
                        OverridingTargetPrefab = go
                    },
                }
            };
            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);

            bool found = npc.TryGetPrefab(sourceHash, out GameObject prefab);

            Assert.IsTrue(found);
            Assert.AreEqual(overrideHash, prefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        [Test]
        public void TryGetPrefab_PrefabOverride_ReturnsTargetPrefab()
        {
            const int sourceHash = 1;
            const int overrideHash = 2;
            var sourceGo = new GameObject();
            sourceGo.AddComponent<NetworkObject>().GlobalObjectIdHash = sourceHash;
            var targetGo = new GameObject();
            targetGo.AddComponent<NetworkObject>().GlobalObjectIdHash = overrideHash;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Prefab,
                        Prefab = sourceGo,
                        OverridingTargetPrefab = targetGo
                    },
                }
            };
            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);

            bool found = npc.TryGetPrefab(sourceHash, out GameObject prefab);

            Assert.IsTrue(found);
            Assert.AreEqual(overrideHash, prefab.GetComponent<NetworkObject>().GlobalObjectIdHash);
        }

        [Test]
        public void TryGetPrefab_NoMatch_ReturnsFailure()
        {
            var npc = new NetworkPrefabConfig();

            bool found = npc.TryGetPrefab(1, out GameObject prefab);

            Assert.IsFalse(found);
            Assert.IsNull(prefab);
        }

        [Test]
        public void TryGetSourcePrefabHash_ReturnsSourceHash()
        {
            const int sourceHash = 1;
            const int overrideHash = 2;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = overrideHash;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = sourceHash,
                        OverridingTargetPrefab = go
                    },
                }
            };
            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);

            var found = npc.TryGetSourcePrefabHash(overrideHash, out uint foundHash);

            Assert.IsTrue(found);
            Assert.AreEqual(sourceHash, foundHash);
        }

        [Test]
        public void TryGetSourcePrefabHash_NoMatchingHash_ReturnsFailure()
        {
            var npc = new NetworkPrefabConfig();

            var found = npc.TryGetSourcePrefabHash(1, out uint foundHash);

            Assert.IsFalse(found);
            Assert.AreEqual(0, foundHash);
        }

        [Test]
        public void GetRegisteredPrefabHashCodes_NoneRegistered_ReturnsEmptyList()
        {
            var npc = new NetworkPrefabConfig();

            CollectionAssert.IsEmpty(npc.GetRegisteredPrefabHashCodes());
        }

        [Test]
        public void GetRegisteredPrefabHashCodes_ReturnsSourceHashes()
        {
            const int firstSourceHash = 10;
            var go = new GameObject();
            go.AddComponent<NetworkObject>().GlobalObjectIdHash = 1;
            const int secondSourceHash = 20;
            var go2 = new GameObject();
            go2.AddComponent<NetworkObject>().GlobalObjectIdHash = 2;
            var networkConfig = new NetworkConfig
            {
                NetworkPrefabs = new List<NetworkPrefab>
                {
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = firstSourceHash,
                        OverridingTargetPrefab = go
                    },
                    new()
                    {
                        Override = NetworkPrefabOverride.Hash,
                        SourceHashToOverride = secondSourceHash,
                        OverridingTargetPrefab = go2
                    },
                }
            };

            var npc = new NetworkPrefabConfig();
            npc.InitializeOverrides(networkConfig, true);
            var registeredHashes = npc.GetRegisteredPrefabHashCodes();

            CollectionAssert.AreEqual(new[]{firstSourceHash, secondSourceHash}, registeredHashes);
        }
    }
}
