using System;
using NUnit.Framework;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine.Networking;

public class TransportTest : MonoBehaviour
{
    // hack, remove any NetworkObject's from the scene to avoid spawning scene objects
    //  which themselves might not be able initialize themselves properly
    private void ForceNetworkObjectShutdown()
    {
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        for (int i = 0; i < networkObjects.Length; i++)
        {
            DestroyImmediate(networkObjects[i]);
        }
    }
}
