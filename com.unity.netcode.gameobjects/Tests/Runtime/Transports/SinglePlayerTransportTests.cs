using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.SinglePlayer;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace Unity.Netcode.RuntimeTests
{
    internal class SinglePlayerTransportTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        public struct SerializableStruct : INetworkSerializable, IEquatable<SerializableStruct>
        {
            public bool BoolValue;
            public ulong ULongValue;

            public bool Equals(SerializableStruct other)
            {
                return other.BoolValue == BoolValue && other.ULongValue == ULongValue;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref BoolValue);
                serializer.SerializeValue(ref ULongValue);
            }
        }

        public class SinglePlayerTestComponent : NetworkBehaviour
        {
            private enum SpawnStates
            {
                PreSpawn,
                Spawn,
                PostSpawn,
            }

            private enum RpcInvocations
            {
                SendToServerRpc,
                SendToEveryoneRpc,
                SendToOwnerRpc,
            }

            private Dictionary<SpawnStates, int> m_SpawnStateInvoked = new Dictionary<SpawnStates, int>();
            private Dictionary<RpcInvocations, int> m_RpcInvocations = new Dictionary<RpcInvocations, int>();
            private NetworkVariable<int> m_IntValue = new NetworkVariable<int>();
            private NetworkVariable<SerializableStruct> m_SerializableValue = new NetworkVariable<SerializableStruct>();


            private void SpawnStateInvoked(SpawnStates spawnState)
            {
                if (!m_SpawnStateInvoked.ContainsKey(spawnState))
                {
                    m_SpawnStateInvoked.Add(spawnState, 1);
                }
                else
                {
                    m_SpawnStateInvoked[spawnState]++;
                }
            }

            private void RpcInvoked(RpcInvocations rpcInvocation)
            {
                if (!m_RpcInvocations.ContainsKey(rpcInvocation))
                {
                    m_RpcInvocations.Add(rpcInvocation, 1);
                }
                else
                {
                    m_RpcInvocations[rpcInvocation]++;
                }
            }

            private void ValidateValues(int someIntValue, SerializableStruct someValues)
            {
                Assert.IsTrue(m_IntValue.Value == someIntValue);
                Assert.IsTrue(someValues.BoolValue == m_SerializableValue.Value.BoolValue);
                Assert.IsTrue(someValues.ULongValue == m_SerializableValue.Value.ULongValue);
            }

            [Rpc(SendTo.Server)]
            private void SendToServerRpc(int someIntValue, SerializableStruct someValues, RpcParams rpcParams = default)
            {
                ValidateValues(someIntValue, someValues);
                RpcInvoked(RpcInvocations.SendToServerRpc);
            }

            [Rpc(SendTo.Everyone)]
            private void SendToEveryoneRpc(int someIntValue, SerializableStruct someValues, RpcParams rpcParams = default)
            {
                ValidateValues(someIntValue, someValues);
                RpcInvoked(RpcInvocations.SendToEveryoneRpc);
            }

            [Rpc(SendTo.Owner)]
            private void SendToOwnerRpc(int someIntValue, SerializableStruct someValues, RpcParams rpcParams = default)
            {
                ValidateValues(someIntValue, someValues);
                RpcInvoked(RpcInvocations.SendToOwnerRpc);
            }


            protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
            {
                SpawnStateInvoked(SpawnStates.PreSpawn);
                base.OnNetworkPreSpawn(ref networkManager);
            }

            public override void OnNetworkSpawn()
            {
                SpawnStateInvoked(SpawnStates.Spawn);
                m_IntValue.Value = Random.Range(0, 100);
                m_SerializableValue.Value = new SerializableStruct()
                {
                    BoolValue = Random.Range(0, 100) >= 50.0 ? true : false,
                    ULongValue = (ulong)Random.Range(0, 100000),
                };
                base.OnNetworkSpawn();
            }

            protected override void OnNetworkPostSpawn()
            {
                SpawnStateInvoked(SpawnStates.PostSpawn);
                SendToServerRpc(m_IntValue.Value, m_SerializableValue.Value);
                SendToEveryoneRpc(m_IntValue.Value, m_SerializableValue.Value);
                SendToOwnerRpc(m_IntValue.Value, m_SerializableValue.Value);
                base.OnNetworkPostSpawn();
            }

            public void ValidateStatesAndRpcInvocations()
            {
                foreach (var entry in m_SpawnStateInvoked)
                {
                    Assert.True(entry.Value == 1, $"{entry.Key} failed with {entry.Value} invocations!");
                }
                foreach (var entry in m_RpcInvocations)
                {
                    Assert.True(entry.Value == 1, $"{entry.Key} failed with {entry.Value} invocations!");
                }
            }
        }

        private GameObject m_PrefabToSpawn;
        private bool m_CanStartHost;

        protected override IEnumerator OnSetup()
        {
            m_CanStartHost = false;
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<SinglePlayerTestComponent>();
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {
            var singlePlayerTransport = m_ServerNetworkManager.gameObject.AddComponent<SinglePlayerTransport>();
            m_ServerNetworkManager.NetworkConfig.NetworkTransport = singlePlayerTransport;
            m_PrefabToSpawn = CreateNetworkObjectPrefab("TestObject");
            m_PrefabToSpawn.AddComponent<SinglePlayerTestComponent>();
            base.OnServerAndClientsCreated();
        }

        protected override bool CanStartServerAndClients()
        {
            return m_CanStartHost;
        }

        [UnityTest]
        public IEnumerator StartSinglePlayerAndSpawn()
        {
            m_CanStartHost = true;

            yield return StartServerAndClients();

            var spawnedInstance = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager).GetComponent<NetworkObject>();
            var testComponent = spawnedInstance.GetComponent<SinglePlayerTestComponent>();
            yield return s_DefaultWaitForTick;
            var playerTestComponent = m_ServerNetworkManager.LocalClient.PlayerObject.GetComponent<SinglePlayerTestComponent>();
            testComponent.ValidateStatesAndRpcInvocations();
            playerTestComponent.ValidateStatesAndRpcInvocations();
        }

        [UnityTest]
        public IEnumerator StartSinglePlayerAsClientError()
        {
            LogAssert.Expect(LogType.Error, $"[Netcode] {SinglePlayerTransport.NotStartingAsHostErrorMessage}");
            LogAssert.Expect(LogType.Error, $"[Netcode] Client is shutting down due to network transport start failure of {nameof(SinglePlayerTransport)}!");
            Assert.IsFalse(m_ServerNetworkManager.StartClient());
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartSinglePlayerAsServerError()
        {
            LogAssert.Expect(LogType.Error, $"[Netcode] {SinglePlayerTransport.NotStartingAsHostErrorMessage}");
            LogAssert.Expect(LogType.Error, $"[Netcode] Server is shutting down due to network transport start failure of {nameof(SinglePlayerTransport)}!");
            Assert.IsFalse(m_ServerNetworkManager.StartServer());
            yield return null;
        }
    }
}
