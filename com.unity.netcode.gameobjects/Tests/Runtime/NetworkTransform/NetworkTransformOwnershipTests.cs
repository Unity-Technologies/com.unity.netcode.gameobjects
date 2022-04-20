using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Components;
using NUnit.Framework;
using Unity.Netcode.Samples;
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
            var clientNetworkTransform = m_ClientNetworkTransformPrefab.AddComponent<ClientNetworkTransform>();
            clientNetworkTransform.Interpolate = false;
            m_ClientNetworkTransformPrefab.AddComponent<Rigidbody>();
            m_ClientNetworkTransformPrefab.AddComponent<NetworkRigidbody>();
            m_ClientNetworkTransformPrefab.AddComponent<SphereCollider>();
            m_ClientNetworkTransformPrefab.AddComponent<VerifyObjectIsSpawnedOnClient>();

            m_NetworkTransformPrefab = CreateNetworkObjectPrefab("ServerAuthorityTest");
            var networkTransform = m_NetworkTransformPrefab.AddComponent<NetworkTransform>();
            m_NetworkTransformPrefab.AddComponent<Rigidbody>();
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
        /// This verifies that when authority is owner authoritative that
        /// we can switch between owners and the owner can update the transform.
        /// </summary>
        /// <param name="spawnWithHostOwnership">determines who starts as the owner (true): host | (false): client</param>
        [UnityTest]
        public IEnumerator OwnerAuthoritativeTest([Values] StartingOwnership spawnWithHostOwnership)
        {
            // Get the current ownership layout
            var networkManagerOwner = spawnWithHostOwnership == StartingOwnership.HostStartsAsOwner ? m_ServerNetworkManager : m_ClientNetworkManagers[0];
            var networkManagerNonOwner = spawnWithHostOwnership == StartingOwnership.HostStartsAsOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;

            // Spawn the m_ClientNetworkTransformPrefab and wait for the client-side to spawn the object
            var serverSideInstance = SpawnObject(m_ClientNetworkTransformPrefab, networkManagerOwner);
            yield return WaitForConditionOrTimeOut(() => VerifyObjectIsSpawnedOnClient.GetClientsThatSpawnedThisPrefab().Contains(m_ClientNetworkManagers[0].LocalClientId));

            // Get owner relative instances
            var ownerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerOwner.LocalClientId);
            var nonOwnerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerNonOwner.LocalClientId);
            Assert.NotNull(ownerInstance);
            Assert.NotNull(nonOwnerInstance);

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
            networkManagerOwner = spawnWithHostOwnership == StartingOwnership.HostStartsAsOwner ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;
            networkManagerNonOwner = spawnWithHostOwnership == StartingOwnership.HostStartsAsOwner ? m_ServerNetworkManager : m_ClientNetworkManagers[0];
            ownerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerOwner.LocalClientId);
            Assert.NotNull(ownerInstance);
            yield return WaitForConditionOrTimeOut(() => VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerNonOwner.LocalClientId) != null);
            nonOwnerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(networkManagerNonOwner.LocalClientId);
            Assert.NotNull(nonOwnerInstance);

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

        [UnityTest]
        public IEnumerator ServerAuthoritativeTest()
        {
            // Spawn the m_NetworkTransformPrefab and wait for the client-side to spawn the object
            var serverSideInstance = SpawnObject(m_NetworkTransformPrefab, m_ServerNetworkManager);
            yield return WaitForConditionOrTimeOut(() => VerifyObjectIsSpawnedOnClient.GetClientsThatSpawnedThisPrefab().Contains(m_ClientNetworkManagers[0].LocalClientId));

            var ownerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(m_ServerNetworkManager.LocalClientId);
            var nonOwnerInstance = VerifyObjectIsSpawnedOnClient.GetClientInstance(m_ClientNetworkManagers[0].LocalClientId);

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

        public class VerifyObjectIsSpawnedOnClient : NetworkBehaviour
        {
            private static Dictionary<ulong, VerifyObjectIsSpawnedOnClient> s_NetworkManagerRelativeSpawnedObjects = new Dictionary<ulong, VerifyObjectIsSpawnedOnClient>();

            public static void ResetObjectTable ()
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
                if(s_NetworkManagerRelativeSpawnedObjects.ContainsKey(clientId))
                {
                    return s_NetworkManagerRelativeSpawnedObjects[clientId];
                }
                return null;
            }

            public override void OnNetworkSpawn()
            {
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
    }
}
