using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVarBufferCopyTest : BaseMultiInstanceTest
    {
        public class DummyNetVar : NetworkVariableBase
        {
            private const int k_DummyValue = 0x13579BDF;
            public bool DeltaWritten;
            public bool FieldWritten;
            public bool DeltaRead;
            public bool FieldRead;
            public bool Dirty = false;

            public override void ResetDirty()
            {
                Dirty = false;
            }

            public override bool IsDirty()
            {
                return Dirty;
            }

            public override void WriteDelta(FastBufferWriter writer)
            {
                writer.TryBeginWrite(FastBufferWriter.GetWriteSize(k_DummyValue) + 1);
                using (var bitWriter = writer.EnterBitwiseContext())
                {
                    bitWriter.WriteBits((byte)1, 1);
                }
                writer.WriteValue(k_DummyValue);

                DeltaWritten = true;
                AdvanceTimeOutPeriod();
            }

            public override void WriteField(FastBufferWriter writer)
            {
                writer.TryBeginWrite(FastBufferWriter.GetWriteSize(k_DummyValue) + 1);
                using (var bitWriter = writer.EnterBitwiseContext())
                {
                    bitWriter.WriteBits((byte)1, 1);
                }
                writer.WriteValue(k_DummyValue);

                FieldWritten = true;
                AdvanceTimeOutPeriod();
            }

            public override void ReadField(FastBufferReader reader)
            {
                reader.TryBeginRead(FastBufferWriter.GetWriteSize(k_DummyValue) + 1);
                using (var bitReader = reader.EnterBitwiseContext())
                {
                    bitReader.ReadBits(out byte b, 1);
                }

                reader.ReadValue(out int i);
                Assert.AreEqual(k_DummyValue, i);

                FieldRead = true;
                AdvanceTimeOutPeriod();
            }

            public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
            {
                reader.TryBeginRead(FastBufferWriter.GetWriteSize(k_DummyValue) + 1);
                using (var bitReader = reader.EnterBitwiseContext())
                {
                    bitReader.ReadBits(out byte b, 1);
                }

                reader.ReadValue(out int i);
                Assert.AreEqual(k_DummyValue, i);

                DeltaRead = true;
                AdvanceTimeOutPeriod();
            }
        }

        public class DummyNetBehaviour : NetworkBehaviour
        {
            public DummyNetVar NetVar = new DummyNetVar();

            public override void OnNetworkSpawn()
            {
                if (!IsServer)
                {
                    ClientDummyNetBehaviourSpawned(this);
                }
                base.OnNetworkSpawn();
            }
        }
        protected override int NbClients => 1;

        private static List<DummyNetBehaviour> s_ClientDummyNetBehavioursSpawned = new List<DummyNetBehaviour>();
        public static void ClientDummyNetBehaviourSpawned(DummyNetBehaviour dummyNetBehaviour)
        {
            s_ClientDummyNetBehavioursSpawned.Add(dummyNetBehaviour);
            AdvanceTimeOutPeriod();
        }

        private const float k_TimeOutWaitPeriod = 5.0f;
        private static float s_TimeOutPeriod;

        /// <summary>
        /// This will simply advance the timeout period
        /// </summary>
        public static void AdvanceTimeOutPeriod()
        {
            s_TimeOutPeriod = Time.realtimeSinceStartup + k_TimeOutWaitPeriod;
        }

        /// <summary>
        /// Checks if the timeout period has elapsed
        /// </summary>
        private static bool HasTimedOut()
        {
            return s_TimeOutPeriod <= Time.realtimeSinceStartup;
        }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            s_ClientDummyNetBehavioursSpawned.Clear();
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var dummyNetBehaviour = playerPrefab.AddComponent<DummyNetBehaviour>();
                });
        }


        [UnityTest]
        public IEnumerator TestEntireBufferIsCopiedOnNetworkVariableDelta()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ClientNetworkManagers[0], clientClientPlayerResult));

            var serverSideClientPlayer = serverClientPlayerResult.Result;
            var clientSideClientPlayer = clientClientPlayerResult.Result;

            var serverComponent = serverSideClientPlayer.GetComponent<DummyNetBehaviour>();
            var clientComponent = clientSideClientPlayer.GetComponent<DummyNetBehaviour>();

            var timedOut = false;
            AdvanceTimeOutPeriod();
            while (!HasTimedOut())
            {
                if (s_ClientDummyNetBehavioursSpawned.Count >= 1)
                {
                    break;
                }
                yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
                timedOut = HasTimedOut();
            }

            Assert.False(timedOut, "Timed out waiting for client side DummyNetBehaviour to register it was spawned!");

            // Send an update
            serverComponent.NetVar.Dirty = true;

            yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            Assert.True(serverComponent.NetVar.FieldWritten);
            yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            serverComponent.NetVar.Dirty = true;
            Assert.True(serverComponent.NetVar.DeltaWritten);

            timedOut = false;
            AdvanceTimeOutPeriod();
            while (!HasTimedOut())
            {
                if (clientComponent.NetVar.FieldRead && clientComponent.NetVar.DeltaRead)
                {
                    break;
                }
                yield return new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
                timedOut = HasTimedOut();
            }

            var timedOutMessage = "Timed out waiting for client reads: ";
            if (timedOut)
            {
                if (!clientComponent.NetVar.FieldRead)
                {
                    timedOutMessage += "[FieldRead]";
                }

                if (!clientComponent.NetVar.DeltaRead)
                {
                    timedOutMessage += "[DeltaRead]";
                }
            }

            Assert.False(timedOut, timedOutMessage);
        }
    }
}
