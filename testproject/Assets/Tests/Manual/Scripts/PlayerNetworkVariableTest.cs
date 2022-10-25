using UnityEngine;
using Unity.Netcode;

public class PlayerNetworkVariableTest : NetworkBehaviour
{
    public NetworkVariable<int> SomeNetworkVariable = new NetworkVariable<int>();
    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer && !IsServer)
        {
            SomeNetworkVariable.OnValueChanged = OnSomeNetworkVariableUpdated;
        }
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            SomeNetworkVariable.Value++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyClientReceivedUpdateServerRpc(int updatedValue, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Client-{serverRpcParams.Receive.SenderClientId} received updated value of {updatedValue} which has a server-side value of {SomeNetworkVariable.Value}");
    }

    private void OnSomeNetworkVariableUpdated(int previous, int current)
    {
        NotifyClientReceivedUpdateServerRpc(current);
    }
}
