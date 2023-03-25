using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.EditorTests
{
    public class NetworkObjectGlobalObjectIdHashTests
    {
        private Scene m_TestScene;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            const string scenePath = "Assets/Tests/Editor/GlobalObjectIdHash/" + nameof(NetworkObjectGlobalObjectIdHashTests) + ".unity";

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

        [Test]
        public void VerifyUniquenessOfNetworkObjects()
        {
            var hashSet = new HashSet<uint>();
            foreach (var rootObject in m_TestScene.GetRootGameObjects())
            {
                foreach (var networkObject in rootObject.GetComponentsInChildren<NetworkObject>())
                {
                    var idHash = networkObject.GlobalObjectIdHash;
                    Assert.That(hashSet.Contains(idHash), Is.False);
                    hashSet.Add(idHash);
                }
            }
        }
    }
}
