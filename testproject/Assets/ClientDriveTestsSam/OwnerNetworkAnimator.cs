using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class OwnerNetworkAnimator : NetworkAnimator
{
    IReadOnlyList<ulong> m_AllClientsButOwnerCache;
    protected override bool m_SendMessagesAllowed => IsOwner;

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        SetupAuthority();
        RefreshAllClientsButOwner();
    }

    public override void OnLostOwnership()
    {
        base.OnLostOwnership();
        RefreshAllClientsButOwner();
    }

    void OnClientRefresh(ulong _)
    {
        RefreshAllClientsButOwner();
    }

    public override void OnNetworkSpawn()
    {
        RefreshAllClientsButOwner();
        NetworkManager.OnClientConnectedCallback += OnClientRefresh;
        NetworkManager.OnClientDisconnectCallback += OnClientRefresh;

        if (IsOwner)
        {
            OnGainedOwnership();
        }

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.OnClientConnectedCallback -= OnClientRefresh;
        NetworkManager.OnClientDisconnectCallback -= OnClientRefresh;
    }

    void RefreshAllClientsButOwner()
    {
        if (IsServer)
        {
            var tmpList = new List<ulong>(NetworkManager.ConnectedClientsIds.Count - 1);
            foreach (var clientID in NetworkManager.ConnectedClientsIds)
            {
                if (clientID != OwnerClientId)
                {
                    tmpList.Add(clientID);
                }
            }

            m_AllClientsButOwnerCache = tmpList.AsReadOnly();
        }
    }

    #region anim state sending
    protected override void DoSendAnimState(AnimationMessage animMsg)
    {
        // ...then tell all the clients to do the same
        if (IsServer)
        {
            SendAnimStateClientRpc(animMsg);
        }
        else
        {
            BroadcastToNonOwnerAnimStateServerRpc(animMsg);
        }
    }

    [ServerRpc]
    void BroadcastToNonOwnerAnimStateServerRpc(AnimationMessage animSnapshot, ServerRpcParams serverRpcParams = default)
    {
        PlayAnimStateLocally(animSnapshot);
        SendAnimStateClientRpc(animSnapshot, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = m_AllClientsButOwnerCache } });
    }
    #endregion

    #region trigger sending
    protected override void DoSendTrigger(AnimationTriggerMessage animMsg)
    {
        // ...then tell all the clients to do the same
        if (IsServer)
        {
            SendAnimTriggerClientRpc(animMsg);
        }
        else
        {
            BroadcastToNonOwnerAnimTriggerServerRpc(animMsg);
        }
    }

    [ServerRpc]
    void BroadcastToNonOwnerAnimTriggerServerRpc(AnimationTriggerMessage animSnapshot, ServerRpcParams serverRpcParams = default)
    {
        // play locally
        PlayAnimLocally(animSnapshot);

        // send to all clients except owner
        SendAnimTriggerClientRpc(animSnapshot, new ClientRpcParams() { Send = new ClientRpcSendParams() { TargetClientIds = m_AllClientsButOwnerCache } });
    }

    #endregion
}
