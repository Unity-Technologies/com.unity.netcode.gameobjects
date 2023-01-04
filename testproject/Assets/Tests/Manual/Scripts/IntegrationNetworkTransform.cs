using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(TestProject.ManualTests.IntegrationNetworkTransform))]
public class IntegrationNetworkTransformEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif

namespace TestProject.ManualTests
{
    public class IntegrationNetworkTransform : NetworkTransform
    {

        public bool IsServerAuthority = true;

        public bool DebugTransform;

        public Vector3 LastUpdatedPosition;
        public Vector3 LastUpdatedScale;
        public Quaternion LastUpdatedRotation;

        public Vector3 PreviousUpdatedPosition;
        public Vector3 PreviousUpdatedScale;
        public Quaternion PreviousUpdatedRotation;

        protected override void Awake()
        {
            base.Awake();
#if DEBUG_NETWORKTRANSFORM
            if (DebugTransform)
            {
                m_AddLogEntry = InternalAddLogEntry;
            }
#endif
        }

        protected override bool OnIsServerAuthoritative()
        {
            return IsServerAuthority;
        }

        private void UpdateTransformHistory(bool updatePosition, bool updateRotation, bool updateScale)
        {
            if (updatePosition)
            {
                if (CanCommitToTransform)
                {
                    PreviousUpdatedPosition = LastUpdatedPosition;
                }
                LastUpdatedPosition = InLocalSpace ? transform.localPosition : transform.position;
            }

            if (updateRotation)
            {
                if (CanCommitToTransform)
                {
                    PreviousUpdatedRotation = LastUpdatedRotation;
                }

                LastUpdatedRotation = InLocalSpace ? transform.localRotation : transform.rotation;
            }

            if (updateScale)
            {
                if (CanCommitToTransform)
                {
                    PreviousUpdatedScale = LastUpdatedScale;
                }
                LastUpdatedScale = transform.localScale;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateTransformHistory(true, true, true);
        }

        protected virtual void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            UpdateTransformHistory(networkTransformStateUpdate.PositionUpdate, networkTransformStateUpdate.RotationUpdate, networkTransformStateUpdate.ScaleUpdate);
        }

        private NetworkTransformStateUpdate m_NetworkTransformStateUpdate = new NetworkTransformStateUpdate();

        protected struct NetworkTransformStateUpdate
        {
            public bool PositionUpdate;
            public bool ScaleUpdate;
            public bool RotationUpdate;
            public ushort NetworkBehaviourId;
            public int NetworkTick;
            public ulong OwnerClientId;

            // The authority's real transform values at the time the new state was applied
            public Vector3 AuthTickPosition;
            public Quaternion AuthTickRotation;

            // The non-authority's last received transform values
            public Vector3 Position;
            public Quaternion Rotation;
        }

        private struct HalfPosDebugStates : INetworkSerializable
        {
            public bool IsPreUpdate;
            public bool IsTeleporting;
            public bool IsSynchronizing;
            public int StateId;
            public int Tick;
            public ulong ClientTarget;

