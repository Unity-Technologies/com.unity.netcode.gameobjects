using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using MLAPI.Messaging;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace MLAPI.RuntimeTests
{
    public class RPCTests
    {
        public class RPCTestNetworkBehaviour : NetworkBehaviour
        {
            public event Action OnServer_RPC;
            public event Action OnClient_RPC;

            [ServerRpc]
            public void MyServerRpc()
            {
                OnServer_RPC();
            }

            [ClientRpc]
            public void MyClientRpc()
            {
                OnClient_RPC();
            }
        }

        [UnityTest]
        public IEnumerator TestRPCs()
        {
            // Set target frameRate to work around ubuntu timings
            int targetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 120;

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
            playerPrefab.AddComponent<RPCTestNetworkBehaviour>();

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
                Debug.LogError("Failed to start instances");
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
            bool hasReceivedServerRPC = false;
            bool hasReceivedClientRPCRemotely = false;
            bool hasReceivedClientRPCLocally = false;

            clientClientPlayerResult.Result.GetComponent<RPCTestNetworkBehaviour>().OnClient_RPC += () =>
            {
                Debug.Log("ClientRPC received on client object");
                hasReceivedClientRPCRemotely = true;
            };

            clientClientPlayerResult.Result.GetComponent<RPCTestNetworkBehaviour>().OnServer_RPC += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Assert.Fail("ServerRPC invoked locally. Weaver failure?");
            };

            serverClientPlayerResult.Result.GetComponent<RPCTestNetworkBehaviour>().OnServer_RPC += () =>
            {
                Debug.Log("ServerRPC received on server object");
                hasReceivedServerRPC = true;
            };

            serverClientPlayerResult.Result.GetComponent<RPCTestNetworkBehaviour>().OnClient_RPC += () =>
            {
                // The RPC invoked locally. (Weaver failure?)
                Debug.Log("ClientRPC received on server object");
                hasReceivedClientRPCLocally = true;
            };

            // Send ServerRPC
            clientClientPlayerResult.Result.GetComponent<RPCTestNetworkBehaviour>().MyServerRpc();

            // Send ClientRPC
            serverClientPlayerResult.Result.GetComponent<RPCTestNetworkBehaviour>().MyClientRpc();

            // Wait for RPCs to be received
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => hasReceivedServerRPC && hasReceivedClientRPCLocally && hasReceivedClientRPCRemotely));

            Assert.True(hasReceivedServerRPC, "ServerRPC was not received");
            Assert.True(hasReceivedClientRPCLocally, "ClientRPC was not locally received on the server");
            Assert.True(hasReceivedClientRPCRemotely, "ClientRPC was not remotely received on the client");

            // Release frame rate
            Application.targetFrameRate = targetFrameRate;

            // Cleanup
            MultiInstanceHelpers.Destroy();
        }
    }
}
