using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class DisconnectReasonObject : NetworkBehaviour
    {

    }

    public class DisconnectReasonTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_PrefabToSpawn;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("DisconnectReasonObject");
            m_PrefabToSpawn.AddComponent<DisconnectReasonObject>();
        }

        private int disconnectCount;

        void OnClientDisconnectCallback(ulong clientId)
        {
            disconnectCount++;
            Debug.Log($"Disconnected {clientId}");
        }

        [UnityTest]
        public IEnumerator DisconnectReasonTest()
        {
            float startTime = Time.realtimeSinceStartup;

            // Add a callback for first client, when they get disconnected
            m_ClientNetworkManagers[0].OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_ClientNetworkManagers[1].OnClientDisconnectCallback += OnClientDisconnectCallback;

            // Disconnect first client, from the server
            m_ServerNetworkManager.DisconnectClient(m_ClientNetworkManagers[0].LocalClientId, "Bogus reason");
            m_ServerNetworkManager.DisconnectClient(m_ClientNetworkManagers[1].LocalClientId, "Bogus reason");

            while (disconnectCount < 2 && Time.realtimeSinceStartup < startTime + 10.0f)
            {
                yield return null;
            }

            Debug.Log($"Disconnect reason is {m_ClientNetworkManagers[0].DisconnectReason}");
            Debug.Log($"Disconnect reason is {m_ClientNetworkManagers[1].DisconnectReason}");
        }
    }
}
