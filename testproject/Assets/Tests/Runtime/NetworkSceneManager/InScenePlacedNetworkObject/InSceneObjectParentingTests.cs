using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    [TestFixture(NetworkTopologyTypes.DistributedAuthority, HostOrServer.DAHost)]
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Host)]
    [TestFixture(NetworkTopologyTypes.ClientServer, HostOrServer.Server)]
    public class InSceneObjectParentingTests : InSceneObjectBase
    {
        protected override int NumberOfClients => 2;

        private const string k_InSceneUnder = "InSceneUnderGameObject";
        private const string k_InSceneUnderWithNetworkTransform = "InSceneUnderGameObjectWithNT";

        public InSceneObjectParentingTests(NetworkTopologyTypes networkTopologyType, HostOrServer hostOrServer) : base(networkTopologyType, hostOrServer) { }

        [UnityTest]
        public IEnumerator ParentedInSceneObjectLateJoiningClient()
        {
            NetworkObjectTestComponent.ServerNetworkObjectInstance = null;

            var authority = GetAuthorityNetworkManager();
            var firstClient = GetNonAuthorityNetworkManager();
            NetworkManager secondClient = m_NetworkManagers.FirstOrDefault(manager => manager != authority && manager != firstClient);
            Assert.IsNotNull(secondClient);

            authority.SceneManager.LoadScene(SceneToLoad, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(HaveAllClientsLoadedScene);
            AssertOnTimeout($"Timed out waiting for {SceneToLoad} scene to be loaded!");

            var serverInSceneObjectInstance = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            Assert.IsNotNull(serverInSceneObjectInstance, $"Could not get the server-side registration of {nameof(NetworkObjectTestComponent)}!");

            var firstClientInSceneObjectInstance = NetworkObjectTestComponent.SpawnedInstances.FirstOrDefault(c => c.NetworkManager == firstClient);
            Assert.IsNotNull(firstClientInSceneObjectInstance, $"Could not get the client-side registration of {nameof(NetworkObjectTestComponent)}!");

            Assert.IsTrue(firstClientInSceneObjectInstance.NetworkManager == firstClient);

            var playerObjectToParent = m_UseHost ? authority.LocalClient.PlayerObject : m_PlayerNetworkObjects[authority.LocalClientId][secondClient.LocalClientId];
            NetworkObject clientSidePlayer;
            if (!m_UseHost)
            {
                playerObjectToParent = m_PlayerNetworkObjects[authority.LocalClientId][secondClient.LocalClientId];
                clientSidePlayer = m_PlayerNetworkObjects[firstClient.LocalClientId][secondClient.LocalClientId];
            }
            else
            {
                clientSidePlayer = m_PlayerNetworkObjects[firstClient.LocalClientId][authority.LocalClientId];
            }

            // Parent the object
            serverInSceneObjectInstance.transform.parent = playerObjectToParent.transform;

            yield return WaitForConditionOrTimeOut(() => firstClientInSceneObjectInstance.transform.parent != null && firstClientInSceneObjectInstance.transform.parent == clientSidePlayer.transform);
            AssertOnTimeout($"Timed out waiting for the client-side id ({authority.LocalClientId}) server player transform to be set on the client-side in-scene object!");

            // Now late join a client
            var lateJoinClient = CreateNewClient();
            yield return StartClient(lateJoinClient);

            var lateJoinClientInSceneObjectInstance = NetworkObjectTestComponent.SpawnedInstances.FirstOrDefault(c => c.NetworkManager == lateJoinClient);
            Assert.IsNotNull(lateJoinClientInSceneObjectInstance, $"Could not get the client-side registration of {nameof(NetworkObjectTestComponent)} for the late joining client!");

            // Now get the late-joining client's instance for the server player
            clientSidePlayer = m_PlayerNetworkObjects[lateJoinClient.LocalClientId][clientSidePlayer.OwnerClientId];

            // Validate the late joined client's in-scene NetworkObject is parented to the server-side player
            yield return WaitForConditionOrTimeOut(() => lateJoinClientInSceneObjectInstance.transform.parent != null && lateJoinClientInSceneObjectInstance.transform.parent == clientSidePlayer.transform);
            AssertOnTimeout($"Timed out waiting for the client-side id ({firstClient.LocalClientId}) player transform to be set on the client-side in-scene object!");
        }

        public enum ParentSyncSettings
        {
            ParentSync,
            NoParentSync
        }

        public enum TransformSyncSettings
        {
            TransformSync,
            NoTransformSync
        }

        public enum TransformSpace
        {
            World,
            Local
        }

        /// <summary>
        /// This test validates the initial synchronization of an in-scene placed NetworkObject parented
        /// underneath a GameObject. There are two scenes for this tests where the child NetworkObject does
        /// and does not have a NetworkTransform component.
        /// </summary>
        /// <param name="inSceneUnderToLoad">Scene to load</param>
        /// <param name="parentSyncSettings"><see cref="NetworkObject.AutoObjectParentSync"/> settings</param>
        /// <param name="transformSyncSettings"><see cref="NetworkObject.SynchronizeTransform"/> settings</param>
        /// <param name="transformSpace"><see cref="NetworkTransform.InLocalSpace"/> setting (when available)</param>
        [UnityTest]
        public IEnumerator ParentedInSceneObjectUnderGameObject([Values(k_InSceneUnder, k_InSceneUnderWithNetworkTransform)] string inSceneUnderToLoad,
            [Values] ParentSyncSettings parentSyncSettings, [Values] TransformSyncSettings transformSyncSettings, [Values] TransformSpace transformSpace)
        {
            var useNetworkTransform = inSceneUnderToLoad == k_InSceneUnderWithNetworkTransform;

            // Note: This test is a modified copy of ParentedInSceneObjectLateJoiningClient.
            // The 1st client is being ignored in this test and the focus is primarily on the late joining
            // 2nd client after adjustments have been made to the child NetworkBehaviour and if applicable
            // NetworkTransform.

            NetworkObjectTestComponent.ServerNetworkObjectInstance = null;

            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager(0);
            var secondClient = GetNonAuthorityNetworkManager(1);

            authority.SceneManager.LoadScene(inSceneUnderToLoad, LoadSceneMode.Additive);

            yield return WaitForConditionOrTimeOut(HaveAllClientsLoadedScene);
            AssertOnTimeout($"Timed out waiting for {SceneToLoad} scene to be loaded!");

            var serverInSceneObjectInstance = NetworkObjectTestComponent.ServerNetworkObjectInstance;
            Assert.IsNotNull(serverInSceneObjectInstance, $"Could not get the server-side registration of {nameof(NetworkObjectTestComponent)}!");
            var firstClientInSceneObjectInstance = NetworkObjectTestComponent.SpawnedInstances.FirstOrDefault(c => c.NetworkManager == nonAuthority);
            Assert.IsNotNull(firstClientInSceneObjectInstance, $"Could not get the client-side registration of {nameof(NetworkObjectTestComponent)}!");
            Assert.IsTrue(firstClientInSceneObjectInstance.NetworkManager == nonAuthority);

            serverInSceneObjectInstance.AutoObjectParentSync = parentSyncSettings == ParentSyncSettings.ParentSync;
            serverInSceneObjectInstance.SynchronizeTransform = transformSyncSettings == TransformSyncSettings.TransformSync;

            var serverNetworkTransform = useNetworkTransform ? serverInSceneObjectInstance.GetComponent<NetworkTransform>() : null;
            if (useNetworkTransform)
            {
                serverNetworkTransform.InLocalSpace = transformSpace == TransformSpace.Local;
            }

            // Now late join a client
            yield return CreateAndStartNewClient();

            var lateJoinClientInSceneObjectInstance = NetworkObjectTestComponent.SpawnedInstances.FirstOrDefault(c => c.NetworkManager == secondClient);
            Assert.IsNotNull(lateJoinClientInSceneObjectInstance, $"Could not get the client-side registration of {nameof(NetworkObjectTestComponent)} for the late joining client!");

            // Now make sure the server and newly joined client transform values match.
            RotationsMatch(serverInSceneObjectInstance.transform, lateJoinClientInSceneObjectInstance.transform, transformSpace == TransformSpace.Local);
            PositionsMatch(serverInSceneObjectInstance.transform, lateJoinClientInSceneObjectInstance.transform, transformSpace == TransformSpace.Local);
            // When testing local space we also do a sanity check and validate the world space values too.
            if (transformSpace == TransformSpace.Local)
            {
                RotationsMatch(serverInSceneObjectInstance.transform, lateJoinClientInSceneObjectInstance.transform);
                PositionsMatch(serverInSceneObjectInstance.transform, lateJoinClientInSceneObjectInstance.transform);
            }
            ScaleValuesMatch(serverInSceneObjectInstance.transform, lateJoinClientInSceneObjectInstance.transform);
        }

        private void RotationsMatch(Transform transformA, Transform transformB, bool inLocalSpace = false)
        {
            var authorityEulerRotation = inLocalSpace ? transformA.localRotation.eulerAngles : transformA.rotation.eulerAngles;
            var nonAuthorityEulerRotation = inLocalSpace ? transformB.localRotation.eulerAngles : transformB.rotation.eulerAngles;
            var xIsEqual = ApproximatelyEuler(authorityEulerRotation.x, nonAuthorityEulerRotation.x);
            var yIsEqual = ApproximatelyEuler(authorityEulerRotation.y, nonAuthorityEulerRotation.y);
            var zIsEqual = ApproximatelyEuler(authorityEulerRotation.z, nonAuthorityEulerRotation.z);

            VerboseDebug($"[{transformA.gameObject.name}][X-{xIsEqual} | Y-{yIsEqual} | Z-{zIsEqual}] " +
                    $"Authority rotation {authorityEulerRotation} != [{transformB.gameObject.name}] NonAuthority rotation {nonAuthorityEulerRotation}");

            Assert.True(xIsEqual && yIsEqual && zIsEqual, $"[{transformA.gameObject.name}][X-{xIsEqual} | Y-{yIsEqual} | Z-{zIsEqual}]" +
                                                          $"Authority rotation {authorityEulerRotation} != [{transformB.gameObject.name}] NonAuthority rotation {nonAuthorityEulerRotation}");
        }

        private void PositionsMatch(Transform transformA, Transform transformB, bool inLocalSpace = false)
        {
            var authorityPosition = inLocalSpace ? transformA.localPosition : transformA.position;
            var nonAuthorityPosition = inLocalSpace ? transformB.localPosition : transformB.position;
            var xIsEqual = Approximately(authorityPosition.x, nonAuthorityPosition.x);
            var yIsEqual = Approximately(authorityPosition.y, nonAuthorityPosition.y);
            var zIsEqual = Approximately(authorityPosition.z, nonAuthorityPosition.z);

            VerboseDebug($"[{transformA.gameObject.name}] Authority position {authorityPosition} != [{transformB.gameObject.name}] NonAuthority position {nonAuthorityPosition}");

            Assert.True(xIsEqual && yIsEqual && zIsEqual, $"[{transformA.gameObject.name}] Authority position {authorityPosition} != [{transformB.gameObject.name}] NonAuthority position {nonAuthorityPosition}");
        }

        private void ScaleValuesMatch(Transform transformA, Transform transformB)
        {
            var authorityScale = transformA.localScale;
            var nonAuthorityScale = transformB.localScale;
            var xIsEqual = Approximately(authorityScale.x, nonAuthorityScale.x);
            var yIsEqual = Approximately(authorityScale.y, nonAuthorityScale.y);
            var zIsEqual = Approximately(authorityScale.z, nonAuthorityScale.z);

            VerboseDebug($"[{transformA.gameObject.name}] Authority scale {authorityScale} == [{transformB.gameObject.name}] NonAuthority scale {nonAuthorityScale}");

            Assert.True(xIsEqual && yIsEqual && zIsEqual, $"[{transformA.gameObject.name}] Authority scale {authorityScale} != [{transformB.gameObject.name}] NonAuthority scale {nonAuthorityScale}");
        }

    }
}
