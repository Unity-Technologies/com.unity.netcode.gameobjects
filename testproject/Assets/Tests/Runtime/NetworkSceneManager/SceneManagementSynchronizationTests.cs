using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.DAHost)]
    public class SceneManagementSynchronizationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;


        public SceneManagementSynchronizationTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        private struct ExpectedEvent
        {
            public bool IsSceneEvent;
            public SceneEvent SceneEvent;
            public ConnectionEventData ConnectionEvent;
        }

        private readonly Queue<ExpectedEvent> m_ExpectedEventQueue = new();

        private static int s_NumEventsProcessed;

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            VerboseDebug($"OnSceneEvent! Type: {sceneEvent.SceneEventType}.");
            AssertEventMatchesExpectedEvent(expectedEvent =>
                ValidateSceneEventsAreEqual(expectedEvent, sceneEvent), sceneEvent.SceneEventType);
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData eventData)
        {
            VerboseDebug($"OnConnectionEvent! Type: {eventData.EventType} -  Client-{eventData.ClientId}");
            AssertEventMatchesExpectedEvent(expectedEvent =>
                ValidateConnectionEventsAreEqual(expectedEvent, eventData), eventData.EventType);
        }

        private void AssertEventMatchesExpectedEvent<T>(Action<ExpectedEvent> predicate, T eventType)
        {
            if (m_ExpectedEventQueue.Count > 0)
            {
                var expectedEvent = m_ExpectedEventQueue.Dequeue();
                try
                {
                    predicate(expectedEvent);
                }
                catch(Exception failure)
                {
                    Debug.LogException(failure);
                }
            }
            else
            {
                Assert.Fail($"Received unexpected event at index {s_NumEventsProcessed}: {eventType}");
            }
            s_NumEventsProcessed++;
        }

        private NetworkManager m_ManagerToTest;

        private void SetManagerToTest(NetworkManager manager)
        {
            m_ManagerToTest = manager;
            m_ManagerToTest.OnConnectionEvent += OnConnectionEvent;
            m_ManagerToTest.SceneManager.OnSceneEvent += OnSceneEvent;
        }

        protected override void OnNewClientStarted(NetworkManager networkManager)
        {
            // If m_ManagerToTest isn't set at this point, it means we are testing the newly created NetworkManager
            if (m_ManagerToTest == null)
            {
                SetManagerToTest(networkManager);
            }
            base.OnNewClientCreated(networkManager);
        }

        protected override IEnumerator OnTearDown()
        {
            m_ManagerToTest.OnConnectionEvent -= OnConnectionEvent;
            m_ManagerToTest.SceneManager.OnSceneEvent -= OnSceneEvent;
            m_ManagerToTest = null;
            m_ExpectedEventQueue.Clear();
            s_NumEventsProcessed = 0;

            yield return base.OnTearDown();
        }

        [UnityTest]
        public IEnumerator SynchronizationCallbacks_Authority()
        {
            SetManagerToTest(GetAuthorityNetworkManager());

            // Calculate the expected ID of the newly connecting networkManager
            var expectedClientId = GetNonAuthorityNetworkManager().LocalClientId + 1;

            // Setup expected events
            AddExpectedEvent(new SceneEvent()
            {
                SceneEventType = SceneEventType.Synchronize,
                ClientId = expectedClientId
            });

            AddExpectedEvent(new SceneEvent()
            {
                SceneEventType = SceneEventType.SynchronizeComplete,
                ClientId = expectedClientId,
            });

            AddExpectedEvent(new ConnectionEventData()
            {
                EventType = ConnectionEvent.ClientConnected,
                ClientId = expectedClientId,
            });

            if (m_UseHost)
            {
                AddExpectedEvent(new ConnectionEventData()
                {
                    EventType = ConnectionEvent.PeerConnected,
                    ClientId = expectedClientId,
                });
            }

            m_EnableVerboseDebug = true;
            //////////////////////////////////////////
            // Testing event notifications
            yield return CreateAndStartNewClient();
            yield return null;

            Assert.IsEmpty(m_ExpectedEventQueue, "Not all expected callbacks were received");
        }

        [UnityTest]
        public IEnumerator SynchronizationCallbacks_NonAuthority()
        {
            var authorityId = GetAuthorityNetworkManager().LocalClientId;
            var peerClientId = GetNonAuthorityNetworkManager().LocalClientId;
            var expectedClientId = peerClientId + 1;

            var expectedPeerClientIds = m_UseHost ? new[] { authorityId, peerClientId } : new[] { peerClientId };

            // Setup expected events
            AddExpectedEvent(new SceneEvent()
            {
                SceneEventType = SceneEventType.Synchronize,
                ClientId = expectedClientId,
            });
            AddExpectedEvent(new ConnectionEventData()
            {
                EventType = ConnectionEvent.ClientConnected,
                ClientId = expectedClientId,
                PeerClientIds = new NativeArray<ulong>(expectedPeerClientIds.ToArray(), Allocator.Persistent),
            });

            AddExpectedEvent(new SceneEvent()
            {
                SceneEventType = SceneEventType.SynchronizeComplete,
                ClientId = expectedClientId,
            });

            Assert.Null(m_ManagerToTest, "m_ManagerToTest should be null as we should be testing newly created client");

            //////////////////////////////////////////
            // Testing event notifications

            // CreateAndStartNewClient will configure m_ManagerToTest inside OnNewClientStarted
            yield return CreateAndStartNewClient();
            yield return null;

            Assert.IsEmpty(m_ExpectedEventQueue, "Not all expected callbacks were received");
        }

        [UnityTest]
        public IEnumerator LateJoiningClient_PeerCallbacks()
        {
            var nonAuthority = GetNonAuthorityNetworkManager();
            var expectedClientId = nonAuthority.LocalClientId + 1;
            SetManagerToTest(nonAuthority);

            // Setup expected events
            AddExpectedEvent(new ConnectionEventData()
            {
                EventType = ConnectionEvent.PeerConnected,
                ClientId = expectedClientId,
            });

            if (m_UseCmbService)
            {
                AddExpectedEvent(new SceneEvent()
                {
                    SceneEventType = SceneEventType.SynchronizeComplete,
                    ClientId = expectedClientId,
                });
            }

            //////////////////////////////////////////
            // Testing event notifications
            yield return CreateAndStartNewClient();
            yield return null;

            Assert.IsEmpty(m_ExpectedEventQueue, "Not all expected callbacks were received");
        }

        private void AddExpectedEvent(SceneEvent expectedEvent)
        {
            m_ExpectedEventQueue.Enqueue(new ExpectedEvent()
            {
                IsSceneEvent = true,
                SceneEvent = expectedEvent,
            });
        }

        private void AddExpectedEvent(ConnectionEventData expectedEvent)
        {
            m_ExpectedEventQueue.Enqueue(new ExpectedEvent()
            {
                IsSceneEvent = false,
                ConnectionEvent = expectedEvent,
            });
        }

        private static void ValidateSceneEventsAreEqual(ExpectedEvent expectedEvent, SceneEvent sceneEvent)
        {
            Assert.That(expectedEvent.IsSceneEvent, Is.True, $"Received unexpected scene event {sceneEvent.SceneEventType} at index {s_NumEventsProcessed}. Expected connection event: {expectedEvent.ConnectionEvent.EventType}");
            AssertField(expectedEvent.SceneEvent.SceneEventType, sceneEvent.SceneEventType, nameof(sceneEvent.SceneEventType), sceneEvent.SceneEventType);
            AssertField(expectedEvent.SceneEvent.ClientId, sceneEvent.ClientId, nameof(sceneEvent.ClientId), sceneEvent.SceneEventType);
        }

        private static void ValidateConnectionEventsAreEqual(ExpectedEvent expectedEvent, ConnectionEventData eventData)
        {
            Assert.That(expectedEvent.IsSceneEvent, Is.False, $"Received unexpected connection event {eventData.EventType} at index {s_NumEventsProcessed}. Expected scene event: {expectedEvent.SceneEvent.SceneEventType}");
            AssertField(expectedEvent.ConnectionEvent.EventType, eventData.EventType, nameof(eventData.EventType), eventData.EventType);
            AssertField(expectedEvent.ConnectionEvent.ClientId, eventData.ClientId, nameof(eventData.ClientId), eventData.EventType);

            AssertField(expectedEvent.ConnectionEvent.PeerClientIds.Length, eventData.PeerClientIds.Length, "length of PeerClientIds", eventData.EventType);
            if (eventData.PeerClientIds.Length > 0)
            {
                var peerIds = eventData.PeerClientIds.ToArray();
                foreach (var expectedClientId in expectedEvent.ConnectionEvent.PeerClientIds)
                {
                    Assert.Contains(expectedClientId, peerIds, "PeerClientIds does not contain all expected client IDs.");
                }
            }
        }

        private static void AssertField<T, TK>(T expected, T actual, string fieldName, TK type)
        {
            Assert.AreEqual(expected, actual, $"Failed on event {s_NumEventsProcessed} - {type}. Incorrect {fieldName}");
        }

    }
}
