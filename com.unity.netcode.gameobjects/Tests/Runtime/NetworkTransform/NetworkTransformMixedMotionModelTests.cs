#if COM_UNITY_MODULES_PHYSICS
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkTransformMixedMotionModelTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_MixedMotionModelPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_MixedMotionModelPrefab = CreateNetworkObjectPrefab("MixedMotionModel");

            // The root is owner authoritative and driven by the rigidbody, which places it in the fixed update registration
            var rootNetworkTransform = m_MixedMotionModelPrefab.AddComponent<NetworkTransform>();
            rootNetworkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            var rigidbody = m_MixedMotionModelPrefab.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.detectCollisions = false;
            m_MixedMotionModelPrefab.AddComponent<NetworkRigidbody>().UseRigidBodyForMotion = true;

            // The nested child is server authoritative and driven by the transform, which places it in the update registration
            var childGameObject = new GameObject();
            childGameObject.transform.parent = m_MixedMotionModelPrefab.transform;
            var childNetworkTransform = childGameObject.AddComponent<NetworkTransform>();
            childNetworkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            childNetworkTransform.InLocalSpace = true;

            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// A NetworkObject that mixes both the authority motion model and the rigidbody motion model has each nested
        /// NetworkTransform registered under a different update. Gaining authority over the instance in one update
        /// should not leave it registered for the other.
        /// </summary>
        [UnityTest]
        public IEnumerator UpdateRegistrationFollowsMotionModel()
        {
            var instance = SpawnObject(m_MixedMotionModelPrefab, m_ServerNetworkManager).GetComponent<NetworkObject>();
            yield return WaitForSpawnedOnAllOrTimeOut(instance);
            AssertOnTimeout($"Failed to spawn {instance.name} on all clients!");

            var newOwner = m_ClientNetworkManagers[0];

            // Establish the baseline before ownership is transferred, otherwise the check below would still pass if this instance was never registered for the fixed update to begin with.
            Assert.True(newOwner.NetworkTransformFixedUpdate.ContainsKey(instance.NetworkObjectId), $"Client-{newOwner.LocalClientId} should initially be registered for the fixed update!");

            instance.ChangeOwnership(newOwner.LocalClientId);
            yield return WaitForConditionOrTimeOut(() => newOwner.SpawnManager.SpawnedObjects[instance.NetworkObjectId].OwnerClientId == newOwner.LocalClientId);
            AssertOnTimeout($"Client-{newOwner.LocalClientId} never gained ownership of {instance.name}!");

            // The new owner is the authority for the rigidbody driven root, so nothing on this instance needs the fixed
            // update any longer. The server authoritative child still needs the standard update.
            Assert.False(newOwner.NetworkTransformFixedUpdate.ContainsKey(instance.NetworkObjectId), $"Client-{newOwner.LocalClientId} is still registered for the fixed update!");
            Assert.True(newOwner.NetworkTransformUpdate.ContainsKey(instance.NetworkObjectId), $"Client-{newOwner.LocalClientId} is not registered for the update!");
        }
    }
}
#endif
