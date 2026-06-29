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

    #region Test helper classes
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
    #endregion
}
#endif
