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

        private GameObject m_ServerPrefab;
        private GameObject m_ClientPrefab;

        private HostAndClientPrefabHandler m_HostAndClientPrefabHandler;

        public class EmptyNetworkBehaviour : NetworkBehaviour { }

        /// <summary>
        /// PrefabHandler that tracks and separates the client GameObject from the host GameObject.
        /// Allows independent management of client and host game world while still instantiating NetworkObjects as expected.
        /// </summary>
        private class HostAndClientPrefabHandler : INetworkPrefabInstanceHandler
        {
            private readonly GameObject m_HostPrefab;
            private readonly GameObject m_ClientPrefab;

            public HostAndClientPrefabHandler(GameObject hostPrefab, GameObject clientPrefab)
            {
                m_HostPrefab = hostPrefab;
                m_ClientPrefab = clientPrefab;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                // Owner clientID
                if (ownerClientId == 0)
                {
                    return Object.Instantiate(m_ClientPrefab).GetComponent<NetworkObject>();
                }
                return Object.Instantiate(m_HostPrefab).GetComponent<NetworkObject>();
            }

            public void Destroy(NetworkObject networkObject)
            {
                Object.Destroy(networkObject.gameObject);
            }

            public void Register(NetworkManager networkManager)
            {
                networkManager.PrefabHandler.AddHandler(m_HostPrefab, this);
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
            // Full non-disabled GameObjects prefab on server side
            m_ServerPrefab = CreateNetworkObjectPrefab("ServerPrefab");
            AddChildToNetworkObject<EmptyNetworkBehaviour>(m_ServerPrefab.transform);
            AddChildToNetworkObject<EmptyNetworkBehaviour>(m_ServerPrefab.transform);
            AddChildToNetworkObject<NetworkTransform>(m_ServerPrefab.transform);

            // Mock disabled GameObjects prefab on client side
            m_ClientPrefab = CreateNetworkObjectPrefab("ClientPrefab");
            AddChildToNetworkObject<NetworkTransform>(m_ClientPrefab.transform);

            // Create and register prefab handler to handle server and client versions of prefabs
            m_HostAndClientPrefabHandler = new HostAndClientPrefabHandler(m_ServerPrefab, m_ClientPrefab);
            m_HostAndClientPrefabHandler.Register(m_ServerNetworkManager);
            foreach (var client in m_ClientNetworkManagers)
            {
                m_HostAndClientPrefabHandler.Register(client);
            }

            base.OnServerAndClientsCreated();
        }


        [UnityTest]
        public IEnumerator DisabledGameObjectErrorTest()
        {
            var instance = SpawnObject(m_ServerPrefab, m_ServerNetworkManager);
            var networkObjectInstance = instance.GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(() => ObjectSpawnedOnAllClients(networkObjectInstance.NetworkObjectId));
            AssertOnTimeout("Timed out waiting for object to spawn!");

            LogAssert.Expect(LogType.Error, "[Netcode] NetworkBehaviour index 3 was out of bounds for ClientPrefab(Clone). NetworkBehaviours must be the same, and in the same order, between server and client.");
            LogAssert.Expect(LogType.Error, "[NetworkTransformMessage][Invalid][length] Targeted NetworkTransform, NetworkBehaviourId (3), does not exist! Make sure you are not spawning NetworkObjects with disabled GameObjects that have NetworkBehaviour components on them.");

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
