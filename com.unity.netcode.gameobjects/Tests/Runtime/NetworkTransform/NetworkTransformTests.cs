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

        public bool ServerAuthority;
        public bool ReadyToReceivePositionUpdate = false;


        protected override bool OnIsServerAuthoritative()
        {
            return ServerAuthority;
        }

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

    [TestFixture(HostOrServer.Host, Authority.Server)]
    [TestFixture(HostOrServer.Host, Authority.Owner)]
    [TestFixture(HostOrServer.Server, Authority.Server)]
    [TestFixture(HostOrServer.Server, Authority.Owner)]

    public class NetworkTransformTests : NetcodeIntegrationTest
    {
        private NetworkObject m_ClientSideClientPlayer;
        private NetworkObject m_ServerSideClientPlayer;

        private NetworkObject m_AuthoritativePlayer;
        private NetworkObject m_NonAuthoritativePlayer;

        private NetworkTransformTestComponent m_AuthoritativeTransform;
        private NetworkTransformTestComponent m_NonAuthoritativeTransform;

        private readonly Authority m_Authority;

        public enum Authority
        {
            Server,
            Owner
        }

        public enum Interpolation
        {
            DisableInterpolate,
            EnableInterpolate
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testWithHost">Value is set by TestFixture</param>
        /// <param name="testWithClientNetworkTransform">Value is set by TestFixture</param>
        public NetworkTransformTests(HostOrServer testWithHost, Authority authority)
        {
            m_UseHost = testWithHost == HostOrServer.Host ? true : false;
            m_Authority = authority;
        }

        protected override int NumberOfClients => 1;

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransformTestComponent = m_PlayerPrefab.AddComponent<NetworkTransformTestComponent>();
            networkTransformTestComponent.ServerAuthority = m_Authority == Authority.Server;
        }

        protected override void OnServerAndClientsCreated()
        {
            if (m_EnableVerboseDebug)
            {
                m_ServerNetworkManager.LogLevel = LogLevel.Developer;
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    clientNetworkManager.LogLevel = LogLevel.Developer;
                }
            }
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            // Get the client player representation on both the server and the client side
            var serverSideClientPlayer = m_ServerNetworkManager.ConnectedClients[m_ClientNetworkManagers[0].LocalClientId].PlayerObject;
            var clientSideClientPlayer = m_ClientNetworkManagers[0].LocalClient.PlayerObject;

            m_AuthoritativePlayer = m_Authority == Authority.Server ? serverSideClientPlayer : clientSideClientPlayer;
            m_NonAuthoritativePlayer = m_Authority == Authority.Server ? clientSideClientPlayer : serverSideClientPlayer;

            // Get the NetworkTransformTestComponent to make sure the client side is ready before starting test
            m_AuthoritativeTransform = m_AuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();
            m_NonAuthoritativeTransform = m_NonAuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(() => m_NonAuthoritativeTransform.ReadyToReceivePositionUpdate == true);
            AssertOnTimeout("Timed out waiting for client-side to notify it is ready!");

            Assert.True(m_AuthoritativeTransform.CanCommitToTransform);
            Assert.False(m_NonAuthoritativeTransform.CanCommitToTransform);


            yield return base.OnServerAndClientsConnected();
        }

        public enum TransformSpace
        {
            World,
            Local
        }

        /// <summary>
        /// Tests that the authoritative transform's changes are applied
        /// to non-authoritative transforms.
        /// </summary>
        [UnityTest]
        public IEnumerator TestAuthoritativeTransformChangeOneAtATime([Values] TransformSpace testLocalTransform, [Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;

            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;

            // test position
            var authPlayerTransform = m_AuthoritativeTransform.transform;

            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "server side pos should be zero at first"); // sanity check

            authPlayerTransform.position = new Vector3(10, 20, 30);

            yield return WaitForConditionOrTimeOut(PositionsMatch);
            AssertOnTimeout($"Timed out waiting for positions to match");

            // test rotation
            authPlayerTransform.rotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            Assert.AreEqual(Quaternion.identity, m_NonAuthoritativeTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            yield return WaitForConditionOrTimeOut(RotationsMatch);
            AssertOnTimeout($"Timed out waiting for rotations to match");

            authPlayerTransform.localScale = new Vector3(2, 3, 4);

            yield return WaitForConditionOrTimeOut(ScaleValuesMatch);
            AssertOnTimeout($"Timed out waiting for scale values to match");
        }

        /// <summary>
        /// Test to verify nonAuthority cannot change the transform
        /// </summary>
        /// <param name="testClientAuthority"></param>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator VerifyNonAuthorityCantChangeTransform([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "other side pos should be zero at first"); // sanity check

            m_NonAuthoritativeTransform.transform.position = new Vector3(4, 5, 6);

            yield return s_DefaultWaitForTick;

            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "[Position] NonAuthority was able to change the position!");

            var nonAuthorityRotation = m_NonAuthoritativeTransform.transform.rotation;
            var originalNonAuthorityEulerRotation = nonAuthorityRotation.eulerAngles;
            var nonAuthorityEulerRotation = originalNonAuthorityEulerRotation;
            // Verify rotation is not marked dirty when rotated by half of the threshold
            nonAuthorityEulerRotation.y += 20.0f;
            nonAuthorityRotation.eulerAngles = nonAuthorityEulerRotation;
            m_NonAuthoritativeTransform.transform.rotation = nonAuthorityRotation;
            yield return s_DefaultWaitForTick;
            var nonAuthorityCurrentEuler = m_NonAuthoritativeTransform.transform.rotation.eulerAngles;
            Assert.True(originalNonAuthorityEulerRotation.Equals(nonAuthorityCurrentEuler), "[Rotation] NonAuthority was able to change the rotation!");

            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            m_NonAuthoritativeTransform.transform.localScale = nonAuthorityScale * 100;

            yield return s_DefaultWaitForTick;

            Assert.True(nonAuthorityScale.Equals(m_NonAuthoritativeTransform.transform.localScale), "[Scale] NonAuthority was able to change the scale!");
        }


        /// <summary>
        /// Validates that rotation checks don't produce false positive
        /// results when rolling over between 0 and 360 degrees
        /// </summary>
        [UnityTest]
        public IEnumerator TestRotationThresholdDeltaCheck([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;

            m_NonAuthoritativeTransform.RotAngleThreshold = m_AuthoritativeTransform.RotAngleThreshold = 5.0f;

            var halfThreshold = m_AuthoritativeTransform.RotAngleThreshold * 0.5001f;
            var authorityRotation = m_AuthoritativeTransform.transform.rotation;
            var authorityEulerRotation = authorityRotation.eulerAngles;

            // Verify rotation is not marked dirty when rotated by half of the threshold
            authorityEulerRotation.y += halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            var results = m_AuthoritativeTransform.ApplyState();
            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {m_AuthoritativeTransform.RotAngleThreshold} degrees and only adjusted by {halfThreshold} degrees!");
            yield return s_DefaultWaitForTick;

            // Verify rotation is marked dirty when rotated by another half threshold value
            authorityEulerRotation.y += halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();
            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by the threshold value: {m_AuthoritativeTransform.RotAngleThreshold} degrees!");
            yield return s_DefaultWaitForTick;

            //Reset rotation back to zero on all axis
            authorityRotation.eulerAngles = authorityEulerRotation = Vector3.zero;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            yield return s_DefaultWaitForTick;

            // Rotate by 360 minus halfThreshold (which is really just negative halfThreshold) and verify rotation is not marked dirty
            authorityEulerRotation.y = 360 - halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();

            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {m_AuthoritativeTransform.RotAngleThreshold} degrees and only adjusted by " +
                $"{Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");

            authorityEulerRotation.y -= halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();

            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by {Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");

            //Reset rotation back to zero on all axis
            authorityRotation.eulerAngles = authorityEulerRotation = Vector3.zero;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            yield return s_DefaultWaitForTick;

            authorityEulerRotation.y -= halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();
            Assert.IsFalse(results.isRotationDirty, $"Rotation is dirty when rotation threshold is {m_AuthoritativeTransform.RotAngleThreshold} degrees and only adjusted by " +
                $"{Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");

            authorityEulerRotation.y -= halfThreshold;
            authorityRotation.eulerAngles = authorityEulerRotation;
            m_AuthoritativeTransform.transform.rotation = authorityRotation;
            results = m_AuthoritativeTransform.ApplyState();

            Assert.IsTrue(results.isRotationDirty, $"Rotation was not dirty when rotated by {Mathf.DeltaAngle(0, authorityEulerRotation.y)} degrees!");
        }

        private bool ValidateBitSetValues(NetworkTransform.NetworkTransformState serverState, NetworkTransform.NetworkTransformState clientState)
        {
            if (serverState.HasPositionX == clientState.HasPositionX && serverState.HasPositionY == clientState.HasPositionY && serverState.HasPositionZ == clientState.HasPositionZ &&
                serverState.HasRotAngleX == clientState.HasRotAngleX && serverState.HasRotAngleY == clientState.HasRotAngleY && serverState.HasRotAngleZ == clientState.HasRotAngleZ &&
                serverState.HasScaleX == clientState.HasScaleX && serverState.HasScaleY == clientState.HasScaleY && serverState.HasScaleZ == clientState.HasScaleZ)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Test to make sure that the bitset value is updated properly
        /// </summary>
        [UnityTest]
        public IEnumerator TestBitsetValue([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.RotAngleThreshold = m_AuthoritativeTransform.RotAngleThreshold = 0.1f;
            yield return s_DefaultWaitForTick;

            m_AuthoritativeTransform.transform.rotation = Quaternion.Euler(1, 2, 3);
            var serverLastSentState = m_AuthoritativeTransform.GetLastSentState();
            var clientReplicatedState = m_NonAuthoritativeTransform.ReplicatedNetworkState.Value;
            yield return WaitForConditionOrTimeOut(() => ValidateBitSetValues(serverLastSentState, clientReplicatedState));
            AssertOnTimeout($"Timed out waiting for Authoritative Bitset state to equal NonAuthoritative replicated Bitset state!");

            yield return WaitForConditionOrTimeOut(RotationsMatch);
            AssertOnTimeout($"[Timed-Out] Authoritative rotation {m_AuthoritativeTransform.transform.rotation.eulerAngles} != Non-Authoritative rotation {m_NonAuthoritativeTransform.transform.rotation.eulerAngles}");
        }

        private float m_DetectedPotentialInterpolatedTeleport;

        /// <summary>
        /// Test Teleporting
        /// </summary>
        [UnityTest]
        public IEnumerator TeleportTest([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            var authTransform = m_AuthoritativeTransform.transform;
            var nonAuthPosition = m_NonAuthoritativeTransform.transform.position;
            var currentTick = m_AuthoritativeTransform.NetworkManager.ServerTime.Tick;
            m_DetectedPotentialInterpolatedTeleport = 0.0f;
            var teleportDestination = new Vector3(100.00f, 100.00f, 100.00f);
            var targetDistance = Mathf.Abs(Vector3.Distance(nonAuthPosition, teleportDestination));
            m_AuthoritativeTransform.Teleport(new Vector3(100.00f, 100.00f, 100.00f), authTransform.rotation, authTransform.localScale);
            yield return WaitForConditionOrTimeOut(() => TeleportPositionMatches(nonAuthPosition));

            AssertOnTimeout($"[Timed-Out][Teleport] Timed out waiting for NonAuthoritative position to !");
            Assert.IsTrue(m_DetectedPotentialInterpolatedTeleport == 0.0f, $"Detected possible interpolation on non-authority side! NonAuthority distance: {m_DetectedPotentialInterpolatedTeleport} | Target distance: {targetDistance}");
        }

        private bool Aproximately(float x, float y)
        {
            return Mathf.Abs(x - y) <= k_AproximateDeltaVariance;
        }

        private const float k_AproximateDeltaVariance = 0.01f;

        private bool TeleportPositionMatches(Vector3 nonAuthorityOriginalPosition)
        {
            var nonAuthorityPosition = m_NonAuthoritativeTransform.transform.position;
            var authorityPosition = m_AuthoritativeTransform.transform.position;
            var targetDistance = Mathf.Abs(Vector3.Distance(nonAuthorityOriginalPosition, authorityPosition));
            var nonAuthorityCurrentDistance = Mathf.Abs(Vector3.Distance(nonAuthorityPosition, nonAuthorityOriginalPosition));
            if (!Aproximately(targetDistance, nonAuthorityCurrentDistance))
            {
                if (nonAuthorityCurrentDistance >= 0.15f * targetDistance && nonAuthorityCurrentDistance <= 0.75f * targetDistance)
                {
                    m_DetectedPotentialInterpolatedTeleport = nonAuthorityCurrentDistance;
                }
                return false;
            }
            var xIsEqual = Aproximately(authorityPosition.x, nonAuthorityPosition.x);
            var yIsEqual = Aproximately(authorityPosition.y, nonAuthorityPosition.y);
            var zIsEqual = Aproximately(authorityPosition.z, nonAuthorityPosition.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Authority position {authorityPosition} != NonAuthority position {nonAuthorityPosition}");
            }
            return xIsEqual && yIsEqual && zIsEqual; ;
        }

        private bool RotationsMatch()
        {
            var authorityEulerRotation = m_AuthoritativeTransform.transform.rotation.eulerAngles;
            var nonAuthorityEulerRotation = m_NonAuthoritativeTransform.transform.rotation.eulerAngles;
            var xIsEqual = Aproximately(authorityEulerRotation.x, nonAuthorityEulerRotation.x);
            var yIsEqual = Aproximately(authorityEulerRotation.y, nonAuthorityEulerRotation.y);
            var zIsEqual = Aproximately(authorityEulerRotation.z, nonAuthorityEulerRotation.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Authority rotation {authorityEulerRotation} != NonAuthority rotation {nonAuthorityEulerRotation}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool PositionsMatch()
        {
            var authorityPosition = m_AuthoritativeTransform.transform.position;
            var nonAuthorityPosition = m_NonAuthoritativeTransform.transform.position;
            var xIsEqual = Aproximately(authorityPosition.x, nonAuthorityPosition.x);
            var yIsEqual = Aproximately(authorityPosition.y, nonAuthorityPosition.y);
            var zIsEqual = Aproximately(authorityPosition.z, nonAuthorityPosition.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Authority position {authorityPosition} != NonAuthority position {nonAuthorityPosition}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool ScaleValuesMatch()
        {
            var authorityScale = m_AuthoritativeTransform.transform.localScale;
            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            var xIsEqual = Aproximately(authorityScale.x, nonAuthorityScale.x);
            var yIsEqual = Aproximately(authorityScale.y, nonAuthorityScale.y);
            var zIsEqual = Aproximately(authorityScale.z, nonAuthorityScale.z);
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"Authority scale {authorityScale} != NonAuthority scale {nonAuthorityScale}");
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
            Object.DestroyImmediate(m_PlayerPrefab);
            yield return base.OnTearDown();
        }
    }
}
