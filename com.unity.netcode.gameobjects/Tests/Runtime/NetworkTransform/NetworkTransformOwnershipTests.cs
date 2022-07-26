#if COM_UNITY_MODULES_PHYSICS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
namespace Unity.Netcode.RuntimeTests
{
    public class NetworkTransformOwnershipTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_ClientNetworkTransformPrefab;
        private GameObject m_NetworkTransformPrefab;

        protected override void OnServerAndClientsCreated()
        {
            VerifyObjectIsSpawnedOnClient.ResetObjectTable();
            m_ClientNetworkTransformPrefab = CreateNetworkObjectPrefab("OwnerAuthorityTest");
            var clientNetworkTransform = m_ClientNetworkTransformPrefab.AddComponent<TestClientNetworkTransform>();
            clientNetworkTransform.Interpolate = false;
            var rigidBody = m_ClientNetworkTransformPrefab.AddComponent<Rigidbody>();
            rigidBody.useGravity = false;
            m_ClientNetworkTransformPrefab.AddComponent<NetworkRigidbody>();
            m_ClientNetworkTransformPrefab.AddComponent<SphereCollider>();
            m_ClientNetworkTransformPrefab.AddComponent<VerifyObjectIsSpawnedOnClient>();

            m_NetworkTransformPrefab = CreateNetworkObjectPrefab("ServerAuthorityTest");
            var networkTransform = m_NetworkTransformPrefab.AddComponent<NetworkTransform>();
            rigidBody = m_NetworkTransformPrefab.AddComponent<Rigidbody>();
            rigidBody.useGravity = false;
            m_NetworkTransformPrefab.AddComponent<NetworkRigidbody>();
            m_NetworkTransformPrefab.AddComponent<SphereCollider>();
            m_NetworkTransformPrefab.AddComponent<VerifyObjectIsSpawnedOnClient>();
            networkTransform.Interpolate = false;

            base.OnServerAndClientsCreated();
        }

        public enum StartingOwnership
        {
            HostStartsAsOwner,
            ClientStartsAsOwner,
        }

