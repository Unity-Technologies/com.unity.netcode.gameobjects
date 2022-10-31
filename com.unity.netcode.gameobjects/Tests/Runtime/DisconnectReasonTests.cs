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

        private bool disconnected;

        void OnClientDisconnectCallback(ulong clientId)
        {
            disconnected = true;
            Debug.Log($"Disconnected {clientId}");
        }

        [UnityTest]
        public IEnumerator DisconnectReasonTest()
        {
            float startTime = Time.realtimeSinceStartup;

            // Add a callback for first client, when they get disconnected
            m_ClientNetworkManagers[0].OnClientDisconnectCallback += OnClientDisconnectCallback;

            Debug.Log($"{m_ServerNetworkManager.ConnectedClientsIds[0]} versus {m_ClientNetworkManagers[0].LocalClientId}");

            // Disconnect first client, from the server
            m_ServerNetworkManager.DisconnectClient(m_ClientNetworkManagers[0].LocalClientId/*, "Bogus reason"*/);

            while (!disconnected && Time.realtimeSinceStartup < startTime + 10.0f)
            {
                yield return null;
            }

            Debug.Log($"Disconnect reason is {m_ClientNetworkManagers[0].DisconnectReason}");
        }
    }
}
