using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;


namespace TestProject.ManualTests
{
    public class BandwidthMover : NetworkTransform
    {
        public Vector3 Direction;

        [Range(0.01f,40.0f)]
        public float MoveSpeed = 5.0f;

        [Range(0.01f,40.0f)]
        public float RotationSpeed = 2.0f;

        [Range(0.001f, 1.0f)]
        public float MinScale = 1.0f;

        [Range(1.0f, 4.0f)]
        public float MaxScale = 1.0f;

        [Range(0.01f, 2.0f)]
        public float GhostLerpFactor = 1.0333333f;

        [Tooltip("When enabled, this will adjust the target of the Ghost to be more aligned with the Authority position (for visualization purposes).")]
        public bool GhostLatentPositionCompensation;

        public GameObject ClientGhost;

        private Vector3 m_Rotation;
        private Vector3 m_RotateBy;
        private Vector3 m_TargetScale;

        private float m_NextRotationDeltaUpdate;

        private void Start()
        {
            Direction.Normalize();

            m_Rotation = transform.eulerAngles;
            GenerateNewRotationDeltaBase();

            if (ClientGhost != null)
            {
                ClientGhost.SetActive(false);
            }
        }

        public delegate void NotifySerializedSizeHandler(int size);

        public NotifySerializedSizeHandler NotifySerializedSize;

        private struct ClientTransformState : INetworkSerializable
        {
            private const byte k_HasPositionBit = 0;
            private const byte k_HasRotationBit = 1;
            private const byte k_HasScaleBit = 2;

            public bool HasPosition
            {
                get => BitGet(k_HasPositionBit);
                internal set
                {
                    BitSet(value, k_HasPositionBit);
                }
            }
            public bool HasRotation
            {
                get => BitGet(k_HasRotationBit);
                internal set
                {
                    BitSet(value, k_HasRotationBit);
                }
            }
            public bool HasScale
            {
                get => BitGet(k_HasScaleBit);
                internal set
                {
                    BitSet(value, k_HasScaleBit);
                }
            }

            private bool BitGet(byte bitPosition)
            {
                return (m_Flags & (1 << bitPosition)) != 0;
            }

            private void BitSet(bool set, byte bitPosition)
            {
                if (set) { m_Flags = (byte)(m_Flags | (byte)(1 << bitPosition)); }
                else { m_Flags = (byte)(m_Flags & (byte)~(1 << bitPosition)); }
            }

