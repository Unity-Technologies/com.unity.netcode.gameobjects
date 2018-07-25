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

        private static byte[] stateUpdateBuffer = new byte[36];
        private static byte[] correctionBuffer = new byte[24];

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        /// <summary>
        /// Registers message handlers
        /// </summary>
        public override void NetworkStart()
        {
            if (isClient)
            {
                //RegisterMessageHandler("MLAPI_OnNavMeshStateUpdate", OnNavMeshStateUpdate);
                //RegisterMessageHandler("MLAPI_OnNavMeshCorrectionUpdate", OnNavMeshCorrectionUpdate);
            }
        }

        private Vector3 lastDestination = Vector3.zero;
        private float lastCorrectionTime = 0f;
        private void Update()
        {
            if (!isServer)
                return;

            if(agent.destination != lastDestination)
            {
                lastDestination = agent.destination;
                using (MemoryStream stream = new MemoryStream(stateUpdateBuffer))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(agent.destination.x);
                        writer.Write(agent.destination.y);
                        writer.Write(agent.destination.z);

                        writer.Write(agent.velocity.x);
                        writer.Write(agent.velocity.y);
                        writer.Write(agent.velocity.z);

                        writer.Write(transform.position.x);
                        writer.Write(transform.position.y);
                        writer.Write(transform.position.z);
                    }
                    if (!EnableProximity)
                    {
                        //SendToClientsTarget("MLAPI_OnNavMeshStateUpdate", "MLAPI_NAV_AGENT_STATE", stateUpdateBuffer);
                    }
                    else
                    {
                        List<uint> proximityClients = new List<uint>();
                        foreach (KeyValuePair<uint, NetworkedClient> client in NetworkingManager.singleton.ConnectedClients)
                        {
                            if (Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                                proximityClients.Add(client.Key);
                        }
                        //SendToClientsTarget(proximityClients, "MLAPI_OnNavMeshStateUpdate", "MLAPI_NAV_AGENT_STATE", stateUpdateBuffer);
                    }
                }
            }

            if(NetworkingManager.singleton.NetworkTime - lastCorrectionTime >= CorrectionDelay)
            {
                using (MemoryStream stream = new MemoryStream(correctionBuffer))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(agent.velocity.x);
                        writer.Write(agent.velocity.y);
                        writer.Write(agent.velocity.z);

                        writer.Write(transform.position.x);
                        writer.Write(transform.position.y);
                        writer.Write(transform.position.z);
                    }

                    if (!EnableProximity)
                    {
                        //SendToClientsTarget("MLAPI_OnNavMeshCorrectionUpdate", "MLAPI_NAV_AGENT_CORRECTION", correctionBuffer);
                    }
                    else
                    {
                        List<uint> proximityClients = new List<uint>();
                        foreach (KeyValuePair<uint, NetworkedClient> client in NetworkingManager.singleton.ConnectedClients)
                        {
                            if (Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                                proximityClients.Add(client.Key);
                        }
                        //SendToClientsTarget(proximityClients, "MLAPI_OnNavMeshCorrectionUpdate", "MLAPI_NAV_AGENT_CORRECTION", correctionBuffer);
                    }
                }
                lastCorrectionTime = NetworkingManager.singleton.NetworkTime;
            }
        }

        private void OnNavMeshStateUpdate(uint clientId, BitReader reader)
        {
            byte[] data = reader.ReadByteArray();
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader bReader = new BinaryReader(stream))
                {
                    float xDestination = bReader.ReadSingle();
                    float yDestination = bReader.ReadSingle();
                    float zDestination = bReader.ReadSingle();

                    float xVel = bReader.ReadSingle();
                    float yVel = bReader.ReadSingle();
                    float zVel = bReader.ReadSingle();

                    float xPos = bReader.ReadSingle();
                    float yPos = bReader.ReadSingle();
                    float zPos = bReader.ReadSingle();

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
        }

        private void OnNavMeshCorrectionUpdate(uint clientId, BitReader reader)
        {
            byte[] data = reader.ReadByteArray();
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader bReader = new BinaryReader(stream))
                {
                    float xVel = bReader.ReadSingle();
                    float yVel = bReader.ReadSingle();
                    float zVel = bReader.ReadSingle();

                    float xPos = bReader.ReadSingle();
                    float yPos = bReader.ReadSingle();
                    float zPos = bReader.ReadSingle();

                    Vector3 velocity = new Vector3(xVel, yVel, zVel);
                    Vector3 position = new Vector3(xPos, yPos, zPos);

                    agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));
                    agent.velocity = velocity;
                }
            }
        }
    }
}
