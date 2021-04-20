using MLAPI;
using System;
using UnityEngine;

public class PlayerColor : MonoBehaviour
{
    private static Color[] colors = {
                Color.red, Color.yellow, Color.green, Color.blue, Color.cyan, Color.magenta, Color.white
            };

        // Start is called before the first frame update
        void Start()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            ulong myId = GetComponent<NetworkObject>().OwnerClientId;
            meshRenderer.material.color = colors[myId % Convert.ToUInt64(colors.Length)];
        }
}