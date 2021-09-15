using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class DisconnectTests
    {
        [UnityTest]
        public IEnumerator RemoteDisconnectPlayerObjectCleanup()
        {
            // create server and client instances
            MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            // create prefab
            var gameObject = new GameObject("PlayerObject");
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.DontDestroyWithOwner = true;
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(networkObject);

            server.NetworkConfig.PlayerPrefab = gameObject;

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = gameObject;
            }

            // start server and connect clients
            MultiInstanceHelpers.Start(false, server, clients);

            // wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

            // wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));

            // disconnect the remote client
            server.DisconnectClient(clients[0].LocalClientId);

            // wait 1 frame because destroys are delayed
            var nextFrameNumber = Time.frameCount + 1;
            yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);

            // ensure the object was destroyed
            Assert.False(server.SpawnManager.SpawnedObjects.Any(x => x.Value.IsPlayerObject && x.Value.OwnerClientId == clients[0].LocalClientId));

            // cleanup
            MultiInstanceHelpers.Destroy();
        }
    }
}
