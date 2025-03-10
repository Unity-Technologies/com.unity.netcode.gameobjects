using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class PlayerColor : NetworkBehaviour
{
    public bool ApplyColorToChildren;
    public Color Color { get; private set; }
    public List<GameObject> IgnoreChildren;


    protected override void OnNetworkPostSpawn()
    {
        UpdatePlayerColor();
        base.OnNetworkPostSpawn();
    }

    protected override void OnNetworkSessionSynchronized()
    {
        UpdatePlayerColor();
        base.OnNetworkSessionSynchronized();
    }

    public void UpdatePlayerColor()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Color = ClientColorExtension.GetClientColor(OwnerClientId);
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
    }
}