            private byte m_Flags;
            public int NetworkTickApplied;
            public Vector3 Position;
            public Vector3 Scale;
            public Vector3 Rotation;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref NetworkTickApplied);
                serializer.SerializeValue(ref m_Flags);
                if (HasPosition)
                {
                    serializer.SerializeValue(ref Position);
                }
                if (HasRotation)
                {
                    serializer.SerializeValue(ref Rotation);
                }
                if (HasScale)
                {
                    serializer.SerializeValue(ref Scale);
                }
            }
        }

        private class ClientPrecisionAverage
        {
            public int PositionUpdates;
            public int RotationUpdates;
            public int ScaleUpdates;
            public Vector3 Position;
            public Vector3 Scale;
            public Vector3 Rotation;
        }

        private Dictionary<ulong, Dictionary<int, ClientTransformState>> m_ClientStateHistory = new Dictionary<ulong, Dictionary<int, ClientTransformState>>();
        private Dictionary<ulong, ClientPrecisionAverage> m_ClientPrecisionAverages = new Dictionary<ulong, ClientPrecisionAverage>();

        private void UpdateClientPrecisionAverages()
        {
            // If not spawned or the owner hasn't updated, then don't do anything
            if (!IsSpawned || !m_ClientStateHistory.ContainsKey(OwnerClientId))
            {
                return;
            }

            var currentTick = NetworkManager.NetworkTickSystem.ServerTime.Tick;
            var ownerStates = m_ClientStateHistory[OwnerClientId];

            foreach (var clientStates in m_ClientStateHistory)
            {
                // Skip the owner
                if (clientStates.Key == OwnerClientId)
                {
                    continue;
                }
                if (!m_ClientPrecisionAverages.ContainsKey(clientStates.Key))
                {
                    m_ClientPrecisionAverages.Add(clientStates.Key, new ClientPrecisionAverage());
                }
                var clientAverages = m_ClientPrecisionAverages[clientStates.Key];

                foreach (var clientState in clientStates.Value)
                {

                    // If the owner's states does not include the network tick
                    // then skip this entry
                    if (!ownerStates.ContainsKey(clientState.Key))
                    {
                        continue;
                    }
                    var ownerState = ownerStates[clientState.Key];
                    var transformState = clientState.Value;

                    if (ownerState.HasPosition && transformState.HasPosition)
                    {
                        clientAverages.Position += ownerState.Position - transformState.Position;
                        clientAverages.PositionUpdates++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Tick-{clientState.Key}][Has Position Mismatch] Owner-{OwnerClientId}: {ownerState.HasPosition} | Client-{clientStates.Key}: {transformState.HasPosition}!");
                    }

                    if (ownerState.HasRotation && transformState.HasRotation)
                    {

                        clientAverages.Rotation += ownerState.Rotation - transformState.Rotation;
                        for(int i = 0; i < 3; i++)
                        {
                            clientAverages.Rotation[i] += Mathf.DeltaAngle(ownerState.Rotation[i], transformState.Rotation[i]);
                        }

                        clientAverages.RotationUpdates++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Tick-{clientState.Key}][Has Rotation Mismatch] Owner-{OwnerClientId}: {ownerState.HasRotation} | Client-{clientStates.Key}: {transformState.HasRotation}!");
                    }

                    if (ownerState.HasScale && transformState.HasScale)
                    {
                        clientAverages.Scale += ownerState.Scale - transformState.Scale;
                        clientAverages.ScaleUpdates++;
                    }
                    else
                    {
                        Debug.LogWarning($"[Tick-{clientState.Key}][Has Scale Mismatch] Owner-{OwnerClientId}: {ownerState.HasScale} | Client-{clientStates.Key}: {transformState.HasScale}!");
                    }
                }
            }

            foreach (var clientAveragesEntry in m_ClientPrecisionAverages)
            {
                // Skip the owner
                if (clientAveragesEntry.Key == OwnerClientId)
                {
                    continue;
                }
                var clientAverages = clientAveragesEntry.Value;
                if (clientAverages.PositionUpdates > 1)
                {
                    var divisor = 1.0f / clientAverages.PositionUpdates;
                    clientAverages.Position *= divisor;
                    clientAverages.PositionUpdates = 0;
                }
                if (clientAverages.RotationUpdates > 1)
                {
                    var divisor = 1.0f / clientAverages.RotationUpdates;
                    clientAverages.Rotation *= divisor;
                    clientAverages.RotationUpdates = 0;
                }
                if (clientAverages.ScaleUpdates > 1)
                {
                    var divisor = 1.0f / clientAverages.ScaleUpdates;
                    clientAverages.Scale *= divisor;
                    clientAverages.ScaleUpdates = 0;
                }
            }

            foreach (var clientStates in m_ClientStateHistory)
            {
                clientStates.Value.Clear();
            }
        }

        private void UpdateClientPrecisionHistory(ulong clientId, ref ClientTransformState clientTransformState)
        {
            if (!m_ClientStateHistory.ContainsKey(clientId))
            {
                m_ClientStateHistory.Add(clientId, new Dictionary<int, ClientTransformState>());
            }

            if (!m_ClientStateHistory[clientId].ContainsKey(clientTransformState.NetworkTickApplied))
            {

                m_ClientStateHistory[clientId].Add(clientTransformState.NetworkTickApplied, clientTransformState);
            }
        }

        private ClientTransformState CreateClientTransformState(ref NetworkTransformState networkTransformState)
        {
            var clientTransformState = new ClientTransformState();
            clientTransformState.NetworkTickApplied = networkTransformState.GetNetworkTick();
            if (networkTransformState.HasPositionChange)
            {
                clientTransformState.HasPosition = true;
                clientTransformState.Position = GetSpaceRelativePosition(true);
            }
            if (networkTransformState.HasRotAngleChange)
            {
                clientTransformState.HasRotation = true;
                clientTransformState.Rotation = GetSpaceRelativeRotation(true).eulerAngles;
            }
            if (networkTransformState.HasScaleChange)
            {
                clientTransformState.HasScale = true;
                clientTransformState.Scale = GetScale(true);
            }
            return clientTransformState;
        }

        protected override void OnAuthorityPushTransformState(ref NetworkTransformState networkTransformState)
        {
            var clientTransformState = CreateClientTransformState(ref networkTransformState);
            if (IsServer)
            {
                UpdateClientPrecisionHistory(NetworkManager.LocalClientId, ref clientTransformState);
            }
            else
            {
                OnClientUpdateTransformStateServerRpc(clientTransformState);
            }

            NotifySerializedSize?.Invoke(networkTransformState.LastSerializedSize);
            base.OnAuthorityPushTransformState(ref networkTransformState);
        }

        private Vector3 m_GhostTargetPosition;
        private Quaternion m_GhostTargetRotation = new Quaternion();
        private Vector3 m_GhostTargetScale;

        [ServerRpc(RequireOwnership = false)]
        private void OnClientUpdateTransformStateServerRpc(ClientTransformState clientTransformState, ServerRpcParams serverRpcParams = default)
        {
            UpdateClientPrecisionHistory(serverRpcParams.Receive.SenderClientId, ref clientTransformState);
            if (ClientGhost != null)
            {
                if (clientTransformState.HasPosition)
                {
                    m_GhostTargetPosition = clientTransformState.Position;
                }
                if (clientTransformState.HasRotation)
                {
                    m_GhostTargetRotation.eulerAngles = clientTransformState.Rotation;
                }
                if (clientTransformState.HasScale)
                {
                    m_GhostTargetScale = clientTransformState.Scale;
                }
            }
        }

        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            var clientTransformState = CreateClientTransformState(ref newState);
            if (IsServer)
            {
                UpdateClientPrecisionHistory(NetworkManager.LocalClientId, ref clientTransformState);
            }
            else
            {
                OnClientUpdateTransformStateServerRpc(clientTransformState);
            }
            NotifySerializedSize?.Invoke(newState.LastSerializedSize);
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
                if (ClientGhost != null)
                {
                    ClientGhost.SetActive(true);
                }
            }

            if (CanCommitToTransform)
            {
                GenerateNewTargetScale();
            }
            else
            {
                var cameraFollower = FindObjectOfType<CameraFollower>();
                var position = transform.position;
                cameraFollower.UpdateOffset(ref position);

            }
            base.OnNetworkSpawn();
        }

        private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
        {
            if(m_ClientPrecisionAverages.ContainsKey(clientId))
            {
                m_ClientPrecisionAverages.Remove(clientId);
            }
        }

        private void GenerateNewRotationDeltaBase()
        {
            m_RotateBy = new Vector3(Random.Range(-RotationSpeed, RotationSpeed), Random.Range(-RotationSpeed, RotationSpeed), Random.Range(-RotationSpeed, RotationSpeed));
            m_NextRotationDeltaUpdate = Time.realtimeSinceStartup + Random.Range(1.0f, 5.0f);
        }

        private void GenerateNewTargetScale()
        {
            m_TargetScale = new Vector3(Random.Range(MinScale, MaxScale), Random.Range(MinScale, MaxScale), Random.Range(MinScale, MaxScale));
        }

        private void GenerateNewScaleIfReached()
        {
            var delta = m_TargetScale - transform.localScale;
            if (delta.sqrMagnitude < 0.15f)
            {
                GenerateNewTargetScale();
            }
        }

        private float m_LastUpdate = 0.0f;

        protected override void Update()
        {
            base.Update();

            if (IsServer)
            {
                if (m_LastUpdate < Time.realtimeSinceStartup)
                {
                    UpdateClientPrecisionAverages();
                    m_LastUpdate = Time.realtimeSinceStartup + 2.0f;
                    foreach (var clientaverages in m_ClientPrecisionAverages)
                    {
                        if (clientaverages.Key != OwnerClientId)
                        {
                            Debug.Log($"[Pos] {clientaverages.Value.Position} | [Rot] {clientaverages.Value.Rotation} | [Sca] {clientaverages.Value.Scale}");
                            clientaverages.Value.Position = Vector3.zero;
                            clientaverages.Value.Rotation = Vector3.zero;
                            clientaverages.Value.Scale = Vector3.zero;
                        }
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !CanCommitToTransform)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, transform.position + (Direction * MoveSpeed), Time.fixedDeltaTime);

            if (m_NextRotationDeltaUpdate < Time.realtimeSinceStartup)
            {
                GenerateNewRotationDeltaBase();
            }
            var targetRotation = m_Rotation + m_RotateBy;

            for (int i = 0; i < 3; i++)
            {
                m_Rotation[i] = Mathf.MoveTowardsAngle(m_Rotation[i], targetRotation[i], RotationSpeed);
            }
            transform.eulerAngles = m_Rotation;
            GenerateNewScaleIfReached();
            transform.localScale = Vector3.Lerp(transform.localScale, m_TargetScale, Time.fixedDeltaTime);

            var ghostTransform = ClientGhost.transform;
            var ghostRotation = ghostTransform.rotation;
            if (NetworkManager.ConnectedClientsList.Count > 1)
            {
                var delta = GhostLatentPositionCompensation ? transform.position - m_GhostTargetPosition : Vector3.zero;
                ghostTransform.position = Vector3.LerpUnclamped(ghostTransform.position, m_GhostTargetPosition + delta, GhostLerpFactor * Time.fixedDeltaTime);

                ghostTransform.rotation = Quaternion.LerpUnclamped(ghostRotation, m_GhostTargetRotation, GhostLerpFactor * Time.fixedDeltaTime);
                ghostTransform.localScale = Vector3.LerpUnclamped(ghostTransform.localScale, m_GhostTargetScale, GhostLerpFactor * Time.fixedDeltaTime);
            }
            else
            {
                ghostTransform.position = m_GhostTargetPosition = transform.position;
                m_GhostTargetRotation = transform.rotation;
                ghostTransform.rotation = m_GhostTargetRotation;
                ghostTransform.localScale = m_GhostTargetScale = transform.localScale;
            }
        }

    }
}
