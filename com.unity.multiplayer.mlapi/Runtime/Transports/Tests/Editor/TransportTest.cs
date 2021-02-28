using System;
using MLAPI;
using MLAPI.Configuration;
using MLAPI.Transports;
using NUnit.Framework;
using UnityEngine;
using MLAPI.Transports.UNET;
using UnityEngine.Networking;

public class TransportTest : MonoBehaviour
{
    // hack, remove any NetworkedObject's from the scene to avoid spawning scene objects
    //  which themselves might not be able initialize themselves properly
    private void ForceNetworkObjectShutdown()
    {
        NetworkedObject[] networkedObjects = FindObjectsOfType<NetworkedObject>();
        for (int i = 0; i < networkedObjects.Length; i++)
        {
            DestroyImmediate(networkedObjects[i]);
        }
    }

    // A Test behaves as an ordinary method
    [Test]
    public void UNetCustomChannelRegistrationTest()
    {
        ForceNetworkObjectShutdown();

        GameObject o = new GameObject();
        NetworkManager nm = (NetworkManager)o.AddComponent(typeof(NetworkManager));
        nm.SetSingleton();
        nm.NetworkConfig = new NetworkConfig();
        UnetTransport ut = (UnetTransport)o.AddComponent(typeof(UnetTransport));

        ut.ServerListenPort = 7777;
        nm.NetworkConfig.NetworkTransport = ut;

        byte CustomChannel = 0;

        // test 1: add a legit channel.
        ut.Channels.Add(new UnetChannel { Id = Channel.ChannelUnused + CustomChannel, Type = QosType.Unreliable });

        try
        {
            nm.StartServer();
        }
        catch
        {
            Assert.Fail("The UNet transport won't allow registration of a legit user channel");
        }

        nm.StopServer();
        nm.Shutdown();

        ut.Channels.Clear();
        // test 2: add a bogus channel (one that intersects with the MLAPI built-in ones.)  Expect failure
        ut.Channels.Add(new UnetChannel { Id = Channel.Internal, Type = QosType.Unreliable });

        try
        {
            nm.StartServer();
            Assert.Fail("The Unet transport allowed registration of an MLAPI-reserved channel");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }

        nm.StopServer();
        nm.Shutdown();
    }
}