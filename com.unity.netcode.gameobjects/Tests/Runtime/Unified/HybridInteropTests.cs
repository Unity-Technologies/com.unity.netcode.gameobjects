#if UNIFIED_NETCODE
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// The N4E half of the interop prefab: relays pings between unified remotes and NGO RPCs, and sends an NGO RPC or
    /// writes a NetworkVariable from <see cref="GhostBehaviour.PredictionUpdate"/> on request.
    /// </summary>
    internal partial class HybridInteropGhost : GhostBehaviour
    {
        public bool SendRpcFromPrediction;
        public bool WriteNetworkVariableFromPrediction;
        public int PredictionSends;
        public int PredictionWrites;
        public int PingValue;

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            if (IsServer)
            {
                return;
            }
            var networkBehaviour = GetComponent<HybridInteropNetworkBehaviour>();
            if (SendRpcFromPrediction)
            {
                PredictionSends++;
                networkBehaviour.PredictionRpc();
            }
            if (WriteNetworkVariableFromPrediction)
            {
                PredictionWrites++;
                networkBehaviour.OwnerWrittenValue.Value++;
            }
        }

        [RPC(SendDirection.ServerToClient)]
        public void PingToClient(int value)
        {
            PingValue = value;
            GetComponent<HybridInteropNetworkBehaviour>().PingToServerRpc(value + 1);
        }

        [RPC(SendDirection.ServerToClient)]
        public void FinalPingToClient(int value)
        {
            PingValue = value;
        }

        [RPC(SendDirection.ClientToServer)]
        public void PingToServer(int value)
        {
            PingValue = value;
            GetComponent<HybridInteropNetworkBehaviour>().FinalPingToClientRpc(value + 1);
        }
    }

    /// <summary>
    /// The NGO half of the interop prefab.
    /// </summary>
    internal class HybridInteropNetworkBehaviour : NetworkBehaviour
    {
        public int PingValue;
        public int PredictionRpcsReceived;
        public NetworkVariable<int> OwnerWrittenValue = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
        public bool GhostWasPredictedOnSpawn;
        public NetworkId GhostOwnerOnSpawn;

        public override void OnNetworkSpawn()
        {
            var ghost = GetComponent<GhostObject>();
            GhostWasPredictedOnSpawn = ghost.CanWriteState;
            GhostOwnerOnSpawn = ghost.OwnerNetworkId;
        }

        [Rpc(SendTo.Server)]
        public void PredictionRpc()
        {
            PredictionRpcsReceived++;
        }

        [Rpc(SendTo.Server)]
        public void PingToServerRpc(int value)
        {
            PingValue = value;
            GetComponent<HybridInteropGhost>().FinalPingToClient(value + 1);
        }

        [Rpc(SendTo.NotServer)]
        public void PingToClientRpc(int value)
        {
            PingValue = value;
            GetComponent<HybridInteropGhost>().PingToServer(value + 1);
        }

        [Rpc(SendTo.NotServer)]
        public void FinalPingToClientRpc(int value)
        {
            PingValue = value;
        }
    }

    /// <summary>
    /// Combines N4E remotes and prediction with NGO RPCs on the same hybrid prefab.
    /// </summary>
    /// <remarks>
    /// One client only: a ClientToServer remote is sent from every client world in the process.
    /// </remarks>
    [TestFixture(HostOrServer.UnifiedHost)]
    [TestFixture(HostOrServer.UnifiedServer)]
    internal class HybridInteropTests : NetcodeIntegrationTest
    {
        // Enough prediction ticks that a second warning would have been logged if it was not limited to one per NetworkBehaviour
        private const int k_MinimumPredictionCalls = 20;
        private const string k_PredictionLoopWarning = "Netcode for Entities prediction loop, which is not supported";

        protected override int NumberOfClients => 1;

        private GameObject m_InteropPrefab;
        private NetworkObject m_ServerInstance;
        private NetworkObject m_ClientInstance;
        private int m_PredictionLoopWarnings;

        public HybridInteropTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        protected override bool UseUnifiedTests()
        {
            return true;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_InteropPrefab = CreateHybridPrefab("InteropPrefab", true, GhostMode.OwnerPredicted);
            m_InteropPrefab.AddComponent<HybridInteropGhost>();
            m_InteropPrefab.AddComponent<HybridInteropNetworkBehaviour>();
            base.OnServerAndClientsCreated();
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            var client = m_ClientNetworkManagers[0];
            m_ServerInstance = SpawnObject(m_InteropPrefab, client).GetComponent<NetworkObject>();
            var clientNetworkId = client.NetcodeWorld.LocalConnection.NetworkId;
            Assert.AreEqual(clientNetworkId, m_ServerInstance.GetComponent<GhostObject>().OwnerNetworkId, "Spawning with an NGO owner did not set the ghost owner!");
            Assert.AreEqual(clientNetworkId, m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>().GhostOwnerOnSpawn,
                "The ghost owner was not set yet when OnNetworkSpawn was invoked!");
            yield return WaitForSpawnedOnAllOrTimeOut(m_ServerInstance);
            AssertOnTimeout($"Timed out waiting for {m_ServerInstance.name} to spawn on all clients!");
            m_ClientInstance = client.SpawnManager.SpawnedObjects[m_ServerInstance.NetworkObjectId];
            yield return WaitForConditionOrTimeOut(() => m_ClientInstance.GetComponent<GhostObject>().CanWriteState);
            AssertOnTimeout($"{m_ClientInstance.name} never became predicted on the client!");

            m_PredictionLoopWarnings = 0;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        protected override IEnumerator OnTearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            return base.OnTearDown();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Warning && condition.Contains(k_PredictionLoopWarning))
            {
                m_PredictionLoopWarnings++;
            }
        }

        /// <summary>
        /// Unified remote to NGO RPC to unified remote, starting on the server.
        /// </summary>
        [UnityTest]
        public IEnumerator UnifiedRemoteToNgoRpcToUnifiedRemote()
        {
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            m_ServerInstance.GetComponent<HybridInteropGhost>().PingToClient(1);
            yield return WaitForConditionOrTimeOut(() => clientGhost.PingValue == 3);
            AssertOnTimeout($"Ping did not complete! Client remote value: {clientGhost.PingValue}, " +
                $"server RPC value: {m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>().PingValue}");
        }

        /// <summary>
        /// NGO RPC to unified remote to NGO RPC, starting on the server.
        /// </summary>
        [UnityTest]
        public IEnumerator NgoRpcToUnifiedRemoteToNgoRpc()
        {
            var clientBehaviour = m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>();
            m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>().PingToClientRpc(1);
            yield return WaitForConditionOrTimeOut(() => clientBehaviour.PingValue == 3);
            AssertOnTimeout($"Ping did not complete! Client RPC value: {clientBehaviour.PingValue}, " +
                $"server remote value: {m_ServerInstance.GetComponent<HybridInteropGhost>().PingValue}");
        }

        /// <summary>
        /// Sending an NGO RPC from <see cref="GhostBehaviour.PredictionUpdate"/> is not supported and logs one warning per NetworkBehaviour.
        /// </summary>
        [UnityTest]
        public IEnumerator RpcSentFromPredictionUpdateLogsWarningOnce()
        {
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            clientGhost.SendRpcFromPrediction = true;
            yield return WaitForConditionOrTimeOut(() => clientGhost.PredictionSends >= k_MinimumPredictionCalls);
            clientGhost.SendRpcFromPrediction = false;
            AssertOnTimeout($"Client prediction only sent {clientGhost.PredictionSends} RPCs!");

            // Wait for the RPCs to reach the server, so none is still queued at teardown.
            var serverBehaviour = m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>();
            yield return WaitForConditionOrTimeOut(() => serverBehaviour.PredictionRpcsReceived == clientGhost.PredictionSends);
            AssertOnTimeout($"Server received {serverBehaviour.PredictionRpcsReceived} of {clientGhost.PredictionSends} RPCs!");

            Assert.AreEqual(1, m_PredictionLoopWarnings, $"Expected one prediction loop warning for {clientGhost.PredictionSends} RPCs sent from prediction.");
        }

        /// <summary>
        /// Writing a NetworkVariable from <see cref="GhostBehaviour.PredictionUpdate"/> is not supported and logs one warning per NetworkBehaviour.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkVariableWrittenFromPredictionUpdateLogsWarningOnce()
        {
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            var clientBehaviour = m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>();
            clientGhost.WriteNetworkVariableFromPrediction = true;
            yield return WaitForConditionOrTimeOut(() => clientGhost.PredictionWrites >= k_MinimumPredictionCalls);
            clientGhost.WriteNetworkVariableFromPrediction = false;
            AssertOnTimeout($"Client prediction only wrote the NetworkVariable {clientGhost.PredictionWrites} times!");

            // Wait for the last write to reach the server, so no NetworkVariable update is still queued at teardown.
            var serverBehaviour = m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>();
            yield return WaitForConditionOrTimeOut(() => serverBehaviour.OwnerWrittenValue.Value == clientBehaviour.OwnerWrittenValue.Value);
            AssertOnTimeout($"Server value {serverBehaviour.OwnerWrittenValue.Value} never matched the client value {clientBehaviour.OwnerWrittenValue.Value}!");

            Assert.AreEqual(1, m_PredictionLoopWarnings, $"Expected one prediction loop warning for {clientGhost.PredictionWrites} NetworkVariable writes from prediction.");
        }

        /// <summary>
        /// The same RPC and NetworkVariable write made outside the prediction loop do not warn.
        /// </summary>
        [UnityTest]
        public IEnumerator RpcAndNetworkVariableOutsidePredictionLoopDoNotWarn()
        {
            var clientBehaviour = m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>();
            var serverBehaviour = m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>();
            clientBehaviour.PredictionRpc();
            clientBehaviour.OwnerWrittenValue.Value++;
            yield return WaitForConditionOrTimeOut(() => serverBehaviour.PredictionRpcsReceived == 1 && serverBehaviour.OwnerWrittenValue.Value == clientBehaviour.OwnerWrittenValue.Value);
            AssertOnTimeout($"Server received {serverBehaviour.PredictionRpcsReceived} RPCs and value {serverBehaviour.OwnerWrittenValue.Value} (client value {clientBehaviour.OwnerWrittenValue.Value})!");

            Assert.AreEqual(0, m_PredictionLoopWarnings, "An RPC or NetworkVariable write made outside the prediction loop logged a prediction loop warning.");
        }

        /// <summary>
        /// An NGO ownership change also changes the ghost's owner, so the new NGO owner is the one that predicts.
        /// </summary>
        [UnityTest]
        public IEnumerator NgoOwnershipChangeUpdatesGhostOwner()
        {
            var client = m_ClientNetworkManagers[0];
            var serverGhost = m_ServerInstance.GetComponent<GhostObject>();
            var clientGhost = m_ClientInstance.GetComponent<GhostObject>();

            // The host's own client owns the ghost when the host takes ownership. A server without a local client leaves it unowned.
            var serverOwnerNetworkId = m_ServerNetworkManager.IsHost ? m_ServerNetworkManager.NetcodeWorld.LocalConnection.NetworkId : default;
            m_ServerInstance.ChangeOwnership(m_ServerNetworkManager.LocalClientId);
            Assert.AreEqual(serverOwnerNetworkId, serverGhost.OwnerNetworkId, "The ghost owner did not follow the NGO owner to the server!");
            yield return WaitForConditionOrTimeOut(() => m_ClientInstance.OwnerClientId == m_ServerNetworkManager.LocalClientId && clientGhost.OwnerNetworkId.Equals(serverOwnerNetworkId));
            AssertOnTimeout($"Client never saw the ghost owner change! NGO owner: {m_ClientInstance.OwnerClientId}, ghost owner: {clientGhost.OwnerNetworkId.Value}");
            Debug.Log($"Client ghost predicted after losing ownership: {clientGhost.CanWriteState}. Predicted on spawn: " +
                $"{m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>().GhostWasPredictedOnSpawn}");

            var clientNetworkId = client.NetcodeWorld.LocalConnection.NetworkId;
            m_ServerInstance.ChangeOwnership(client.LocalClientId);
            Assert.AreEqual(clientNetworkId, serverGhost.OwnerNetworkId, "The ghost owner did not follow the NGO owner back to the client!");
            yield return WaitForConditionOrTimeOut(() => clientGhost.OwnerNetworkId.Equals(clientNetworkId) && clientGhost.CanWriteState);
            AssertOnTimeout($"The client did not predict the ghost after regaining ownership! Ghost owner: {clientGhost.OwnerNetworkId.Value}, predicted: {clientGhost.CanWriteState}");
        }
    }
}
#endif
