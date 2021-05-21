// using System;
// using System.Collections;
// using MLAPI.Prototyping;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;
//
// namespace MLAPI.RuntimeTests
// {
//     public class NetworkTransformTests
//     {
//         private NetworkObject m_Player;
//         private NetworkObject m_PlayerGhost;
//
//         [UnitySetUp]
//         public IEnumerator Setup()
//         {
//             LogAssert.ignoreFailingMessages = true;
//
//             // Create multiple NetworkManager instances
//             if (!MultiInstanceHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients))
//             {
//                 Debug.LogError("Failed to create instances");
//                 Assert.Fail("Failed to create instances");
//             }
//
//             // Create playerPrefab
//             GameObject playerPrefab = new GameObject("Player");
//             NetworkObject networkObject = playerPrefab.AddComponent<NetworkObject>();
//             var networkTransform = playerPrefab.AddComponent<NetworkTransform>();
//             networkTransform.authority = NetworkTransform.Authority.Client;
//
//             // Make it a prefab
//             MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObject);
//
//             // Set the player prefab
//             server.NetworkConfig.PlayerPrefab = playerPrefab;
//
//             for (int i = 0; i < clients.Length; i++)
//             {
//                 clients[i].NetworkConfig.PlayerPrefab = playerPrefab;
//             }
//
//             // Start the instances
//             if (!MultiInstanceHelpers.Start(true, server, clients))
//             {
//                 Debug.LogError("Failed to start instances");
//                 Assert.Fail("Failed to start instances");
//             }
//
//             // Wait for connection on client side
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnected(clients[0]));
//
//             // Wait for connection on server side
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientConnectedToServer(server));
//
//             // This is the *SERVER VERSION* of the *CLIENT PLAYER*
//             var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), server, serverClientPlayerResult));
//
//             // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
//             var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == clients[0].LocalClientId), clients[0], clientClientPlayerResult));
//
//             m_PlayerGhost = serverClientPlayerResult.Result;
//             m_Player = clientClientPlayerResult.Result;
//         }
//
//         [UnityTest()]
//         public IEnumerator TestMove()
//         {
//             Debug.Log("Testing position");
//             var playerTransform = m_Player.transform;
//             playerTransform.position = new Vector3(10, 0, 0);
//             Assert.AreEqual(0f, m_PlayerGhost.transform.position.x, "wrong initial value"); // sanity check
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_PlayerGhost.transform.position.x != 0 ));
//             Assert.AreEqual(10, m_PlayerGhost.transform.position.x, "wrong position on ghost");
//             Debug.Log("Testing rotation");
//
//             playerTransform.rotation = Quaternion.Euler(90, 0, 0);
//             Assert.AreEqual(90, playerTransform.rotation.eulerAngles.x); // sanity check
//             Assert.AreEqual(0f, m_PlayerGhost.transform.rotation.x, "wrong initial value"); // sanity check
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_PlayerGhost.transform.rotation.eulerAngles.x != 0 ));
//             Assert.True(Math.Abs(90 - m_PlayerGhost.transform.rotation.eulerAngles.x) < 0.05f, $"wrong rotation on ghost, expected 90, got {m_PlayerGhost.transform.rotation.eulerAngles.x}");
//             Debug.Log("Testing scale");
//
//             playerTransform.localScale = new Vector3(2, 2, 2);
//             Assert.AreEqual(1f, m_PlayerGhost.transform.lossyScale.x, "wrong initial value"); // sanity check
//             yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => m_PlayerGhost.transform.lossyScale.x > 1f ));
//             Assert.AreEqual(2, m_PlayerGhost.transform.lossyScale.x, "wrong scale on ghost");
//
//             // todo reparent and test
//             // todo add tests for authority
//             // todo test all public API
//
//         }
//     }
// }
