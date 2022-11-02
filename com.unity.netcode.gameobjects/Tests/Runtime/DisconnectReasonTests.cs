using System;
using System.Collections;
using System.Text.RegularExpressions;
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

        private int m_DisconnectCount;

        public void OnClientDisconnectCallback(ulong clientId)
        {
            m_DisconnectCount++;
            throw new SystemException("whatever");
        }

        [UnityTest]
        public IEnumerator DisconnectExceptionTest()
        {
            float startTime = Time.realtimeSinceStartup;

            // Add a callback for first client, when they get disconnected
            m_ClientNetworkManagers[0].OnClientDisconnectCallback += OnClientDisconnectCallback;
            m_ClientNetworkManagers[1].OnClientDisconnectCallback += OnClientDisconnectCallback;

            // Disconnect first client, from the server
            LogAssert.Expect(LogType.Exception, new Regex(".*whatever.*"));
            m_ServerNetworkManager.DisconnectClient(m_ClientNetworkManagers[0].LocalClientId);

            // Disconnect second client, from the server
            LogAssert.Expect(LogType.Exception, new Regex(".*whatever.*"));
            m_ServerNetworkManager.DisconnectClient(m_ClientNetworkManagers[1].LocalClientId);

            while (m_DisconnectCount < 2 && Time.realtimeSinceStartup < startTime + 10.0f)
            {
                yield return null;
            }

            Debug.Assert(m_DisconnectCount == 2);
        }
    }
}
