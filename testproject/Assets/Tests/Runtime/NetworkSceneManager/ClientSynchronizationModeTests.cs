using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Validates that <see cref="NetworkSceneManager.ClientSynchronizationMode"/> set to <see cref="LoadSceneMode.Additive"/>
    /// Will synchronize clients properly when scenes are preloaded and when the server changes the currently active scene
    /// prior to a client joining.
    /// </summary>
    /// <remarks>
    /// Note: If a client does not preload a scene prior to the server setting it as the active scene then the client(s) and the
    /// server will end up sharing the same scene. More info: <see cref="IntegrationTestSceneHandler.GetSceneFromLoadedScenes"/>
    /// </remarks>
    [TestFixture(ServerPreloadStates.NoPreloadOnServer)]
    [TestFixture(ServerPreloadStates.PreloadOnServer)]
    public class ClientSynchronizationModeTests : NetcodeIntegrationTest
    {
        // Two scenes with different in-scene placed NetworkObjects and one empty scene used to test active scene switching and client synchronization.
        private List<string> m_TestScenes = new List<string>() { "InSceneNetworkObject", "GenericInScenePlacedObject", "EmptyScene1" };

        protected override int NumberOfClients => 0;

        public enum ServerPreloadStates
        {
            PreloadOnServer,
            NoPreloadOnServer
        }

        public enum ClientPreloadStates
        {
            PreloadOnClient,
            NoPreloadOnClient
        }

        public enum ActiveSceneStates
        {
            DefaultActiveScene,
            SwitchActiveScene,
        }

        private ServerPreloadStates m_ServerPreloadState;
        private List<Scene> m_ServerLoadedScenes = new List<Scene>();
        private List<Scene> m_TempClientPreLoadedScenes = new List<Scene>();

        private Dictionary<ulong, List<Scene>> m_ClientScenesLoaded = new Dictionary<ulong, List<Scene>>();

        private readonly List<NetworkObject> m_ObjectsInScenes = new();


        public ClientSynchronizationModeTests(ServerPreloadStates serverPreloadStates)
        {
            m_ServerPreloadState = serverPreloadStates;
        }

        // TODO: [CmbServiceTests] Adapt to run with the service
        protected override bool UseCMBService()
        {
            return false;
        }

        protected override IEnumerator OnSetup()
        {
            m_TempClientPreLoadedScenes.Clear();
            m_ServerLoadedScenes.Clear();
            m_ObjectsInScenes.Clear();
            if (m_ServerPreloadState == ServerPreloadStates.PreloadOnServer)
            {
                yield return LoadScenesOnServer();
            }
            yield return base.OnSetup();
        }

        private IEnumerator LoadScenesOnServer()
        {
            if (m_ServerPreloadState == ServerPreloadStates.PreloadOnServer)
            {
                SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                foreach (var sceneToLoad in m_TestScenes)
                {
                    SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
                }
                yield return WaitForConditionOrTimeOut(AllScenesLoadedOnServer);
                SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                AssertOnTimeout($"[{m_ServerPreloadState}] Timed out waiting for all server-side scenes to be loaded!");
            }
            else
            {
                m_ServerNetworkManager.SceneManager.OnSceneEvent += ServerSide_OnSceneEvent;

                foreach (var sceneToLoad in m_TestScenes)
                {
                    m_ServerNetworkManager.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);
                    yield return WaitForConditionOrTimeOut(() => SceneLoadedOnServer(sceneToLoad));
                    AssertOnTimeout($"[{m_ServerPreloadState}] Timed out waiting for scene {sceneToLoad} to be loaded!");
                }
                m_ServerNetworkManager.SceneManager.OnSceneEvent -= ServerSide_OnSceneEvent;

            }
        }

        private bool SceneLoadedOnServer(string sceneName)
        {
            foreach (var scene in m_ServerLoadedScenes)
            {
                if (scene.name == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool AllScenesLoadedOnServer()
        {
            if (m_ServerLoadedScenes.Count == m_TestScenes.Count)
            {
                foreach (var loadedScene in m_ServerLoadedScenes)
                {
                    if (!m_TestScenes.Contains(loadedScene.name))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private bool AllScenesPreloadedForClient()
        {
            if (m_TempClientPreLoadedScenes.Count == m_TestScenes.Count)
            {
                foreach (var loadedScene in m_TempClientPreLoadedScenes)
                {
                    if (!m_TestScenes.Contains(loadedScene.name))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private bool AllScenesLoadedOnClients()
        {
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (!m_ClientScenesLoaded.ContainsKey(clientNetworkManager.LocalClientId))
                {
                    return false;
                }

                if (m_ClientScenesLoaded[clientNetworkManager.LocalClientId].Count != m_TestScenes.Count)
                {
                    return false;
                }
                foreach (var loadedScene in m_ClientScenesLoaded[clientNetworkManager.LocalClientId])
                {
                    if (!m_TestScenes.Contains(loadedScene.name))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void TrackObjectsInScene(Scene scene)
        {
            foreach (var networkObject in FindObjects.FromSceneByType<NetworkObject>(scene, true))
            {
                if (networkObject.InScenePlaced)
                {
                    m_ObjectsInScenes.Add(networkObject);
                }
            }
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            m_ServerLoadedScenes.Add(scene);
            TrackObjectsInScene(scene);
        }


        protected override IEnumerator OnStartedServerAndClients()
        {
            m_ServerNetworkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
            return base.OnStartedServerAndClients();
        }

        protected override void OnNewClientStarted(NetworkManager networkManager)
        {
            networkManager.SceneManager.OnSceneEvent += ClientSide_OnSceneEvent;
            base.OnNewClientStarted(networkManager);
        }

        /// <summary>
        /// Verifies that both clients and the server will utilize preloaded scenes and that
        /// in-scene placed NetworkObjects synchronize properly if the active scene changes
        /// prior to a client connecting.
        /// </summary>
        /// <remarks>
        /// Notes:
        /// ClientPreloadStates.NoPreloadOnClient: verifies that if no scene that needs to
        /// be synchronized is preloaded the client will load the scene to be synchronized.
        ///
        /// ClientPreloadStates.PreloadOnClient: verifies that if a client has scenes that
        /// will be synchronized preloaded the client will use those scenes as opposed to
        /// loading duplicates.
        /// </remarks>
        [UnityTest]
        public IEnumerator PreloadedScenesTest([Values] ClientPreloadStates clientPreloadStates, [Values] ActiveSceneStates activeSceneState)
        {
            // If we didn't preload the scenes, then load the scenes via NetworkSceneManager
            if (m_ServerPreloadState == ServerPreloadStates.NoPreloadOnServer)
            {
                // m_ServerNetworkManager.SceneManager.OnSceneEvent += ServerSide_OnSceneEvent;
                yield return LoadScenesOnServer();
            }

            // This tests that a change in the active scene will not impact in-scene placed
            // NetworkObject synchronization (if it does then the clients would get a soft
            // synchronization error).
            if (activeSceneState == ActiveSceneStates.SwitchActiveScene)
            {
                SceneManager.SetActiveScene(m_ServerLoadedScenes[2]);
            }

            var authority = GetAuthorityNetworkManager();

            foreach (var networkObject in m_ObjectsInScenes)
            {
                Assert.IsTrue(networkObject.IsSpawned, $"[Client-{authority.LocalClientId}] Server object {networkObject.name}, {networkObject.GlobalObjectIdHash} is not spawned!");
                Assert.AreEqual(networkObject.NetworkManagerOwner, authority, $"[Client-{authority.LocalClientId}] networkObject doesn't belong to the client");
            }

            var objToDisable = m_ObjectsInScenes[0];
            objToDisable.Despawn(false);


            // Late join some clients
            for (int i = 0; i < 1; i++)
            {
                // This tests that clients can have scenes preloaded prior to
                // connecting and will use those scenes for synchronization
                if (clientPreloadStates == ClientPreloadStates.PreloadOnClient)
                {
                    m_TempClientPreLoadedScenes.Clear();
                    m_ObjectsInScenes.Clear();
                    SceneManager.sceneLoaded += PreLoadClient_SceneLoaded;
                    foreach (var sceneToLoad in m_TestScenes)
                    {
                        SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
                    }
                    yield return WaitForConditionOrTimeOut(AllScenesPreloadedForClient);
                    SceneManager.sceneLoaded -= PreLoadClient_SceneLoaded;
                    AssertOnTimeout($"[{clientPreloadStates}] Timed out waiting for client-side scenes to be preloaded!");
                }

                var newClient = CreateNewClient();
                yield return StartClient(newClient);
                AssertOnTimeout($"[Client Instance {i + 1}] Timed out waiting for client to start and connect!");

                yield return WaitForConditionOrTimeOut(AllScenesLoadedOnClients);
                AssertOnTimeout($"[Client-{newClient.LocalClientId}] Timed out waiting for all scenes to be synchronized for new client!");

                if (clientPreloadStates == ClientPreloadStates.PreloadOnClient)
                {
                    foreach (var networkObject in m_ObjectsInScenes)
                    {
                        if (networkObject.GlobalObjectIdHash == objToDisable.GlobalObjectIdHash)
                        {
                            Assert.IsFalse(networkObject.IsSpawned);
                        }
                        else
                        {
                            Assert.IsTrue(networkObject.IsSpawned, $"[Client-{newClient.LocalClientId}] Client side preloaded object {networkObject.name}, {networkObject.GlobalObjectIdHash} is not spawned!");
                        }
                        Assert.AreEqual(networkObject.NetworkManagerOwner, newClient, $"[Client-{newClient.LocalClientId}] networkObject doesn't belong to the client");
                    }
                }
            }
        }


        private void PreLoadClient_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            m_TempClientPreLoadedScenes.Add(scene);
            TrackObjectsInScene(scene);
        }

        private void ClientSide_OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        if (!m_ClientScenesLoaded.ContainsKey(sceneEvent.ClientId))
                        {
                            m_ClientScenesLoaded.Add(sceneEvent.ClientId, new List<Scene>());
                        }
                        m_ClientScenesLoaded[sceneEvent.ClientId].Add(sceneEvent.Scene);
                        break;
                    }
            }
        }
        private void ServerSide_OnSceneEvent(SceneEvent sceneEvent)
        {
            // Filter for server-side only scene events
            if (sceneEvent.ClientId != m_ServerNetworkManager.LocalClientId)
            {
                return;
            }

            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadComplete:
                    {
                        m_ServerLoadedScenes.Add(sceneEvent.Scene);
                        TrackObjectsInScene(sceneEvent.Scene);
                        break;
                    }
            }
        }

        protected override IEnumerator OnTearDown()
        {
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            m_TempClientPreLoadedScenes.Clear();
            m_ClientScenesLoaded.Clear();
            m_ObjectsInScenes.Clear();
            return base.OnTearDown();
        }
    }
}
