using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVarBufferCopyTest : NetcodeIntegrationTest
    {
        public class DummyNetVar : NetworkVariableBase
        {
            private const int k_DummyValue = 0x13579BDF;
            public bool DeltaWritten;
            public bool FieldWritten;
            public bool DeltaRead;
            public bool FieldRead;

            public override void WriteDelta(FastBufferWriter writer)
            {
                writer.TryBeginWrite(FastBufferWriter.GetWriteSize(k_DummyValue) + 1);
                using (var bitWriter = writer.EnterBitwiseContext())
                {
                    bitWriter.WriteBits((byte)1, 1);
                }
                writer.WriteValue(k_DummyValue);

                DeltaWritten = true;
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
        protected override int NumberOfClients => 1;

        private static List<DummyNetBehaviour> s_ClientDummyNetBehavioursSpawned = new List<DummyNetBehaviour>();
        public static void ClientDummyNetBehaviourSpawned(DummyNetBehaviour dummyNetBehaviour)
        {
            s_ClientDummyNetBehavioursSpawned.Add(dummyNetBehaviour);
        }

        protected override IEnumerator OnSetup()
        {
            s_ClientDummyNetBehavioursSpawned.Clear();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<DummyNetBehaviour>();
        }

        [UnityTest]
        public IEnumerator TestEntireBufferIsCopiedOnNetworkVariableDelta()
        {
            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, serverClientPlayerResult);

            var serverSideClientPlayer = serverClientPlayerResult.Result;
            var serverComponent = serverSideClientPlayer.GetComponent<DummyNetBehaviour>();

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ClientNetworkManagers[0], clientClientPlayerResult);

            var clientSideClientPlayer = clientClientPlayerResult.Result;
            var clientComponent = clientSideClientPlayer.GetComponent<DummyNetBehaviour>();

            // Wait for the DummyNetBehaviours on the client side to notify they have been initialized and spawned
            yield return WaitForConditionOrTimeOut(() => s_ClientDummyNetBehavioursSpawned.Count >= 1);
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client side DummyNetBehaviour to register it was spawned!");

            // Check that FieldWritten is written when dirty
            serverComponent.NetVar.SetDirty(true);
            yield return s_DefaultWaitForTick;
            Assert.True(serverComponent.NetVar.FieldWritten);

            // Check that DeltaWritten is written when dirty
            serverComponent.NetVar.SetDirty(true);
            yield return s_DefaultWaitForTick;
            Assert.True(serverComponent.NetVar.DeltaWritten);

            // Check that both FieldRead and DeltaRead were invoked on the client side
            yield return WaitForConditionOrTimeOut(() => clientComponent.NetVar.FieldRead == true && clientComponent.NetVar.DeltaRead == true);

            var timedOutMessage = "Timed out waiting for client reads: ";
            if (s_GlobalTimeoutHelper.TimedOut)
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
            Assert.IsFalse(s_GlobalTimeoutHelper.TimedOut, timedOutMessage);
        }
    }
}
