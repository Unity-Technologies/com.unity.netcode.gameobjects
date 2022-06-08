using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Netcode;

namespace TestProject.EditorTests
{
    public class NetworkPrefabGlobalObjectIdHashTests
    {
        private Scene m_TestScene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            const string scenePath = "Assets/Tests/Editor/GlobalObjectIdHash/" + nameof(NetworkPrefabGlobalObjectIdHashTests) + ".unity";
            m_TestScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            Assert.That(m_TestScene.isLoaded, Is.True);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (m_TestScene.isLoaded)
            {
                Assert.True(EditorSceneManager.CloseScene(m_TestScene, true));
            }
            yield return null;
        }

        // TODO(jesseo): Not really sure what this test is supposed to be verifying... but maybe it can be done more programmatically now?
        // Issue: Shouldn't need a static checked in scene - Shouldn't even need NetworkManager
        // Issue: The saved scene is already only unique prefabs - are we supposed to be testing that duplicates are culled?
        // Issue: We're iterating over a dictionary and verifying that each key is unique... Dictionary already guarantees this?
        [Test]
        public void VerifyUniquenessOfNetworkPrefabs()
        {
            Assert.That(m_TestScene.isLoaded, Is.True);
            var networkManagerObject = GameObject.Find("[NetworkManager]");
            var networkManager = networkManagerObject.GetComponent<NetworkManager>();
            Assert.That(networkManager, Is.Not.Null);
            Assert.That(networkManager.NetworkConfig, Is.Not.Null);
            Assert.That(networkManager.NetworkConfig.Prefabs, Is.Not.Null);
            networkManager.NetworkConfig.Prefabs.Initialize(false);
            Assert.That(networkManager.NetworkConfig.Prefabs.Prefabs.Count, Is.GreaterThan(1));

            var hashSet = new HashSet<uint>();
            foreach (var networkPrefab in networkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks)
            {
                var idHash = networkPrefab.Key;
                Assert.That(idHash, Is.Not.EqualTo(0));
                Assert.That(hashSet.Contains(idHash), Is.False);
                hashSet.Add(idHash);
            }
        }
    }
}
