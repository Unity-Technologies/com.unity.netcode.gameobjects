using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    internal class TestDeferredMessageManager : DeferredMessageManager
    {
        public bool DeferMessageCalled;
        public bool ProcessTriggersCalled;

        public delegate void BeforePurgeDelegate(TestDeferredMessageManager manager, ulong key);
        public delegate void BeforeDeferDelegate(TestDeferredMessageManager manager, ulong key);

        public BeforePurgeDelegate OnBeforePurge;
        public BeforeDeferDelegate OnBeforeDefer;

        // The way that OnBeforePurge gets used in these tests involves counting things that are still in the
        // deferred message queue. This is something the base class doesn't support so the way it purges individual
        // messages does not make it safe to access structures during purging. This set contains a list of
        // keys that are unsafe to access because they've been purged/disposed, but not removed from the dictionary
        // yet, so that we can avoid accessing those and record them instead as 0 (which is what they will be once
        // the purge completes)
        private HashSet<ulong> m_PurgedKeys = new HashSet<ulong>();

        public TestDeferredMessageManager(NetworkManager networkManager) :
            base(networkManager)
        {
        }

        public void ClearCallFlags()
        {
            DeferMessageCalled = false;
            ProcessTriggersCalled = false;
        }

        public int DeferredMessageCountTotal()
        {
            int ret = 0;
            foreach (var kvp in m_Triggers)
            {
                ret += DeferredMessageCountForType(kvp.Key);
            }

            return ret;
        }

        public int DeferredMessageCountForType(IDeferredMessageManager.TriggerType trigger)
        {
            var count = 0;
            if (m_Triggers.TryGetValue(trigger, out var dict))
            {
                foreach (var kvp in dict)
                {
                    if (m_PurgedKeys.Contains(kvp.Key))
                    {
                        continue;
                    }
                    count += kvp.Value.TriggerData.Length;
                }
            }

            return count;
        }

        public int DeferredMessageCountForKey(IDeferredMessageManager.TriggerType trigger, ulong key)
        {
            if (m_PurgedKeys.Contains(key))
            {
                return 0;
            }
            if (m_Triggers.TryGetValue(trigger, out var dict))
            {
                if (dict.TryGetValue(key, out var subdict))
                {
                    return subdict.TriggerData.Length;
                }
            }

            return 0;
        }

        public override void DeferMessage(IDeferredMessageManager.TriggerType trigger, ulong key, FastBufferReader reader, ref NetworkContext context)
        {
            OnBeforeDefer?.Invoke(this, key);
            DeferMessageCalled = true;
            base.DeferMessage(trigger, key, reader, ref context);
        }

        public override void ProcessTriggers(IDeferredMessageManager.TriggerType trigger, ulong key)
        {
            ProcessTriggersCalled = true;
            base.ProcessTriggers(trigger, key);
        }

        protected override void PurgeTrigger(IDeferredMessageManager.TriggerType triggerType, ulong key, TriggerInfo triggerInfo)
        {
            OnBeforePurge?.Invoke(this, key);
            base.PurgeTrigger(triggerType, key, triggerInfo);
            m_PurgedKeys.Add(key);
        }

        public override void CleanupStaleTriggers()
        {
            base.CleanupStaleTriggers();
            m_PurgedKeys.Clear();
        }
    }

    internal class SpawnCatcher : INetworkHooks
    {
        public struct TriggerData
        {
            public FastBufferReader Reader;
            public MessageHeader Header;
            public ulong SenderId;
            public float Timestamp;
            public int SerializedHeaderSize;
        }
        public readonly List<TriggerData> CaughtMessages = new List<TriggerData>();

        public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
        {
        }

        public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
        {
        }

        public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
        {
        }

        public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
        {
        }

        public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
        {
        }

        public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
        {
            return true;
        }

        public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context)
        {
            if (messageType == typeof(CreateObjectMessage))
            {
                CaughtMessages.Add(new TriggerData
                {
                    Reader = new FastBufferReader(messageContent, Allocator.Persistent),
                    Header = context.Header,
                    Timestamp = context.Timestamp,
                    SenderId = context.SenderId,
                    SerializedHeaderSize = context.SerializedHeaderSize
                });
                return false;
            }

            return true;
        }

        public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }

        public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
        {
        }
    }

    public class DeferredMessageTestRpcComponent : NetworkBehaviour
    {
        public bool ClientRpcCalled;

        [ClientRpc]
        public void SendTestClientRpc()
        {
            ClientRpcCalled = true;
        }

        public static readonly List<ulong> ClientInstances = new List<ulong>();
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                ClientInstances.Add(NetworkManager.LocalClientId);
            }
            base.OnNetworkSpawn();
        }
    }

    public class DeferredMessageTestNetworkVariableComponent : NetworkBehaviour
    {
        public static readonly List<ulong> ClientInstances = new List<ulong>();

        public NetworkVariable<int> TestNetworkVariable;

        public void Awake()
        {
            TestNetworkVariable = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                ClientInstances.Add(NetworkManager.LocalClientId);
            }
            base.OnNetworkSpawn();
        }
    }

    public class DeferredMessageTestRpcAndNetworkVariableComponent : NetworkBehaviour
    {
        public static readonly List<ulong> ClientInstances = new List<ulong>();
        public bool ClientRpcCalled;
        public NetworkVariable<int> TestNetworkVariable;

        public void Awake()
        {
            TestNetworkVariable = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                ClientInstances.Add(NetworkManager.LocalClientId);
            }
            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void SendTestClientRpc()
        {
            ClientRpcCalled = true;
        }
    }

    public class DeferredMessagingTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private List<SpawnCatcher> m_ClientSpawnCatchers = new List<SpawnCatcher>();

        private GameObject m_RpcPrefab;
        private GameObject m_NetworkVariablePrefab;
        private GameObject m_RpcAndNetworkVariablePrefab;

        private int m_NumberOfClientsToLateJoin = 2;

        protected override IEnumerator OnSetup()
        {
            DeferredMessageTestRpcAndNetworkVariableComponent.ClientInstances.Clear();
            DeferredMessageTestRpcComponent.ClientInstances.Clear();
            DeferredMessageTestNetworkVariableComponent.ClientInstances.Clear();
            m_SkipAddingPrefabsToClient = false;
            // Host is irrelevant, messages don't get sent to the host "client"
            m_UseHost = false;

            // Replace the IDeferredMessageManager component with our test one in the component factory
            ComponentFactory.Register<IDeferredMessageManager>(networkManager => new TestDeferredMessageManager(networkManager));
            yield return null;
        }

        protected override IEnumerator OnTearDown()
        {
            // Revert the IDeferredMessageManager component to its default (DeferredMessageManager)
            ComponentFactory.Deregister<IDeferredMessageManager>();
            m_ClientSpawnCatchers.Clear();
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            // Note: This is where prefabs should be created
            m_RpcPrefab = CreateNetworkObjectPrefab("Object With RPC");
            var networkObject = m_RpcPrefab.GetComponent<NetworkObject>();
            m_RpcPrefab.AddComponent<DeferredMessageTestRpcComponent>();

            m_NetworkVariablePrefab = CreateNetworkObjectPrefab("Object With NetworkVariable");
            networkObject = m_NetworkVariablePrefab.GetComponent<NetworkObject>();
            m_NetworkVariablePrefab.AddComponent<DeferredMessageTestNetworkVariableComponent>();

            m_RpcAndNetworkVariablePrefab = CreateNetworkObjectPrefab("Object With NetworkVariable And RPC");
            networkObject = m_RpcAndNetworkVariablePrefab.GetComponent<NetworkObject>();
            m_RpcAndNetworkVariablePrefab.AddComponent<DeferredMessageTestRpcAndNetworkVariableComponent>();

            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;

        }

        private bool m_SkipAddingPrefabsToClient = false;

        private void AddPrefabsToClient(NetworkManager networkManager)
        {
            networkManager.AddNetworkPrefab(m_RpcPrefab);
            networkManager.AddNetworkPrefab(m_NetworkVariablePrefab);
            networkManager.AddNetworkPrefab(m_RpcAndNetworkVariablePrefab);
        }

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.ForceSamePrefabs = false;
            if (!m_SkipAddingPrefabsToClient)
            {
                AddPrefabsToClient(networkManager);
            }

            base.OnNewClientCreated(networkManager);
        }

        private IEnumerator SpawnClients(bool clearTestDeferredMessageManagerCallFlags = true)
        {
            for (int i = 0; i < m_NumberOfClientsToLateJoin; i++)
            {
                // Create and join client
                yield return CreateAndStartNewClient();
            }

            if (clearTestDeferredMessageManagerCallFlags)
            {
                ClearTestDeferredMessageManagerCallFlags();
            }
        }

        private T GetComponentForClient<T>(ulong clientId) where T : NetworkBehaviour
        {
            foreach (var component in Object.FindObjectsOfType<T>())
            {
                if (component.IsSpawned && component.NetworkManager.LocalClientId == clientId)
                {
                    return component;
                }
            }

            return null;
        }

        private void CatchSpawns()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                var catcher = new SpawnCatcher();
                m_ClientSpawnCatchers.Add(catcher);
                client.MessagingSystem.Hook(catcher);
            }
        }

        private void ReleaseSpawns()
        {
            for (var i = 0; i < m_ClientNetworkManagers.Length; ++i)
            {
                // Unhook first so the spawn catcher stops catching spawns
                m_ClientNetworkManagers[i].MessagingSystem.Unhook(m_ClientSpawnCatchers[i]);
                foreach (var caughtSpawn in m_ClientSpawnCatchers[i].CaughtMessages)
                {
                    // Reader will be disposed within HandleMessage
                    m_ClientNetworkManagers[i].MessagingSystem.HandleMessage(caughtSpawn.Header, caughtSpawn.Reader, caughtSpawn.SenderId, caughtSpawn.Timestamp, caughtSpawn.SerializedHeaderSize);
                }
            }
            m_ClientSpawnCatchers.Clear();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            // Clear out these values from whatever might have set them during the initial startup.
            ClearTestDeferredMessageManagerCallFlags();
            yield return null;
        }

        private IEnumerator WaitForClientsToCatchSpawns(int count = 1)
        {
            yield return WaitForConditionOrTimeOut(() =>
            {
                foreach (var catcher in m_ClientSpawnCatchers)
                {
                    if (catcher.CaughtMessages.Count != count)
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        private void ClearTestDeferredMessageManagerCallFlags()
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.ClearCallFlags();
            }
        }

        private void AssertSpawnTriggerCountForObject(TestDeferredMessageManager manager, GameObject serverObject, int expectedCount = 1)
        {
            Assert.AreEqual(expectedCount, manager.DeferredMessageCountTotal());
            Assert.AreEqual(expectedCount, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
            Assert.AreEqual(expectedCount, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
            Assert.AreEqual(0, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnAddPrefab));
        }

        private static CoroutineRunner s_CoroutineRunner;

        private Coroutine Run(IEnumerator enumerator)
        {
            if (s_CoroutineRunner == null)
            {
                s_CoroutineRunner = new GameObject(nameof(CoroutineRunner)).AddComponent<CoroutineRunner>();
            }

            return s_CoroutineRunner.StartCoroutine(enumerator);
        }

        private IEnumerator RunMultiple(List<IEnumerator> waitFor)
        {
            yield return WaitMultiple(StartMultiple(waitFor));
        }

        private List<Coroutine> StartMultiple(List<IEnumerator> waitFor)
        {
            var runningCoroutines = new List<Coroutine>();
            foreach (var enumerator in waitFor)
            {
                runningCoroutines.Add(Run(enumerator));
            }

            return runningCoroutines;
        }

        private IEnumerator WaitMultiple(List<Coroutine> runningCoroutines)
        {
            foreach (var coroutine in runningCoroutines)
            {
                yield return coroutine;
            }
        }

        private List<IEnumerator> WaitForAllClientsToReceive<T>() where T : INetworkMessage
        {
            var waiters = new List<IEnumerator>();
            foreach (var client in m_ClientNetworkManagers)
            {
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<T>(client));
            }

            return waiters;
        }

        private List<IEnumerator> WaitForAllClientsToReceive<TFirstMessage, TSecondMessage>()
            where TFirstMessage : INetworkMessage
            where TSecondMessage : INetworkMessage
        {
            var waiters = new List<IEnumerator>();
            foreach (var client in m_ClientNetworkManagers)
            {
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TFirstMessage>(client));
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TSecondMessage>(client));
            }

            return waiters;
        }

        private List<IEnumerator> WaitForAllClientsToReceive<TFirstMessage, TSecondMessage, TThirdMessage>()
        where TFirstMessage : INetworkMessage
        where TSecondMessage : INetworkMessage
        where TThirdMessage : INetworkMessage
        {
            var waiters = new List<IEnumerator>();
            foreach (var client in m_ClientNetworkManagers)
            {
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TFirstMessage>(client));
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TSecondMessage>(client));
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TThirdMessage>(client));
            }
            return waiters;
        }

        private List<IEnumerator> WaitForAllClientsToReceive<TFirstMessage, TSecondMessage, TThirdMessage, TFourthMessage>()
            where TFirstMessage : INetworkMessage
            where TSecondMessage : INetworkMessage
            where TThirdMessage : INetworkMessage
            where TFourthMessage : INetworkMessage
        {
            var waiters = new List<IEnumerator>();
            foreach (var client in m_ClientNetworkManagers)
            {
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TFirstMessage>(client));
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TSecondMessage>(client));
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TThirdMessage>(client));
                waiters.Add(NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<TFourthMessage>(client));
            }

            return waiters;
        }

        [UnityTest]
        public IEnumerator WhenAnRpcArrivesBeforeASpawnArrives_ItIsDeferred()
        {
            yield return SpawnClients();
            CatchSpawns();
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            serverObject.GetComponent<DeferredMessageTestRpcComponent>().SendTestClientRpc();

            yield return RunMultiple(WaitForAllClientsToReceive<ClientRpcMessage>());

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);
                AssertSpawnTriggerCountForObject(manager, serverObject);
            }
        }

        [UnityTest]
        public IEnumerator WhenADespawnArrivesBeforeASpawnArrives_ItIsDeferred()
        {
            yield return SpawnClients();
            CatchSpawns();
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            serverObject.GetComponent<NetworkObject>().Despawn(false);

            yield return RunMultiple(WaitForAllClientsToReceive<DestroyObjectMessage>());

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);
                AssertSpawnTriggerCountForObject(manager, serverObject);
            }
        }

        [UnityTest]
        public IEnumerator WhenAChangeOwnershipMessageArrivesBeforeASpawnArrives_ItIsDeferred()
        {
            yield return SpawnClients();
            CatchSpawns();
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);
            yield return RunMultiple(WaitForAllClientsToReceive<ChangeOwnershipMessage>());
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);
                AssertSpawnTriggerCountForObject(manager, serverObject);
            }
        }

        [UnityTest]
        public IEnumerator WhenANetworkVariableDeltaMessageArrivesBeforeASpawnArrives_ItIsDeferred()
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            CatchSpawns();

            // Have to start these before spawning here because spawning sends a NetworkVariableDeltaMessage, too
            // Depending on timing, if we start this after spawning, we may end up missing the first one.
            var waiters = WaitForAllClientsToReceive<NetworkVariableDeltaMessage>();
            var coroutines = StartMultiple(waiters);

            var serverObject = Object.Instantiate(m_NetworkVariablePrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            serverObject.GetComponent<DeferredMessageTestNetworkVariableComponent>().TestNetworkVariable.Value = 1;

            yield return WaitMultiple(coroutines);

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);
                // TODO: Network Variables generate an extra message immediately at spawn for some reason...
                // Seems like a bug since the network variable data is in the spawn message already.
                AssertSpawnTriggerCountForObject(manager, serverObject, 1);
            }
        }

        [UnityTest]
        //[Ignore("Disabling this temporarily until it is migrated into new integration test.")]
        public IEnumerator WhenASpawnMessageArrivesBeforeThePrefabIsAvailable_ItIsDeferred()
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();

            yield return RunMultiple(WaitForAllClientsToReceive<CreateObjectMessage>());

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);
                Assert.AreEqual(1, manager.DeferredMessageCountTotal());
                Assert.AreEqual(0, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(1, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnAddPrefab));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnAddPrefab, serverObject.GetComponent<NetworkObject>().GlobalObjectIdHash));

                var component = GetComponentForClient<DeferredMessageTestRpcComponent>(client.LocalClientId);
                Assert.IsNull(component);
            }
        }

        [UnityTest]
        public IEnumerator WhenAnRpcIsDeferred_ItIsProcessedOnSpawn()
        {
            yield return WhenAnRpcArrivesBeforeASpawnArrives_ItIsDeferred();
            ReleaseSpawns();

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
                Assert.IsTrue(manager.ProcessTriggersCalled);
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());

                var component = GetComponentForClient<DeferredMessageTestRpcComponent>(client.LocalClientId);
                Assert.IsTrue(component.ClientRpcCalled);
            }
        }

        [UnityTest]
        public IEnumerator WhenADespawnIsDeferred_ItIsProcessedOnSpawn()
        {
            yield return WhenADespawnArrivesBeforeASpawnArrives_ItIsDeferred();
            ReleaseSpawns();

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
                Assert.IsTrue(manager.ProcessTriggersCalled);
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());

                // Should have been immediately despawned.
                var component = GetComponentForClient<DeferredMessageTestRpcComponent>(client.LocalClientId);
                Assert.IsTrue(!component);
            }
        }

        [UnityTest]
        public IEnumerator WhenAChangeOwnershipMessageIsDeferred_ItIsProcessedOnSpawn()
        {
            yield return WhenAChangeOwnershipMessageArrivesBeforeASpawnArrives_ItIsDeferred();
            ReleaseSpawns();

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.ProcessTriggersCalled);
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());

                var component = GetComponentForClient<DeferredMessageTestRpcComponent>(client.LocalClientId);
                Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, component.OwnerClientId);
            }
        }

        [UnityTest]
        public IEnumerator WhenANetworkVariableDeltaMessageIsDeferred_ItIsProcessedOnSpawn()
        {
            yield return WhenANetworkVariableDeltaMessageArrivesBeforeASpawnArrives_ItIsDeferred();

            foreach (var client in m_ClientNetworkManagers)
            {
                AddPrefabsToClient(client);
            }

            ReleaseSpawns();

            // Wait for the clients to spawn the NetworkObjects
            bool HaveAllClientsSpawned()
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    if (!DeferredMessageTestNetworkVariableComponent.ClientInstances.Contains(client.LocalClientId))
                    {
                        return false;
                    }
                }
                return true;
            }
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawned);

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.ProcessTriggersCalled);
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());

                var component = GetComponentForClient<DeferredMessageTestNetworkVariableComponent>(client.LocalClientId);
                Assert.AreEqual(1, component.TestNetworkVariable.Value);
            }
        }

        [UnityTest]
        public IEnumerator WhenASpawnMessageIsDeferred_ItIsProcessedOnAddPrefab()
        {
            // This will prevent spawned clients from adding prefabs
            m_SkipAddingPrefabsToClient = true;
            yield return WhenASpawnMessageArrivesBeforeThePrefabIsAvailable_ItIsDeferred();

            // Now add the prefabs
            foreach (var client in m_ClientNetworkManagers)
            {
                AddPrefabsToClient(client);
            }

            // Wait for the clients to spawn the NetworkObjects
            bool HaveAllClientsSpawned()
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    if (!DeferredMessageTestRpcComponent.ClientInstances.Contains(client.LocalClientId))
                    {
                        return false;
                    }
                }
                return true;
            }
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawned);

            // Validate this test
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.ProcessTriggersCalled);
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());

                var component = GetComponentForClient<DeferredMessageTestRpcComponent>(client.LocalClientId);
                Assert.IsNotNull(component);
            }
        }

        [UnityTest]
        public IEnumerator WhenMultipleSpawnTriggeredMessagesAreDeferred_TheyAreAllProcessedOnSpawn()
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            CatchSpawns();

            // We wait for the RPC message, the NetworkVariableDeltaMessage, and the ChangeOwnershipMessage messages
            var waiters = WaitForAllClientsToReceive<ClientRpcMessage, NetworkVariableDeltaMessage, ChangeOwnershipMessage>();
            var coroutines = StartMultiple(waiters);

            var serverObject = Object.Instantiate(m_RpcAndNetworkVariablePrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().SendTestClientRpc();
            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().TestNetworkVariable.Value = 1;
            // TODO: Remove this if we figure out how to work around the NetworkVariableDeltaMessage.Serialized issue at line 59
            // Otherwise, we have to wait for at least 1 tick for the NetworkVariableDeltaMessage to be generated before changing ownership
            yield return s_DefaultWaitForTick;
            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // Should be received in order so we'll wait for the last one.
            yield return WaitMultiple(coroutines);

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);

                Assert.AreEqual(3, manager.DeferredMessageCountTotal());
                Assert.AreEqual(3, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(3, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
                Assert.AreEqual(0, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnAddPrefab));
                AddPrefabsToClient(client);
            }

            ReleaseSpawns();

            // Wait for the clients to spawn the NetworkObjects
            bool HaveAllClientsSpawned()
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    if (!DeferredMessageTestRpcAndNetworkVariableComponent.ClientInstances.Contains(client.LocalClientId))
                    {
                        return false;
                    }
                }
                return true;
            }
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawned);
            yield return new WaitForSeconds(0.1f);

            // Validate the spawned objects
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.ProcessTriggersCalled, "Process triggers were not called!");
                Assert.AreEqual(0, manager.DeferredMessageCountTotal(), $"Deferred message count ({manager.DeferredMessageCountTotal()}) is not zero!");

                var component = GetComponentForClient<DeferredMessageTestRpcAndNetworkVariableComponent>(client.LocalClientId);
                Assert.IsTrue(component.ClientRpcCalled, "Client RPC was not called!");
                Assert.AreEqual(1, component.TestNetworkVariable.Value, $"Test {nameof(NetworkVariable<int>)} ({component.TestNetworkVariable.Value}) does not equal 1!");
                Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, component.OwnerClientId, $"{component.name} owner id ({component.OwnerClientId}) does not equal first client id ({m_ClientNetworkManagers[0].LocalClientId})");
            }
        }

        [UnityTest]
        public IEnumerator WhenMultipleAddPrefabTriggeredMessagesAreDeferred_TheyAreAllProcessedOnAddNetworkPrefab()
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();

            var serverObject2 = Object.Instantiate(m_RpcPrefab);
            serverObject2.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject2.GetComponent<NetworkObject>().Spawn();

            yield return RunMultiple(WaitForAllClientsToReceive<CreateObjectMessage, CreateObjectMessage>());

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);

                Assert.AreEqual(2, manager.DeferredMessageCountTotal());
                Assert.AreEqual(0, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(2, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnAddPrefab));
                Assert.AreEqual(2, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnAddPrefab, serverObject.GetComponent<NetworkObject>().GlobalObjectIdHash));
                AddPrefabsToClient(client);
            }

            // Wait for the clients to spawn the NetworkObjects
            bool HaveAllClientsSpawned()
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    if (!DeferredMessageTestRpcComponent.ClientInstances.Contains(client.LocalClientId))
                    {
                        return false;
                    }
                }
                return true;
            }
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawned);


            foreach (var client in m_ClientNetworkManagers)
            {
                var found1 = false;
                var found2 = false;
                foreach (var component in Object.FindObjectsOfType<DeferredMessageTestRpcComponent>())
                {
                    if (component.IsSpawned && component.NetworkManager.LocalClientId == client.LocalClientId)
                    {
                        if (component.NetworkObjectId == serverObject.GetComponent<NetworkObject>().NetworkObjectId)
                        {
                            found1 = true;
                        }
                        else if (component.NetworkObjectId == serverObject2.GetComponent<NetworkObject>().NetworkObjectId)
                        {
                            found2 = true;
                        }
                    }
                }

                Assert.IsTrue(found1);
                Assert.IsTrue(found2);

                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.ProcessTriggersCalled);

                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
            }
        }

        [UnityTest]
        public IEnumerator WhenSpawnTriggeredMessagesAreDeferredBeforeThePrefabIsAdded_AddingThePrefabCausesThemToBeProcessed()
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();

            var waiters = WaitForAllClientsToReceive<CreateObjectMessage, ClientRpcMessage, NetworkVariableDeltaMessage, ChangeOwnershipMessage>();
            var coroutines = StartMultiple(waiters);

            var serverObject = Object.Instantiate(m_RpcAndNetworkVariablePrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();

            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().SendTestClientRpc();
            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().TestNetworkVariable.Value = 1;
            // TODO: Remove this if we figure out how to work around the NetworkVariableDeltaMessage.Serialized issue at line 59
            // Otherwise, we have to wait for at least 1 tick for the NetworkVariableDeltaMessage to be generated before changing ownership
            yield return s_DefaultWaitForTick;
            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return WaitMultiple(coroutines);

            // Validate messages are deferred and pending
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.DeferMessageCalled);
                Assert.IsFalse(manager.ProcessTriggersCalled);
                Assert.AreEqual(4, manager.DeferredMessageCountTotal());
                Assert.AreEqual(3, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(3, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
                Assert.AreEqual(1, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnAddPrefab));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnAddPrefab, serverObject.GetComponent<NetworkObject>().GlobalObjectIdHash));
                AddPrefabsToClient(client);
            }

            // Wait for the clients to spawn the NetworkObjects
            bool HaveAllClientsSpawned()
            {
                foreach (var client in m_ClientNetworkManagers)
                {
                    if (!DeferredMessageTestRpcAndNetworkVariableComponent.ClientInstances.Contains(client.LocalClientId))
                    {
                        return false;
                    }
                }
                return true;
            }
            yield return WaitForConditionOrTimeOut(HaveAllClientsSpawned);

            yield return new WaitForSeconds(0.1f);

            // Validate the test
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.IsTrue(manager.ProcessTriggersCalled);
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());

                var component = GetComponentForClient<DeferredMessageTestRpcAndNetworkVariableComponent>(client.LocalClientId);
                Assert.NotNull(component);
                Assert.IsTrue(component.ClientRpcCalled);
                Assert.AreEqual(1, component.TestNetworkVariable.Value);
                Assert.AreEqual(m_ClientNetworkManagers[0].LocalClientId, component.OwnerClientId);
            }
        }

        [UnityTest]
        [Ignore("This test is unstable (MTT-4146)")]
        public IEnumerator WhenAMessageIsDeferredForMoreThanTheConfiguredTime_ItIsRemoved([Values(1, 2, 3)] int timeout)
        {
            yield return SpawnClients();
            CatchSpawns();
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = timeout;
            }
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            var start = 0f;

            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforeDeferDelegate beforeDefer = (manager, key) =>
                {
                    if (start == 0)
                    {
                        start = Time.realtimeSinceStartup;
                    }
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforeDefer = beforeDefer;
            }

            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return WaitForAllClientsToReceive<ChangeOwnershipMessage>();

            foreach (var unused in m_ClientNetworkManagers)
            {
                LogAssert.Expect(LogType.Warning, $"[Netcode] Deferred messages were received for a trigger of type {IDeferredMessageManager.TriggerType.OnSpawn} with key {serverObject.GetComponent<NetworkObject>().NetworkObjectId}, but that trigger was not received within within {timeout} second(s).");
            }

            int purgeCount = 0;
            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforePurgeDelegate beforePurge = (manager, key) =>
                {
                    ++purgeCount;
                    var elapsed = Time.realtimeSinceStartup - start;
                    Assert.GreaterOrEqual(elapsed, timeout - 0.05f);
                    Assert.AreEqual(1, manager.DeferredMessageCountTotal());
                    Assert.AreEqual(1, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                    Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, key));
                    Assert.AreEqual(serverObject.GetComponent<NetworkObject>().NetworkObjectId, key);
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforePurge = beforePurge;
            }

            yield return new WaitForSeconds(timeout + 0.1f);

            Assert.AreEqual(m_NumberOfClientsToLateJoin, purgeCount);
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
            }
        }

        [UnityTest]
        [Ignore("This test is unstable on standalones")]
        public IEnumerator WhenMultipleMessagesForTheSameObjectAreDeferredForMoreThanTheConfiguredTime_TheyAreAllRemoved([Values(1, 2, 3)] int timeout)
        {
            yield return SpawnClients();
            CatchSpawns();
            // Have to start these before spawning here because spawning sends a NetworkVariableDeltaMessage, too
            // Depending on timing, if we start this after spawning, we may end up missing the first one.
            var waiters = WaitForAllClientsToReceive<ClientRpcMessage, NetworkVariableDeltaMessage, NetworkVariableDeltaMessage, ChangeOwnershipMessage>();
            var coroutines = StartMultiple(waiters);

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = timeout;
            }
            var serverObject = Object.Instantiate(m_RpcAndNetworkVariablePrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            var start = 0f;

            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforeDeferDelegate beforeDefer = (manager, key) =>
                {
                    if (start == 0)
                    {
                        start = Time.realtimeSinceStartup;
                    }
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforeDefer = beforeDefer;
            }

            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().SendTestClientRpc();
            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().TestNetworkVariable.Value = 1;
            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return WaitMultiple(coroutines);

            foreach (var unused in m_ClientNetworkManagers)
            {
                LogAssert.Expect(LogType.Warning, $"[Netcode] Deferred messages were received for a trigger of type {IDeferredMessageManager.TriggerType.OnSpawn} with key {serverObject.GetComponent<NetworkObject>().NetworkObjectId}, but that trigger was not received within within {timeout} second(s).");
            }

            int purgeCount = 0;
            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforePurgeDelegate beforePurge = (manager, key) =>
                {
                    ++purgeCount;
                    var elapsed = Time.realtimeSinceStartup - start;
                    Assert.GreaterOrEqual(elapsed, timeout - 0.05f);
                    Assert.AreEqual(4, manager.DeferredMessageCountTotal());
                    Assert.AreEqual(4, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                    Assert.AreEqual(4, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, key));
                    Assert.AreEqual(serverObject.GetComponent<NetworkObject>().NetworkObjectId, key);
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforePurge = beforePurge;
            }

            yield return new WaitForSeconds(timeout + 0.1f);

            Assert.AreEqual(m_NumberOfClientsToLateJoin, purgeCount);
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
            }
        }

        [UnityTest]
        public IEnumerator WhenMultipleMessagesForDifferentObjectsAreDeferredForMoreThanTheConfiguredTime_TheyAreAllRemoved([Values(1, 2, 3)] int timeout)
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            CatchSpawns();

            // Have to start these before spawning here because spawning sends a NetworkVariableDeltaMessage, too
            // Depending on timing, if we start this after spawning, we may end up missing the first one.
            var waiters = WaitForAllClientsToReceive<ClientRpcMessage, NetworkVariableDeltaMessage, ChangeOwnershipMessage>();
            waiters.AddRange(WaitForAllClientsToReceive<ClientRpcMessage, NetworkVariableDeltaMessage, ChangeOwnershipMessage>());
            var coroutines = StartMultiple(waiters);

            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = timeout;
            }
            var serverObject = Object.Instantiate(m_RpcAndNetworkVariablePrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();

            var serverObject2 = Object.Instantiate(m_RpcAndNetworkVariablePrefab);
            serverObject2.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject2.GetComponent<NetworkObject>().Spawn();

            yield return WaitForClientsToCatchSpawns(2);

            var start = 0f;

            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforeDeferDelegate beforeDefer = (manager, key) =>
                {
                    if (start == 0)
                    {
                        start = Time.realtimeSinceStartup;
                    }
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforeDefer = beforeDefer;
            }

            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().SendTestClientRpc();
            serverObject.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().TestNetworkVariable.Value = 1;
            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            serverObject2.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().SendTestClientRpc();
            serverObject2.GetComponent<DeferredMessageTestRpcAndNetworkVariableComponent>().TestNetworkVariable.Value = 1;
            serverObject2.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return WaitMultiple(coroutines);

            foreach (var unused in m_ClientNetworkManagers)
            {

                LogAssert.Expect(LogType.Warning, $"[Netcode] Deferred messages were received for a trigger of type {IDeferredMessageManager.TriggerType.OnSpawn} with key {serverObject.GetComponent<NetworkObject>().NetworkObjectId}, but that trigger was not received within within {timeout} second(s).");
                LogAssert.Expect(LogType.Warning, $"[Netcode] Deferred messages were received for a trigger of type {IDeferredMessageManager.TriggerType.OnSpawn} with key {serverObject2.GetComponent<NetworkObject>().NetworkObjectId}, but that trigger was not received within within {timeout} second(s).");
            }

            int purgeCount = 0;
            foreach (var client in m_ClientNetworkManagers)
            {
                var remainingMessagesTotalThisClient = 6;
                TestDeferredMessageManager.BeforePurgeDelegate beforePurge = (manager, key) =>
                {
                    ++purgeCount;
                    var elapsed = Time.realtimeSinceStartup - start;
                    Assert.GreaterOrEqual(elapsed, timeout - 0.05f);
                    Assert.AreEqual(remainingMessagesTotalThisClient, manager.DeferredMessageCountTotal());
                    Assert.AreEqual(remainingMessagesTotalThisClient, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                    Assert.AreEqual(3, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, key));
                    remainingMessagesTotalThisClient -= 3;
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforePurge = beforePurge;
            }

            yield return new WaitForSeconds(timeout + 0.1f);
            foreach (var client in m_ClientNetworkManagers)
            {
                AddPrefabsToClient(client);
            }
            Assert.AreEqual(m_NumberOfClientsToLateJoin * 2, purgeCount);
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
            }
        }

        [UnityTest]
        public IEnumerator WhenADeferredMessageIsRemoved_OtherMessagesForSameObjectAreRemoved([Values(1, 2, 3)] int timeout)
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            CatchSpawns();
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = timeout;
            }
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns();

            var start = 0f;

            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforeDeferDelegate beforeDefer = (manager, key) =>
                {
                    if (start == 0)
                    {
                        start = Time.realtimeSinceStartup;
                    }
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforeDefer = beforeDefer;
            }

            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return RunMultiple(WaitForAllClientsToReceive<ChangeOwnershipMessage>());

            yield return new WaitForSeconds(timeout - 0.5f);

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(1, manager.DeferredMessageCountTotal());
                Assert.AreEqual(1, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
            }

            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ServerNetworkManager.LocalClientId);
            yield return RunMultiple(WaitForAllClientsToReceive<ChangeOwnershipMessage>());

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(2, manager.DeferredMessageCountTotal());
                Assert.AreEqual(2, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(2, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
            }

            foreach (var unused in m_ClientNetworkManagers)
            {
                LogAssert.Expect(LogType.Warning, $"[Netcode] Deferred messages were received for a trigger of type {IDeferredMessageManager.TriggerType.OnSpawn} with key {serverObject.GetComponent<NetworkObject>().NetworkObjectId}, but that trigger was not received within within {timeout} second(s).");
            }

            int purgeCount = 0;
            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforePurgeDelegate beforePurge = (manager, key) =>
                {
                    ++purgeCount;
                    var elapsed = Time.realtimeSinceStartup - start;
                    Assert.GreaterOrEqual(elapsed, timeout - 0.05f);
                    Assert.AreEqual(2, manager.DeferredMessageCountTotal());
                    Assert.AreEqual(2, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                    Assert.AreEqual(2, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, key));
                    Assert.AreEqual(serverObject.GetComponent<NetworkObject>().NetworkObjectId, key);
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforePurge = beforePurge;
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                AddPrefabsToClient(client);
            }

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(m_NumberOfClientsToLateJoin, purgeCount);
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(0, manager.DeferredMessageCountTotal());
            }
        }

        [UnityTest]
        public IEnumerator WhenADeferredMessageIsRemoved_OtherMessagesForDifferentObjectsAreNotRemoved([Values(1, 2, 3)] int timeout)
        {
            m_SkipAddingPrefabsToClient = true;
            yield return SpawnClients();
            CatchSpawns();
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.SpawnTimeout = timeout;
            }
            var serverObject = Object.Instantiate(m_RpcPrefab);
            serverObject.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject.GetComponent<NetworkObject>().Spawn();
            var serverObject2 = Object.Instantiate(m_RpcPrefab);
            serverObject2.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
            serverObject2.GetComponent<NetworkObject>().Spawn();
            yield return WaitForClientsToCatchSpawns(2);

            var start = 0f;

            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforeDeferDelegate beforeDefer = (manager, key) =>
                {
                    if (start == 0)
                    {
                        start = Time.realtimeSinceStartup;
                    }
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforeDefer = beforeDefer;
            }

            serverObject.GetComponent<NetworkObject>().ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            yield return RunMultiple(WaitForAllClientsToReceive<ChangeOwnershipMessage>());

            yield return new WaitForSeconds(timeout - 0.5f);

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(1, manager.DeferredMessageCountTotal());
                Assert.AreEqual(1, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
                Assert.AreEqual(0, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject2.GetComponent<NetworkObject>().NetworkObjectId));
            }

            serverObject2.GetComponent<NetworkObject>().ChangeOwnership(m_ServerNetworkManager.LocalClientId);
            yield return RunMultiple(WaitForAllClientsToReceive<ChangeOwnershipMessage>());

            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(2, manager.DeferredMessageCountTotal());
                Assert.AreEqual(2, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject2.GetComponent<NetworkObject>().NetworkObjectId));
            }

            foreach (var unused in m_ClientNetworkManagers)
            {
                LogAssert.Expect(LogType.Warning, $"[Netcode] Deferred messages were received for a trigger of type {IDeferredMessageManager.TriggerType.OnSpawn} with key {serverObject.GetComponent<NetworkObject>().NetworkObjectId}, but that trigger was not received within within {timeout} second(s).");
            }

            int purgeCount = 0;
            foreach (var client in m_ClientNetworkManagers)
            {
                TestDeferredMessageManager.BeforePurgeDelegate beforePurge = (manager, key) =>
                {
                    ++purgeCount;
                    var elapsed = Time.realtimeSinceStartup - start;
                    Assert.GreaterOrEqual(elapsed, timeout - 0.05f);
                    Assert.AreEqual(2, manager.DeferredMessageCountTotal());
                    Assert.AreEqual(2, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));

                    Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
                    Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject2.GetComponent<NetworkObject>().NetworkObjectId));

                    Assert.AreEqual(serverObject.GetComponent<NetworkObject>().NetworkObjectId, key);
                };
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforePurge = beforePurge;
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                AddPrefabsToClient(client);
            }

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(m_NumberOfClientsToLateJoin, purgeCount);
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                Assert.AreEqual(1, manager.DeferredMessageCountTotal());
                Assert.AreEqual(1, manager.DeferredMessageCountForType(IDeferredMessageManager.TriggerType.OnSpawn));
                Assert.AreEqual(0, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject.GetComponent<NetworkObject>().NetworkObjectId));
                Assert.AreEqual(1, manager.DeferredMessageCountForKey(IDeferredMessageManager.TriggerType.OnSpawn, serverObject2.GetComponent<NetworkObject>().NetworkObjectId));
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                var manager = (TestDeferredMessageManager)client.DeferredMessageManager;
                manager.OnBeforePurge = null;
            }
        }
    }
}
