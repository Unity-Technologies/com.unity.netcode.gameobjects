using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkVarBufferCopyTest : BaseMultiInstanceTest
    {
        public class DummyNetVar : INetworkVariable
        {
            private const int k_DummyValue = 0x13579BDF;
            public bool DeltaWritten;
            public bool FieldWritten;
            public bool DeltaRead;
            public bool FieldRead;
            public bool Dirty = true;

            public override void ResetDirty()
            {
                Dirty = false;
            }

            public override bool IsDirty()
            {
                return Dirty;
            }
            public override NetworkChannel GetChannel()
            {
                return NetworkChannel.NetworkVariable;
            }

            public override void WriteDelta(Stream stream)
            {
                using (var writer = PooledNetworkWriter.Get(stream))
                {
                    writer.WriteBits((byte)1, 1);
                    writer.WriteInt32(k_DummyValue);
                }

                DeltaWritten = true;
            }

            public override void WriteField(Stream stream)
            {
                using (var writer = PooledNetworkWriter.Get(stream))
                {
                    writer.WriteBits((byte)1, 1);
                    writer.WriteInt32(k_DummyValue);
                }

                FieldWritten = true;
            }

            public override void ReadField(Stream stream)
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    reader.ReadBits(1);
                    Assert.AreEqual(k_DummyValue, reader.ReadInt32());
                }

                FieldRead = true;
            }

            public override void ReadDelta(Stream stream, bool keepDirtyDelta)
            {
                using (var reader = PooledNetworkReader.Get(stream))
                {
                    reader.ReadBits(1);
                    Assert.AreEqual(k_DummyValue, reader.ReadInt32());
                }

                DeltaRead = true;
            }
        }

        public class DummyNetBehaviour : NetworkBehaviour
        {
            public DummyNetVar NetVar;
        }
        protected override int NbClients => 1;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
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

            var serverComponent = (serverSideClientPlayer).GetComponent<DummyNetBehaviour>();
            var clientComponent = (clientSideClientPlayer).GetComponent<DummyNetBehaviour>();

            var waitResult = new MultiInstanceHelpers.CoroutineResultWrapper<bool>();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(
                () => clientComponent.NetVar.DeltaRead == true,
                waitResult,
                maxFrames: 120));

            if (!waitResult.Result)
            {
                Assert.Fail("Failed to send a delta within 120 frames");
            }
            Assert.True(serverComponent.NetVar.FieldWritten);
            Assert.True(serverComponent.NetVar.DeltaWritten);
            Assert.True(clientComponent.NetVar.FieldRead);
            Assert.True(clientComponent.NetVar.DeltaRead);
        }
    }
}
