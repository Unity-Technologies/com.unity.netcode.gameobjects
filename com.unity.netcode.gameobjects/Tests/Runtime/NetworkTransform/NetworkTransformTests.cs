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

        public (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) ApplyState()
        {
            return ApplyLocalNetworkState(transform);
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
                networkTransform.Interpolate = true;
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
            m_ServerSideClientPlayer = m_ServerNetworkManager.ConnectedClients[m_ClientNetworkManagers[0].LocalClientId].PlayerObject;
            m_ClientSideClientPlayer = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

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

            yield return WaitForConditionOrTimeOut(ServerClientPositionMatches);

            AssertOnTimeout($"timeout while waiting for position change! Otherside value {otherSideNetworkTransform.transform.position.x} vs. Approximation {approximation}");

            //Assert.True(new Vector3(10, 20, 30) == otherSideNetworkTransform.transform.position, $"wrong position on ghost, {otherSideNetworkTransform.transform.position}"); // Vector3 already does float approximation with ==

            // test rotation
            authPlayerTransform.rotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            Assert.AreEqual(Quaternion.identity, otherSideNetworkTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            yield return WaitForConditionOrTimeOut(ServerClientRotationMatches);

            AssertOnTimeout("timeout while waiting for rotation change");

            //// approximation needed here since eulerAngles isn't super precise.
            //Assert.LessOrEqual(Math.Abs(45 - otherSideNetworkTransform.transform.rotation.eulerAngles.x), approximation, $"wrong rotation on ghost on x, got {otherSideNetworkTransform.transform.rotation.eulerAngles.x}");
            //Assert.LessOrEqual(Math.Abs(40 - otherSideNetworkTransform.transform.rotation.eulerAngles.y), approximation, $"wrong rotation on ghost on y, got {otherSideNetworkTransform.transform.rotation.eulerAngles.y}");
            //Assert.LessOrEqual(Math.Abs(35 - otherSideNetworkTransform.transform.rotation.eulerAngles.z), approximation, $"wrong rotation on ghost on z, got {otherSideNetworkTransform.transform.rotation.eulerAngles.z}");

            // test scale
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.x, "wrong initial value for scale"); // sanity check
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.y, "wrong initial value for scale"); // sanity check
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.z, "wrong initial value for scale"); // sanity check
            authPlayerTransform.localScale = new Vector3(2, 3, 4);

            yield return WaitForConditionOrTimeOut(ServerClientScaleMatches);

            AssertOnTimeout("timeout while waiting for scale change");

            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, otherSideNetworkTransform.transform.lossyScale.x, "wrong scale on ghost");
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, otherSideNetworkTransform.transform.lossyScale.y, "wrong scale on ghost");
            //UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, otherSideNetworkTransform.transform.lossyScale.z, "wrong scale on ghost");

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


        /// <summary>
        /// Validates that rotation checks don't produce false positive
        /// results when rolling over between 0 and 360 degrees
        /// </summary>
        [UnityTest]
        public IEnumerator TestRotationThresholdDeltaCheck()
        {
            // Get the client player's NetworkTransform for both instances
            var authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransformTestComponent>();
            var otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransformTestComponent>();
            otherSideNetworkTransform.RotAngleThreshold = authoritativeNetworkTransform.RotAngleThreshold = 5.0f;

            var halfThreshold = authoritativeNetworkTransform.RotAngleThreshold * 0.5001f;
            var serverRotation = authoritativeNetworkTransform.transform.rotation;
            var serverEulerRotation = serverRotation.eulerAngles;

            // Verify rotation is not marked dirty when rotated by half of the threshold
            serverEulerRotation.y += halfThreshold;
            serverRotation.eulerAngles = serverEulerRotation;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            var results = authoritativeNetworkTransform.ApplyState();
            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {authoritativeNetworkTransform.RotAngleThreshold} degrees and only adjusted by {halfThreshold} degrees!");
            yield return s_DefaultWaitForTick;

            // Verify rotation is marked dirty when rotated by another half threshold value
            serverEulerRotation.y += halfThreshold;
            serverRotation.eulerAngles = serverEulerRotation;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            results = authoritativeNetworkTransform.ApplyState();
            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by the threshold value: {authoritativeNetworkTransform.RotAngleThreshold} degrees!");
            yield return s_DefaultWaitForTick;

            //Reset rotation back to zero on all axis
            serverRotation.eulerAngles = serverEulerRotation = Vector3.zero;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            yield return s_DefaultWaitForTick;

            // Rotate by 360 minus halfThreshold (which is really just negative halfThreshold) and verify rotation is not marked dirty
            serverEulerRotation.y = 360 - halfThreshold;
            serverRotation.eulerAngles = serverEulerRotation;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            results = authoritativeNetworkTransform.ApplyState();

            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {authoritativeNetworkTransform.RotAngleThreshold} degrees and only adjusted by " +
                $"{Mathf.DeltaAngle(0, serverEulerRotation.y)} degrees!");

            serverEulerRotation.y -= halfThreshold;
            serverRotation.eulerAngles = serverEulerRotation;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            results = authoritativeNetworkTransform.ApplyState();

            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by {Mathf.DeltaAngle(0, serverEulerRotation.y)} degrees!");

            //Reset rotation back to zero on all axis
            serverRotation.eulerAngles = serverEulerRotation = Vector3.zero;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            yield return s_DefaultWaitForTick;

            serverEulerRotation.y -= halfThreshold;
            serverRotation.eulerAngles = serverEulerRotation;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            results = authoritativeNetworkTransform.ApplyState();
            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {authoritativeNetworkTransform.RotAngleThreshold} degrees and only adjusted by " +
                $"{Mathf.DeltaAngle(0, serverEulerRotation.y)} degrees!");

            serverEulerRotation.y -= halfThreshold;
            serverRotation.eulerAngles = serverEulerRotation;
            authoritativeNetworkTransform.transform.rotation = serverRotation;
            results = authoritativeNetworkTransform.ApplyState();

            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by {Mathf.DeltaAngle(0, serverEulerRotation.y)} degrees!");
        }

        /// <summary>
        /// </summary>
        [UnityTest]
        public IEnumerator TestBitsetValue()
        {
            // Get the client player's NetworkTransform for both instances
            var authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransformTestComponent>();
            var otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransformTestComponent>();
            otherSideNetworkTransform.RotAngleThreshold = authoritativeNetworkTransform.RotAngleThreshold = 0.1f;
            yield return s_DefaultWaitForTick;

            authoritativeNetworkTransform.Interpolate = true;
            otherSideNetworkTransform.Interpolate = true;

            yield return s_DefaultWaitForTick;

            authoritativeNetworkTransform.transform.rotation = Quaternion.Euler(1, 2, 3);
            var serverLastSentState = authoritativeNetworkTransform.GetLastSentState();
            var clientReplicatedState = otherSideNetworkTransform.GetReplicatedNetworkState().Value;
            yield return WaitForConditionOrTimeOut(() => clientReplicatedState.Bitset.Equals(serverLastSentState.Bitset));
            AssertOnTimeout($"Server-side sent state Bitset {serverLastSentState.Bitset} != Client-side replicated state Bitset {clientReplicatedState.Bitset}");

            yield return WaitForConditionOrTimeOut(ServerClientRotationMatches);
            AssertOnTimeout($"[Timed-Out] Server-side client rotation {m_ServerSideClientPlayer.transform.rotation.eulerAngles} != Client-side client rotation {m_ClientSideClientPlayer.transform.rotation.eulerAngles}");
        }

        private bool Aproximately(float x, float y)
        {
            return Mathf.Abs(x - y) <= k_AproximateDeltaVariance;
        }

        private const float k_AproximateDeltaVariance = 0.01f;

        private bool ServerClientRotationMatches()
        {
            var serverEulerRotation = m_ServerSideClientPlayer.transform.rotation.eulerAngles;
            var clientEulerRotation = m_ClientSideClientPlayer.transform.rotation.eulerAngles;
            var xIsEqual = Aproximately(serverEulerRotation.x, clientEulerRotation.x);
            var yIsEqual = Aproximately(serverEulerRotation.y, clientEulerRotation.y);
            var zIsEqual = Aproximately(serverEulerRotation.z, clientEulerRotation.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Server-side client rotation {m_ServerSideClientPlayer.transform.rotation.eulerAngles} != Client-side client rotation {m_ClientSideClientPlayer.transform.rotation.eulerAngles}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool ServerClientPositionMatches()
        {
            var serverPosition = m_ServerSideClientPlayer.transform.position;
            var clientPosition = m_ClientSideClientPlayer.transform.position;
            var xIsEqual = Aproximately(serverPosition.x, clientPosition.x);
            var yIsEqual = Aproximately(serverPosition.y, clientPosition.y);
            var zIsEqual = Aproximately(serverPosition.z, clientPosition.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Server-side client position {m_ServerSideClientPlayer.transform.position} != Client-side client position {m_ClientSideClientPlayer.transform.position}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool ServerClientScaleMatches()
        {
            var serverScale = m_ServerSideClientPlayer.transform.localScale;
            var clientScale = m_ClientSideClientPlayer.transform.localScale;
            var xIsEqual = Aproximately(serverScale.x, clientScale.x);
            var yIsEqual = Aproximately(serverScale.y, clientScale.y);
            var zIsEqual = Aproximately(serverScale.z, clientScale.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Server-side client scale {m_ServerSideClientPlayer.transform.localScale} != Client-side client scale {m_ClientSideClientPlayer.transform.localScale}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }


        /*
         * ownership change
         * test teleport with interpolation
         * test teleport without interpolation
         * test dynamic spawning
         */
        protected override IEnumerator OnTearDown()
        {
            m_EnableVerboseDebug = false;
            UnityEngine.Object.DestroyImmediate(m_PlayerPrefab);
            yield return base.OnTearDown();
        }
    }
}
