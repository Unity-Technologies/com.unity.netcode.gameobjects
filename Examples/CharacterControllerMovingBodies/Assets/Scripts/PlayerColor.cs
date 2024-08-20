using System;
using Unity.Netcode;
using UnityEngine;


public class PlayerColor : NetworkBehaviour
{
    private static Color[] s_Colors = { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow};
    public bool ApplyColorToChildren;
    public Color Color { get; private set; }

    public override void OnNetworkSpawn()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        ulong myId = GetComponent<NetworkObject>().OwnerClientId - (ulong)(NetworkManager.DistributedAuthorityMode ? 1 : 0);
        Color = s_Colors[myId % Convert.ToUInt64(s_Colors.Length)];
        meshRenderer.material.color = Color;
        if (ApplyColorToChildren)
        {
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var childMeshRenderer in meshRenderers)
            {
                childMeshRenderer.material.color = Color;
            }
        }

        if (IsLocalPlayer)
        {
            var gameObject = GameObject.Find("ServerHostClientDisplay");
            if (gameObject != null)
            {
                var serverHost = gameObject.GetComponent<ServerHostClientText>();
                serverHost?.SetColor(Color);
            }
        }
        base.OnNetworkSpawn();
    }
}
