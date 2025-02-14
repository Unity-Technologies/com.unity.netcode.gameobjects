using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class PlayerColor : NetworkBehaviour
{
    private static Color[] s_Colors = { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow };
    public bool ApplyColorToChildren;
    public Color Color { get; private set; }
    public List<GameObject> IgnoreChildren;

    protected override void OnNetworkPostSpawn()
    {
        UpdatePlayerColor();
        base.OnNetworkPostSpawn();
    }

    protected override void OnInSceneObjectsSpawned()
    {
        if (IsOwner)
        {
            SetTextColor(OwnerClientId);
        }
        base.OnInSceneObjectsSpawned();
    }

    private Color GetClientColor(ulong clientId)
    {
        ulong myId = clientId - (ulong)(NetworkManager.DistributedAuthorityMode && NetworkManager.CMBServiceConnection ? 1 : 0);
        return s_Colors[myId % Convert.ToUInt64(s_Colors.Length)];
    }

    public void UpdatePlayerColor()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Color = GetClientColor(GetComponent<NetworkObject>().OwnerClientId);
        meshRenderer.material.color = Color;
        if (ApplyColorToChildren)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var childMeshRenderer in meshRenderers)
            {
                if (IgnoreChildren != null && IgnoreChildren.Contains(childMeshRenderer.gameObject))
                {
                    continue;
                }
                childMeshRenderer.material.color = Color;
            }
        }

        if (IsOwner)
        {
            SetTextColor(OwnerClientId);
        }
    }

    public void SetTextColor(ulong clientId)
    {
        var gameObject = GameObject.Find("ServerHostClientDisplay");
        if (gameObject != null)
        {
            var serverHost = gameObject.GetComponent<ServerHostClientText>();
            var color = GetClientColor(clientId);
            serverHost?.SetColor(color);
            serverHost.UpdateTextColor();
        }
    }
}
