using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    /// <summary>
    /// Validates that a client promoted to session owner registers the scenes it loaded outside of
    /// <see cref="NetworkSceneManager"/> so it can synchronize newly joining clients.
    /// Runs against the DAHost by default and against the live CMB service when USE_CMB_SERVICE is set.
    /// </summary>
    internal class SessionOwnerPromotionSceneTests : NetcodeIntegrationTest
    {
        private const string k_LocallyLoadedScene = "EmptyScene";
        protected override int NumberOfClients => 2;

        private Scene m_OriginalActiveScene;
        private Scene m_LocallyLoadedScene;

        public SessionOwnerPromotionSceneTests() : base(HostOrServer.DAHost) { }

        protected override void OnOneTimeSetup()
        {
            m_OriginalActiveScene = SceneManager.GetActiveScene();
            base.OnOneTimeSetup();
        }

        protected override IEnumerator OnTearDown()
        {
            SceneManager.SetActiveScene(m_OriginalActiveScene);
            if (m_LocallyLoadedScene.IsValid() && m_LocallyLoadedScene.isLoaded)
            {
                var scene = m_LocallyLoadedScene;
                SceneManager.UnloadSceneAsync(scene);
                yield return WaitForConditionOrTimeOut(() => !scene.isLoaded);
            }
            yield return base.OnTearDown();
        }

        private NetworkManager m_InitialSessionOwner;

        /// <summary>
        /// Avoids <see cref="NetcodeIntegrationTest.GetAuthorityNetworkManager"/> since it fails the test when
        /// invoked before a new session owner has been promoted.
        /// </summary>
        private bool IsNewSessionOwnerPromoted(StringBuilder errorLog)
        {
            // Have to do our own loop looking for a session owner as GetAuthorityNetworkManager will fail the test if no session owner is found.
            // It is expected that no session owner will be found for a frame or two during session owner promotion.
            NetworkManager sessionOwner = null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.LocalClient.IsSessionOwner)
                {
                    sessionOwner = networkManager;
                    break;
                }
            }

            if (sessionOwner == null || sessionOwner == m_InitialSessionOwner)
            {
                errorLog.AppendLine("No new session owner has been promoted.");
                return false;
            }

            var allClientsSynchronized = true;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.CurrentSessionOwner != sessionOwner.LocalClientId)
                {
                    errorLog.AppendLine($"[Client-{networkManager.LocalClientId}] Incorrect {nameof(NetworkManager.CurrentSessionOwner)}! Expected: {sessionOwner.LocalClientId}. Has: {networkManager.CurrentSessionOwner}.");
                    allClientsSynchronized = false;
                }
            }
            return allClientsSynchronized;
        }

        /// <summary>
        /// Loads and activates a scene without using <see cref="NetworkSceneManager"/>, promotes a new session owner,
        /// and then verifies the promoted session owner can synchronize late joining clients.
        /// </summary>
        [UnityTest]
        public IEnumerator PromotedSessionOwnerSynchronizesLocallyLoadedActiveScene()
        {
            // Load and activate a scene outside of NetworkSceneManager after the session has started,
            // so no NetworkManager instance has a network scene handle registered for it.
            var loadOperation = SceneManager.LoadSceneAsync(k_LocallyLoadedScene, LoadSceneMode.Additive);
            Assert.NotNull(loadOperation, "Failed to load test scene!");

            yield return WaitForConditionOrTimeOut(() => loadOperation.isDone);
            AssertOnTimeout($"Timed out waiting for {k_LocallyLoadedScene} to load!");

            // Set the newly loaded scene as the active scene
            // Now all clients have this unregistered scene as their active scene.
            m_LocallyLoadedScene = SceneManager.GetSceneByName(k_LocallyLoadedScene);
            SceneManager.SetActiveScene(m_LocallyLoadedScene);

            // Now all clients have this unregistered scene as their active scene.
            m_InitialSessionOwner = GetAuthorityNetworkManager();

            // Sanity check: Double-check the newly loaded scene is unregistered on all clients
            foreach (var networkManager in m_NetworkManagers)
            {
                var isSceneRegistered = networkManager.SceneManager.ClientSceneHandleToServerSceneHandle.ContainsKey(m_LocallyLoadedScene.handle);
                Assert.That(isSceneRegistered, Is.False, $"[Client-{networkManager.LocalClientId}] Scene loaded by the UnityEngine SceneManager should not have been registered!");
            }

            if (m_UseCmbService)
            {
                // the CMB service promotes a client when the current session owner disconnects
                yield return StopOneClient(m_InitialSessionOwner);
            }
            else
            {
                // The DAHost can manually promote another client as the session owner.
                m_InitialSessionOwner.PromoteSessionOwner(GetNonAuthorityNetworkManager().LocalClientId);
            }

            yield return WaitForConditionOrTimeOut(IsNewSessionOwnerPromoted);
            AssertOnTimeout("Session owner promotion failed!");

            // Connect a late-joining client to verify the new session owner handles the previously unregistered scene.
            yield return CreateAndStartNewClient();
        }
    }
}
