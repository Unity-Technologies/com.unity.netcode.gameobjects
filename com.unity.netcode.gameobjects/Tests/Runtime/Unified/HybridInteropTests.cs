#if UNIFIED_NETCODE
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// A NetworkVariable value stamped with the tick it applies from, so prediction can apply it tick-aligned.
    /// </summary>
    internal struct TickStampedValue : INetworkSerializable, IEquatable<TickStampedValue>
    {
        public int Value;
        public int PreviousValue;
        public uint Tick;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Value);
            serializer.SerializeValue(ref PreviousValue);
            serializer.SerializeValue(ref Tick);
        }

        public bool Equals(TickStampedValue other)
        {
            return Value == other.Value && PreviousValue == other.PreviousValue && Tick == other.Tick;
        }
    }

    /// <summary>
    /// The N4E half of the interop prefab: sends NGO RPCs from <see cref="GhostBehaviour.PredictionUpdate"/> and
    /// relays pings between unified remotes and NGO RPCs.
    /// </summary>
    internal partial class HybridInteropGhost : GhostBehaviour
    {
        public bool SendRpcFromPrediction;
        public bool GateOnFirstTimeTick;
        public int PredictionSends;
        public int ResimulatedTicks;
        public int PingValue;

        public bool WriteNetworkVariableFromPrediction;
        public bool RecordStampedValue;
        public uint LatestPredictedTick;
        public int EarlyReadsOfNewValue;
        public int InconsistentRawTicks;
        public int InconsistentStampedTicks;
        public int StampedValueAtStampTick;
        private readonly Dictionary<uint, int> m_RawValueByTick = new Dictionary<uint, int>();
        private readonly Dictionary<uint, int> m_StampedValueByTick = new Dictionary<uint, int>();

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            if (IsServer)
            {
                return;
            }
            var networkTime = Ghost.World.NetworkTime;
            if (RecordStampedValue)
            {
                RecordStampedValueAtTick(networkTime.ServerTick.TickIndexForValidTick);
            }
            if (WriteNetworkVariableFromPrediction && (!GateOnFirstTimeTick || networkTime.IsFirstTimeFullyPredictingTick))
            {
                GetComponent<HybridInteropNetworkBehaviour>().OwnerWrittenTick.Value = networkTime.ServerTick.TickIndexForValidTick;
            }
            if (!SendRpcFromPrediction)
            {
                return;
            }
            if (!networkTime.IsFirstTimeFullyPredictingTick)
            {
                ResimulatedTicks++;
                if (GateOnFirstTimeTick)
                {
                    return;
                }
            }
            PredictionSends++;
            GetComponent<HybridInteropNetworkBehaviour>().PredictionTickRpc(networkTime.ServerTick.SerializedData);
        }

        /// <summary>
        /// Records, per predicted tick, the raw NetworkVariable value and the value the tick stamp says applies to that tick.
        /// </summary>
        private void RecordStampedValueAtTick(uint tick)
        {
            var stamped = GetComponent<HybridInteropNetworkBehaviour>().StampedValue.Value;
            var hasStamp = stamped.Tick != 0;
            if (hasStamp && tick < stamped.Tick && stamped.Value == HybridInteropNetworkBehaviour.StampedNewValue)
            {
                EarlyReadsOfNewValue++;
            }
            // The pattern under test: apply the value only when the tick being predicted is at or past its stamp.
            var applied = hasStamp && tick >= stamped.Tick ? stamped.Value : stamped.PreviousValue;
            if (hasStamp && tick == stamped.Tick)
            {
                StampedValueAtStampTick = applied;
            }
            InconsistentRawTicks += RecordValue(m_RawValueByTick, tick, stamped.Value);
            InconsistentStampedTicks += RecordValue(m_StampedValueByTick, tick, applied);
            if (tick > LatestPredictedTick)
            {
                LatestPredictedTick = tick;
            }
        }

        /// <returns>1 if this tick was already predicted with a different value, otherwise 0.</returns>
        private static int RecordValue(Dictionary<uint, int> valueByTick, uint tick, int value)
        {
            if (valueByTick.TryGetValue(tick, out var previous))
            {
                return previous == value ? 0 : 1;
            }
            valueByTick.Add(tick, value);
            return 0;
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
        public const int StampedNewValue = 1;

        public readonly List<uint> ReceivedPredictionTicks = new List<uint>();
        public int PingValue;
        public NetworkVariable<TickStampedValue> StampedValue = new NetworkVariable<TickStampedValue>();
        public NetworkVariable<uint> OwnerWrittenTick = new NetworkVariable<uint>(writePerm: NetworkVariableWritePermission.Owner);
        public int OwnerWrittenTickChanges;
        public int OwnerWrittenTickDecreases;
        public bool GhostWasPredictedOnSpawn;
        public NetworkId GhostOwnerOnSpawn;

        public override void OnNetworkSpawn()
        {
            var ghost = GetComponent<GhostObject>();
            GhostWasPredictedOnSpawn = ghost.CanWriteState;
            GhostOwnerOnSpawn = ghost.OwnerNetworkId;
            OwnerWrittenTick.OnValueChanged += OnOwnerWrittenTickChanged;
        }

        public override void OnNetworkDespawn()
        {
            OwnerWrittenTick.OnValueChanged -= OnOwnerWrittenTickChanged;
        }

        private void OnOwnerWrittenTickChanged(uint previous, uint current)
        {
            OwnerWrittenTickChanges++;
            if (current < previous)
            {
                OwnerWrittenTickDecreases++;
            }
        }

        [Rpc(SendTo.Server)]
        public void PredictionTickRpc(uint tick)
        {
            ReceivedPredictionTicks.Add(tick);
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
        private const int k_MinimumPredictionSends = 20;
        private const uint k_StampLeadTicks = 20;
        private const uint k_TicksPastStamp = 5;

        protected override int NumberOfClients => 1;

        private GameObject m_InteropPrefab;
        private NetworkObject m_ServerInstance;
        private NetworkObject m_ClientInstance;

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
        }

        /// <summary>
        /// An NGO RPC sent from <see cref="GhostBehaviour.PredictionUpdate"/> is sent again for every re-simulated tick.
        /// </summary>
        [UnityTest]
        public IEnumerator RpcFromPredictionUpdateRepeatsForResimulatedTicks()
        {
            yield return SendRpcsFromPrediction(false);
            var duplicates = CountDuplicateTicks();
            Assert.Greater(duplicates, 0, "Expected re-simulated ticks to send the same tick more than once.");
        }

        /// <summary>
        /// Gating the send on <c>IsFirstTimeFullyPredictingTick</c> sends each predicted tick once.
        /// </summary>
        [UnityTest]
        public IEnumerator RpcFromPredictionUpdateGatedOnFirstTimeTickSendsEachTickOnce()
        {
            yield return SendRpcsFromPrediction(true);
            var duplicates = CountDuplicateTicks();
            Assert.AreEqual(0, duplicates, $"{duplicates} ticks were sent more than once.");
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
        /// A NetworkVariable is not rolled back: re-simulating a tick can read a different value than its first prediction did.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkVariableReadDuringPredictionIsNotTickAligned()
        {
            yield return RecordAcrossStampedValueChange();
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            Assert.Greater(clientGhost.InconsistentRawTicks, 0, "Expected a re-simulated tick to read a different value than its first prediction.");
        }

        /// <summary>
        /// Applying a NetworkVariable only from its stamped tick gives every re-simulation of a tick the same value.
        /// </summary>
        [UnityTest]
        public IEnumerator TickStampedNetworkVariableIsConsistentAcrossResimulation()
        {
            yield return RecordAcrossStampedValueChange();
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            Assert.AreEqual(0, clientGhost.InconsistentStampedTicks, $"{clientGhost.InconsistentStampedTicks} ticks applied a different stamped value on re-simulation.");
            Assert.AreEqual(HybridInteropNetworkBehaviour.StampedNewValue, clientGhost.StampedValueAtStampTick, "The stamped value was not applied at its stamp tick.");
        }

        /// <summary>
        /// A NetworkVariable written from <see cref="GhostBehaviour.PredictionUpdate"/> is written again when older ticks
        /// re-simulate, so the owner's value moves backwards.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkVariableWrittenFromPredictionUpdateMovesBackwards()
        {
            yield return WriteNetworkVariableFromPrediction(false);
            var clientBehaviour = m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>();
            Assert.Greater(clientBehaviour.OwnerWrittenTickDecreases, 0, "Expected re-simulated ticks to write an older tick.");
        }

        /// <summary>
        /// Gating the write on <c>IsFirstTimeFullyPredictingTick</c> only moves the value forward.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkVariableWrittenFromPredictionUpdateGatedOnFirstTimeTickOnlyMovesForward()
        {
            yield return WriteNetworkVariableFromPrediction(true);
            var clientBehaviour = m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>();
            Assert.AreEqual(0, clientBehaviour.OwnerWrittenTickDecreases, $"The value moved backwards {clientBehaviour.OwnerWrittenTickDecreases} times.");
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

        private IEnumerator WriteNetworkVariableFromPrediction(bool gateOnFirstTimeTick)
        {
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            var clientBehaviour = m_ClientInstance.GetComponent<HybridInteropNetworkBehaviour>();
            clientGhost.GateOnFirstTimeTick = gateOnFirstTimeTick;
            clientGhost.WriteNetworkVariableFromPrediction = true;
            // Ungated, keep writing until a re-simulation has happened, so a backwards move had a chance to occur.
            yield return WaitForConditionOrTimeOut(() => clientBehaviour.OwnerWrittenTickChanges >= k_MinimumPredictionSends && (gateOnFirstTimeTick || clientBehaviour.OwnerWrittenTickDecreases > 0));
            clientGhost.WriteNetworkVariableFromPrediction = false;
            AssertOnTimeout($"Only {clientBehaviour.OwnerWrittenTickChanges} value changes were written, with {clientBehaviour.OwnerWrittenTickDecreases} backwards moves!");

            // Wait for the last write to reach the server, so no NetworkVariable update is still queued at teardown.
            var serverBehaviour = m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>();
            yield return WaitForConditionOrTimeOut(() => serverBehaviour.OwnerWrittenTick.Value == clientBehaviour.OwnerWrittenTick.Value);
            AssertOnTimeout($"Server value {serverBehaviour.OwnerWrittenTick.Value} never matched the client value {clientBehaviour.OwnerWrittenTick.Value}!");
            Debug.Log($"Gated: {gateOnFirstTimeTick}, value changes: {clientBehaviour.OwnerWrittenTickChanges}, backwards moves: {clientBehaviour.OwnerWrittenTickDecreases}");
        }

        private IEnumerator RecordAcrossStampedValueChange()
        {
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            clientGhost.RecordStampedValue = true;

            // Stamp far enough ahead that the value reaches the client before it predicts the stamp tick.
            var serverTick = m_ServerInstance.GetComponent<GhostObject>().World.NetworkTime.ServerTick.TickIndexForValidTick;
            var stampTick = serverTick + k_StampLeadTicks;
            m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>().StampedValue.Value = new TickStampedValue
            {
                Value = HybridInteropNetworkBehaviour.StampedNewValue,
                PreviousValue = 0,
                Tick = stampTick,
            };

            yield return WaitForConditionOrTimeOut(() => clientGhost.LatestPredictedTick >= stampTick + k_TicksPastStamp);
            clientGhost.RecordStampedValue = false;
            AssertOnTimeout($"Client never predicted past tick {stampTick + k_TicksPastStamp}! Latest predicted tick: {clientGhost.LatestPredictedTick}");
            Debug.Log($"Stamp tick: {stampTick}, early reads of the new value: {clientGhost.EarlyReadsOfNewValue}, " +
                $"inconsistent raw ticks: {clientGhost.InconsistentRawTicks}, inconsistent stamped ticks: {clientGhost.InconsistentStampedTicks}");
        }

        private IEnumerator SendRpcsFromPrediction(bool gateOnFirstTimeTick)
        {
            var clientGhost = m_ClientInstance.GetComponent<HybridInteropGhost>();
            clientGhost.GateOnFirstTimeTick = gateOnFirstTimeTick;
            clientGhost.SendRpcFromPrediction = true;
            yield return WaitForConditionOrTimeOut(() => clientGhost.PredictionSends >= k_MinimumPredictionSends && clientGhost.ResimulatedTicks > 0);
            clientGhost.SendRpcFromPrediction = false;
            AssertOnTimeout($"Client prediction did not send enough RPCs! Sends: {clientGhost.PredictionSends}, re-simulated ticks: {clientGhost.ResimulatedTicks}");

            var serverBehaviour = m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>();
            yield return WaitForConditionOrTimeOut(() => serverBehaviour.ReceivedPredictionTicks.Count == clientGhost.PredictionSends);
            AssertOnTimeout($"Server received {serverBehaviour.ReceivedPredictionTicks.Count} of {clientGhost.PredictionSends} RPCs!");
            Debug.Log($"Gated: {gateOnFirstTimeTick}, sends: {clientGhost.PredictionSends}, re-simulated ticks: {clientGhost.ResimulatedTicks}, duplicate ticks: {CountDuplicateTicks()}");
        }

        private int CountDuplicateTicks()
        {
            var seen = new HashSet<uint>();
            var duplicates = 0;
            foreach (var tick in m_ServerInstance.GetComponent<HybridInteropNetworkBehaviour>().ReceivedPredictionTicks)
            {
                if (!seen.Add(tick))
                {
                    duplicates++;
                }
            }
            return duplicates;
        }
    }
}
#endif
