using MLAPI.Data;
using System.Collections.Generic;
using System.IO;
using MLAPI.Serialization;
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
        [Tooltip("Everytime a correction packet is recieved. This is the percentage (between 0 & 1) that we will move towards the goal.")]
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
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {

                        writer.WriteSinglePacked(agent.destination.x);
                        writer.WriteSinglePacked(agent.destination.y);
                        writer.WriteSinglePacked(agent.destination.z);

                        writer.WriteSinglePacked(agent.velocity.x);
                        writer.WriteSinglePacked(agent.velocity.y);
                        writer.WriteSinglePacked(agent.velocity.z);

                        writer.WriteSinglePacked(transform.position.x);
                        writer.WriteSinglePacked(transform.position.y);
                        writer.WriteSinglePacked(transform.position.z);


                        if (!EnableProximity)
                        {
                            InvokeClientRpcOnEveryone(OnNavMeshStateUpdate, stream);
                        }
                        else
                        {
                            List<uint> proximityClients = new List<uint>();
                            foreach (KeyValuePair<uint, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                            {
                                if (Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                                    proximityClients.Add(client.Key);
                            }
                            InvokeClientRpc(OnNavMeshStateUpdate, proximityClients, stream);
                        }
                    }
                }
            }

            if (NetworkingManager.Singleton.NetworkTime - lastCorrectionTime >= CorrectionDelay)
            {
                using (PooledBitStream stream = PooledBitStream.Get())
                {
                    using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                    {
                        writer.WriteSinglePacked(agent.velocity.x);
                        writer.WriteSinglePacked(agent.velocity.y);
                        writer.WriteSinglePacked(agent.velocity.z);

                        writer.WriteSinglePacked(transform.position.x);
                        writer.WriteSinglePacked(transform.position.y);
                        writer.WriteSinglePacked(transform.position.z);


                        if (!EnableProximity)
                        {
                            InvokeClientRpcOnEveryone(OnNavMeshCorrectionUpdate, stream);
                        }
                        else
                        {
                            List<uint> proximityClients = new List<uint>();
                            foreach (KeyValuePair<uint, NetworkedClient> client in NetworkingManager.Singleton.ConnectedClients)
                            {
                                if (Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                                    proximityClients.Add(client.Key);
                            }
                            InvokeClientRpc(OnNavMeshCorrectionUpdate, proximityClients, stream);
                        }
                    }
                }
                lastCorrectionTime = NetworkingManager.Singleton.NetworkTime;
            }
        }

        [ClientRPC]
        private void OnNavMeshStateUpdate(uint clientId, Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                float xDestination = reader.ReadSinglePacked();
                float yDestination = reader.ReadSinglePacked();
                float zDestination = reader.ReadSinglePacked();

                float xVel = reader.ReadSinglePacked();
                float yVel = reader.ReadSinglePacked();
                float zVel = reader.ReadSinglePacked();

                float xPos = reader.ReadSinglePacked();
                float yPos = reader.ReadSinglePacked();
                float zPos = reader.ReadSinglePacked();

                Vector3 destination = new Vector3(xDestination, yDestination, zDestination);
                Vector3 velocity = new Vector3(xVel, yVel, zVel);
                Vector3 position = new Vector3(xPos, yPos, zPos);

                if (WarpOnDestinationChange)
                    agent.Warp(position);
                else
                    agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));

                agent.SetDestination(destination);
                agent.velocity = velocity;
            }
        }

        [ClientRPC]
        private void OnNavMeshCorrectionUpdate(uint clientId, Stream stream)
        {
            using (PooledBitReader reader = PooledBitReader.Get(stream))
            {
                float xVel = reader.ReadSinglePacked();
                float yVel = reader.ReadSinglePacked();
                float zVel = reader.ReadSinglePacked();

                float xPos = reader.ReadSinglePacked();
                float yPos = reader.ReadSinglePacked();
                float zPos = reader.ReadSinglePacked();

                Vector3 velocity = new Vector3(xVel, yVel, zVel);
                Vector3 position = new Vector3(xPos, yPos, zPos);

                agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));
                agent.velocity = velocity;
            }
        }
    }
}
