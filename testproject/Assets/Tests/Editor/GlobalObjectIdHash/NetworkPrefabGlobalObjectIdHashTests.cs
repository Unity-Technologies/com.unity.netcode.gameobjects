using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
    }
}
