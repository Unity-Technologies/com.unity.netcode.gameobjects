using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Integration tests for NetworkTransform that will test both
    /// server and host operating modes and will test both authoritative
    /// models for each operating mode.
    /// </summary>
    [TestFixture(HostOrServer.Host, Authority.ServerAuthority)]
    [TestFixture(HostOrServer.Host, Authority.OwnerAuthority)]

    public class NetworkTransformPacketLossTests : IntegrationTestWithApproximation
    {
        private NetworkObject m_AuthoritativePlayer;
        private NetworkObject m_NonAuthoritativePlayer;
        private NetworkObject m_ChildObject;
        private NetworkObject m_SubChildObject;
        private NetworkObject m_ParentObject;

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

        public enum Precision
        {
            Half,
            Full
        }

        public enum Rotation
        {
            Euler,
            Quaternion
        }

        public enum RotationCompression
        {
            None,
            QuaternionCompress
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

        public enum Axis
        {
            X,
            Y,
            Z,
            XY,
            XZ,
            YZ,
            XYZ
        }

        public enum NetworkConditions
        {
            PacketLoss,
            LatencyAndPacketLoss
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="testWithHost">Determines if we are running as a server or host</param>
        /// <param name="authority">Determines if we are using server or owner authority</param>
        public NetworkTransformPacketLossTests(HostOrServer testWithHost, Authority authority)
        {
            m_UseHost = testWithHost == HostOrServer.Host ? true : false;
            m_Authority = authority;
        }

        protected override int NumberOfClients => 1;
        protected override bool m_SetupIsACoroutine => false;
        protected override bool m_TearDownIsACoroutine => false;

        private const int k_TickRate = 60;
        private int m_OriginalTargetFrameRate;
        protected override void OnOneTimeSetup()
        {
            m_OriginalTargetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = 120;
            base.OnOneTimeSetup();
        }

        protected override void OnOneTimeTearDown()
        {
            Application.targetFrameRate = m_OriginalTargetFrameRate;
            base.OnOneTimeTearDown();
        }

        protected override void OnInlineSetup()
        {
            NetworkTransformTestComponent.AuthorityInstance = null;
            m_Precision = Precision.Full;
            ChildObjectComponent.Reset();
        }

        protected override void OnInlineTearDown()
        {
            m_EnableVerboseDebug = false;
            Object.DestroyImmediate(m_PlayerPrefab);
        }

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransformTestComponent = m_PlayerPrefab.AddComponent<NetworkTransformTestComponent>();
            networkTransformTestComponent.ServerAuthority = m_Authority == Authority.ServerAuthority;
        }

        protected override void OnServerAndClientsCreated()
        {
            var subChildObject = CreateNetworkObjectPrefab("SubChildObject");
            var subChildNetworkTransform = subChildObject.AddComponent<SubChildObjectComponent>();
            subChildNetworkTransform.ServerAuthority = m_Authority == Authority.ServerAuthority;
            m_SubChildObject = subChildObject.GetComponent<NetworkObject>();

            var childObject = CreateNetworkObjectPrefab("ChildObject");
            var childNetworkTransform = childObject.AddComponent<ChildObjectComponent>();
            childNetworkTransform.ServerAuthority = m_Authority == Authority.ServerAuthority;
            m_ChildObject = childObject.GetComponent<NetworkObject>();

            var parentObject = CreateNetworkObjectPrefab("ParentObject");
            var parentNetworkTransform = parentObject.AddComponent<NetworkTransformTestComponent>();
            parentNetworkTransform.ServerAuthority = m_Authority == Authority.ServerAuthority;
            m_ParentObject = parentObject.GetComponent<NetworkObject>();

            // Now apply local transform values
            m_ChildObject.transform.position = m_ChildObjectLocalPosition;
            var childRotation = m_ChildObject.transform.rotation;
            childRotation.eulerAngles = m_ChildObjectLocalRotation;
            m_ChildObject.transform.rotation = childRotation;
            m_ChildObject.transform.localScale = m_ChildObjectLocalScale;

            m_SubChildObject.transform.position = m_SubChildObjectLocalPosition;
            var subChildRotation = m_SubChildObject.transform.rotation;
            subChildRotation.eulerAngles = m_SubChildObjectLocalRotation;
            m_SubChildObject.transform.rotation = childRotation;
            m_SubChildObject.transform.localScale = m_SubChildObjectLocalScale;

            if (m_EnableVerboseDebug)
            {
                m_ServerNetworkManager.LogLevel = LogLevel.Developer;
                foreach (var clientNetworkManager in m_ClientNetworkManagers)
                {
                    clientNetworkManager.LogLevel = LogLevel.Developer;
                }
            }

            m_ServerNetworkManager.NetworkConfig.TickRate = k_TickRate;

            var unityTransport = m_ServerNetworkManager.NetworkConfig.NetworkTransport as Transports.UTP.UnityTransport;
            unityTransport.SetDebugSimulatorParameters(100, 0, 5);
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.TickRate = k_TickRate;
            }
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForClientsConnectedOrTimeOut();
            AssertOnTimeout("Timed out waiting for client-side to notify it is ready!");

            // Get the client player representation on both the server and the client side
            var serverSideClientPlayer = m_PlayerNetworkObjects[0][m_ClientNetworkManagers[0].LocalClientId];// m_ServerNetworkManager.ConnectedClients[m_ClientNetworkManagers[0].LocalClientId].PlayerObject;
            var clientSideClientPlayer = m_PlayerNetworkObjects[m_ClientNetworkManagers[0].LocalClientId][m_ClientNetworkManagers[0].LocalClientId];

            m_AuthoritativePlayer = m_Authority == Authority.ServerAuthority ? serverSideClientPlayer : clientSideClientPlayer;
            m_NonAuthoritativePlayer = m_Authority == Authority.ServerAuthority ? clientSideClientPlayer : serverSideClientPlayer;

            // Get the NetworkTransformTestComponent to make sure the client side is ready before starting test
            m_AuthoritativeTransform = m_AuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();
            m_NonAuthoritativeTransform = m_NonAuthoritativePlayer.GetComponent<NetworkTransformTestComponent>();

            m_OwnerTransform = m_AuthoritativeTransform.IsOwner ? m_AuthoritativeTransform : m_NonAuthoritativeTransform;

            Assert.True(m_AuthoritativeTransform.CanCommitToTransform);
            Assert.False(m_NonAuthoritativeTransform.CanCommitToTransform);

            yield return s_DefaultWaitForTick;
        }

        /// <summary>
        /// Returns true when the server-host and all clients have
        /// instantiated the child object to be used in <see cref="NetworkTransformParentingLocalSpaceOffsetTests"/>
        /// </summary>
        /// <returns></returns>
        private bool AllChildObjectInstancesAreSpawned()
        {
            if (ChildObjectComponent.AuthorityInstance == null)
            {
                return false;
            }

            if (ChildObjectComponent.HasSubChild && ChildObjectComponent.AuthoritySubInstance == null)
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
            if (ChildObjectComponent.HasSubChild)
            {
                foreach (var instance in ChildObjectComponent.ClientSubChildInstances.Values)
                {
                    if (instance.transform.parent == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // To test that local position, rotation, and scale remain the same when parented.
        private Vector3 m_ChildObjectLocalPosition = new Vector3(5.0f, 0.0f, -5.0f);
        private Vector3 m_ChildObjectLocalRotation = new Vector3(-35.0f, 90.0f, 270.0f);
        private Vector3 m_ChildObjectLocalScale = new Vector3(0.1f, 0.5f, 0.4f);
        private Vector3 m_SubChildObjectLocalPosition = new Vector3(2.0f, 1.0f, -1.0f);
        private Vector3 m_SubChildObjectLocalRotation = new Vector3(5.0f, 15.0f, 124.0f);
        private Vector3 m_SubChildObjectLocalScale = new Vector3(1.0f, 0.15f, 0.75f);


        /// <summary>
        /// A wait condition specific method that assures the local space coordinates
        /// are not impacted by NetworkTransform when parented.
        /// </summary>
        private bool AllInstancesKeptLocalTransformValues()
        {
            var authorityObjectLocalPosition = m_AuthorityChildObject.transform.localPosition;
            var authorityObjectLocalRotation = m_AuthorityChildObject.transform.localRotation.eulerAngles;
            var authorityObjectLocalScale = m_AuthorityChildObject.transform.localScale;

            foreach (var childInstance in ChildObjectComponent.Instances)
            {
                var childLocalPosition = childInstance.transform.localPosition;
                var childLocalRotation = childInstance.transform.localRotation.eulerAngles;
                var childLocalScale = childInstance.transform.localScale;
                // Adjust approximation based on precision
                if (m_Precision == Precision.Half)
                {
                    m_CurrentHalfPrecision = k_HalfPrecisionPosScale;
                }
                if (!Approximately(childLocalPosition, authorityObjectLocalPosition))
                {
                    return false;
                }
                if (!Approximately(childLocalScale, authorityObjectLocalScale))
                {
                    return false;
                }
                // Adjust approximation based on precision
                if (m_Precision == Precision.Half || m_RotationCompression == RotationCompression.QuaternionCompress)
                {
                    m_CurrentHalfPrecision = k_HalfPrecisionRot;
                }
                if (!ApproximatelyEuler(childLocalRotation, authorityObjectLocalRotation))
                {
                    return false;
                }
            }
            return true;
        }

        private enum ChildrenTransformCheckType
        {
            Connected_Clients,
            Late_Join_Client
        }

        /// <summary>
        /// Handles validating the local space values match the original local space values.
        /// If not, it generates a message containing the axial values that did not match
        /// the target/start local space values.
        /// </summary>
        private IEnumerator AllChildrenLocalTransformValuesMatch(bool useSubChild, ChildrenTransformCheckType checkType)
        {
            yield return WaitForConditionOrTimeOut(AllInstancesKeptLocalTransformValues);

            var infoMessage = new StringBuilder($"[{checkType}][{useSubChild}] Timed out waiting for all children to have the correct local space values:\n");
            var authorityObjectLocalPosition = useSubChild ? m_AuthoritySubChildObject.transform.localPosition : m_AuthorityChildObject.transform.localPosition;
            var authorityObjectLocalRotation = useSubChild ? m_AuthoritySubChildObject.transform.localRotation.eulerAngles : m_AuthorityChildObject.transform.localRotation.eulerAngles;
            var authorityObjectLocalScale = useSubChild ? m_AuthoritySubChildObject.transform.localScale : m_AuthorityChildObject.transform.localScale;
            var success = !s_GlobalTimeoutHelper.TimedOut;
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                // If we timed out, then wait for a full range of ticks (plus 1) to assure it sent synchronization data.
                for (int j = 0; j < m_ServerNetworkManager.NetworkConfig.TickRate; j++)
                {
                    var instances = useSubChild ? ChildObjectComponent.SubInstances : ChildObjectComponent.Instances;
                    foreach (var childInstance in ChildObjectComponent.Instances)
                    {
                        var childLocalPosition = childInstance.transform.localPosition;
                        var childLocalRotation = childInstance.transform.localRotation.eulerAngles;
                        var childLocalScale = childInstance.transform.localScale;
                        // Adjust approximation based on precision
                        if (m_Precision == Precision.Half || m_RotationCompression == RotationCompression.QuaternionCompress)
                        {
                            m_CurrentHalfPrecision = k_HalfPrecisionPosScale;
                        }
                        if (!Approximately(childLocalPosition, authorityObjectLocalPosition))
                        {
                            infoMessage.AppendLine($"[{childInstance.name}] Child's Local Position ({childLocalPosition}) | Authority Local Position ({authorityObjectLocalPosition})");
                            success = false;
                        }
                        if (!Approximately(childLocalScale, authorityObjectLocalScale))
                        {
                            infoMessage.AppendLine($"[{childInstance.name}] Child's Local Scale ({childLocalScale}) | Authority Local Scale ({authorityObjectLocalScale})");
                            success = false;
                        }

                        // Adjust approximation based on precision
                        if (m_Precision == Precision.Half || m_RotationCompression == RotationCompression.QuaternionCompress)
                        {
                            m_CurrentHalfPrecision = k_HalfPrecisionRot;
                        }
                        if (!ApproximatelyEuler(childLocalRotation, authorityObjectLocalRotation))
                        {
                            infoMessage.AppendLine($"[{childInstance.name}] Child's Local Rotation ({childLocalRotation}) | Authority Local Rotation ({authorityObjectLocalRotation})");
                            success = false;
                        }
                    }
                    yield return s_DefaultWaitForTick;
                }

                if (!success)
                {
                    Assert.True(success, infoMessage.ToString());
                }
            }
        }

        private NetworkObject m_AuthorityParentObject;
        private NetworkTransformTestComponent m_AuthorityParentNetworkTransform;
        private NetworkObject m_AuthorityChildObject;
        private NetworkObject m_AuthoritySubChildObject;
        private ChildObjectComponent m_AuthorityChildNetworkTransform;

        private ChildObjectComponent m_AuthoritySubChildNetworkTransform;

        /// <summary>
        /// Validates that transform values remain the same when a NetworkTransform is
        /// parented under another NetworkTransform under all of the possible axial conditions
        /// as well as when the parent has a varying scale.
        /// </summary>
        [UnityTest]
        public IEnumerator ParentedNetworkTransformTest([Values] Precision precision, [Values] Rotation rotation,
            [Values] RotationCompression rotationCompression, [Values] Interpolation interpolation, [Values] bool worldPositionStays,
            [Values(0.5f, 1.0f, 5.0f)] float scale)
        {
            // Set the precision being used for threshold adjustments
            m_Precision = precision;
            m_RotationCompression = rotationCompression;

            // Get the NetworkManager that will have authority in order to spawn with the correct authority
            var isServerAuthority = m_Authority == Authority.ServerAuthority;
            var authorityNetworkManager = m_ServerNetworkManager;
            if (!isServerAuthority)
            {
                authorityNetworkManager = m_ClientNetworkManagers[0];
            }

            // Spawn a parent and children
            ChildObjectComponent.HasSubChild = true;
            var serverSideParent = SpawnObject(m_ParentObject.gameObject, authorityNetworkManager).GetComponent<NetworkObject>();
            var serverSideChild = SpawnObject(m_ChildObject.gameObject, authorityNetworkManager).GetComponent<NetworkObject>();
            var serverSideSubChild = SpawnObject(m_SubChildObject.gameObject, authorityNetworkManager).GetComponent<NetworkObject>();

            // Assure all of the child object instances are spawned before proceeding to parenting
            yield return WaitForConditionOrTimeOut(AllChildObjectInstancesAreSpawned);
            AssertOnTimeout("Timed out waiting for all child instances to be spawned!");

            // Get the authority parent and child instances
            m_AuthorityParentObject = NetworkTransformTestComponent.AuthorityInstance.NetworkObject;
            m_AuthorityChildObject = ChildObjectComponent.AuthorityInstance.NetworkObject;
            m_AuthoritySubChildObject = ChildObjectComponent.AuthoritySubInstance.NetworkObject;

            // The child NetworkTransform will use world space when world position stays and
            // local space when world position does not stay when parenting.
            ChildObjectComponent.AuthorityInstance.InLocalSpace = !worldPositionStays;
            ChildObjectComponent.AuthorityInstance.UseHalfFloatPrecision = precision == Precision.Half;
            ChildObjectComponent.AuthorityInstance.UseQuaternionSynchronization = rotation == Rotation.Quaternion;
            ChildObjectComponent.AuthorityInstance.UseQuaternionCompression = rotationCompression == RotationCompression.QuaternionCompress;

            ChildObjectComponent.AuthoritySubInstance.InLocalSpace = !worldPositionStays;
            ChildObjectComponent.AuthoritySubInstance.UseHalfFloatPrecision = precision == Precision.Half;
            ChildObjectComponent.AuthoritySubInstance.UseQuaternionSynchronization = rotation == Rotation.Quaternion;
            ChildObjectComponent.AuthoritySubInstance.UseQuaternionCompression = rotationCompression == RotationCompression.QuaternionCompress;

            // Set whether we are interpolating or not
            m_AuthorityParentNetworkTransform = m_AuthorityParentObject.GetComponent<NetworkTransformTestComponent>();
            m_AuthorityParentNetworkTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthorityChildNetworkTransform = m_AuthorityChildObject.GetComponent<ChildObjectComponent>();
            m_AuthorityChildNetworkTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;
            m_AuthoritySubChildNetworkTransform = m_AuthoritySubChildObject.GetComponent<ChildObjectComponent>();
            m_AuthoritySubChildNetworkTransform.Interpolate = interpolation == Interpolation.EnableInterpolate;


            // Apply a scale to the parent object to make sure the scale on the child is properly updated on
            // non-authority instances.
            var halfScale = scale * 0.5f;
            m_AuthorityParentObject.transform.localScale = GetRandomVector3(scale - halfScale, scale + halfScale);
            m_AuthorityChildObject.transform.localScale = GetRandomVector3(scale - halfScale, scale + halfScale);
            m_AuthoritySubChildObject.transform.localScale = GetRandomVector3(scale - halfScale, scale + halfScale);

            // Allow one tick for authority to update these changes

            yield return WaitForConditionOrTimeOut(PositionRotationScaleMatches);

            AssertOnTimeout("All transform values did not match prior to parenting!");

            // Parent the child under the parent with the current world position stays setting
            Assert.True(serverSideChild.TrySetParent(serverSideParent.transform, worldPositionStays), "[Server-Side Child] Failed to set child's parent!");

            // Parent the sub-child under the child with the current world position stays setting
            Assert.True(serverSideSubChild.TrySetParent(serverSideChild.transform, worldPositionStays), "[Server-Side SubChild] Failed to set sub-child's parent!");

            // This waits for all child instances to be parented
            yield return WaitForConditionOrTimeOut(AllChildObjectInstancesHaveChild);
            AssertOnTimeout("Timed out waiting for all instances to have parented a child!");

            yield return s_DefaultWaitForTick;
            // This validates each child instance has preserved their local space values
            yield return AllChildrenLocalTransformValuesMatch(false, ChildrenTransformCheckType.Connected_Clients);

            // This validates each sub-child instance has preserved their local space values
            yield return AllChildrenLocalTransformValuesMatch(true, ChildrenTransformCheckType.Connected_Clients);

            // Verify that a late joining client will synchronize to the parented NetworkObjects properly
            yield return CreateAndStartNewClient();

            // Assure all of the child object instances are spawned (basically for the newly connected client)
            yield return WaitForConditionOrTimeOut(AllChildObjectInstancesAreSpawned);
            AssertOnTimeout("Timed out waiting for all child instances to be spawned!");

            // This waits for all child instances to be parented
            yield return WaitForConditionOrTimeOut(AllChildObjectInstancesHaveChild);
            AssertOnTimeout("Timed out waiting for all instances to have parented a child!");

            // This validates each child instance has preserved their local space values
            yield return AllChildrenLocalTransformValuesMatch(false, ChildrenTransformCheckType.Late_Join_Client);

            // This validates each sub-child instance has preserved their local space values
            yield return AllChildrenLocalTransformValuesMatch(true, ChildrenTransformCheckType.Late_Join_Client);
        }

        /// <summary>
        /// Validates that moving, rotating, and scaling the authority side with a single
        /// tick will properly synchronize the non-authoritative side with the same values.
        /// </summary>
        private void MoveRotateAndScaleAuthority(Vector3 position, Vector3 rotation, Vector3 scale, OverrideState overrideState)
        {
            switch (overrideState)
            {
                case OverrideState.SetState:
                    {
                        var authoritativeRotation = m_AuthoritativeTransform.GetSpaceRelativeRotation();
                        authoritativeRotation.eulerAngles = rotation;
                        if (m_Authority == Authority.OwnerAuthority)
                        {
                            // Under the scenario where the owner is not the server, and non-auth is the server we set the state from the server
                            // to be updated to the owner.
                            if (m_AuthoritativeTransform.IsOwner && !m_AuthoritativeTransform.IsServer && m_NonAuthoritativeTransform.IsServer)
                            {
                                m_NonAuthoritativeTransform.SetState(position, authoritativeRotation, scale);
                            }
                            else
                            {
                                m_AuthoritativeTransform.SetState(position, authoritativeRotation, scale);
                            }
                        }
                        else
                        {
                            m_AuthoritativeTransform.SetState(position, authoritativeRotation, scale);
                        }

                        break;
                    }
                case OverrideState.Update:
                default:
                    {
                        m_AuthoritativeTransform.transform.position = position;

                        var authoritativeRotation = m_AuthoritativeTransform.GetSpaceRelativeRotation();
                        authoritativeRotation.eulerAngles = rotation;
                        m_AuthoritativeTransform.transform.rotation = authoritativeRotation;
                        m_AuthoritativeTransform.transform.localScale = scale;
                        break;
                    }
            }
        }

        // The number of iterations to change position, rotation, and scale for NetworkTransformMultipleChangesOverTime       
        private const int k_PositionRotationScaleIterations = 3;
        private const int k_PositionRotationScaleIterations3Axis = 8;

        protected override void OnNewClientCreated(NetworkManager networkManager)
        {
            networkManager.NetworkConfig.Prefabs = m_ServerNetworkManager.NetworkConfig.Prefabs;
            networkManager.NetworkConfig.TickRate = k_TickRate;
            base.OnNewClientCreated(networkManager);
        }

        private Precision m_Precision = Precision.Full;
        private RotationCompression m_RotationCompression = RotationCompression.None;
        private float m_CurrentHalfPrecision = 0.0f;
        private const float k_HalfPrecisionPosScale = 0.041f;
        private const float k_HalfPrecisionRot = 0.725f;

        protected override float GetDeltaVarianceThreshold()
        {
            if (m_Precision == Precision.Half || m_RotationCompression == RotationCompression.QuaternionCompress)
            {
                return m_CurrentHalfPrecision;
            }
            return base.GetDeltaVarianceThreshold();
        }


        private Axis m_CurrentAxis;

        private bool m_AxisExcluded;

        /// <summary>
        /// Randomly determine if an axis should be excluded.
        /// If so, then randomly pick one of the axis to be excluded.
        /// </summary>
        private Vector3 RandomlyExcludeAxis(Vector3 delta)
        {
            if (Random.Range(0.0f, 1.0f) >= 0.5f)
            {
                m_AxisExcluded = true;
                var axisToIgnore = Random.Range(0, 2);
                switch (axisToIgnore)
                {
                    case 0:
                        {
                            delta.x = 0;
                            break;
                        }
                    case 1:
                        {
                            delta.y = 0;
                            break;
                        }
                    case 2:
                        {
                            delta.z = 0;
                            break;
                        }
                }
            }
            return delta;
        }

        /// <summary>
        /// This validates that multiple changes can occur within the same tick or over
        /// several ticks while still keeping non-authoritative instances synchronized.
        /// </summary>
        /// <remarks>
        /// When testing < 3 axis: Interpolation is disabled and only 3 delta updates are applied per unique test
        /// When testing 3 axis: Interpolation is enabled, sometimes an axis is intentionally excluded during a
        /// delta update, and it runs through 8 delta updates per unique test.
        /// </remarks>
        [UnityTest]
        public IEnumerator NetworkTransformMultipleChangesOverTime([Values] TransformSpace testLocalTransform, [Values] OverrideState overideState,
            [Values] Precision precision, [Values] Rotation rotationSynch, [Values] Axis axis)
        {
            yield return s_DefaultWaitForTick;

            var tickRelativeTime = new WaitForSeconds(1.0f / m_ServerNetworkManager.NetworkConfig.TickRate);
            m_AuthoritativeTransform.InLocalSpace = testLocalTransform == TransformSpace.Local;
            bool axisX = axis == Axis.X || axis == Axis.XY || axis == Axis.XZ || axis == Axis.XYZ;
            bool axisY = axis == Axis.Y || axis == Axis.XY || axis == Axis.YZ || axis == Axis.XYZ;
            bool axisZ = axis == Axis.Z || axis == Axis.XZ || axis == Axis.YZ || axis == Axis.XYZ;

            var axisCount = axisX ? 1 : 0;
            axisCount += axisY ? 1 : 0;
            axisCount += axisZ ? 1 : 0;

            m_AuthoritativeTransform.StatePushed = false;
            // Enable interpolation when all 3 axis are selected to make sure we are synchronizing properly
            // when interpolation is enabled.
            m_AuthoritativeTransform.Interpolate = axisCount == 3 ? true : false;

            m_CurrentAxis = axis;

            // Authority dictates what is synchronized and what the precision is going to be
            // so we only need to set this on the authoritative side.
            m_AuthoritativeTransform.UseHalfFloatPrecision = precision == Precision.Half;
            m_AuthoritativeTransform.UseQuaternionSynchronization = rotationSynch == Rotation.Quaternion;
            m_Precision = precision;

            m_AuthoritativeTransform.SyncPositionX = axisX;
            m_AuthoritativeTransform.SyncPositionY = axisY;
            m_AuthoritativeTransform.SyncPositionZ = axisZ;

            if (!m_AuthoritativeTransform.UseQuaternionSynchronization)
            {
                m_AuthoritativeTransform.SyncRotAngleX = axisX;
                m_AuthoritativeTransform.SyncRotAngleY = axisY;
                m_AuthoritativeTransform.SyncRotAngleZ = axisZ;
            }
            else
            {
                // This is not required for usage (setting the value should not matter when quaternion synchronization is enabled)
                // but is required for this test so we don't get a failure on an axis that is marked to not be synchronized when
                // validating the authority's values on non-authority instances.
                m_AuthoritativeTransform.SyncRotAngleX = true;
                m_AuthoritativeTransform.SyncRotAngleY = true;
                m_AuthoritativeTransform.SyncRotAngleZ = true;
            }

            m_AuthoritativeTransform.SyncScaleX = axisX;
            m_AuthoritativeTransform.SyncScaleY = axisY;
            m_AuthoritativeTransform.SyncScaleZ = axisZ;

            var positionStart = GetRandomVector3(0.25f, 1.75f);
            var rotationStart = GetRandomVector3(1f, 15f);
            var scaleStart = GetRandomVector3(0.25f, 2.0f);
            var position = positionStart;
            var rotation = rotationStart;
            var scale = scaleStart;
            var success = false;


            // Wait for the deltas to be pushed
            yield return WaitForConditionOrTimeOut(() => m_AuthoritativeTransform.StatePushed);

            // Just in case we drop the first few state updates 
            if (s_GlobalTimeoutHelper.TimedOut)
            {
                // Set the local state to not reflect the authority state's local space settings
                // to trigger the state update (it would eventually get there, but this is an integration test)
                var state = m_AuthoritativeTransform.LocalAuthoritativeNetworkState;
                state.InLocalSpace = !m_AuthoritativeTransform.InLocalSpace;
                m_AuthoritativeTransform.LocalAuthoritativeNetworkState = state;
                // Wait for the deltas to be pushed
                yield return WaitForConditionOrTimeOut(() => m_AuthoritativeTransform.StatePushed);
            }
            AssertOnTimeout("State was never pushed!");

            // Allow the precision settings to propagate first as changing precision
            // causes a teleport event to occur
            yield return s_DefaultWaitForTick;
            yield return s_DefaultWaitForTick;
            yield return s_DefaultWaitForTick;
            yield return s_DefaultWaitForTick;
            yield return s_DefaultWaitForTick;
            var iterations = axisCount == 3 ? k_PositionRotationScaleIterations3Axis : k_PositionRotationScaleIterations;

            // Move and rotate within the same tick, validate the non-authoritative instance updates
            // to each set of changes.  Repeat several times.
            for (int i = 0; i < iterations; i++)
            {
                // Always reset this per delta update pass
                m_AxisExcluded = false;
                var deltaPositionDelta = GetRandomVector3(-1.5f, 1.5f);
                var deltaRotationDelta = GetRandomVector3(-3.5f, 3.5f);
                var deltaScaleDelta = GetRandomVector3(-0.5f, 0.5f);

                m_NonAuthoritativeTransform.StateUpdated = false;
                m_AuthoritativeTransform.StatePushed = false;

                // With two or more axis, excluding one of them while chaging another will validate that
                // full precision updates are maintaining their target state value(s) to interpolate towards
                if (axisCount == 3)
                {
                    position += RandomlyExcludeAxis(deltaPositionDelta);
                    rotation += RandomlyExcludeAxis(deltaRotationDelta);
                    scale += RandomlyExcludeAxis(deltaScaleDelta);
                }
                else
                {
                    position += deltaPositionDelta;
                    rotation += deltaRotationDelta;
                    scale += deltaScaleDelta;
                }

                // Apply delta between ticks
                MoveRotateAndScaleAuthority(position, rotation, scale, overideState);

                // Wait for the deltas to be pushed (unlike the original test, we don't wait for state to be updated as that could be dropped here)
                yield return WaitForConditionOrTimeOut(() => m_AuthoritativeTransform.StatePushed);
                AssertOnTimeout($"[Non-Interpolate {i}] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");

                // For 3 axis, we will skip validating that the non-authority interpolates to its target point at least once.
                // This will validate that non-authoritative updates are maintaining their target state axis values if only 2 
                // of the axis are being updated to assure interpolation maintains the targeted axial value per axis.
                // For 2 and 1 axis tests we always validate per delta update
                if (m_AxisExcluded || axisCount < 3)
                {
                    // Wait for deltas to synchronize on non-authoritative side
                    yield return WaitForConditionOrTimeOut(PositionRotationScaleMatches);
                    // Provide additional debug info about what failed (if it fails)
                    if (s_GlobalTimeoutHelper.TimedOut)
                    {
                        Debug.Log("[Synch Issue Start - 1]");
                        // If we timed out, then wait for a full range of ticks (plus 1) to assure it sent synchronization data.
                        for (int j = 0; j < m_ServerNetworkManager.NetworkConfig.TickRate * 2; j++)
                        {
                            success = PositionRotationScaleMatches();
                            if (success)
                            {
                                // If we matched, then something was dropped and recovered when synchronized
                                break;
                            }
                            yield return s_DefaultWaitForTick;
                        }

                        // Only if we still didn't match
                        if (!success)
                        {
                            m_EnableVerboseDebug = true;
                            success = PositionRotationScaleMatches();
                            m_EnableVerboseDebug = false;
                            Debug.Log("[Synch Issue END - 1]");
                            AssertOnTimeout($"[Non-Interpolate {i}] Timed out waiting for non-authority to match authority's position or rotation");
                        }
                    }
                }
            }

            if (axisCount == 3)
            {
                // As a final test, wait for deltas to synchronize on non-authoritative side to assure it interpolates to th
                yield return WaitForConditionOrTimeOut(PositionRotationScaleMatches);
                // Provide additional debug info about what failed (if it fails)
                if (s_GlobalTimeoutHelper.TimedOut)
                {
                    Debug.Log("[Synch Issue Start - 2]");
                    // If we timed out, then wait for a full range of ticks (plus 1) to assure it sent synchronization data.
                    for (int j = 0; j < m_ServerNetworkManager.NetworkConfig.TickRate * 2; j++)
                    {
                        success = PositionRotationScaleMatches();
                        if (success)
                        {
                            // If we matched, then something was dropped and recovered when synchronized
                            break;
                        }
                        yield return s_DefaultWaitForTick;
                    }

                    // Only if we still didn't match
                    if (!success)
                    {
                        m_EnableVerboseDebug = true;
                        PositionRotationScaleMatches();
                        m_EnableVerboseDebug = false;
                        Debug.Log("[Synch Issue END - 2]");
                        AssertOnTimeout("Timed out waiting for non-authority to match authority's position or rotation");

                    }
                }

            }
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

            m_AuthoritativeTransform.StatePushed = false;
            var nextPosition = GetRandomVector3(2f, 30f);
            m_AuthoritativeTransform.transform.position = nextPosition;
            if (overideState != OverrideState.SetState)
            {
                authPlayerTransform.position = nextPosition;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(nextPosition, null, null, m_AuthoritativeTransform.Interpolate);
            }

            if (overideState != OverrideState.Update)
            {
                // Wait for the deltas to be pushed
                yield return WaitForConditionOrTimeOut(() => m_AuthoritativeTransform.StatePushed);
                AssertOnTimeout($"[Position] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");
            }

            yield return WaitForConditionOrTimeOut(() => PositionsMatch());
            AssertOnTimeout($"Timed out waiting for positions to match {m_AuthoritativeTransform.transform.position} | {m_NonAuthoritativeTransform.transform.position}");

            // test rotation
            Assert.AreEqual(Quaternion.identity, m_NonAuthoritativeTransform.transform.rotation, "wrong initial value for rotation"); // sanity check

            m_AuthoritativeTransform.StatePushed = false;
            var nextRotation = Quaternion.Euler(GetRandomVector3(5, 60)); // using euler angles instead of quaternions directly to really see issues users might encounter
            if (overideState != OverrideState.SetState)
            {
                authPlayerTransform.rotation = nextRotation;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(null, nextRotation, null, m_AuthoritativeTransform.Interpolate);
            }
            if (overideState != OverrideState.Update)
            {
                // Wait for the deltas to be pushed
                yield return WaitForConditionOrTimeOut(() => m_AuthoritativeTransform.StatePushed);
                AssertOnTimeout($"[Rotation] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");
            }

            // Make sure the values match
            yield return WaitForConditionOrTimeOut(() => RotationsMatch());
            AssertOnTimeout($"Timed out waiting for rotations to match");

            m_AuthoritativeTransform.StatePushed = false;
            var nextScale = GetRandomVector3(1, 6);
            if (overrideUpdate)
            {
                authPlayerTransform.localScale = nextScale;
                m_OwnerTransform.CommitToTransform();
            }
            else
            {
                m_OwnerTransform.SetState(null, null, nextScale, m_AuthoritativeTransform.Interpolate);
            }
            if (overideState != OverrideState.Update)
            {
                // Wait for the deltas to be pushed
                yield return WaitForConditionOrTimeOut(() => m_AuthoritativeTransform.StatePushed);
                AssertOnTimeout($"[Rotation] Timed out waiting for state to be pushed ({m_AuthoritativeTransform.StatePushed})!");
            }

            // Make sure the scale values match
            yield return WaitForConditionOrTimeOut(() => ScaleValuesMatch());
            AssertOnTimeout($"Timed out waiting for scale values to match");
        }

        private bool PositionRotationScaleMatches()
        {
            return RotationsMatch() && PositionsMatch() && ScaleValuesMatch();
        }

        private bool RotationsMatch(bool printDeltas = false)
        {
            m_CurrentHalfPrecision = k_HalfPrecisionRot;
            var authorityEulerRotation = m_AuthoritativeTransform.GetSpaceRelativeRotation().eulerAngles;
            var nonAuthorityEulerRotation = m_NonAuthoritativeTransform.GetSpaceRelativeRotation().eulerAngles;
            var xIsEqual = ApproximatelyEuler(authorityEulerRotation.x, nonAuthorityEulerRotation.x) || !m_AuthoritativeTransform.SyncRotAngleX;
            var yIsEqual = ApproximatelyEuler(authorityEulerRotation.y, nonAuthorityEulerRotation.y) || !m_AuthoritativeTransform.SyncRotAngleY;
            var zIsEqual = ApproximatelyEuler(authorityEulerRotation.z, nonAuthorityEulerRotation.z) || !m_AuthoritativeTransform.SyncRotAngleZ;
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}][X-{xIsEqual} | Y-{yIsEqual} | Z-{zIsEqual}][{m_CurrentAxis}]" +
                    $"[Sync: X-{m_AuthoritativeTransform.SyncRotAngleX} |  Y-{m_AuthoritativeTransform.SyncRotAngleY} |  Z-{m_AuthoritativeTransform.SyncRotAngleZ}] Authority rotation {authorityEulerRotation} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority rotation {nonAuthorityEulerRotation}");
            }
            if (printDeltas)
            {
                Debug.Log($"[Rotation Match] Euler Delta {EulerDelta(authorityEulerRotation, nonAuthorityEulerRotation)}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool PositionsMatch(bool printDeltas = false)
        {
            m_CurrentHalfPrecision = k_HalfPrecisionPosScale;
            var authorityPosition = m_AuthoritativeTransform.GetSpaceRelativePosition();
            var nonAuthorityPosition = m_NonAuthoritativeTransform.GetSpaceRelativePosition();
            var xIsEqual = Approximately(authorityPosition.x, nonAuthorityPosition.x) || !m_AuthoritativeTransform.SyncPositionX;
            var yIsEqual = Approximately(authorityPosition.y, nonAuthorityPosition.y) || !m_AuthoritativeTransform.SyncPositionY;
            var zIsEqual = Approximately(authorityPosition.z, nonAuthorityPosition.z) || !m_AuthoritativeTransform.SyncPositionZ;
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}] Authority position {authorityPosition} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority position {nonAuthorityPosition}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }

        private bool ScaleValuesMatch(bool printDeltas = false)
        {
            m_CurrentHalfPrecision = k_HalfPrecisionPosScale;
            var authorityScale = m_AuthoritativeTransform.transform.localScale;
            var nonAuthorityScale = m_NonAuthoritativeTransform.transform.localScale;
            var xIsEqual = Approximately(authorityScale.x, nonAuthorityScale.x) || !m_AuthoritativeTransform.SyncScaleX;
            var yIsEqual = Approximately(authorityScale.y, nonAuthorityScale.y) || !m_AuthoritativeTransform.SyncScaleY;
            var zIsEqual = Approximately(authorityScale.z, nonAuthorityScale.z) || !m_AuthoritativeTransform.SyncScaleZ;
            if (!xIsEqual || !yIsEqual || !zIsEqual)
            {
                VerboseDebug($"[{m_AuthoritativeTransform.gameObject.name}] Authority scale {authorityScale} != [{m_NonAuthoritativeTransform.gameObject.name}] NonAuthority scale {nonAuthorityScale}");
            }
            return xIsEqual && yIsEqual && zIsEqual;
        }
    }
}
