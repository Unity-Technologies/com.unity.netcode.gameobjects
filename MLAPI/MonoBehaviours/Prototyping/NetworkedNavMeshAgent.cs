using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace MLAPI.MonoBehaviours.Prototyping
{
    public class NetworkedNavMeshAgent : NetworkedBehaviour
    {
        private NavMeshAgent agent;
        public bool EnableProximity = false;
        public float ProximityRange = 50f;
        public float CorrectionDelay = 3f;
        //TODO rephrase.
        [Tooltip("Everytime a correction packet is recieved. This is the percentage (between 0 & 1) that we will move towards the goal.")]
        public float DriftCorrectionPercentage = 0.1f;
        public bool WarpOnDestinationChange = false;

        private static byte[] stateUpdateBuffer = new byte[36];
        private static byte[] correctionBuffer = new byte[24];

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        public override void NetworkStart()
        {
            if (isClient)
            {
                RegisterMessageHandler("MLAPI_OnNavMeshStateUpdate", OnNavMeshStateUpdate);
                RegisterMessageHandler("MLAPI_OnNavMeshCorrectionUpdate", OnNavMeshCorrectionUpdate);
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
                        SendToClientsTarget("MLAPI_OnNavMeshStateUpdate", "MLAPI_NAV_AGENT_STATE", stream.GetBuffer());
                    }
                    else
                    {
                        List<int> proximityClients = new List<int>();
                        foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                        {
                            if (Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                                proximityClients.Add(client.Key);
                        }
                        SendToClientsTarget(proximityClients, "MLAPI_OnNavMeshStateUpdate", "MLAPI_NAV_AGENT_STATE", stream.GetBuffer());
                    }
                }
            }

            if(Time.time - lastCorrectionTime >= CorrectionDelay)
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
                        SendToClientsTarget("MLAPI_OnNavMeshCorrectionUpdate", "MLAPI_NAV_AGENT_CORRECTION", stream.GetBuffer());
                    }
                    else
                    {
                        List<int> proximityClients = new List<int>();
                        foreach (KeyValuePair<int, NetworkedClient> client in NetworkingManager.singleton.connectedClients)
                        {
                            if (Vector3.Distance(client.Value.PlayerObject.transform.position, transform.position) <= ProximityRange)
                                proximityClients.Add(client.Key);
                        }
                        SendToClientsTarget(proximityClients, "MLAPI_OnNavMeshCorrectionUpdate", "MLAPI_NAV_AGENT_CORRECTION", stream.GetBuffer());
                    }
                }
                lastCorrectionTime = Time.time;
            }
        }

        private void OnNavMeshStateUpdate(int clientId, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    float xDestination = reader.ReadSingle();
                    float yDestination = reader.ReadSingle();
                    float zDestination = reader.ReadSingle();

                    float xVel = reader.ReadSingle();
                    float yVel = reader.ReadSingle();
                    float zVel = reader.ReadSingle();

                    float xPos = reader.ReadSingle();
                    float yPos = reader.ReadSingle();
                    float zPos = reader.ReadSingle();

                    Vector3 destination = new Vector3(xDestination, yDestination, zDestination);
                    Vector3 velocity = new Vector3(xVel, yVel, zVel);
                    Vector3 position = new Vector3(xPos, yPos, zPos);

                    agent.SetDestination(destination);
                    agent.velocity = velocity;
                    if (WarpOnDestinationChange)
                        agent.Warp(position);
                    else
                        agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));
                }
            }
        }

        private void OnNavMeshCorrectionUpdate(int clinetId, byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    float xVel = reader.ReadSingle();
                    float yVel = reader.ReadSingle();
                    float zVel = reader.ReadSingle();

                    float xPos = reader.ReadSingle();
                    float yPos = reader.ReadSingle();
                    float zPos = reader.ReadSingle();

                    Vector3 velocity = new Vector3(xVel, yVel, zVel);
                    Vector3 position = new Vector3(xPos, yPos, zPos);

                    agent.velocity = velocity;
                    agent.Warp(Vector3.Lerp(transform.position, position, DriftCorrectionPercentage));
                }
            }
        }
    }
}