            public Vector3 BasePosition;
            public Vector3 DeltaPosition;
            public Vector3 HalfBackDelta;
            public Vector3 FullPosition;
            public Vector3 TransformPosition;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref IsPreUpdate);
                serializer.SerializeValue(ref IsTeleporting);
                serializer.SerializeValue(ref IsSynchronizing);
                serializer.SerializeValue(ref StateId);
                serializer.SerializeValue(ref Tick);
                serializer.SerializeValue(ref ClientTarget);
                serializer.SerializeValue(ref BasePosition);
                serializer.SerializeValue(ref HalfBackDelta);
                serializer.SerializeValue(ref DeltaPosition);
                serializer.SerializeValue(ref FullPosition);
                serializer.SerializeValue(ref TransformPosition);
            }
        }
        private const int k_StatesToLog = 80;
        private bool m_StopLoggingStates = false;
        private Dictionary<ulong, Dictionary<ulong, List<HalfPosDebugStates>>> m_FirstInitialStateUpdates = new Dictionary<ulong, Dictionary<ulong, List<HalfPosDebugStates>>>();

        private void InternalAddLogEntry(ref NetworkTransformState networkTransformState, ulong targetClient, bool preUpdate = false)
        {
            if (!DebugTransform)
            {
                return;
            }
#if DEBUG_NETWORKTRANSFORM
            var halfPositionState = GetHalfPositionState();
            var state = new HalfPosDebugStates();
            state.IsPreUpdate = preUpdate;
            state.IsTeleporting = networkTransformState.IsTeleportingNextFrame;
            state.IsSynchronizing = networkTransformState.IsSynchronizing;

            state.StateId = GetStateId(ref networkTransformState);

            state.Tick = networkTransformState.GetNetworkTick();
            state.ClientTarget = targetClient;
            state.BasePosition = halfPositionState.CurrentBasePosition;
            state.HalfBackDelta = halfPositionState.GetConvertedDelta();
            state.DeltaPosition = halfPositionState.GetDeltaPosition();
            state.FullPosition = halfPositionState.GetFullPosition();
            state.TransformPosition = InLocalSpace ? transform.localPosition : transform.position;
            var localClientId = NetworkManager.LocalClientId;
            var ownerId = NetworkObject.OwnerClientId;
            if (!m_FirstInitialStateUpdates.ContainsKey(ownerId))
            {
                m_FirstInitialStateUpdates.Add(ownerId, new Dictionary<ulong, List<HalfPosDebugStates>>());
            }
            var ownerTable = m_FirstInitialStateUpdates[ownerId];

            if (!ownerTable.ContainsKey(localClientId))
            {
                ownerTable.Add(localClientId, new List<HalfPosDebugStates>());
            }
            ownerTable[localClientId].Add(state);
            if (ownerTable[localClientId].Count == k_StatesToLog)
            {
                m_StopLoggingStates = true;
                if (IsServer)
                {
                    LogInitialTransformStates(localClientId, ownerId);
                }
                else
                {
                    SendStateLogToServer(ownerId);
                    LogInitialTransformStates(localClientId, ownerId);
                }
            }
#endif
        }

        private void SendStateLogToServer(ulong ownerId)
        {
            var ownerTable = m_FirstInitialStateUpdates[ownerId];
            var ownerTableClientRelative = ownerTable[NetworkManager.LocalClientId];
            foreach (var entry in ownerTableClientRelative)
            {
                AddLogEntryServerRpc(entry, OwnerClientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddLogEntryServerRpc(HalfPosDebugStates logEntry, ulong ownerId, ServerRpcParams serverRpcParams = default)
        {
            if (!m_FirstInitialStateUpdates.ContainsKey(ownerId))
            {
                m_FirstInitialStateUpdates.Add(ownerId, new Dictionary<ulong, List<HalfPosDebugStates>>());
            }
            var ownerTable = m_FirstInitialStateUpdates[ownerId];
            var senderId = serverRpcParams.Receive.SenderClientId;
            if (!ownerTable.ContainsKey(senderId))
            {
                ownerTable.Add(senderId, new List<HalfPosDebugStates>());
            }
            var ownerTableClientRelative = ownerTable[senderId];

            ownerTableClientRelative.Add(logEntry);

            if (ownerTableClientRelative.Count == k_StatesToLog)
            {
                LogInitialTransformStates(serverRpcParams.Receive.SenderClientId, ownerId);
            }
        }

        private System.Text.StringBuilder m_LogEntry = new System.Text.StringBuilder();
        private void LogInitialTransformStates(ulong clientId, ulong ownerId)
        {
            List<HalfPosDebugStates> states = m_FirstInitialStateUpdates[ownerId][clientId];
            var sections = 1;
            m_LogEntry.Append($"[#{sections}][{gameObject.name}][Client-{clientId}] Transform States Captured For Client-{ownerId}:\n");
            var lineCounter = 0;
            foreach (var stateEntry in states)
            {
                var isPreUpdate = stateEntry.IsPreUpdate ? "[PreUpdate]" : "";
                var isSynchronization = stateEntry.IsSynchronizing ? "[Synchronizing]" : "";
                var isTeleporing = stateEntry.IsTeleporting ? "[Teleporting]" : "";
                var isApppliedLocal = stateEntry.ClientTarget == clientId ? "[Applied Local]" : $"[Targeting Client-{stateEntry.ClientTarget}]";
                m_LogEntry.Append($"{isPreUpdate}{isApppliedLocal} State Entry ({stateEntry.StateId}) on Tick ({stateEntry.Tick}). {isSynchronization}{isTeleporing}\n");
                m_LogEntry.Append($"[BasePos]{stateEntry.BasePosition} [DeltaPos] {stateEntry.DeltaPosition} [HalfBack]{ stateEntry.HalfBackDelta}\n");
                m_LogEntry.Append($"[FullPos]{stateEntry.FullPosition} [TransPos] {stateEntry.TransformPosition}\n");
                if (lineCounter >= 75)
                {
                    Debug.Log(m_LogEntry);
                    m_LogEntry.Clear();
                    lineCounter = 0;
                    sections++;
                    m_LogEntry.Append($"[#{sections}][{gameObject.name}][Client-{clientId}] Transform States Captured For Client-{ownerId}:\n");
                }
                else
                {
                    lineCounter++;
                }

            }

            if (m_LogEntry.Length > 0)
            {
                Debug.Log(m_LogEntry);
                m_LogEntry.Clear();
            }
        }

        private void DebugTransformStateUpdate(NetworkTransformState oldState, NetworkTransformState newState)
        {
            m_NetworkTransformStateUpdate.PositionUpdate = newState.HasPositionChange;
            m_NetworkTransformStateUpdate.ScaleUpdate = newState.HasScaleChange;
            m_NetworkTransformStateUpdate.RotationUpdate = newState.HasRotAngleChange;
            m_NetworkTransformStateUpdate.NetworkTick = newState.GetNetworkTick();
            m_NetworkTransformStateUpdate.NetworkBehaviourId = NetworkBehaviourId;
            m_NetworkTransformStateUpdate.OwnerClientId = OwnerClientId;
            m_NetworkTransformStateUpdate.Position = GetHalfPositionState().GetFullPosition();

            OnNetworkTransformStateUpdate(ref m_NetworkTransformStateUpdate);
        }


        private NetworkVariable<NetworkTransformState> m_CurrentReplicatedState = new NetworkVariable<NetworkTransformState>();

        protected override void OnInitialize(ref NetworkVariable<NetworkTransformState> replicatedState)
        {
            if (CanCommitToTransform && DebugTransform)
            {
                m_CurrentReplicatedState = replicatedState;
                // Sanity check to assure we only subscribe to OnValueChanged once
                replicatedState.OnValueChanged -= OnStateUpdate;
                replicatedState.OnValueChanged += OnStateUpdate;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (DebugTransform)
            {
                m_CurrentReplicatedState.OnValueChanged -= OnStateUpdate;
            }

            base.OnNetworkDespawn();
        }

        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            if (networkTransformState.HasPositionChange)
            {

            }
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }

        /// <summary>
        /// Authoritative State Update
        /// </summary>
        private void OnStateUpdate(NetworkTransformState oldState, NetworkTransformState newState)
        {

            if (DebugTransform)
            {
                if (IsOwner && !IsServerAuthoritative() && !m_StopLoggingStates)
                {
                    if (IsServer && NetworkManager.ConnectedClientsIds.Count < 2)
                    {
                        return;
                    }
                    InternalAddLogEntry(ref newState, OwnerClientId);
                }
            }
        }

        /// <summary>
        /// Non-Authoritative State Update
        /// </summary>
        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            DebugTransformStateUpdate(oldState, newState);
            if (DebugTransform)
            {
                if (!IsOwner && !IsServerAuthoritative() && transform.parent != null && !m_StopLoggingStates)
                {
                    InternalAddLogEntry(ref newState, OwnerClientId);
                }
            }
        }
    }
}
