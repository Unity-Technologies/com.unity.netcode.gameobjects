using MLAPI;
using MLAPI.Messaging;
using UnityEngine;

namespace MLAPI_Examples
{
    public class ConvenienceMessagingPing : NetworkedBehaviour
    {
        public override void NetworkStart()
        {
            if (IsClient)
            {
                int rnd = Random.Range(0, int.MaxValue);
                
                Debug.LogFormat("Pining server with number {0}", rnd);
                
                InvokeServerRpc(PingServer, rnd);
            }
        }
        
        [ServerRPC(RequireOwnership = false)]
        public void PingServer(int number)
        {
            ulong sender = ExecutingRpcSender;

            Debug.LogFormat("Got pinged by {0} with the number {1}", sender, number);
            
            // Sends the number back to client
            InvokeClientRpcOnClient(PingClient, sender, number);
        }

        [ClientRPC]
        public void PingClient(int number)
        {
            Debug.LogFormat("Server replied with {0}!", number);
        }
    }
}