using System;
using System.Collections;
using Unity.Netcode.Components;
using NUnit.Framework;
using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(true, true)]
    [TestFixture(true, false)]
    [TestFixture(false, true)]
    [TestFixture(false, false)]
    public class NetworkTransformTests : BaseMultiInstanceTest
    {
        private NetworkObject m_ClientSideClientPlayer;
        private NetworkObject m_ServerSideClientPlayer;

        private readonly bool m_TestWithClientNetworkTransform;

        private readonly bool m_TestWithHost;

        public NetworkTransformTests(bool testWithHost, bool testWithClientNetworkTransform)
        {
            m_TestWithHost = testWithHost; // from test fixture
            m_TestWithClientNetworkTransform = testWithClientNetworkTransform;
        }

        protected override int NbClients => 1;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: m_TestWithHost, nbClients: NbClients, updatePlayerPrefab: playerPrefab =>
            {
                if (m_TestWithClientNetworkTransform)
                {
                    playerPrefab.AddComponent<ClientNetworkTransform>();
                }
                else
                {
                    playerPrefab.AddComponent<NetworkTransform>();
                }
            });

            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ServerNetworkManager, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ClientNetworkManagers[0], clientClientPlayerResult));

            m_ServerSideClientPlayer = serverClientPlayerResult.Result;
            m_ClientSideClientPlayer = clientClientPlayerResult.Result;
        }

        // TODO: rewrite after perms & authority changes
        //*
        [UnityTest]
        public IEnumerator TestAuthoritativeTransformChangeOneAtATime([Values] bool testLocalTransform)
        {
            var waitResult = new MultiInstanceHelpers.CoroutineResultWrapper<bool>();

            NetworkTransform authoritativeNetworkTransform;
            NetworkTransform otherSideNetworkTransform;
            if (m_TestWithClientNetworkTransform)
            {
                // client auth net transform can write from client, not from server
                otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
                authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            }
            else
            {
                // server auth net transform can't write from client, not from client
                authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            }
            Assert.That(!otherSideNetworkTransform.CanWriteToTransform);
            Assert.That(authoritativeNetworkTransform.CanWriteToTransform);

            // // server auth net transform can't write from client, not from client
            // var authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
            // Assert.That(!authoritativeNetworkTransform.CanCommitToTransform);
            // var otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            // Assert.That(otherSideNetworkTransform.CanCommitToTransform);

            // if (testClientAuthority)
            // {
            //     if (m_TestWithClientNetworkTransform)
            //     {
            //         // client auth net transform can write from client, not from server
            //         authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //         Assert.That(authoritativeNetworkTransform.CanCommitToTransform);
            //         otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //         Assert.That(!otherSideNetworkTransform.CanCommitToTransform);
            //     }
            //     else
            //     {
            //         // server auth net transform can't write from client, not from client
            //         authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            //         Assert.That(!authoritativeNetworkTransform.CanCommitToTransform);
            //         otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
            //         Assert.That(otherSideNetworkTransform.CanCommitToTransform);
            //     }
            // }
            // else
            // {
            //     if (m_TestWithClientNetworkTransform)
            //     {
            //         // client auth net transform can write from client, not from server
            //         authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //         Assert.That(authoritativeNetworkTransform.CanCommitToTransform);
            //         otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //         Assert.That(!otherSideNetworkTransform.CanCommitToTransform);
            //     }
            //     else
            //     {
            //         // server auth net transform can't write from client, not from client
            //         authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
            //         Assert.That(!authoritativeNetworkTransform.CanCommitToTransform);
            //         otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            //         Assert.That(otherSideNetworkTransform.CanCommitToTransform);
            //     }
            // }

            authoritativeNetworkTransform.Interpolate = false;
            otherSideNetworkTransform.Interpolate = false;

            if (authoritativeNetworkTransform.CanWriteToTransform)
            {
                authoritativeNetworkTransform.InLocalSpace = testLocalTransform;
            }

            if (otherSideNetworkTransform.CanWriteToTransform)
            {
                otherSideNetworkTransform.InLocalSpace = testLocalTransform;
            }

            float approximation = 0.05f;

            // test position
            var authPlayerTransform = authoritativeNetworkTransform.transform;
            authPlayerTransform.position = new Vector3(10, 20, 30);
            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "server side pos should be zero at first"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.position.x > approximation, waitResult, maxFrames: 120));
            if (!waitResult.Result)
            {
                throw new Exception("timeout while waiting for position change");
            }
            Assert.True(new Vector3(10, 20, 30) == otherSideNetworkTransform.transform.position, $"wrong position on ghost, {otherSideNetworkTransform.transform.position}"); // Vector3 already does float approximation with ==

            // test rotation
            authPlayerTransform.rotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            Assert.AreEqual(Quaternion.identity, otherSideNetworkTransform.transform.rotation, "wrong initial value for rotation"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.rotation.eulerAngles.x > approximation, waitResult, maxFrames: 120));
            if (!waitResult.Result)
            {
                throw new Exception("timeout while waiting for rotation change");
            }
            // approximation needed here since eulerAngles isn't super precise.
            Assert.LessOrEqual(Math.Abs(45 - otherSideNetworkTransform.transform.rotation.eulerAngles.x), approximation, $"wrong rotation on ghost on x, got {otherSideNetworkTransform.transform.rotation.eulerAngles.x}");
            Assert.LessOrEqual(Math.Abs(40 - otherSideNetworkTransform.transform.rotation.eulerAngles.y), approximation, $"wrong rotation on ghost on y, got {otherSideNetworkTransform.transform.rotation.eulerAngles.y}");
            Assert.LessOrEqual(Math.Abs(35 - otherSideNetworkTransform.transform.rotation.eulerAngles.z), approximation, $"wrong rotation on ghost on z, got {otherSideNetworkTransform.transform.rotation.eulerAngles.z}");

            // test scale
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.x, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.y, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.z, "wrong initial value for scale"); // sanity check
            authPlayerTransform.localScale = new Vector3(2, 3, 4);
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.lossyScale.x > 1f + approximation, waitResult, maxFrames: 120));
            if (!waitResult.Result)
            {
                throw new Exception("timeout while waiting for scale change");
            }
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, otherSideNetworkTransform.transform.lossyScale.x, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, otherSideNetworkTransform.transform.lossyScale.y, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, otherSideNetworkTransform.transform.lossyScale.z, "wrong scale on ghost");

            // todo reparent and test
            // todo test all public API
        }

        [UnityTest]
        public IEnumerator TestCantChangeTransformFromOtherSideAuthority([Values] bool testClientAuthority)
        {
            // test server can't change client authoritative transform
            NetworkTransform networkTransform;
            NetworkTransform otherSideNetworkTransform;
            // var networkTransform = (testClientAuthority ? m_ClientSideClientPlayer : m_ServerSideClientPlayer).GetComponent<NetworkTransform>();
            if (testClientAuthority)
            {
                if (m_TestWithClientNetworkTransform)
                {
                    networkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
                    otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
                }
                else
                {
                    networkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
                    otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                }
            }
            else
            {
                if (m_TestWithClientNetworkTransform)
                {
                    networkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
                    otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();

                }
                else
                {
                    networkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                    otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
                }
            }
            networkTransform.Interpolate = false;
            otherSideNetworkTransform.Interpolate = false;
            // networkTransform.SetAuthority(authorityToTest);
            // networkTransform.InitializeInterpolator<NoInterpolator<float>, NoInterpolator<Quaternion>, NoInterpolator<float>>();

            // var otherSideNetworkTransform = (authorityToTest == NetworkAuthority.Client ? m_ServerSideClientPlayer : m_ClientSideClientPlayer).GetComponent<NetworkTransform>();
            // otherSideNetworkTransform.SetAuthority(authorityToTest);
            // otherSideNetworkTransform.InitializeInterpolator<NoInterpolator<float>, NoInterpolator<Quaternion>, NoInterpolator<float>>();

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "other side pos should be zero at first"); // sanity check
            otherSideNetworkTransform.transform.position = new Vector3(4, 5, 6);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "got authority error, but other side still moved!");
        }
        //*/

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return base.Teardown();
            UnityEngine.Object.DestroyImmediate(m_PlayerPrefab);
        }
    }
}
