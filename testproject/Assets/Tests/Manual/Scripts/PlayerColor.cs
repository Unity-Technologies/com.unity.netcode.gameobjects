using Unity.Netcode;
using System;
using UnityEngine;

namespace TestProject.ManualTests
{
    public class PlayerColor : MonoBehaviour
    {
        private static Color[] s_Colors = { Color.red, Color.yellow, Color.green, Color.blue, Color.cyan, Color.magenta, Color.white };
        public bool ApplyColorToChildren;

        /// <summary>
        /// Sets the mesh renderer's material's color based on the NetworkObject's OwnerClientId
        /// </summary>
        private void Start()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            ulong myId = GetComponent<NetworkObject>().OwnerClientId;
            meshRenderer.material.color = s_Colors[myId % Convert.ToUInt64(s_Colors.Length)];
            if (ApplyColorToChildren)
            {
                var meshRenderers = GetComponentsInChildren<MeshRenderer>();
                foreach (var childMeshRenderer in meshRenderers)
                {
                    childMeshRenderer.material.color = s_Colors[myId % Convert.ToUInt64(s_Colors.Length)];
                }
            }
        }
    }
}
