using System;
using System.Collections;
using MLAPI.Messaging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace MLAPI.RuntimeTests
{
    public class RpcTests
    {
        public class RpcTestNB : NetworkBehaviour
        {
            public event Action OnServer_Rpc;
            public event Action OnClient_Rpc;

            [ServerRpc]
            public void MyServerRpc()
            {
                OnServer_Rpc();
            }

            [ClientRpc]
            public void MyClientRpc()
            {
                OnClient_Rpc();
            }
        }

        [UnityTest]
        public IEnumerator TestRpcs()
        {
            // Create multiple NetworkManager instances
            if (!MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients))
            {
                Debug.LogError("Failed to create instances");
                Assert.Fail("Failed to create instances");
            }

            /*
             * Normally we would only allow player prefabs to be set to a prefab. Not runtime created objects.
             * In order to prevent having a Resource folder full of a TON of prefabs that we have to maintain,
             * MultiInstanceHelper has a helper function that lets you mark a runtime created object to be
             * treated as a prefab by the MLAPI. That's how we can get away with creating the player prefab
             * at runtime without it being treated as a SceneObject or causing other conflicts with the MLAPI.
             */

            // Create playerPrefab
            var playerPrefab = new GameObject("Player");
            NetworkObject networkObject = playerPrefab.AddComponent<NetworkObject>();
            playerPrefab.AddComponent<RpcTestNB>();

            // Make it a prefab
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);

            // Set the player prefab
            server.NetworkConfig.PlayerPrefab = playerPrefab;

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].NetworkConfig.PlayerPrefab = playerPrefab;
            }

            // Start the instances
            if (!MultiInstanceHelpers.Start(true, server, clients))
            {
                Assert.Fail("Failed to start instances");
            }


            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(clients));

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));

            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), server, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), clients[0], clientClientPlayerResult));

            // Setup state
            bool hasReceivedServerRpc = false;
            bool hasReceivedClientRpcRemotely = false;
            bool hasReceivedClientRpcLocally = false;

            clientClientPlayerResult.Result.GetComponent<RpcTestNB>().OnClient_Rpc += () =>
            {
                Debug.Log("ClientRpc received on client object");
                hasReceivedClientRpcRemotely = true;
            };

            clientClientPlayerResult.Result.GetComponent<RpcTestNB>().OnServer_Rpc += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Assert.Fail("ServerRpc invoked locally. Weaver failure?");
            };

            serverClientPlayerResult.Result.GetComponent<RpcTestNB>().OnServer_Rpc += () =>
            {
                Debug.Log("ServerRpc received on server object");
                hasReceivedServerRpc = true;
            };

            serverClientPlayerResult.Result.GetComponent<RpcTestNB>().OnClient_Rpc += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Debug.Log("ClientRpc received on server object");
                hasReceivedClientRpcLocally = true;
            };

            // Send ServerRpc
            clientClientPlayerResult.Result.GetComponent<RpcTestNB>().MyServerRpc();

            // Send ClientRpc
            serverClientPlayerResult.Result.GetComponent<RpcTestNB>().MyClientRpc();

            // Wait for RPCs to be received
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => hasReceivedServerRpc && hasReceivedClientRpcLocally && hasReceivedClientRpcRemotely));

            Assert.True(hasReceivedServerRpc, "ServerRpc was not received");
            Assert.True(hasReceivedClientRpcLocally, "ClientRpc was not locally received on the server");
            Assert.True(hasReceivedClientRpcRemotely, "ClientRpc was not remotely received on the client");

            // Cleanup
            MultiInstanceHelpers.Destroy();
        }
    }
}
