using System;
using System.Collections;
#if NGO_TRANSFORM_DEBUG
using System.Text.RegularExpressions;
#endif
using Unity.Netcode.Components;
using NUnit.Framework;
// using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkTransformTestComponent : NetworkTransform
    {
        public bool ReadyToReceivePositionUpdate = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            ReadyToReceivePositionUpdate = true;
        }
    }

    // [TestFixture(true, true)]
    [TestFixture(true, false)]
    // [TestFixture(false, true)]
    [TestFixture(false, false)]
    public class NetworkTransformTests : NetcodeIntegrationTest
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
        protected override bool CanStartServerAndClients()
        {
            return false;
        }

        public IEnumerator InitializeServerAndClients()
        {
            if (m_TestWithClientNetworkTransform)
            {
                // m_PlayerPrefab.AddComponent<ClientNetworkTransform>();
            }
            else
            {
                var networkTransform = m_PlayerPrefab.AddComponent<NetworkTransformTestComponent>();
                networkTransform.Interpolate = false;
            }


            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            m_ClientNetworkManagers[0].NetworkConfig.PlayerPrefab = m_PlayerPrefab;

#if NGO_TRANSFORM_DEBUG
            // Log assert for writing without authority is a developer log...
            // TODO: This is why monolithic test base classes and test helpers are an anti-pattern - this is part of an individual test case setup but is separated from the code verifying it!
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;
#endif
            Assert.True(NetcodeIntegrationTestHelpers.Start(m_TestWithHost, m_ServerNetworkManager, m_ClientNetworkManagers), "Failed to start server and client instances");

            RegisterSceneManagerHandler();

            // Wait for connection on client and server side
            yield return WaitForClientsConnectedOrTimeOut();

            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject &&
            x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ServerNetworkManager, serverClientPlayerResult);
            m_ServerSideClientPlayer = serverClientPlayerResult.Result;

            // Wait for 1 tick
            yield return s_DefaultWaitForTick;

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject &&
            x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ClientNetworkManagers[0], clientClientPlayerResult);
            m_ClientSideClientPlayer = clientClientPlayerResult.Result;
            var otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransformTestComponent>();

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(() => otherSideNetworkTransform.ReadyToReceivePositionUpdate == true);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Timed out waiting for client-side to notify it is ready!");

            // Wait for 1 more tick before starting tests
            yield return s_DefaultWaitForTick;
        }

        // TODO: rewrite after perms & authority changes
        [UnityTest]
        public IEnumerator TestAuthoritativeTransformChangeOneAtATime([Values] bool testLocalTransform)
        {
            yield return InitializeServerAndClients();


            var waitResult = new NetcodeIntegrationTestHelpers.ResultWrapper<bool>();

            NetworkTransform authoritativeNetworkTransform;
            NetworkTransform otherSideNetworkTransform;
            // if (m_TestWithClientNetworkTransform)
            // {
            //     // client auth net transform can write from client, not from server
            //     otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //     authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            // }
            // else
            {
                // server auth net transform can't write from client, not from client
                authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            }
            Assert.That(!otherSideNetworkTransform.CanCommitToTransform);
            Assert.That(authoritativeNetworkTransform.CanCommitToTransform);

            if (authoritativeNetworkTransform.CanCommitToTransform)
            {
                authoritativeNetworkTransform.InLocalSpace = testLocalTransform;
            }

            if (otherSideNetworkTransform.CanCommitToTransform)
            {
                otherSideNetworkTransform.InLocalSpace = testLocalTransform;
            }

            float approximation = 0.05f;

            // test position
            var authPlayerTransform = authoritativeNetworkTransform.transform;

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "server side pos should be zero at first"); // sanity check

            authPlayerTransform.position = new Vector3(10, 20, 30);

            yield return WaitForConditionOrTimeOut(() => otherSideNetworkTransform.transform.position.x > approximation);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, $"timeout while waiting for position change! Otherside value {otherSideNetworkTransform.transform.position.x} vs. Approximation {approximation}");

            Assert.True(new Vector3(10, 20, 30) == otherSideNetworkTransform.transform.position, $"wrong position on ghost, {otherSideNetworkTransform.transform.position}"); // Vector3 already does float approximation with ==

            // test rotation
            authPlayerTransform.rotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            Assert.AreEqual(Quaternion.identity, otherSideNetworkTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            yield return WaitForConditionOrTimeOut(() => otherSideNetworkTransform.transform.rotation.eulerAngles.x > approximation);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "timeout while waiting for rotation change");

            // approximation needed here since eulerAngles isn't super precise.
            Assert.LessOrEqual(Math.Abs(45 - otherSideNetworkTransform.transform.rotation.eulerAngles.x), approximation, $"wrong rotation on ghost on x, got {otherSideNetworkTransform.transform.rotation.eulerAngles.x}");
            Assert.LessOrEqual(Math.Abs(40 - otherSideNetworkTransform.transform.rotation.eulerAngles.y), approximation, $"wrong rotation on ghost on y, got {otherSideNetworkTransform.transform.rotation.eulerAngles.y}");
            Assert.LessOrEqual(Math.Abs(35 - otherSideNetworkTransform.transform.rotation.eulerAngles.z), approximation, $"wrong rotation on ghost on z, got {otherSideNetworkTransform.transform.rotation.eulerAngles.z}");

            // test scale
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.x, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.y, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.z, "wrong initial value for scale"); // sanity check
            authPlayerTransform.localScale = new Vector3(2, 3, 4);

            yield return WaitForConditionOrTimeOut(() => otherSideNetworkTransform.transform.lossyScale.x > 1f + approximation);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "timeout while waiting for scale change");

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, otherSideNetworkTransform.transform.lossyScale.x, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, otherSideNetworkTransform.transform.lossyScale.y, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, otherSideNetworkTransform.transform.lossyScale.z, "wrong scale on ghost");

            // todo reparent and test
            // todo test all public API
        }

        [UnityTest]
        // [Ignore("skipping for now, still need to figure weird multiinstance issue with hosts")]
        public IEnumerator TestCantChangeTransformFromOtherSideAuthority([Values] bool testClientAuthority)
        {
            yield return InitializeServerAndClients();

            // test server can't change client authoritative transform
            NetworkTransform authoritativeNetworkTransform;
            NetworkTransform otherSideNetworkTransform;

            // if (m_TestWithClientNetworkTransform)
            // {
            //     // client auth net transform can write from client, not from server
            //     otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //     authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            // }
            // else
            {
                // server auth net transform can't write from client, not from client
                authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            }

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "other side pos should be zero at first"); // sanity check

            otherSideNetworkTransform.transform.position = new Vector3(4, 5, 6);

            yield return s_DefaultWaitForTick;

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "got authority error, but other side still moved!");
#if NGO_TRANSFORM_DEBUG
            // We are no longer emitting this warning, and we are banishing tests that rely on console output, so
            //  needs re-implementation
            // TODO: This should be a separate test - verify 1 behavior per test
            LogAssert.Expect(LogType.Warning, new Regex(".*without authority detected.*"));
#endif
        }

        /*
         * ownership change
         * test teleport with interpolation
         * test teleport without interpolation
         * test dynamic spawning
         */
        protected override IEnumerator OnPostTearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_PlayerPrefab);
            yield return base.OnPostTearDown();
        }
    }
}
