using Unity.Netcode;
using UnityEngine;

public class ServerRPCTestMessage : NetworkBehaviour
{
    public float MessageInterval = 1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            InvokeRepeating(nameof(SendServerDebugMessageServerRPC), MessageInterval, MessageInterval);
        }
    }

    [ServerRpc]
    public void SendServerDebugMessageServerRPC()
    {
        Debug.Log($"Invoked server rpc {nameof(ServerRPCTestMessage)}");
    }
}
