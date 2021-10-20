using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// For validating basic functionality of a SceneReference
    /// This will:
    /// - load the master scene from a scene reference group
    /// - enter into host mode
    /// - send some RPCs to do a very fundamental "working" validation check
    /// - shutdown the NetworkManager
    /// - destroy any remaining NetworkObjects
    /// </summary>
    public class SceneReferenceValidationState : SceneAwareSmokeTestState
    {
        protected string m_MasterSceneName;
        protected List<string> m_SupportScenes;

        private Scene m_MasterScene;
        private Scene m_OriginalActiveScene;
        private NetworkManager m_NetworkManager;

        public void SetScenes(List<string> scenes)
        {
            m_SceneIsLoading = false;
            m_MasterScene = new Scene();
            m_NetworkManager = null;
            m_MasterSceneName = scenes[0];
            m_SupportScenes = new List<string>(scenes.Count > 1 ? new List<string>() : scenes.GetRange(1, scenes.Count - 1));
        }

        public override bool OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            if (sceneLoaded.name == m_MasterSceneName)
            {
                m_OriginalActiveScene = SceneManager.GetActiveScene();
                m_MasterScene = sceneLoaded;
                SceneManager.SetActiveScene(sceneLoaded);
                SceneIsProcessed = true;

                // Find NetworkManager
                var networkManagers = Object.FindObjectsOfType<NetworkManager>();
                Assert.That(networkManagers.Length >= 1);
                m_NetworkManager = networkManagers.First();
                if (networkManagers.Length > 1)
                {
                    Debug.Break();
                }
                m_NetworkManager.DontDestroy = false;
                SceneManager.MoveGameObjectToScene(m_NetworkManager.gameObject, m_MasterScene);

                // Connect as Host
                Assert.That(m_NetworkManager.StartHost() == true);

                return base.OnSceneLoaded(sceneLoaded, loadMode);
            }
            return false;
        }

        private bool m_SceneIsLoading = false;

        protected override IEnumerator OnStartState()
        {
            if (!m_SceneIsLoading)
            {
                Debug.Log($"Loading scene {m_MasterSceneName}");
                // Load Master Scene
                Assert.That(StartLoadingScene(m_MasterSceneName) == true);
                m_SceneIsLoading = true;
            }

            while (!SceneIsProcessed)
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return base.OnStartState();
        }

        private bool m_ReceivedClientRpc;
        private bool m_ReceivedServerRpc;

        [ClientRpc]
        private void OnTestHostConnectedClientRpc(ClientRpcParams clientRpcParams = default)
        {
            m_ReceivedClientRpc = true;
            OnTestHostConnectedServerRpc();
        }

        [ServerRpc]
        private void OnTestHostConnectedServerRpc()
        {
            m_ReceivedServerRpc = true;
        }

        protected override bool OnProcessState()
        {

            // Validate Connected
            var clientRpcParams = new ClientRpcParams();
            clientRpcParams.Send.TargetClientIds = new List<ulong>() { m_NetworkManager.LocalClientId };
            OnTestHostConnectedClientRpc(clientRpcParams);
            var timeOutMarker = Time.realtimeSinceStartup + 10.0f;
            var timedOut = false;
            while (!m_ReceivedClientRpc && !m_ReceivedServerRpc)
            {
                if (timeOutMarker < Time.realtimeSinceStartup)
                {
                    timedOut = true;
                    break;
                }
            }
            Assert.That(timedOut == false);

            return base.OnProcessState();
        }

        protected override bool OnSceneUnloaded(Scene sceneUnloaded)
        {
            SceneIsProcessed = sceneUnloaded == m_MasterScene;
            if (SceneIsProcessed && m_OriginalActiveScene.isLoaded)
            {
                m_NetworkManager.Shutdown();
                SceneManager.SetActiveScene(m_OriginalActiveScene);
            }
            return base.OnSceneUnloaded(sceneUnloaded);
        }

        protected override IEnumerator OnStopState()
        {
            if (m_MasterScene.IsValid() && m_MasterScene.isLoaded)
            {
                Debug.Log($"Unloading scene {m_MasterSceneName}");
                // Unload Scene
                Assert.That(StartUnloadingScene(m_MasterScene) == true);
            }

            while (!SceneIsProcessed)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Now clean up any remaining NetworkObjects that might have been
            // instantiated outside of the active scene (i.e. DDOL scene)
            var remainingNetworkObjects = Object.FindObjectsOfType<NetworkObject>();
            foreach (var networkObject in remainingNetworkObjects)
            {
                Object.DestroyImmediate(networkObject.gameObject);
            }

            // Done
            yield return base.OnStopState();
        }
    }
}
