using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Helper component for all NetworkTransformTests
    /// </summary>
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

        public void CommitToTransform()
        {
            TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
        }

        public (bool isDirty, bool isPositionDirty, bool isRotationDirty, bool isScaleDirty) ApplyState()
        {
            var transformState = ApplyLocalNetworkState(transform);
            return (transformState.IsDirty, transformState.HasPositionChange, transformState.HasRotAngleChange, transformState.HasScaleChange);
        }
    }

    /// <summary>
    /// Helper component for NetworkTransform parenting tests
    /// </summary>
    public class ChildObjectComponent : NetworkBehaviour
    {
        public readonly static List<ChildObjectComponent> Instances = new List<ChildObjectComponent>();
        public static ChildObjectComponent ServerInstance { get; internal set; }
        public readonly static Dictionary<ulong, NetworkObject> ClientInstances = new Dictionary<ulong, NetworkObject>();

        public static void Reset()
        {
            ServerInstance = null;
            ClientInstances.Clear();
            Instances.Clear();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ServerInstance = this;
            }
            else
            {
                ClientInstances.Add(NetworkManager.LocalClientId, NetworkObject);
            }
            Instances.Add(this);
            base.OnNetworkSpawn();
        }
    }

    /// <summary>
    /// Integration tests for NetworkTransform that will test both
    /// server and host operating modes and will test both authoritative
    /// models for each operating mode.
    /// </summary>
    [TestFixture(HostOrServer.Host, Authority.ServerAuthority)]
    [TestFixture(HostOrServer.Host, Authority.OwnerAuthority)]
    [TestFixture(HostOrServer.Server, Authority.ServerAuthority)]
    [TestFixture(HostOrServer.Server, Authority.OwnerAuthority)]

    public class NetworkTransformTests : NetcodeIntegrationTest
    {
        private NetworkObject m_AuthoritativePlayer;
        private NetworkObject m_NonAuthoritativePlayer;
        private NetworkObject m_ChildObjectToBeParented;

        private NetworkTransformTestComponent m_AuthoritativeTransform;
        private NetworkTransformTestComponent m_NonAuthoritativeTransform;
        private NetworkTransformTestComponent m_OwnerTransform;

        private readonly Authority m_Authority;

        public enum Authority
        {
            ServerAuthority,
            OwnerAuthority
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

        protected override IEnumerator OnSetup()
        {
            ChildObjectComponent.Reset();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransformTestComponent = m_PlayerPrefab.AddComponent<NetworkTransformTestComponent>();
            networkTransformTestComponent.ServerAuthority = m_Authority == Authority.ServerAuthority;
        }

        protected override void OnServerAndClientsCreated()
        {
            var childObject = CreateNetworkObjectPrefab("ChildObject");
            childObject.AddComponent<ChildObjectComponent>();
            var childNetworkTransform = childObject.AddComponent<NetworkTransform>();
            childNetworkTransform.InLocalSpace = true;
            m_ChildObjectToBeParented = childObject.GetComponent<NetworkObject>();

            // Now apply local transform values
            m_ChildObjectToBeParented.transform.position = m_ChildObjectLocalPosition;
            var childRotation = m_ChildObjectToBeParented.transform.rotation;
            childRotation.eulerAngles = m_ChildObjectLocalRotation;
            m_ChildObjectToBeParented.transform.rotation = childRotation;
            m_ChildObjectToBeParented.transform.localScale = m_ChildObjectLocalScale;
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

            m_AuthoritativePlayer = m_Authority == Authority.ServerAuthority ? serverSideClientPlayer : clientSideClientPlayer;
            m_NonAuthoritativePlayer = m_Authority == Authority.ServerAuthority ? clientSideClientPlayer : serverSideClientPlayer;

            // Get the NetworkTransformTestComponent to make sure the client side is ready before starting test
            m_AuthoritativeTransform = m_AuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();
            m_NonAuthoritativeTransform = m_NonAuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();

            m_OwnerTransform = m_AuthoritativeTransform.IsOwner ? m_AuthoritativeTransform : m_NonAuthoritativeTransform;

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

        public enum OverrideState
        {
            Update,
            CommitToTransform,
            SetState
        }

        /// <summary>
        /// Returns true when the server-host and all clients have
        /// instantiated the child object to be used in <see cref="NetworkTransformParentingLocalSpaceOffsetTests"/>
        /// </summary>
        /// <returns></returns>
        private bool AllChildObjectInstancesAreSpawned()
        {
            if (ChildObjectComponent.ServerInstance == null)
            {
                return false;
            }

            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                if (!ChildObjectComponent.ClientInstances.ContainsKey(clientNetworkManager.LocalClientId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AllChildObjectInstancesHaveChild()
        {
            foreach (var instance in ChildObjectComponent.ClientInstances.Values)
            {
                if (instance.transform.parent == null)
                {
                    return false;
                }
            }
            return true;
        }

        // To test that local position, rotation, and scale remain the same when parented.
        private Vector3 m_ChildObjectLocalPosition = new Vector3(5.0f, 0.0f, -5.0f);
        private Vector3 m_ChildObjectLocalRotation = new Vector3(-35.0f, 90.0f, 270.0f);
        private Vector3 m_ChildObjectLocalScale = new Vector3(0.1f, 0.5f, 0.4f);

        /// <summary>
        /// A wait condition specific method that assures the local space coordinates
        /// are not impacted by NetworkTransform when parented.
        /// </summary>
        private bool AllInstancesKeptLocalTransformValues()
        {
            foreach (var childInstance in ChildObjectComponent.Instances)
            {
                var childLocalPosition = childInstance.transform.localPosition;
                var childLocalRotation = childInstance.transform.localRotation.eulerAngles;
                var childLocalScale = childInstance.transform.localScale;

                if (!Aproximately(childLocalPosition, m_ChildObjectLocalPosition))
                {
                    return false;
                }
                if (!AproximatelyEuler(childLocalRotation, m_ChildObjectLocalRotation))
                {
                    return false;
                }
                if (!Aproximately(childLocalScale, m_ChildObjectLocalScale))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Handles validating the local space values match the original local space values.
        /// If not, it generates a message containing the axial values that did not match
        /// the target/start local space values.
        /// </summary>
        private IEnumerator WaitForAllChildrenLocalTransformValuesToMatch()
        {
            yield return WaitForConditionOrTimeOut(AllInstancesKeptLocalTransformValues);
            var infoMessage = string.Empty;
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                foreach (var childInstance in ChildObjectComponent.Instances)
                {
                    var childLocalPosition = childInstance.transform.localPosition;
                    var childLocalRotation = childInstance.transform.localRotation.eulerAngles;
                    var childLocalScale = childInstance.transform.localScale;

                    if (!Aproximately(childLocalPosition, m_ChildObjectLocalPosition))
                    {
                        infoMessage += $"[{childInstance.name}] Child's Local Position ({childLocalPosition}) | Original Local Position ({m_ChildObjectLocalPosition})\n";
                    }
                    if (!AproximatelyEuler(childLocalRotation, m_ChildObjectLocalRotation))
                    {
                        infoMessage += $"[{childInstance.name}] Child's Local Rotation ({childLocalRotation}) | Original Local Rotation ({m_ChildObjectLocalRotation})\n";
                    }
                    if (!Aproximately(childLocalScale, m_ChildObjectLocalScale))
                    {
                        infoMessage += $"[{childInstance.name}] Child's Local Scale ({childLocalScale}) | Original Local Rotation ({m_ChildObjectLocalScale})\n";
                    }
                }
                AssertOnTimeout($"Timed out waiting for all children to have the correct local space values:\n {infoMessage}");
            }
            yield return null;
        }

        /// <summary>
        /// Validates that local space transform values remain the same when a NetworkTransform is
        /// parented under another NetworkTransform
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkTransformParentedLocalSpaceTest([Values] Interpolation interpolation)
        {
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            var authoritativeChildObject = SpawnObject(m_ChildObjectToBeParented.gameObject, m_AuthoritativeTransform.NetworkManager);

            // Assure all of the child object instances are spawned
            yield return WaitForConditionOrTimeOut(AllChildObjectInstancesAreSpawned);
            AssertOnTimeout("Timed out waiting for all child instances to be spawned!");
            // Just a sanity check as it should have timed out before this check
            Assert.IsNotNull(ChildObjectComponent.ServerInstance, $"The server-side {nameof(ChildObjectComponent)} instance is null!");

            // This determines which parent on the server side should be the parent
            if (m_AuthoritativeTransform.IsServerAuthoritative())
            {
                Assert.True(ChildObjectComponent.ServerInstance.NetworkObject.TrySetParent(m_AuthoritativeTransform.transform, false), "[Authoritative] Failed to parent the child object!");
            }
            else
            {
                Assert.True(ChildObjectComponent.ServerInstance.NetworkObject.TrySetParent(m_NonAuthoritativeTransform.transform, false), "[Non-Authoritative] Failed to parent the child object!");
            }

            // This waits for all child instances to be parented
            yield return WaitForConditionOrTimeOut(AllChildObjectInstancesHaveChild);
            AssertOnTimeout("Timed out waiting for all instances to have parented a child!");

            // This validates each child instance has preserved their local space values
            yield return WaitForAllChildrenLocalTransformValuesToMatch();
        }

        /// <summary>
        /// Validates that moving, rotating, and scaling the authority side with a single
        /// tick will properly synchronize the non-authoritative side with the same values.
        /// </summary>
        private IEnumerator MoveRotateAndScaleAuthority(Vector3 position, Vector3 rotation, Vector3 scale, OverrideState overrideState)
        {
            switch (overrideState)
            {
                case OverrideState.SetState:
                    {
                        m_AuthoritativeTransform.SetState(position, Quaternion.Euler(rotation), scale);
                        break;
                    }
                case OverrideState.Update:
                default:
                    {
                        m_AuthoritativeTransform.transform.position = position;
                        yield return null;
                        var authoritativeRotation = m_AuthoritativeTransform.transform.rotation;
                        authoritativeRotation.eulerAngles = rotation;
                        m_AuthoritativeTransform.transform.rotation = authoritativeRotation;
                        yield return null;
                        m_AuthoritativeTransform.transform.localScale = scale;
                        break;
                    }
            }
        }

        /// <summary>
        /// Validates we don't extrapolate beyond the target value
        /// </summary>
        /// <remarks>
        /// This will first wait for any authoritative changes to have been synchronized
        /// with the non-authoritative side.  It will then wait for the specified number
        /// of tick periods to assure the values don't change
        /// </remarks>
        private IEnumerator WaitForPositionRotationAndScaleToMatch(int ticksToWait)
        {
            // Validate we interpolate to the appropriate position and rotation
            yield return WaitForConditionOrTimeOut(PositionRotationScaleMatches);
            AssertOnTimeout("Timed out waiting for non-authority to match authority's position or rotation");

            // Wait for the specified number of ticks
            for (int i = 0; i < ticksToWait; i++)
            {
                yield return s_DefaultWaitForTick;
            }

            // Verify both sides match (i.e. no drifting or over-extrapolating)
            Assert.IsTrue(PositionsMatch(), $"Non-authority position did not match after waiting for {ticksToWait} ticks! " +
                $"Authority ({m_AuthoritativeTransform.transform.position}) Non-Authority ({m_NonAuthoritativeTransform.transform.position})");
            Assert.IsTrue(RotationsMatch(), $"Non-authority rotation did not match after waiting for {ticksToWait} ticks! " +
                $"Authority ({m_AuthoritativeTransform.transform.rotation.eulerAngles}) Non-Authority ({m_NonAuthoritativeTransform.transform.rotation.eulerAngles})");
        }

        /// <summary>
        /// Waits until the next tick
        /// </summary>
        private IEnumerator WaitForNextTick()
        {
            var currentTick = m_AuthoritativeTransform.NetworkManager.LocalTime.Tick;
            while (m_AuthoritativeTransform.NetworkManager.LocalTime.Tick == currentTick)
            {
                yield return null;
            }
        }

        // The number of iterations to change position, rotation, and scale for NetworkTransformMultipleChangesOverTime
        private const int k_PositionRotationScaleIterations = 8;

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.NetworkPrefabs = m_ServerNetworkManager.NetworkConfig.NetworkPrefabs;
            base.OnNewClientCreated(networkManager);
        }

        /// <summary>
        /// This validates that multiple changes can occur within the same tick or over
        /// several ticks while still keeping non-authoritative instances synchronized.
        /// </summary>
        [UnityTest]
        public IEnumerator NetworkTransformMultipleChangesOverTime([Values] TransformSpace testLocalTransform, [Values] OverrideState overideState)
        {
            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;

            var positionStart = new Vector3(1.0f, 0.5f, 2.0f);
            var rotationStart = new Vector3(0.0f, 45.0f, 0.0f);
            var scaleStart = new Vector3(1.0f, 1.0f, 1.0f);
            var position = positionStart;
            var rotation = rotationStart;
            var scale = scaleStart;

            // Move and rotate within the same tick, validate the non-authoritative instance updates
            // to each set of changes.  Repeat several times.
            for (int i = 1; i < k_PositionRotationScaleIterations + 1; i++)
            {
                position = positionStart * i;
                rotation = rotationStart * i;
                scale = scaleStart * i;
                // Wait for tick to change so we cam start close to the beginning the next tick in order
                // to apply both deltas within the same tick period.
                yield return WaitForNextTick();

                // Apply deltas
                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);

                // Wait for deltas to synchronize on non-authoritative side
                yield return WaitForPositionRotationAndScaleToMatch(4);
            }

            // Check scale for all player instances when a client late joins
            // NOTE: This validates the use of the spawned object's transform values as opposed to the replicated state (which now is only the last deltas)
            yield return CreateAndStartNewClient();
            var newClientNetworkManager = m_ClientNetworkManagers[NumberOfClients];
            foreach (var playerRelativeEntry in m_PlayerNetworkObjects)
            {
                foreach (var playerInstanceEntry in playerRelativeEntry.Value)
                {
                    var playerInstance = playerInstanceEntry.Value;
                    if (newClientNetworkManager.LocalClientId == playerInstance.OwnerClientId)
                    {
                        Assert.IsTrue(Aproximately(m_PlayerPrefab.transform.localScale, playerInstance.transform.localScale), $"{playerInstance.name}'s cloned instance's scale does not match original scale!\n" +
                            $"[ClientId-{playerRelativeEntry.Key} Relative] Player-{playerInstance.OwnerClientId}'s LocalScale ({playerInstance.transform.localScale}) vs Target Scale ({m_PlayerPrefab.transform.localScale})");
                    }
                }
            }

            // Repeat this in the opposite direction
            for (int i = -1; i > -1 * (k_PositionRotationScaleIterations + 1); i--)
            {
                position = positionStart * i;
                rotation = rotationStart * i;
                scale = scaleStart * i;
                // Wait for tick to change so we cam start close to the beginning the next tick in order
                // to apply both deltas within the same tick period.
                yield return WaitForNextTick();

                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);
                yield return WaitForPositionRotationAndScaleToMatch(4);
            }

            // Wait for tick to change so we cam start close to the beginning the next tick in order
            // to apply as many deltas within the same tick period as we can (if not all)
            yield return WaitForNextTick();

            // Move and rotate within the same tick several times, then validate the non-authoritative
            // instance updates to the authoritative instance's final position and rotation.
            for (int i = 1; i < k_PositionRotationScaleIterations + 1; i++)
            {
                position = positionStart * i;
                rotation = rotationStart * i;
                scale = scaleStart * i;

                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);
            }

            yield return WaitForPositionRotationAndScaleToMatch(1);

            // Wait for tick to change so we cam start close to the beginning the next tick in order
            // to apply as many deltas within the same tick period as we can (if not all)
            yield return WaitForNextTick();

            // Repeat this in the opposite direction and rotation
            for (int i = -1; i > -1 * (k_PositionRotationScaleIterations + 1); i--)
            {
                position = positionStart * i;
                rotation = rotationStart * i;
                scale = scaleStart * i;
                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);
            }
            yield return WaitForPositionRotationAndScaleToMatch(1);
        }

        /// <summary>
        /// Tests changing all axial values one at a time.
        /// These tests are performed:
        /// - While in local space and world space
        /// - While interpolation is enabled and disabled
        /// - Using the TryCommitTransformToServer "override" that can be used
        /// from a child derived or external class.
        /// </summary>
        [UnityTest]
        public IEnumerator TestAuthoritativeTransformChangeOneAtATime([Values] TransformSpace testLocalTransform, [Values] Interpolation interpolation, [Values] OverrideState overideState)
        {
            var overrideUpdate = overideState == OverrideState.CommitToTransform;
            m_AuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;

            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;

            // test position
            var authPlayerTransform = overrideUpdate ? m_OwnerTransform.transform : m_AuthoritativeTransform.transform;

            Assert.AreEqual(Vector3.zero, m_NonAuthoritativeTransform.transform.position, "server side pos should be zero at first"); // sanity check

            var nextPosition = new Vector3(10, 20, 30);
            if (overideState != OverrideState.SetState)
            {
                authPlayerTransform.position = nextPosition;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(nextPosition, null, null, m_AuthoritativeTransform.Interpolate);
            }

            yield return WaitForConditionOrTimeOut(PositionsMatch);
            AssertOnTimeout($"Timed out waiting for positions to match");

            // test rotation
            Assert.AreEqual(Quaternion.identity, m_NonAuthoritativeTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            var nextRotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            if (overideState != OverrideState.SetState)
            {
                authPlayerTransform.rotation = nextRotation;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(null, nextRotation, null, m_AuthoritativeTransform.Interpolate);
            }

            yield return WaitForConditionOrTimeOut(RotationsMatch);
            AssertOnTimeout($"Timed out waiting for rotations to match");

            var nextScale = new Vector3(2, 3, 4);
            if (overrideUpdate)
            {
                authPlayerTransform.localScale = nextScale;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(null, null, nextScale, m_AuthoritativeTransform.Interpolate);
            }

            yield return WaitForConditionOrTimeOut(ScaleValuesMatch);
            AssertOnTimeout($"Timed out waiting for scale values to match");
        }

        /// <summary>
        /// Test to verify nonAuthority cannot change the transform directly
        /// </summary>
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
        /// The tests teleporting with and without interpolation
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


        /// <summary>
        /// This test validates the <see cref="NetworkTransform.SetState(Vector3?, Quaternion?, Vector3?, bool)"/> method
        /// usage for the non-authoritative side.  It will either be the owner or the server making/requesting state changes.
        /// This validates that:
        /// - The owner authoritative mode can still be controlled by the server (i.e. owner authoritative with server authority override capabilities)
        /// - The server authoritative mode can still be directed by the client owner.
        /// </summary>
        /// <remarks>
        /// This also tests that the original server authoritative model with client-owner driven NetworkTransforms is preserved.
        /// </remarks>
        [UnityTest]
        public IEnumerator NonAuthorityOwnerSettingStateTest([Values] Interpolation interpolation)
        {
            var interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritativeTransform.Interpolate = interpolate;
            m_NonAuthoritativeTransform.Interpolate = interpolate;
            m_NonAuthoritativeTransform.RotAngleThreshold = m_AuthoritativeTransform.RotAngleThreshold = 0.1f;

            // Test one parameter at a time first
            var newPosition = new Vector3(125f, 35f, 65f);
            var newRotation = Quaternion.Euler(1, 2, 3);
            var newScale = new Vector3(2.0f, 2.0f, 2.0f);
            m_NonAuthoritativeTransform.SetState(newPosition, null, null, interpolate);
            yield return WaitForConditionOrTimeOut(() => PositionsMatchesValue(newPosition));
            AssertOnTimeout($"Timed out waiting for non-authoritative position state request to be applied!");
            Assert.True(Aproximately(newPosition, m_AuthoritativeTransform.transform.position), "Authoritative position does not match!");
            Assert.True(Aproximately(newPosition, m_NonAuthoritativeTransform.transform.position), "Non-Authoritative position does not match!");

            m_NonAuthoritativeTransform.SetState(null, newRotation, null, interpolate);
            yield return WaitForConditionOrTimeOut(() => RotationMatchesValue(newRotation.eulerAngles));
            AssertOnTimeout($"Timed out waiting for non-authoritative rotation state request to be applied!");
            Assert.True(Aproximately(newRotation.eulerAngles, m_AuthoritativeTransform.transform.rotation.eulerAngles), "Authoritative rotation does not match!");
            Assert.True(Aproximately(newRotation.eulerAngles, m_NonAuthoritativeTransform.transform.rotation.eulerAngles), "Non-Authoritative rotation does not match!");

            m_NonAuthoritativeTransform.SetState(null, null, newScale, interpolate);
            yield return WaitForConditionOrTimeOut(() => ScaleMatchesValue(newScale));
            AssertOnTimeout($"Timed out waiting for non-authoritative scale state request to be applied!");
            Assert.True(Aproximately(newScale, m_AuthoritativeTransform.transform.localScale), "Authoritative scale does not match!");
            Assert.True(Aproximately(newScale, m_NonAuthoritativeTransform.transform.localScale), "Non-Authoritative scale does not match!");

            // Test all parameters at once
            newPosition = new Vector3(55f, 95f, -25f);
            newRotation = Quaternion.Euler(20, 5, 322);
            newScale = new Vector3(0.5f, 0.5f, 0.5f);

            m_NonAuthoritativeTransform.SetState(newPosition, newRotation, newScale, interpolate);
            yield return WaitForConditionOrTimeOut(() => PositionRotationScaleMatches(newPosition, newRotation.eulerAngles, newScale));
            AssertOnTimeout($"Timed out waiting for non-authoritative position, rotation, and scale state request to be applied!");
            Assert.True(Aproximately(newPosition, m_AuthoritativeTransform.transform.position), "Authoritative position does not match!");
            Assert.True(Aproximately(newPosition, m_NonAuthoritativeTransform.transform.position), "Non-Authoritative position does not match!");
            Assert.True(Aproximately(newRotation.eulerAngles, m_AuthoritativeTransform.transform.rotation.eulerAngles), "Authoritative rotation does not match!");
            Assert.True(Aproximately(newRotation.eulerAngles, m_NonAuthoritativeTransform.transform.rotation.eulerAngles), "Non-Authoritative rotation does not match!");
            Assert.True(Aproximately(newScale, m_AuthoritativeTransform.transform.localScale), "Authoritative scale does not match!");
            Assert.True(Aproximately(newScale, m_NonAuthoritativeTransform.transform.localScale), "Non-Authoritative scale does not match!");
        }

        private bool Aproximately(float x, float y)
        {
            return Mathf.Abs(x - y) <= k_AproximateDeltaVariance;
        }

        private bool Aproximately(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) <= k_AproximateDeltaVariance &&
                Mathf.Abs(a.y - b.y) <= k_AproximateDeltaVariance &&
                Mathf.Abs(a.z - b.z) <= k_AproximateDeltaVariance;
        }

        private bool AproximatelyEuler(Vector3 a, Vector3 b)
        {
            return Mathf.DeltaAngle(a.x, b.x) <= k_AproximateDeltaVariance &&
                Mathf.DeltaAngle(a.y, b.y) <= k_AproximateDeltaVariance &&
                Mathf.DeltaAngle(a.z, b.z) <= k_AproximateDeltaVariance;
        }

        private const float k_AproximateDeltaVariance = 0.01f;
        private bool PositionsMatchesValue(Vector3 positionToMatch)
        {
            var authorityPosition = m_AuthoritativeTransform.transform.position;
            var nonAuthorityPosition = m_NonAuthoritativeTransform.transform.position;
            var auhtorityIsEqual = Aproximately(authorityPosition, positionToMatch);
            var nonauthorityIsEqual = Aproximately(nonAuthorityPosition, positionToMatch);

            if (!auhtorityIsEqual)
            {
                VerboseDebug($"Authority position {authorityPosition} != position to match: {positionToMatch}!");
            }
            if (!nonauthorityIsEqual)
            {
                VerboseDebug($"NonAuthority position {nonAuthorityPosition} != position to match: {positionToMatch}!");
            }
            return auhtorityIsEqual && nonauthorityIsEqual;
        }

        private bool RotationMatchesValue(Vector3 rotationEulerToMatch)
        {
            var authorityRotationEuler = m_AuthoritativeTransform.transform.rotation.eulerAngles;
            var nonAuthorityRotationEuler = m_NonAuthoritativeTransform.transform.rotation.eulerAngles;
            var auhtorityIsEqual = Aproximately(authorityRotationEuler, rotationEulerToMatch);
            var nonauthorityIsEqual = Aproximately(nonAuthorityRotationEuler, rotationEulerToMatch);

            if (!auhtorityIsEqual)
            {
                VerboseDebug($"Authority rotation {authorityRotationEuler} != rotation to match: {rotationEulerToMatch}!");
            }
            if (!nonauthorityIsEqual)
            {
                VerboseDebug($"NonAuthority position {nonAuthorityRotationEuler} != rotation to match: {rotationEulerToMatch}!");
            }
            return auhtorityIsEqual && nonauthorityIsEqual;
        }

        private bool ScaleMatchesValue(Vector3 scaleToMatch)
        {
            var authorityScale = m_AuthoritativeTransform.transform.localScale;
            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            var auhtorityIsEqual = Aproximately(authorityScale, scaleToMatch);
            var nonauthorityIsEqual = Aproximately(nonAuthorityScale, scaleToMatch);

            if (!auhtorityIsEqual)
            {
                VerboseDebug($"Authority scale {authorityScale} != scale to match: {scaleToMatch}!");
            }
            if (!nonauthorityIsEqual)
            {
                VerboseDebug($"NonAuthority scale {nonAuthorityScale} != scale to match: {scaleToMatch}!");
            }
            return auhtorityIsEqual && nonauthorityIsEqual;
        }


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

        private bool PositionRotationScaleMatches(Vector3 position, Vector3 eulerRotation, Vector3 scale)
        {
            return PositionsMatchesValue(position) && RotationMatchesValue(eulerRotation) && ScaleMatchesValue(scale);
        }

        private bool PositionRotationScaleMatches()
        {
            return RotationsMatch() && PositionsMatch() && ScaleValuesMatch();
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

        protected override IEnumerator OnTearDown()
        {
            m_EnableVerboseDebug = false;
            Object.DestroyImmediate(m_PlayerPrefab);
            yield return base.OnTearDown();
        }
    }
}
