// using System.Collections;
// using NUnit.Framework;
// using Unity.Netcode.Components;
// using Unity.Netcode.Samples;
// using UnityEngine;
// using UnityEngine.TestTools;
//
// // Tests for ClientNetworkTransform (CNT) + NetworkRigidbody. This test is in TestProject because it needs access to ClientNetworkTransform
// namespace Unity.Netcode.RuntimeTests.Physics
// {
//     public class NetworkRigidbodyDynamicCntTest : NetworkRigidbodyCntTestBase
//     {
//         public override bool Kinematic => false;
//     }
//
//     public class NetworkRigidbodyKinematicCntTest : NetworkRigidbodyCntTestBase
//     {
//         public override bool Kinematic => true;
//     }
//
//     public abstract class NetworkRigidbodyCntTestBase : NetcodeIntegrationTest
//     {
//         protected override int NumberOfClients => 1;
//
//         public abstract bool Kinematic { get; }
//
//         [UnitySetUp]
//         public override IEnumerator Setup()
//         {
//             yield return StartSomeClientsAndServerWithPlayers(true, NumberOfClients, playerPrefab =>
//             {
//                 playerPrefab.AddComponent<ClientNetworkTransform>();
//                 playerPrefab.AddComponent<Rigidbody>();
//                 playerPrefab.AddComponent<NetworkRigidbody>();
//                 playerPrefab.GetComponent<Rigidbody>().isKinematic = Kinematic;
//             });
//         }
//         /// <summary>
//         /// Tests that a server can destroy a NetworkObject and that it gets despawned correctly.
//         /// </summary>
//         /// <returns></returns>
//         [UnityTest]
//         public IEnumerator TestRigidbodyKinematicEnableDisable()
//         {
//             // This is the *SERVER VERSION* of the *CLIENT PLAYER*
//             var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
//             yield returnNetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult);
//             var serverPlayer = serverClientPlayerResult.Result.gameObject;
//
//             // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
//             var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
//             yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult);
//             var clientPlayer = clientClientPlayerResult.Result.gameObject;
//
//             Assert.IsNotNull(serverPlayer);
//             Assert.IsNotNull(clientPlayer);
//
//             int waitFor = Time.frameCount + 2;
//             yield return new WaitUntil(() => Time.frameCount >= waitFor);
//
//             TestKinematicSetCorrectly(clientPlayer, serverPlayer);
//
//             // despawn the server player
//             serverPlayer.GetComponent<NetworkObject>().Despawn(false);
//
//             yield return null;
//             yield return null;
//
//             Assert.IsTrue(clientPlayer == null); // safety check that object is actually despawned.
//         }
//
//         private void TestKinematicSetCorrectly(GameObject canCommitPlayer, GameObject canNotCommitPlayer)
//         {
//
//             // can commit player has authority and should have a kinematic mode of false (or true in case body was already kinematic).
//             Assert.True(canCommitPlayer.GetComponent<Rigidbody>().isKinematic == Kinematic);
//
//             // can not commit player has no authority and should have a kinematic mode of true
//             Assert.True(canNotCommitPlayer.GetComponent<Rigidbody>().isKinematic);
//         }
//     }
// }
