using System.Collections;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;


namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkTransformErrorTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_AuthorityPrefab;
        private GameObject m_NonAuthorityPrefab;

        private HostAndClientPrefabHandler m_HostAndClientPrefabHandler;

        public class EmptyNetworkBehaviour : NetworkBehaviour { }

        /// <summary>
        /// PrefabHandler that tracks and separates the client GameObject from the host GameObject.
        /// Allows independent management of client and host game world while still instantiating NetworkObjects as expected.
        /// </summary>
        private class HostAndClientPrefabHandler : INetworkPrefabInstanceHandler
        {
            /// <summary>
            /// The registered prefab is the prefab the networking stack is instantiated with.
            /// Registering the prefab simulates the prefab that exists on the authority.
            /// </summary>
            private readonly GameObject m_RegisteredPrefab;

            /// <summary>
            /// Mocks the registered prefab changing on the non-authority after registration.
            /// Allows testing situations mismatched GameObject state between the authority and non-authority.
            /// </summary>
            private readonly GameObject m_InstantiatedPrefab;

            public HostAndClientPrefabHandler(GameObject authorityPrefab, GameObject nonAuthorityPrefab)
            {
                m_RegisteredPrefab = authorityPrefab;
                m_InstantiatedPrefab = nonAuthorityPrefab;
            }

            /// <summary>
            /// Returns the prefab that will mock the instantiated prefab not matching the registered prefab
            /// </summary>
            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                return Object.Instantiate(m_InstantiatedPrefab).GetComponent<NetworkObject>();
            }

            public void Destroy(NetworkObject networkObject)
            {
                Object.Destroy(networkObject.gameObject);
            }

            public void Register(NetworkManager networkManager)
            {
                // Register the version that will be spawned by the authority (i.e. Host)
                networkManager.PrefabHandler.AddHandler(m_RegisteredPrefab, this);
            }
        }

        /// <summary>
        /// Creates a GameObject and sets the transform parent to the given transform
        /// Adds a component of the given type to the GameObject
        /// </summary>
        private static void AddChildToNetworkObject<T>(Transform transform) where T : Component
        {
            var gameObj = new GameObject();
            gameObj.transform.parent = transform;
            gameObj.AddComponent<T>();
        }

        protected override void OnServerAndClientsCreated()
        {
            // Create a prefab that has many child NetworkBehaviours
            m_AuthorityPrefab = CreateNetworkObjectPrefab("AuthorityPrefab");
            AddChildToNetworkObject<EmptyNetworkBehaviour>(m_AuthorityPrefab.transform);
            AddChildToNetworkObject<EmptyNetworkBehaviour>(m_AuthorityPrefab.transform);
            AddChildToNetworkObject<NetworkTransform>(m_AuthorityPrefab.transform);

            // Create a second prefab with only one NetworkBehaviour
            // This simulates the GameObjects on the other NetworkBehaviours being disabled
            m_NonAuthorityPrefab = CreateNetworkObjectPrefab("NonAuthorityPrefab");
            AddChildToNetworkObject<NetworkTransform>(m_NonAuthorityPrefab.transform);

            // Create and register a prefab handler
            // The prefab handler will behave as if the GameObjects have been disabled on the non-authority client
            m_HostAndClientPrefabHandler = new HostAndClientPrefabHandler(m_AuthorityPrefab, m_NonAuthorityPrefab);
            m_HostAndClientPrefabHandler.Register(m_ServerNetworkManager);
            foreach (var client in m_ClientNetworkManagers)
            {
                m_HostAndClientPrefabHandler.Register(client);
            }

            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Validates the fix where <see cref="NetworkTransformMessage"/> would throw an exception
        /// if a user sets a <see cref="GameObject"/> with one or more <see cref="NetworkBehaviour"/> components
        /// to inactive.
        /// </summary>
        [UnityTest]
        public IEnumerator DisabledGameObjectErrorTest()
        {
            var instance = SpawnObject(m_AuthorityPrefab, m_ServerNetworkManager);
            var networkObjectInstance = instance.GetComponent<NetworkObject>();
            var networkTransformInstance = instance.GetComponentInChildren<NetworkTransform>();

            yield return WaitForConditionOrTimeOut(() => ObjectSpawnedOnAllClients(networkObjectInstance.NetworkObjectId));
            AssertOnTimeout("Timed out waiting for object to spawn!");

            var errorMessage = $"[Netcode] {nameof(NetworkBehaviour)} index {networkTransformInstance.NetworkBehaviourId} was out of bounds for {m_NonAuthorityPrefab.name}(Clone). " +
                $"{nameof(NetworkBehaviour)}s must be the same, and in the same order, between server and client.";
            LogAssert.Expect(LogType.Error, errorMessage);
            errorMessage = $"[{nameof(NetworkTransformMessage)}][Invalid] Targeted {nameof(NetworkTransform)}, {nameof(NetworkBehaviour.NetworkBehaviourId)} " +
                $"({networkTransformInstance.NetworkBehaviourId}), does not exist! Make sure you are not spawning {nameof(NetworkObject)}s with disabled {nameof(GameObject)}s that have " +
                $"{nameof(NetworkBehaviour)} components on them.";
            LogAssert.Expect(LogType.Error, errorMessage);

            yield return new WaitForSeconds(0.3f);
        }

        private bool ObjectSpawnedOnAllClients(ulong networkObjectId)
        {
            foreach (var client in m_ClientNetworkManagers)
            {
                if (!client.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
                {
                    return false;
                }
            }
            return true;
        }
    }

}
