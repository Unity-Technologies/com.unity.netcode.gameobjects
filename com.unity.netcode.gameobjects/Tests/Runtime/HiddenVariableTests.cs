using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class HiddenVariableTest : NetworkBehaviour
    {

    }

    public class HiddenVariableObject : NetworkBehaviour
    {
        public NetworkVariable<int> MyNetworkVariable = new NetworkVariable<int>();
        public static int ChangeCount = 0;

        public override void OnNetworkSpawn()
        {
            Debug.Log($"HiddenVariableObject.OnNetworkSpawn()");

            MyNetworkVariable.Settings.SendNetworkChannel = NetworkChannel.NetworkVariable;
            MyNetworkVariable.Settings.SendTickrate = 10;
            MyNetworkVariable.OnValueChanged += Changed;

            base.OnNetworkSpawn();
        }

        public void Changed(int before, int after)
        {
            Debug.Log($"Value changed from {before} to {after} on {NetworkManager.LocalClientId}");
            ChangeCount++;
        }
    }

    public class HiddenVariableTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 4;

        private NetworkObject m_NetSpawnedObject;
        private GameObject m_TestNetworkPrefab;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: true, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var networkTransform = playerPrefab.AddComponent<HiddenVariableTest>();
                    m_TestNetworkPrefab = PreparePrefab();
                });
        }

        public GameObject PreparePrefab()
        {
            var prefabToSpawn = new GameObject("MyTestObject");
            var networkObjectPrefab = prefabToSpawn.AddComponent<NetworkObject>();
            MultiInstanceHelpers.MakeNetworkedObjectTestPrefab(networkObjectPrefab);
            prefabToSpawn.AddComponent<HiddenVariableObject>();

            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            foreach (var clientNetworkManager in m_ClientNetworkManagers)
            {
                clientNetworkManager.NetworkConfig.NetworkPrefabs.Add(new NetworkPrefab() { Prefab = prefabToSpawn });
            }
            return prefabToSpawn;
        }

        public IEnumerator WaitForConnectedCount(int targetCount)
        {
            var endTime = Time.realtimeSinceStartup + 1.0;
            while (m_ServerNetworkManager.ConnectedClientsList.Count < targetCount && Time.realtimeSinceStartup < endTime)
            {
                yield return new WaitForSeconds(0.01f);
            }
        }

        public IEnumerator WaitForChangeCount(int targetCount)
        {
            var endTime = Time.realtimeSinceStartup + 1.0;
            while (HiddenVariableObject.ChangeCount != targetCount && Time.realtimeSinceStartup < endTime)
            {
                yield return new WaitForSeconds(0.01f);
            }
        }

        [UnityTest]
        public IEnumerator HiddenVariableTest()
        {
            Debug.Log("Running test");

            var spawnedObject = Object.Instantiate(m_TestNetworkPrefab);
            m_NetSpawnedObject = spawnedObject.GetComponent<NetworkObject>();
            m_NetSpawnedObject.NetworkManagerOwner = m_ServerNetworkManager;
            yield return WaitForConnectedCount(NbClients);

            // Spawn object with ownership on one client
            var client = m_ServerNetworkManager.ConnectedClientsList[1];
            var otherClient = m_ServerNetworkManager.ConnectedClientsList[2];
            m_NetSpawnedObject.SpawnWithOwnership(client.ClientId);

            // Set the NetworkVariable value to 2
            HiddenVariableObject.ChangeCount = 0;
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkVariable.Value = 2;
            yield return WaitForChangeCount(NbClients + 1);
            Debug.Assert(HiddenVariableObject.ChangeCount == NbClients + 1);

            // Hide our object to a different client
            HiddenVariableObject.ChangeCount = 0;
            m_NetSpawnedObject.NetworkHide(otherClient.ClientId);

            // Change the NetworkVariable value
            // we should get one less notification of value changing and no errors or exception
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkVariable.Value = 3;
            yield return new WaitForSeconds(1.0f);
            Debug.Assert(HiddenVariableObject.ChangeCount == NbClients);

            // Show our object again to this client
            HiddenVariableObject.ChangeCount = 0;
            m_NetSpawnedObject.NetworkShow(otherClient.ClientId);

            // Change the NetworkVariable value
            // we should get all notifications of value changing and no errors or exception
            m_NetSpawnedObject.GetComponent<HiddenVariableObject>().MyNetworkVariable.Value = 4;
            yield return WaitForChangeCount(NbClients + 1);
            Debug.Assert(HiddenVariableObject.ChangeCount == NbClients + 1);

            // Hide our object to that different client again, and then destroy it
            m_NetSpawnedObject.NetworkHide(otherClient.ClientId);
            yield return new WaitForSeconds(0.2f);
            m_NetSpawnedObject.Despawn();
            yield return new WaitForSeconds(0.2f);
        }
    }
}
