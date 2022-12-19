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
        public struct AuthorityStateUpdate
        {
            public int Tick;
            public ulong OwnerClientId;
            public Vector3 AuthorityPosition;
            public Vector3 PredictedPosition;
            public Vector3 PrecisionLoss;
        }

        protected Dictionary<ulong, Dictionary<int, AuthorityStateUpdate>> m_TickPositionTable = new Dictionary<ulong, Dictionary<int, AuthorityStateUpdate>>();

        protected override void OnNetworkTransformStateUpdate(ref NetworkTransformStateUpdate networkTransformStateUpdate)
        {
            if (networkTransformStateUpdate.PositionUpdate)
            {
                if (CanCommitToTransform)
                {
                    if (!m_TickPositionTable.ContainsKey(OwnerClientId))
                    {
                        m_TickPositionTable.Add(OwnerClientId, new Dictionary<int, AuthorityStateUpdate>());
                    }

                    if (!m_TickPositionTable[OwnerClientId].ContainsKey(networkTransformStateUpdate.NetworkTick))
                    {
                        m_TickPositionTable[OwnerClientId].Add(networkTransformStateUpdate.NetworkTick, new AuthorityStateUpdate()
                        {
                            Tick = networkTransformStateUpdate.NetworkTick,
                            OwnerClientId = NetworkObject.OwnerClientId,
                            AuthorityPosition = transform.position,
                            PredictedPosition = networkTransformStateUpdate.TargetPosition,
                            PrecisionLoss = networkTransformStateUpdate.PrecisionLoss
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"{gameObject.name} Authority is trying to add more than one position change on network tick {networkTransformStateUpdate.NetworkTick}!\n");
                    }
                }
                else
                {
                    // If we are server authoritative, then send position validation to the server
                    if (OnIsServerAuthoritative() && IsOwnedByServer)
                    {
                        PositionValidationServerRpc(networkTransformStateUpdate.TargetPosition, networkTransformStateUpdate.NetworkTick);
                    }
                    else // Otherwise, server sends the position validation back to the owner/authority
                    {
                        var clientRpcParams = new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = new List<ulong>() { OwnerClientId } } };
                        PositionValidationClientRpc(networkTransformStateUpdate.TargetPosition, networkTransformStateUpdate.NetworkTick, clientRpcParams);
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

        private void PositionValidation(Vector3 positionUpdate, int tick)
        {
            var serverState = m_TickPositionTable[OwnerClientId][tick];
            var position = positionUpdate;
            if (tick == m_LastTickSent)
            {
                Debug.LogWarning($"Client sent two RPCs with the same tick {tick}");
            }
            OnPositionValidation(ref position, ref serverState);
            m_LastTickSent = tick;
            m_TickPositionTable[OwnerClientId].Remove(tick);
        }

        private int m_LastTickSent;

        [ServerRpc(RequireOwnership = false)]
        private void PositionValidationServerRpc(Vector3 positionUpdate, int tick)
        {
            PositionValidation(positionUpdate, tick);
        }

        [ClientRpc]
        private void PositionValidationClientRpc(Vector3 positionUpdate, int tick, ClientRpcParams clientRpcParams = default)
        {
            PositionValidation(positionUpdate, tick);
        }
#endif
    }
}
