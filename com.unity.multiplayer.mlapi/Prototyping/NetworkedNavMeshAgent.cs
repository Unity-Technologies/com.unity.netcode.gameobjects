using System.Collections.Generic;
using MLAPI.Connection;
using MLAPI.Messaging;
using UnityEngine;
using UnityEngine.AI;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// A prototype component for syncing navmeshagents
    /// </summary>
    [AddComponentMenu("MLAPI/NetworkedNavMeshAgent")]
    public class NetworkedNavMeshAgent : NetworkedBehaviour
    {
        private NavMeshAgent agent;

        /// <summary>
        /// Is proximity enabled
        /// </summary>
        public bool EnableProximity = false;

        /// <summary>
        /// The proximity range
        /// </summary>
        public float ProximityRange = 50f;

        /// <summary>
        /// The delay in seconds between corrections
        /// </summary>
        public float CorrectionDelay = 3f;

        //TODO rephrase.
        /// <summary>
        /// The percentage to lerp on corrections
        /// </summary>
        [Tooltip("Everytime a correction packet is received. This is the percentage (between 0 & 1) that we will move towards the goal.")]
        public float DriftCorrectionPercentage = 0.1f;

        /// <summary>
        /// Should we warp on destination change
        /// </summary>
        public bool WarpOnDestinationChange = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private Vector3 lastDestination = Vector3.zero;
        private float lastCorrectionTime = 0f;

        private void Update()
        {
            if (!IsOwner)
                return;

            if (agent.destination != lastDestination)
            {
                lastDestination = agent.destination;
                if (!EnableProximity)
                {
                    OnNavMeshStateUpdateClientRpc(agent.destination, agent.velocity, transform.position);
                }
                else
                {
                    List<ulong> proximityClients = new List<ulong>();
                    foreach (KeyValuePair<ulong, NetworkedClient> client in MLAPI.NetworkManager.Singleton.ConnectedClients)
                    {
                        if (client.Value.PlayerObject == null || Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                            proximityClients.Add(client.Key);
                    }

                    OnNavMeshStateUpdateClientRpc(agent.destination, agent.velocity, transform.position,
                        new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = proximityClients.ToArray()}});
                }
            }

            if (MLAPI.NetworkManager.Singleton.NetworkTime - lastCorrectionTime >= CorrectionDelay)
            {
                if (!EnableProximity)
                {
                    OnNavMeshCorrectionUpdateClientRpc(agent.velocity, transform.position);
                }
                else
                {
                    List<ulong> proximityClients = new List<ulong>();
                    foreach (KeyValuePair<ulong, NetworkedClient> client in MLAPI.NetworkManager.Singleton.ConnectedClients)
                    {
                        if (client.Value.PlayerObject == null || Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                            proximityClients.Add(client.Key);
                    }

                    OnNavMeshCorrectionUpdateClientRpc(agent.velocity, transform.position,
                        new ClientRpcParams {Send = new ClientRpcSendParams {TargetClientIds = proximityClients.ToArray()}});
                }

                lastCorrectionTime = MLAPI.NetworkManager.Singleton.NetworkTime;
            }
        }

        [ClientRpc]
        private void OnNavMeshStateUpdateClientRpc(Vector3 destination, Vector3 velocity, Vector3 position, ClientRpcParams rpcParams = default)
        {
            if (WarpOnDestinationChange)
                agent.Warp(position);
            else
                agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));

            agent.SetDestination(destination);
            agent.velocity = velocity;
        }

        [ClientRpc]
        private void OnNavMeshCorrectionUpdateClientRpc(Vector3 velocity, Vector3 position, ClientRpcParams rpcParams = default)
        {
            agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));
            agent.velocity = velocity;
        }
    }
}
