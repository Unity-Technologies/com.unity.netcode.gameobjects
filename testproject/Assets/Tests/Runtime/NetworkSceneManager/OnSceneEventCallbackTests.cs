using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    public class OnSceneEventCallbackTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private const string k_SceneToLoad = "EmptyScene";
        private const string k_PathToLoad = "Assets/Scenes/EmptyScene.unity";

        public OnSceneEventCallbackTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        private struct ExpectedEvent
        {
            public SceneEvent SceneEvent;
            public string SceneName;
            public string ScenePath;
        }

        private readonly Queue<ExpectedEvent> m_ExpectedEventQueue = new();

        private static int s_NumEventsProcessed;
        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            VerboseDebug($"OnSceneEvent! Type: {sceneEvent.SceneEventType}. for client: {sceneEvent.ClientId}");
            if (m_ExpectedEventQueue.Count > 0)
            {
                var expectedEvent = m_ExpectedEventQueue.Dequeue();

                ValidateEventsAreEqual(expectedEvent.SceneEvent, sceneEvent);

                // Only LoadComplete events have an attached scene
                if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
                {
                    ValidateReceivedScene(expectedEvent, sceneEvent.Scene);
                }
            }
            else
            {
                Assert.Fail($"Received unexpected event at index {s_NumEventsProcessed}: {sceneEvent.SceneEventType}");
            }
            s_NumEventsProcessed++;
        }

        public enum ClientType
        {
            Authority,
            NonAuthority,
        }

        public enum Action
        {
            Load,
            Unload,
        }

        [UnityTest]
        public IEnumerator LoadAndUnloadCallbacks([Values] ClientType clientType, [Values] Action action)
        {
            yield return RunSceneEventCallbackTest(clientType, action, k_SceneToLoad);
        }

        [UnityTest]
        public IEnumerator LoadSceneFromPath([Values] ClientType clientType)
        {
            yield return RunSceneEventCallbackTest(clientType, Action.Load, k_PathToLoad);
        }

        private IEnumerator RunSceneEventCallbackTest(ClientType clientType, Action action, string loadCall)
        {
            m_EnableVerboseDebug = true;
            var client = m_ClientNetworkManagers[0];
            var managerToTest = clientType == ClientType.Authority ? m_ServerNetworkManager : client;


            var expectedCompletedClients = new List<ulong> { client.LocalClientId };
            // the authority ID is not inside ClientsThatCompleted when running as a server
            if (m_UseHost)
            {
                expectedCompletedClients.Insert(0, m_ServerNetworkManager.LocalClientId);
            }

            Scene loadedScene = default;
            if (action == Action.Unload)
            {
                // Load the scene initially
                m_ServerNetworkManager.SceneManager.LoadScene(k_SceneToLoad, LoadSceneMode.Additive);

                yield return WaitForConditionOrTimeOut(ValidateSceneIsLoaded);
                AssertOnTimeout($"[Setup] Timed out waiting for client to load the scene {k_SceneToLoad}!");

                // Wait for any pending messages to be processed
                yield return s_DefaultWaitForTick;

                // Get a reference to the scene to test
                loadedScene = SceneManager.GetSceneByName(k_SceneToLoad);
            }

            s_NumEventsProcessed = 0;
            m_ExpectedEventQueue.Clear();
            m_ExpectedEventQueue.Enqueue(new ExpectedEvent()
            {
                SceneEvent = new SceneEvent()
                {
                    SceneEventType = action == Action.Load ? SceneEventType.Load : SceneEventType.Unload,
                    LoadSceneMode = LoadSceneMode.Additive,
                    SceneName = k_SceneToLoad,
                    ScenePath = k_PathToLoad,
                    ClientId = managerToTest.LocalClientId,
                },
            });
            m_ExpectedEventQueue.Enqueue(new ExpectedEvent()
            {
                SceneEvent = new SceneEvent()
                {
                    SceneEventType = action == Action.Load ? SceneEventType.LoadComplete : SceneEventType.UnloadComplete,
                    LoadSceneMode = LoadSceneMode.Additive,
                    SceneName = k_SceneToLoad,
                    ScenePath = k_PathToLoad,
                    ClientId = managerToTest.LocalClientId,
                },
                SceneName = action == Action.Load ? k_SceneToLoad : null,
                ScenePath = action == Action.Load ? k_PathToLoad : null
            });

            if (clientType == ClientType.Authority)
            {
                m_ExpectedEventQueue.Enqueue(new ExpectedEvent()
                {
                    SceneEvent = new SceneEvent()
                    {
                        SceneEventType = action == Action.Load ? SceneEventType.LoadComplete : SceneEventType.UnloadComplete,
                        LoadSceneMode = LoadSceneMode.Additive,
                        SceneName = k_SceneToLoad,
                        ScenePath = k_PathToLoad,
                        ClientId = client.LocalClientId,
                    }
                });
            }

            m_ExpectedEventQueue.Enqueue(new ExpectedEvent()
            {
                SceneEvent = new SceneEvent()
                {
                    SceneEventType = action == Action.Load ? SceneEventType.LoadEventCompleted : SceneEventType.UnloadEventCompleted,
                    LoadSceneMode = LoadSceneMode.Additive,
                    SceneName = k_SceneToLoad,
                    ScenePath = k_PathToLoad,
                    ClientId = m_ServerNetworkManager.LocalClientId,
                    ClientsThatCompleted = expectedCompletedClients,
                    ClientsThatTimedOut = new List<ulong>()
                }
            });

            //////////////////////////////////////////
            // Testing event notifications
            managerToTest.SceneManager.OnSceneEvent += OnSceneEvent;

            if (action == Action.Load)
            {
                Assert.That(m_ServerNetworkManager.SceneManager.LoadScene(loadCall, LoadSceneMode.Additive) == SceneEventProgressStatus.Started);

                yield return WaitForConditionOrTimeOut(ValidateSceneIsLoaded);
                AssertOnTimeout($"[Test] Timed out waiting for client to load the scene {k_SceneToLoad}!");
            }
            else
            {
                Assert.That(loadedScene.name, Is.EqualTo(k_SceneToLoad), "scene was not loaded!");
                Assert.That(m_ServerNetworkManager.SceneManager.UnloadScene(loadedScene) == SceneEventProgressStatus.Started);

                yield return WaitForConditionOrTimeOut(ValidateSceneIsUnloaded);
                AssertOnTimeout($"[Test] Timed out waiting for client to unload the scene {k_SceneToLoad}!");
            }

            // Wait for all messages to process
            yield return s_DefaultWaitForTick;

            if (m_ExpectedEventQueue.Count > 0)
            {
                Assert.Fail($"Failed to invoke all expected OnSceneEvent callbacks. {m_ExpectedEventQueue.Count} callbacks missing. First missing event is {m_ExpectedEventQueue.Dequeue().SceneEvent.SceneEventType}");
            }

            managerToTest.SceneManager.OnSceneEvent -= OnSceneEvent;
        }

        private bool ValidateSceneIsLoaded(StringBuilder errorBuilder)
        {
            var loadedScene = m_ServerNetworkManager.SceneManager.ScenesLoaded.Values.FirstOrDefault(scene => scene.name == k_SceneToLoad);
            if (!loadedScene.isLoaded)
            {
                errorBuilder.AppendLine($"[ValidateIsLoaded] Scene {loadedScene.name} exists but is not loaded!");
                return false;
            }
            if (m_ServerNetworkManager.SceneManager.SceneEventProgressTracking.Count > 0)
            {
                errorBuilder.AppendLine($"[ValidateIsLoaded] Server NetworkManager still has progress tracking events.");
                return false;
            }

            foreach (var manager in m_ClientNetworkManagers)
            {
                // default will have isLoaded as false so we can get the scene or default and test on isLoaded
                loadedScene = manager.SceneManager.ScenesLoaded.Values.FirstOrDefault(scene => scene.name == k_SceneToLoad);
                if (!loadedScene.isLoaded)
                {
                    errorBuilder.AppendLine($"[ValidateIsLoaded] Scene {loadedScene.name} exists but is not loaded!");
                    return false;
                }

                if (manager.SceneManager.SceneEventProgressTracking.Count > 0)
                {
                    errorBuilder.AppendLine($"[ValidateIsLoaded] Client-{manager.name} still has progress tracking events.");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateSceneIsUnloaded()
        {
            if (m_ServerNetworkManager.SceneManager.ScenesLoaded.Values.Any(scene => scene.name == k_SceneToLoad))
            {
                return false;
            }
            if (m_ServerNetworkManager.SceneManager.SceneEventProgressTracking.Count > 0)
            {
                return false;
            }

            foreach (var manager in m_ClientNetworkManagers)
            {
                if (manager.SceneManager.ScenesLoaded.Values.Any(scene => scene.name == k_SceneToLoad))
                {
                    return false;
                }

                if (manager.SceneManager.SceneEventProgressTracking.Count > 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static void ValidateEventsAreEqual(SceneEvent expectedEvent, SceneEvent sceneEvent)
        {
            AssertField(expectedEvent.SceneEventType, sceneEvent.SceneEventType, nameof(sceneEvent.SceneEventType), sceneEvent.SceneEventType);
            AssertField(expectedEvent.LoadSceneMode, sceneEvent.LoadSceneMode, nameof(sceneEvent.LoadSceneMode), sceneEvent.SceneEventType);
            AssertField(expectedEvent.SceneName, sceneEvent.SceneName, nameof(sceneEvent.SceneName), sceneEvent.SceneEventType);
            AssertField(expectedEvent.ClientId, sceneEvent.ClientId, nameof(sceneEvent.ClientId), sceneEvent.SceneEventType);
            AssertField(expectedEvent.ClientsThatCompleted, sceneEvent.ClientsThatCompleted, nameof(sceneEvent.SceneEventType), sceneEvent.SceneEventType);
            AssertField(expectedEvent.ClientsThatTimedOut, sceneEvent.ClientsThatTimedOut, nameof(sceneEvent.ClientsThatTimedOut), sceneEvent.SceneEventType);
        }

        // The LoadCompleted event includes the scene being loaded
        private static void ValidateReceivedScene(ExpectedEvent expectedEvent, Scene scene)
        {
            AssertField(expectedEvent.SceneName, scene.name, "Scene.name", SceneEventType.LoadComplete);
            AssertField(expectedEvent.ScenePath, scene.path, "Scene.path", SceneEventType.LoadComplete);
        }

        private static void AssertField<T>(T expected, T actual, string fieldName, SceneEventType type)
        {
            Assert.AreEqual(expected, actual, $"Failed on event {s_NumEventsProcessed} - {type}: Expected {fieldName} to be {expected}. Found {actual}");
        }
    }
}
