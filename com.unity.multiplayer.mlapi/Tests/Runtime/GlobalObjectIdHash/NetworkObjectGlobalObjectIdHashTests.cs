using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkObjectGlobalObjectIdHashTests
    {
        private Scene m_TestScene;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == nameof(NetworkObjectGlobalObjectIdHashTests))
            {
                m_TestScene = scene;
            }
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            var execAssembly = Assembly.GetExecutingAssembly();
            var packagePath = PackageInfo.FindForAssembly(execAssembly).assetPath;
            var scenePath = Path.Combine(packagePath, $"Tests/Runtime/GlobalObjectIdHash/{nameof(NetworkObjectGlobalObjectIdHashTests)}.unity");

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            Assert.That(m_TestScene.isLoaded, Is.True);
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (m_TestScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(m_TestScene);
            }
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
