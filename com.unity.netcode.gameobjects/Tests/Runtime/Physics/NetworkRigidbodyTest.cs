#if COM_UNITY_MODULES_PHYSICS || COM_UNITY_MODULES_PHYSICS2D
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkRigidbodyTest : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private List<(RigidbodyInterpolation interpolationType, bool enableInterpolation, bool useRigidbodyForMotion)> m_TestConfigurations =
            new List<(RigidbodyInterpolation interpolationType, bool enableInterpolation, bool useRigidbodyForMotion)>()
            {
                (RigidbodyInterpolation.Interpolate, true, true), // This should be allowed under all condistions when using Rigidbody motion
                (RigidbodyInterpolation.Extrapolate, true, true), // This should not allow extrapolation on non-auth instances when using Rigidbody motion & NT interpolation
                (RigidbodyInterpolation.Extrapolate, false, true), // This should allow extrapolation on non-auth instances when using Rigidbody & NT has no interpolation
                (RigidbodyInterpolation.Interpolate, true, false), // This should not allow kinematic instances to have Rigidbody interpolation enabled
                (RigidbodyInterpolation.Interpolate, false, false) // Testing that rigidbody interpolation remains the same if NT interpolate is disabled
            };

        /// <summary>
        /// The current test configuration applied to the current test running.
        /// </summary>
        private (RigidbodyInterpolation interpolationType, bool enableInterpolation, bool useRigidbodyForMotion) m_CurrentConfiguration;

        public NetworkRigidbodyTest(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        /// <summary>
        /// Base prefab for <see cref="Rigidbody"/> and <see cref="NetworkRigidbody"/>
        /// </summary>
        private GameObject m_RigidbodyPrefab;
        private NetworkTransform m_3DNetworkTransform;
        private Rigidbody m_PrefabRigidbody;
        private NetworkRigidbody m_PrefabNetworkRigidbody;
        private NetworkObject m_3DAuthorityInstance;

        /// <summary>
        /// Base prefab for <see cref="Rigidbody2D"/> and <see cref="NetworkRigidbody2D"/>
        /// </summary>
        private GameObject m_Rigidbody2DPrefab;
        private NetworkTransform m_2DNetworkTransform;
        private Rigidbody2D m_PrefabRigidbody2D;
        private NetworkRigidbody2D m_PrefabNetworkRigidbody2D;
        private NetworkObject m_2DAuthorityInstance;

        protected override void OnServerAndClientsCreated()
        {
            m_RigidbodyPrefab = CreateNetworkObjectPrefab("RBTest");
            m_3DNetworkTransform = m_RigidbodyPrefab.AddComponent<NetworkTransform>();
            m_PrefabRigidbody = m_RigidbodyPrefab.AddComponent<Rigidbody>();
            m_PrefabNetworkRigidbody = m_RigidbodyPrefab.AddComponent<NetworkRigidbody>();

            m_Rigidbody2DPrefab = CreateNetworkObjectPrefab("RB2DTest");
            m_2DNetworkTransform = m_Rigidbody2DPrefab.AddComponent<NetworkTransform>();
            m_PrefabRigidbody2D = m_Rigidbody2DPrefab.AddComponent<Rigidbody2D>();
            m_PrefabNetworkRigidbody2D = m_Rigidbody2DPrefab.AddComponent<NetworkRigidbody2D>();

            base.OnServerAndClientsCreated();
        }

        private string m_ConfigHeader;
        private void ApplyCurrentTestConfiguration()
        {
            // Configure both 3D and 2D versions based on the current test configuration 
            m_3DNetworkTransform.Interpolate = m_CurrentConfiguration.enableInterpolation;
            m_PrefabRigidbody.interpolation = m_CurrentConfiguration.interpolationType;
            m_PrefabNetworkRigidbody.UseRigidBodyForMotion = m_CurrentConfiguration.useRigidbodyForMotion;
            m_2DNetworkTransform.Interpolate = m_CurrentConfiguration.enableInterpolation;
            m_PrefabRigidbody2D.interpolation = m_CurrentConfiguration.interpolationType == RigidbodyInterpolation.Interpolate ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.Extrapolate;
            m_PrefabNetworkRigidbody2D.UseRigidBodyForMotion = m_CurrentConfiguration.useRigidbodyForMotion;

            // Build a header used in assert messages
            m_ConfigHeader = $"[{m_CurrentConfiguration.interpolationType}][Interpolate: {m_CurrentConfiguration.enableInterpolation}][RB-Motion: {m_CurrentConfiguration.useRigidbodyForMotion}]";
        }

        /// <summary>
        /// Iterates through the <see cref="m_TestConfigurations"/> to validate various
        /// Rigidbody interpolation settings and kinematic states for authority and non-authority
        /// instances.
        /// </summary>
        [UnityTest]
        public IEnumerator TestRigidbodyKinematicEnableDisable()
        {
            foreach (var configuration in m_TestConfigurations)
            {
                m_CurrentConfiguration = configuration;
                ApplyCurrentTestConfiguration();
                yield return RunTestConfiguration();
            }
        }

        /// <summary>
        /// Validates the current applied test configuration.
        /// </summary>
        private IEnumerator RunTestConfiguration()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            // Spawn instances of both the 3D and 2D prefabs configured for the current test.
            m_3DAuthorityInstance = SpawnObject(m_RigidbodyPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_3DAuthorityInstance);
            AssertOnTimeout($"Failed to spawn {m_3DAuthorityInstance.name} on all clients!");

            m_2DAuthorityInstance = SpawnObject(m_Rigidbody2DPrefab, authority).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_2DAuthorityInstance);
            AssertOnTimeout($"Failed to spawn {m_2DAuthorityInstance.name} on all clients!");

            // Test 3D Rigidbody
            #region 3D Rigidbody validation
            var authorityRigidbody = m_3DAuthorityInstance.GetComponent<Rigidbody>();
            var nonAuthorityInstance = nonAuthority.SpawnManager.SpawnedObjects[m_3DAuthorityInstance.NetworkObjectId];
            var nonAuthorityRigidbody = nonAuthorityInstance.GetComponent<Rigidbody>();
            var authorityHeader = $"{m_ConfigHeader}[Authority] Client-{authority.LocalClientId}'s instance of {m_3DAuthorityInstance.name}";
            // The authority instance should always be non-kinematic
            Assert.False(authorityRigidbody.isKinematic, $"{authorityHeader} is kinematic!");

            var nonAuthorityHeader = $"{m_ConfigHeader}[Non-Authority] Client-{nonAuthority.LocalClientId}'s instance of {nonAuthorityInstance.name}";
            // Non-authority instances should always be kinematic
            Assert.True(nonAuthorityRigidbody.isKinematic, $"{nonAuthorityHeader} is not kinematic!");
            var interpolateCompareNonAuthoritative = RigidbodyInterpolation.None;

            if (m_CurrentConfiguration.useRigidbodyForMotion)
            {
                // The authoritative instance can be None, Interpolate, or Extrapolate for the Rigidbody interpolation settings.
                Assert.AreEqual(m_CurrentConfiguration.interpolationType, authorityRigidbody.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody.interpolation} " +
                    $"and not {m_CurrentConfiguration.interpolationType}!");

                // When using Rigidbody motion, authoritative and non-authoritative Rigidbody interpolation settings should be preserved (except when extrapolation is used
                interpolateCompareNonAuthoritative = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation.Interpolate : m_CurrentConfiguration.interpolationType;

            }
            else
            {
                Assert.AreEqual(RigidbodyInterpolation.Interpolate, authorityRigidbody.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody.interpolation} " +
                    $"and not {RigidbodyInterpolation.Interpolate}!");

                // client rigidbody has no authority with NT interpolation disabled should allow Rigidbody interpolation
                interpolateCompareNonAuthoritative = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
            }

            Assert.AreEqual(interpolateCompareNonAuthoritative, nonAuthorityRigidbody.interpolation, $"{nonAuthorityHeader} interpolation is {nonAuthorityRigidbody.interpolation} " +
                $"and not {interpolateCompareNonAuthoritative}!");
            #endregion

            // Test 2D Rigidbody
            #region 2D Rigidbody validation
            var authorityRigidbody2D = m_2DAuthorityInstance.GetComponent<Rigidbody2D>();
            var nonAuthorityInstance2D = nonAuthority.SpawnManager.SpawnedObjects[m_2DAuthorityInstance.NetworkObjectId];
            var nonAuthorityRigidbody2D = nonAuthorityInstance2D.GetComponent<Rigidbody2D>();

            authorityHeader = $"{m_ConfigHeader}[Authority] Client-{authority.LocalClientId}'s instance of {m_2DAuthorityInstance.name}";
            // The authority instance should always be non-kinematic
            Assert.False(authorityRigidbody2D.bodyType == RigidbodyType2D.Kinematic, $"{authorityHeader} is kinematic!");

            nonAuthorityHeader = $"{m_ConfigHeader}[Non-Authority] Client-{nonAuthority.LocalClientId}'s instance of {nonAuthorityInstance.name}";
            // Non-authority instances should always be kinematic
            Assert.True(nonAuthorityRigidbody2D.bodyType == RigidbodyType2D.Kinematic, $"{nonAuthorityHeader} is not kinematic!");
            var interpolateCompareNonAuthoritative2D = RigidbodyInterpolation2D.None;
            var configInterpolation2D = m_CurrentConfiguration.interpolationType == RigidbodyInterpolation.Interpolate ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.Extrapolate;
            if (m_CurrentConfiguration.useRigidbodyForMotion)
            {
                // The authoritative instance can be None, Interpolate, or Extrapolate for the Rigidbody interpolation settings.
                Assert.AreEqual(configInterpolation2D, authorityRigidbody2D.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody2D.interpolation} " +
                    $"and not {m_CurrentConfiguration.interpolationType}!");

                // When using Rigidbody motion, authoritative and non-authoritative Rigidbody interpolation settings should be preserved (except when extrapolation is used
                interpolateCompareNonAuthoritative2D = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation2D.Interpolate : configInterpolation2D;
            }
            else
            {
                Assert.AreEqual(RigidbodyInterpolation2D.Interpolate, authorityRigidbody2D.interpolation, $"{authorityHeader} interpolation is {authorityRigidbody2D.interpolation} " +
                    $"and not {RigidbodyInterpolation2D.Interpolate}!");

                // client rigidbody has no authority with NT interpolation disabled should allow Rigidbody interpolation
                interpolateCompareNonAuthoritative2D = m_CurrentConfiguration.enableInterpolation ? RigidbodyInterpolation2D.None : RigidbodyInterpolation2D.Interpolate;
            }

            Assert.AreEqual(interpolateCompareNonAuthoritative2D, nonAuthorityRigidbody2D.interpolation, $"{nonAuthorityHeader} interpolation is {nonAuthorityRigidbody2D.interpolation} " +
                $"and not {interpolateCompareNonAuthoritative}!");
            #endregion

            var spawnedInstances = new List<NetworkObject>() { m_3DAuthorityInstance, m_2DAuthorityInstance };
            m_3DAuthorityInstance.Despawn();
            m_2DAuthorityInstance.Despawn();
            yield return WaitForDespawnedOnAllOrTimeOut(spawnedInstances);
            AssertOnTimeout($"Failed to de-spawn instances on all clients!");
            m_3DAuthorityInstance = null;
            m_2DAuthorityInstance = null;
        }

        /// <summary>
        /// Handle clean up in case of a failed test
        /// </summary>
        protected override IEnumerator OnTearDown()
        {
            // If either of these are not null then we most likely failed and didn't cleanup.

            // Clean-up m_3DAuthorityInstance
            if (m_3DAuthorityInstance)
            {
                Object.Destroy(m_3DAuthorityInstance);
                m_3DAuthorityInstance = null;
            }

            // Clean-up m_2DAuthorityInstance
            if (m_2DAuthorityInstance)
            {
                Object.Destroy(m_2DAuthorityInstance);
                m_2DAuthorityInstance = null;
            }

            return base.OnTearDown();
        }
    }

    internal class ContactEventTransformHelperWithInfo : ContactEventTransformHelper, IContactEventHandlerWithInfo
    {
        public ContactEventHandlerInfo GetContactEventHandlerInfo()
        {
            var contactEventHandlerInfo = new ContactEventHandlerInfo()
            {
                HasContactEventPriority = IsOwner,
                ProvideNonRigidBodyContactEvents = m_EnableNonRigidbodyContacts.Value,
            };
            return contactEventHandlerInfo;
        }

        protected override void OnRegisterForContactEvents(bool isRegistering)
        {
            RigidbodyContactEventManager.Instance.RegisterHandler(this, isRegistering);
        }
    }


    internal class ContactEventTransformHelper : NetworkTransform, IContactEventHandler
    {
        public static Vector3 SessionOwnerSpawnPoint;
        public static Vector3 ClientSpawnPoint;
        public static bool VerboseDebug;
        public enum HelperStates
        {
            None,
            MoveForward,
        }

        private HelperStates m_HelperState;

        public void SetHelperState(HelperStates state)
        {
            m_HelperState = state;
            if (!m_NetworkRigidbody.IsKinematic())
            {
                m_NetworkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
                m_NetworkRigidbody.Rigidbody.linearVelocity = Vector3.zero;
            }
            m_NetworkRigidbody.Rigidbody.isKinematic = m_HelperState == HelperStates.None;
            if (!m_NetworkRigidbody.IsKinematic())
            {
                m_NetworkRigidbody.Rigidbody.angularVelocity = Vector3.zero;
                m_NetworkRigidbody.Rigidbody.linearVelocity = Vector3.zero;
            }

        }

        protected struct ContactEventInfo
        {
            public ulong EventId;
            public Vector3 AveragedCollisionNormal;
            public Rigidbody CollidingBody;
            public Vector3 ContactPoint;
        }

        protected List<ContactEventInfo> m_ContactEvents = new List<ContactEventInfo>();

        protected NetworkVariable<bool> m_EnableNonRigidbodyContacts = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        protected NetworkRigidbody m_NetworkRigidbody;
        public ContactEventTransformHelper Target;

        public bool HasContactEvents()
        {
            return m_ContactEvents.Count > 0;
        }

        public Rigidbody GetRigidbody()
        {
            return m_NetworkRigidbody.Rigidbody;
        }

        public bool HadContactWith(ContactEventTransformHelper otherObject)
        {
            if (otherObject == null)
            {
                return false;
            }
            foreach (var contactEvent in m_ContactEvents)
            {
                if (contactEvent.CollidingBody == otherObject.m_NetworkRigidbody.Rigidbody)
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual void CheckToStopMoving()
        {
            SetHelperState(HadContactWith(Target) ? HelperStates.None : HelperStates.MoveForward);
        }

        public void ContactEvent(ulong eventId, Vector3 averagedCollisionNormal, Rigidbody collidingBody, Vector3 contactPoint, bool hasCollisionStay = false, Vector3 averagedCollisionStayNormal = default)
        {
            if (Target == null)
            {
                return;
            }

            if (collidingBody != null)
            {
                Log($">>>>>>> contact event with {collidingBody.name}!");
            }
            else
            {
                Log($">>>>>>> contact event with non-rigidbody!");
            }

            m_ContactEvents.Add(new ContactEventInfo()
            {
                EventId = eventId,
                AveragedCollisionNormal = averagedCollisionNormal,
                CollidingBody = collidingBody,
                ContactPoint = contactPoint,
            });
            CheckToStopMoving();
        }

        private void SetInitialPositionClientServer()
        {
            if (IsServer)
            {
                if (!NetworkManager.DistributedAuthorityMode && !IsLocalPlayer)
                {
                    transform.position = ClientSpawnPoint;
                    m_NetworkRigidbody.Rigidbody.position = ClientSpawnPoint;
                }
                else
                {
                    transform.position = SessionOwnerSpawnPoint;
                    m_NetworkRigidbody.Rigidbody.position = SessionOwnerSpawnPoint;
                }
            }
            else
            {
                transform.position = ClientSpawnPoint;
                m_NetworkRigidbody.Rigidbody.position = ClientSpawnPoint;
            }
        }

        private void SetInitialPositionDistributedAuthority()
        {
            if (HasAuthority)
            {
                if (IsSessionOwner)
                {
                    transform.position = SessionOwnerSpawnPoint;
                    m_NetworkRigidbody.Rigidbody.position = SessionOwnerSpawnPoint;
                }
                else
                {
                    transform.position = ClientSpawnPoint;
                    m_NetworkRigidbody.Rigidbody.position = ClientSpawnPoint;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            m_NetworkRigidbody = GetComponent<NetworkRigidbody>();

            m_NetworkRigidbody.Rigidbody.maxLinearVelocity = 15;
            m_NetworkRigidbody.Rigidbody.maxAngularVelocity = 10;

            if (NetworkManager.DistributedAuthorityMode)
            {
                SetInitialPositionDistributedAuthority();
            }
            else
            {
                SetInitialPositionClientServer();
            }
            if (IsLocalPlayer)
            {
                RegisterForContactEvents(true);
            }
            else
            {
                m_NetworkRigidbody.Rigidbody.detectCollisions = false;
            }
            base.OnNetworkSpawn();
        }

        protected virtual void OnRegisterForContactEvents(bool isRegistering)
        {
            RigidbodyContactEventManager.Instance.RegisterHandler(this, isRegistering);
        }

        public void RegisterForContactEvents(bool isRegistering)
        {
            OnRegisterForContactEvents(isRegistering);
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner || m_HelperState != HelperStates.MoveForward)
            {
                return;
            }
            var distance = Vector3.Distance(Target.transform.position, transform.position);
            var moveAmount = Mathf.Max(1.2f, distance);
            // Head towards our target
            var dir = (Target.transform.position - transform.position).normalized;
            var deltaMove = dir * moveAmount * Time.fixedDeltaTime;
            m_NetworkRigidbody.Rigidbody.MovePosition(m_NetworkRigidbody.Rigidbody.position + deltaMove);


            Log($" Loc: {transform.position} | Dest: {Target.transform.position} | Dist: {distance} | MoveDelta: {deltaMove}");
        }

        protected void Log(string msg)
        {
            if (VerboseDebug)
            {
                Debug.Log($"Client-{OwnerClientId} {msg}");
            }
        }
    }

    [TestFixture(HostOrServer.Host, ContactEventTypes.Default)]
    [TestFixture(HostOrServer.DAHost, ContactEventTypes.Default)]
    [TestFixture(HostOrServer.Host, ContactEventTypes.WithInfo)]
    [TestFixture(HostOrServer.DAHost, ContactEventTypes.WithInfo)]
    internal class RigidbodyContactEventManagerTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 1;

        private GameObject m_RigidbodyContactEventManager;

        public enum ContactEventTypes
        {
            Default,
            WithInfo
        }

        private ContactEventTypes m_ContactEventType;
        private StringBuilder m_ErrorLogger = new StringBuilder();

        public RigidbodyContactEventManagerTests(HostOrServer hostOrServer, ContactEventTypes contactEventType) : base(hostOrServer)
        {
            m_ContactEventType = contactEventType;
        }

        protected override void OnCreatePlayerPrefab()
        {
            ContactEventTransformHelper.SessionOwnerSpawnPoint = GetRandomVector3(-4, -3);
            ContactEventTransformHelper.ClientSpawnPoint = GetRandomVector3(3, 4);
            if (m_ContactEventType == ContactEventTypes.Default)
            {
                var helper = m_PlayerPrefab.AddComponent<ContactEventTransformHelper>();
                helper.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            }
            else
            {
                var helperWithInfo = m_PlayerPrefab.AddComponent<ContactEventTransformHelperWithInfo>();
                helperWithInfo.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            }

            var rigidbody = m_PlayerPrefab.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.mass = 5.0f;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var sphereCollider = m_PlayerPrefab.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.5f;
            sphereCollider.providesContacts = true;

            var networkRigidbody = m_PlayerPrefab.AddComponent<NetworkRigidbody>();
            networkRigidbody.UseRigidBodyForMotion = true;
            networkRigidbody.AutoUpdateKinematicState = false;

            m_RigidbodyContactEventManager = new GameObject();
            m_RigidbodyContactEventManager.AddComponent<RigidbodyContactEventManager>();
        }



        private bool PlayersSpawnedInRightLocation()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            var position = authority.LocalClient.PlayerObject.transform.position;
            if (!Approximately(ContactEventTransformHelper.SessionOwnerSpawnPoint, position))
            {
                m_ErrorLogger.AppendLine($"Client-{authority.LocalClientId} player position {position} does not match the assigned player position {ContactEventTransformHelper.SessionOwnerSpawnPoint}!");
                return false;
            }

            position = nonAuthority.LocalClient.PlayerObject.transform.position;
            if (!Approximately(ContactEventTransformHelper.ClientSpawnPoint, position))
            {
                m_ErrorLogger.AppendLine($"Client-{nonAuthority.LocalClientId} player position {position} does not match the assigned player position {ContactEventTransformHelper.ClientSpawnPoint}!");
                return false;
            }
            var playerObject = (NetworkObject)null;
            if (!authority.SpawnManager.SpawnedObjects.ContainsKey(nonAuthority.LocalClient.PlayerObject.NetworkObjectId))
            {
                m_ErrorLogger.AppendLine($"Client-{authority.LocalClientId} cannot find a local spawned instance of Client-{nonAuthority.LocalClientId}'s player object!");
                return false;
            }
            playerObject = authority.SpawnManager.SpawnedObjects[nonAuthority.LocalClient.PlayerObject.NetworkObjectId];
            position = playerObject.transform.position;

            if (!Approximately(ContactEventTransformHelper.ClientSpawnPoint, position))
            {
                m_ErrorLogger.AppendLine($"Client-{authority.LocalClientId} player position {position} for Client-{playerObject.OwnerClientId} does not match the assigned player position {ContactEventTransformHelper.ClientSpawnPoint}!");
                return false;
            }

            if (!nonAuthority.SpawnManager.SpawnedObjects.ContainsKey(authority.LocalClient.PlayerObject.NetworkObjectId))
            {
                m_ErrorLogger.AppendLine($"Client-{nonAuthority.LocalClientId} cannot find a local spawned instance of Client-{authority.LocalClientId}'s player object!");
                return false;
            }
            playerObject = nonAuthority.SpawnManager.SpawnedObjects[authority.LocalClient.PlayerObject.NetworkObjectId];
            position = playerObject.transform.position;
            if (!Approximately(ContactEventTransformHelper.SessionOwnerSpawnPoint, playerObject.transform.position))
            {
                m_ErrorLogger.AppendLine($"Client-{nonAuthority.LocalClientId} player position {position} for Client-{playerObject.OwnerClientId} does not match the assigned player position {ContactEventTransformHelper.SessionOwnerSpawnPoint}!");
                return false;
            }
            return true;
        }


        [UnityTest]
        public IEnumerator TestContactEvents()
        {
            ContactEventTransformHelper.VerboseDebug = m_EnableVerboseDebug;

            m_PlayerPrefab.SetActive(false);
            m_ErrorLogger.Clear();
            // Validate all instances are spawned in the right location
            yield return WaitForConditionOrTimeOut(PlayersSpawnedInRightLocation);
            AssertOnTimeout($"Timed out waiting for all player instances to spawn in the corect location:\n {m_ErrorLogger}");
            m_ErrorLogger.Clear();

            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            var sessionOwnerPlayer = m_ContactEventType == ContactEventTypes.Default ? authority.LocalClient.PlayerObject.GetComponent<ContactEventTransformHelper>() :
                authority.LocalClient.PlayerObject.GetComponent<ContactEventTransformHelperWithInfo>();
            var clientPlayer = m_ContactEventType == ContactEventTypes.Default ? nonAuthority.LocalClient.PlayerObject.GetComponent<ContactEventTransformHelper>() :
                nonAuthority.LocalClient.PlayerObject.GetComponent<ContactEventTransformHelperWithInfo>();

            // Get both players to point towards each other
            sessionOwnerPlayer.Target = clientPlayer;
            clientPlayer.Target = sessionOwnerPlayer;

            sessionOwnerPlayer.SetHelperState(ContactEventTransformHelper.HelperStates.MoveForward);
            clientPlayer.SetHelperState(ContactEventTransformHelper.HelperStates.MoveForward);


            yield return WaitForConditionOrTimeOut(() => sessionOwnerPlayer.HadContactWith(clientPlayer) || clientPlayer.HadContactWith(sessionOwnerPlayer));
            AssertOnTimeout("Timed out waiting for a player to collide with another player!");

            clientPlayer.RegisterForContactEvents(false);
            sessionOwnerPlayer.RegisterForContactEvents(false);
            var otherPlayer = m_ContactEventType == ContactEventTypes.Default ? authority.SpawnManager.SpawnedObjects[clientPlayer.NetworkObjectId].GetComponent<ContactEventTransformHelper>() :
                authority.SpawnManager.SpawnedObjects[clientPlayer.NetworkObjectId].GetComponent<ContactEventTransformHelperWithInfo>();
            otherPlayer.RegisterForContactEvents(false);
            otherPlayer = m_ContactEventType == ContactEventTypes.Default ? nonAuthority.SpawnManager.SpawnedObjects[sessionOwnerPlayer.NetworkObjectId].GetComponent<ContactEventTransformHelper>() :
                nonAuthority.SpawnManager.SpawnedObjects[sessionOwnerPlayer.NetworkObjectId].GetComponent<ContactEventTransformHelperWithInfo>();
            otherPlayer.RegisterForContactEvents(false);

            Object.Destroy(m_RigidbodyContactEventManager);
            m_RigidbodyContactEventManager = null;
        }

        protected override IEnumerator OnTearDown()
        {
            // In case of a test failure
            if (m_RigidbodyContactEventManager)
            {
                Object.Destroy(m_RigidbodyContactEventManager);
                m_RigidbodyContactEventManager = null;
            }

            return base.OnTearDown();
        }
    }
}
#endif // COM_UNITY_MODULES_PHYSICS
