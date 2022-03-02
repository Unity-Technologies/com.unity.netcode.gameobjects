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

        public NetworkTransformTests(bool testWithHost, bool testWithClientNetworkTransform)
        {
            m_UseHost = testWithHost; // from test fixture
            m_TestWithClientNetworkTransform = testWithClientNetworkTransform;
        }

        protected override int NumberOfClients => 1;

        protected override void OnCreatePlayerPrefab()
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
        }

        protected override void OnServerAndClientsCreated()
        {
#if NGO_TRANSFORM_DEBUG
            // Log assert for writing without authority is a developer log...
            // TODO: This is why monolithic test base classes and test helpers are an anti-pattern - this is part of an individual test case setup but is separated from the code verifying it!
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;
#endif
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            // Get the client player representation on both the server and the client side
            m_ServerSideClientPlayer = m_PlayerNetworkObjects[m_ServerNetworkManager.LocalClientId][m_ClientNetworkManagers[0].LocalClientId];
            m_ClientSideClientPlayer = m_PlayerNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][m_ClientNetworkManagers[0].LocalClientId];

            // Get the NetworkTransformTestComponent to make sure the client side is ready before starting test
            var otherSideNetworkTransformComponent = m_ClientSideClientPlayer.GetComponent<NetworkTransformTestComponent>();

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(() => otherSideNetworkTransformComponent.ReadyToReceivePositionUpdate == true);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "Timed out waiting for client-side to notify it is ready!");

            yield return base.OnServerAndClientsConnected();
        }

        // TODO: rewrite after perms & authority changes
        [UnityTest]
        public IEnumerator TestAuthoritativeTransformChangeOneAtATime([Values] bool testLocalTransform)
        {
            // Get the client player's NetworkTransform for both instances
            var authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
            var otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();

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

            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"timeout while waiting for position change! Otherside value {otherSideNetworkTransform.transform.position.x} vs. Approximation {approximation}");

            Assert.True(new Vector3(10, 20, 30) == otherSideNetworkTransform.transform.position, $"wrong position on ghost, {otherSideNetworkTransform.transform.position}"); // Vector3 already does float approximation with ==

            // test rotation
            authPlayerTransform.rotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            Assert.AreEqual(Quaternion.identity, otherSideNetworkTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            yield return WaitForConditionOrTimeOut(() => otherSideNetworkTransform.transform.rotation.eulerAngles.x > approximation);

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "timeout while waiting for rotation change");

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

            Assert.False(s_GlobalTimeoutHelper.TimedOut, "timeout while waiting for scale change");

            UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, otherSideNetworkTransform.transform.lossyScale.x, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, otherSideNetworkTransform.transform.lossyScale.y, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, otherSideNetworkTransform.transform.lossyScale.z, "wrong scale on ghost");

            // todo reparent and test
            // todo test all public API
        }

        [UnityTest]
        public IEnumerator TestCantChangeTransformFromOtherSideAuthority([Values] bool testClientAuthority)
        {
            // Get the client player's NetworkTransform for both instances
            var authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
            var otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();

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
        protected override IEnumerator OnTearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_PlayerPrefab);
            yield return base.OnTearDown();
        }
    }
}
