using System;
using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

namespace MLAPI_Examples
{
    public class BasicRpcPositionSync : NetworkedBehaviour
    {
        // Send position no more than 20 times a second
        public int SendsPerSecond = 20;
        // Dont send position unless we have moved at least 1 cm
        public float MinimumMovement = 0.01f;
        // Dont send position unless we have rotated at least 1 degree
        public float MinimumRotation = 1f;
        // Allows you to change the channel position updates are sent on. Its prefered to be UnreliableSequenced for fast paced.
        public string Channel = "MLAPI_DEFAULT_MESSAGE";

        private float lastSendTime;
        private Vector3 lastSendPosition;
        private Quaternion lastSendRotation;

        public override void NetworkStart()
        {
            // This is called when the object is spawned. Once this gets invoked. The object is ready for RPC and var changes.

            // Set the defaults to prevent a position update straight after spawn with the same redundant values. (The MLAPI syncs positions on spawn)
            lastSendPosition = transform.position;
            lastSendRotation = transform.rotation;
        }

        private void Update()
        {
            // Check if its time to send a new position update
            if (IsOwner && Time.time - lastSendTime > (1f / SendsPerSecond))
            {
                // Check if we have moved enough or rotated enough for a position update
                if (Vector3.Distance(lastSendPosition, transform.position) >= MinimumMovement || Quaternion.Angle(lastSendRotation, transform.rotation) > MinimumRotation)
                {
                    // We moved enough.

                    // Set the last states
                    lastSendTime = Time.time;
                    lastSendPosition = transform.position;
                    lastSendRotation = transform.rotation;

                    if (IsClient)
                    {
                        // If we are a client. (A client can be either a normal client or a HOST), we want to send a ServerRPC. ServerRPCs does work for host to make code consistent.
                        InvokeServerRpc(SendPositionToServer, transform.position, transform.rotation, Channel);
                    }
                    else if (IsServer)
                    {
                        // This is a strict server with no client attached. We can thus send the ClientRPC straight away without the server inbetween.
                        InvokeClientRpcOnEveryone(SetPosition, transform.position, transform.rotation, Channel);
                    }
                }
            }
        }

        [ServerRPC(RequireOwnership = true)]
        public void SendPositionToServer(Vector3 position, Quaternion rotation)
        {
            // This code gets ran on the server at the request of clients or the host

            // Tell every client EXCEPT the owner (since they are the ones that actually send the position) to apply the new position
            InvokeClientRpcOnEveryoneExcept(SetPosition, OwnerClientId, position, rotation, Channel);
        }

        [ClientRPC]
        public void SetPosition(Vector3 position, Quaternion rotation)
        {
            // This code gets ran on the clients at the request of the server.

            transform.position = position;
            transform.rotation = rotation;
        }
    }
}
