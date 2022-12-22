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
#if DEBUG_NETWORKTRANSFORM
        public bool IsServerAuthoritative = true;
        public struct AuthorityStateUpdate
        {
            public int Tick;
            public ulong OwnerClientId;
            public ushort NetworkBehaviourId;
            public Vector3 AuthorityPosition;
            public Vector3 PredictedPosition;
            public Vector3 PrecisionLoss;
        }

        protected Dictionary<ulong, Dictionary<ushort, Dictionary<int, AuthorityStateUpdate>>> m_TickPositionTable = new Dictionary<ulong, Dictionary<ushort, Dictionary<int, AuthorityStateUpdate>>>();

        public Vector3 LastUpdatedPosition;
        public Vector3 LastUpdatedScale;
        public Quaternion LastUpdatedRotation;

        public Vector3 PreviousUpdatedPosition;
        public Vector3 PreviousUpdatedScale;
        public Quaternion PreviousUpdatedRotation;

        protected override bool OnIsServerAuthoritative()
        {
            return IsServerAuthoritative;
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

        protected override void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            UpdateTransformHistory(networkTransformStateUpdate.PositionUpdate, networkTransformStateUpdate.RotationUpdate, networkTransformStateUpdate.ScaleUpdate);
            if (networkTransformStateUpdate.PositionUpdate)
            {
                if (CanCommitToTransform)
                {
                    if (!m_TickPositionTable.ContainsKey(OwnerClientId))
                    {
                        m_TickPositionTable.Add(OwnerClientId, new Dictionary<ushort, Dictionary<int, AuthorityStateUpdate>>());
                    }

                    if (!m_TickPositionTable[OwnerClientId].ContainsKey(networkTransformStateUpdate.NetworkBehaviourId))
                    {
                        m_TickPositionTable[OwnerClientId].Add(networkTransformStateUpdate.NetworkBehaviourId, new Dictionary<int, AuthorityStateUpdate>());
                    }

                    if (!m_TickPositionTable[OwnerClientId][networkTransformStateUpdate.NetworkBehaviourId].ContainsKey(networkTransformStateUpdate.NetworkTick))
                    {
                        m_TickPositionTable[OwnerClientId][networkTransformStateUpdate.NetworkBehaviourId].Add(networkTransformStateUpdate.NetworkTick, new AuthorityStateUpdate()
                        {
                            Tick = networkTransformStateUpdate.NetworkTick,
                            OwnerClientId = NetworkObject.OwnerClientId,
                            NetworkBehaviourId = networkTransformStateUpdate.NetworkBehaviourId,
                            AuthorityPosition = transform.position,
                            PredictedPosition = networkTransformStateUpdate.TargetPosition,
                            PrecisionLoss = networkTransformStateUpdate.PrecisionLoss
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"{gameObject.name}-{OwnerClientId}-{networkTransformStateUpdate.NetworkBehaviourId} Authority is trying to add more than one position change on network tick {networkTransformStateUpdate.NetworkTick}!\n");
                    }
                }
                else
                {
                    // If we are server authoritative, then send position validation to the server
                    if (OnIsServerAuthoritative() && IsOwnedByServer)
                    {
                        PositionValidationServerRpc(networkTransformStateUpdate.TargetPosition, networkTransformStateUpdate.NetworkTick, networkTransformStateUpdate.NetworkBehaviourId);
                    }
                    else // Otherwise, server sends the position validation back to the owner/authority
                    {
                        var clientRpcParams = new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>() { OwnerClientId } } };
                        PositionValidationClientRpc(networkTransformStateUpdate.TargetPosition, networkTransformStateUpdate.NetworkTick, networkTransformStateUpdate.NetworkBehaviourId, clientRpcParams);
                    }
                }
            }
        }


        public delegate void PositionValidationHandler(ref NetworkObject networkObject, ref Vector3 position, ref AuthorityStateUpdate serverStateUpdate);
        public PositionValidationHandler PositionValidationCallback;


        protected virtual void OnPositionValidation(ref Vector3 position, ref AuthorityStateUpdate serverStateUpdate)
        {
            var networkObject = NetworkObject;
            PositionValidationCallback?.Invoke(ref networkObject, ref position, ref serverStateUpdate);
        }

        private void PositionValidation(Vector3 positionUpdate, int tick, ushort networkBehaviourId)
        {
            if (m_TickPositionTable.ContainsKey(OwnerClientId))
            {
                if (m_TickPositionTable[OwnerClientId].ContainsKey(networkBehaviourId))
                {
                    if (m_TickPositionTable[OwnerClientId][networkBehaviourId].ContainsKey(tick))
                    {

                        var serverState = m_TickPositionTable[OwnerClientId][networkBehaviourId][tick];
                        var position = positionUpdate;
                        if (tick == m_LastTickSent)
                        {
                            Debug.LogWarning($"Client sent two RPCs with the same tick {tick}");
                        }
                        OnPositionValidation(ref position, ref serverState);
                        m_LastTickSent = tick;
                        m_TickPositionTable[OwnerClientId][networkBehaviourId].Remove(tick);
                    }

                }
            }
        }

        private int m_LastTickSent;

        [ServerRpc(RequireOwnership = false)]
        private void PositionValidationServerRpc(Vector3 positionUpdate, int tick, ushort networkBehaviourId)
        {
            PositionValidation(positionUpdate, tick, networkBehaviourId);
        }

        [ClientRpc]
        private void PositionValidationClientRpc(Vector3 positionUpdate, int tick, ushort networkBehaviourId, ClientRpcParams clientRpcParams = default)
        {
            PositionValidation(positionUpdate, tick, networkBehaviourId);
        }
#endif
    }
}