        /// <summary>
        /// This verifies that when authority is owner authoritative the owner's
        /// Rigidbody is kinematic and the non-owner's is not.
        /// This also verifies that we can switch between owners and that only the
        /// owner can update the transform while non-owners cannot.
        /// </summary>
        /// <param name="spawnWithHostOwnership">determines who starts as the owner (true): host | (false): client</param>
        [UnityTest]
        public IEnumerator OwnerAuthoritativeTest([Values] StartingOwnership startingOwnership)
        {
            // Get the current ownership layout
            var networkManagerOwner = startingOwnership == StartingOwnership.HostStartsAsOwner ? m_ServerNetworkManager : m_ClientNetworkManagers[0];
            var networkManagerNonOwner = startingOwnership == StartingOwnership.HostStartsAsOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;

            // Spawn the m_ClientNetworkTransformPrefab and wait for the client-side to spawn the object
            var serverSideInstance = SpawnObject(m_ClientNetworkTransformPrefab, networkManagerOwner);
            yield return WaitForConditionOrTimeOut(() => VerifyObjectIsSpawnedOnClient.GetClientsThatSpawnedThisPrefab().Contains(m_ClientNetworkManagers[0].LocalClientId));

            // Get owner relative instances
            var ownerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerOwner.LocalClientId);
            var nonOwnerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerNonOwner.LocalClientId);
            Assert.NotNull(ownerInstance);
            Assert.NotNull(nonOwnerInstance);

            // Make sure the owner is not kinematic and the non-owner(s) are kinematic
            Assert.True(nonOwnerInstance.GetComponent<Rigidbody>().isKinematic, $"{networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} is not kinematic when it should be!");
            Assert.False(ownerInstance.GetComponent<Rigidbody>().isKinematic, $"{networkManagerOwner.name}'s object instance {ownerInstance.name} is kinematic when it should not be!");

            // Owner changes transform values
            var valueSetByOwner = Vector3.one * 2;
            ownerInstance.transform.position = valueSetByOwner;
            ownerInstance.transform.localScale = valueSetByOwner;
            var rotation = new Quaternion();
            rotation.eulerAngles = valueSetByOwner;
            ownerInstance.transform.rotation = rotation;
            var transformToTest = nonOwnerInstance.transform;
            yield return WaitForConditionOrTimeOut(() => transformToTest.position == valueSetByOwner && transformToTest.localScale == valueSetByOwner && transformToTest.rotation == rotation);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} to change its transform!\n" +
                $"Expected Position: {valueSetByOwner} | Current Position: {transformToTest.position}\n" +
                $"Expected Rotation: {valueSetByOwner} | Current Rotation: {transformToTest.rotation.eulerAngles}\n" +
                $"Expected Scale: {valueSetByOwner} | Current Scale: {transformToTest.localScale}");

            // Verify non-owners cannot change transform values
            nonOwnerInstance.transform.position = Vector3.zero;
            yield return s_DefaultWaitForTick;
            Assert.True(nonOwnerInstance.transform.position == valueSetByOwner, $"{networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} was allowed to change its position! Expected: {Vector3.one} Is Currently:{nonOwnerInstance.transform.position}");

            // Change ownership and wait for the non-owner to reflect the change
            VerifyObjectIsSpawnedOnClient.ResetObjectTable();
            m_ServerNetworkManager.SpawnManager.ChangeOwnership(serverSideInstance.GetComponent<NetworkObject>(), networkManagerNonOwner.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => nonOwnerInstance.GetComponent<NetworkObject>().OwnerClientId == networkManagerNonOwner.LocalClientId);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} to change ownership!");

            // Re-assign the ownership references and wait for the non-owner instance to be notified of ownership change
            networkManagerOwner = startingOwnership == StartingOwnership.HostStartsAsOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            networkManagerNonOwner = startingOwnership == StartingOwnership.HostStartsAsOwner ? m_ServerNetworkManager : m_ClientNetworkManagers[0];
            ownerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerOwner.LocalClientId);
            Assert.NotNull(ownerInstance);
            yield return WaitForConditionOrTimeOut(() => VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerNonOwner.LocalClientId) != null);
            nonOwnerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerNonOwner.LocalClientId);
            Assert.NotNull(nonOwnerInstance);

            // Make sure the owner is not kinematic and the non-owner(s) are kinematic
            Assert.False(ownerInstance.GetComponent<Rigidbody>().isKinematic, $"{networkManagerOwner.name}'s object instance {ownerInstance.name} is kinematic when it should not be!");
            Assert.True(nonOwnerInstance.GetComponent<Rigidbody>().isKinematic, $"{networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} is not kinematic when it should be!");

            // Have the new owner change transform values and wait for those values to be applied on the non-owner side.
            valueSetByOwner = Vector3.one * 50;
            ownerInstance.transform.position = valueSetByOwner;
            ownerInstance.transform.localScale = valueSetByOwner;
            rotation.eulerAngles = valueSetByOwner;
            ownerInstance.transform.rotation = rotation;
            transformToTest = nonOwnerInstance.transform;
            yield return WaitForConditionOrTimeOut(() => transformToTest.position == valueSetByOwner && transformToTest.localScale == valueSetByOwner && transformToTest.rotation == rotation);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} to change its transform!\n" +
                $"Expected Position: {valueSetByOwner} | Current Position: {transformToTest.position}\n" +
                $"Expected Rotation: {valueSetByOwner} | Current Rotation: {transformToTest.rotation.eulerAngles}\n" +
                $"Expected Scale: {valueSetByOwner} | Current Scale: {transformToTest.localScale}");

            // The last check is to verify non-owners cannot change transform values after ownership has changed
            nonOwnerInstance.transform.position = Vector3.zero;
            yield return s_DefaultWaitForTick;
            Assert.True(nonOwnerInstance.transform.position == valueSetByOwner, $"{networkManagerNonOwner.name}'s object instance {nonOwnerInstance.name} was allowed to change its position! Expected: {Vector3.one} Is Currently:{nonOwnerInstance.transform.position}");
        }

        /// <summary>
        /// This verifies that when authority is server authoritative the
        /// client's Rigidbody is kinematic and the server is not.
        /// This also verifies only the server can apply updates to the
        /// transform while the clients cannot.
        /// </summary>
        [UnityTest]
        public IEnumerator ServerAuthoritativeTest()
        {
            // Spawn the m_NetworkTransformPrefab and wait for the client-side to spawn the object
            var serverSideInstance = SpawnObject(m_NetworkTransformPrefab, m_ServerNetworkManager);
            yield return WaitForConditionOrTimeOut(() => VerifyObjectIsSpawnedOnClient.GetClientsThatSpawnedThisPrefab().Contains(m_ClientNetworkManagers[0].LocalClientId));

            var ownerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(m_ServerNetworkManager.LocalClientId);
            var nonOwnerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(m_ClientNetworkManagers[0].LocalClientId);

            // Make sure the owner is not kinematic and the non-owner(s) are kinematic
            Assert.False(ownerInstance.GetComponent<Rigidbody>().isKinematic, $"{m_ServerNetworkManager.name}'s object instance {ownerInstance.name} is kinematic when it should not be!");
            Assert.True(nonOwnerInstance.GetComponent<Rigidbody>().isKinematic, $"{m_ClientNetworkManagers[0].name}'s object instance {nonOwnerInstance.name} is not kinematic when it should be!");

            // Server changes transform values
            var valueSetByOwner = Vector3.one * 2;
            ownerInstance.transform.position = valueSetByOwner;
            ownerInstance.transform.localScale = valueSetByOwner;
            var rotation = new Quaternion();
            rotation.eulerAngles = valueSetByOwner;
            ownerInstance.transform.rotation = rotation;
            var transformToTest = nonOwnerInstance.transform;
            yield return WaitForConditionOrTimeOut(() => transformToTest.position == valueSetByOwner && transformToTest.localScale == valueSetByOwner && transformToTest.rotation == rotation);
            Assert.False(s_GlobalTimeoutHelper.TimedOut, $"Timed out waiting for {m_ClientNetworkManagers[0].name}'s object instance {nonOwnerInstance.name} to change its transform!\n" +
                $"Expected Position: {valueSetByOwner} | Current Position: {transformToTest.position}\n" +
                $"Expected Rotation: {valueSetByOwner} | Current Rotation: {transformToTest.rotation.eulerAngles}\n" +
                $"Expected Scale: {valueSetByOwner} | Current Scale: {transformToTest.localScale}");

            // The last check is to verify clients cannot change transform values
            nonOwnerInstance.transform.position = Vector3.zero;
            yield return s_DefaultWaitForTick;
            Assert.True(nonOwnerInstance.transform.position == valueSetByOwner, $"{m_ClientNetworkManagers[0].name}'s object instance {nonOwnerInstance.name} was allowed to change its position! Expected: {Vector3.one} Is Currently:{nonOwnerInstance.transform.position}");
        }

        /// <summary>
        /// NetworkTransformOwnershipTests helper behaviour
        /// </summary>
        public class VerifyObjectIsSpawnedOnClient : NetworkBehaviour
        {
            private static Dictionary<ulong, VerifyObjectIsSpawnedOnClient> s_NetworkManagerRelativeSpawnedObjects = new Dictionary<ulong, VerifyObjectIsSpawnedOnClient>();

            public static void ResetObjectTable()
            {
                s_NetworkManagerRelativeSpawnedObjects.Clear();
            }

            public override void OnGainedOwnership()
            {
                if (!s_NetworkManagerRelativeSpawnedObjects.ContainsKey(NetworkManager.LocalClientId))
                {
                    s_NetworkManagerRelativeSpawnedObjects.Add(NetworkManager.LocalClientId, this);
                }
                base.OnGainedOwnership();
            }

            public override void OnLostOwnership()
            {
                if (!s_NetworkManagerRelativeSpawnedObjects.ContainsKey(NetworkManager.LocalClientId))
                {
                    s_NetworkManagerRelativeSpawnedObjects.Add(NetworkManager.LocalClientId, this);
                }
                base.OnLostOwnership();
            }

            public static List<ulong> GetClientsThatSpawnedThisPrefab()
            {
                return s_NetworkManagerRelativeSpawnedObjects.Keys.ToList();
            }

            public static VerifyObjectIsSpawnedOnClient GetClientInstance(ulong clientId)
            {
                if (s_NetworkManagerRelativeSpawnedObjects.ContainsKey(clientId))
                {
                    return s_NetworkManagerRelativeSpawnedObjects[clientId];
                }
                return null;
            }

            public override void OnNetworkSpawn()
            {
                // This makes sure that the NetworkManager relative NetworkObject instances don't collide with each other
                // and skew the expected changes to the transforms
                foreach (var entry in s_NetworkManagerRelativeSpawnedObjects)
                {
                    Physics.IgnoreCollision(entry.Value.GetComponent<SphereCollider>(), GetComponent<SphereCollider>());
                }

                if (!s_NetworkManagerRelativeSpawnedObjects.ContainsKey(NetworkManager.LocalClientId))
                {
                    s_NetworkManagerRelativeSpawnedObjects.Add(NetworkManager.LocalClientId, this);
                }
                base.OnNetworkSpawn();
            }

            public override void OnNetworkDespawn()
            {
                if (s_NetworkManagerRelativeSpawnedObjects.ContainsKey(NetworkManager.LocalClientId))
                {
                    s_NetworkManagerRelativeSpawnedObjects.Remove(NetworkManager.LocalClientId);
                }
                base.OnNetworkDespawn();
            }
        }

        /// <summary>
        /// Until we can better locate the ClientNetworkTransform
        /// This will have to be used to verify the ownership authority
        /// </summary>
        [DisallowMultipleComponent]
        public class TestClientNetworkTransform : NetworkTransform
        {
            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();
                CanCommitToTransform = IsOwner;
            }

            protected override void Update()
            {
                CanCommitToTransform = IsOwner;
                base.Update();
                if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening))
                {
                    if (CanCommitToTransform)
                    {
                        TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
                    }
                }
            }

            protected override bool OnIsServerAuthoritative()
            {
                return false;
            }
        }
    }
}
#endif
