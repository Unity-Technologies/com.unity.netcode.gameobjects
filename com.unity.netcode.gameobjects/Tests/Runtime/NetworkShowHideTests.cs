using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;

namespace Unity.Netcode.RuntimeTests
{
    public class NetworkShowHideTestComponent : NetworkBehaviour
    {

    }

    public class ShowHideObject : NetworkBehaviour
    {
        public static List<ShowHideObject> ClientTargetedNetworkObjects = new List<ShowHideObject>();
        public static ulong ClientIdToTarget;
        public static bool Silent;

        public static NetworkObject GetNetworkObjectById(ulong networkObjectId)
        {
            foreach (var entry in ClientTargetedNetworkObjects)
            {
                if (entry.NetworkObjectId == networkObjectId)
                {
                    return entry.NetworkObject;
                }
            }
            return null;
        }

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.LocalClientId == ClientIdToTarget)
            {
                ClientTargetedNetworkObjects.Add(this);
            }

            if (IsServer)
            {
                MyListSetOnSpawn.Add(45);
            }
            else
            {
                Debug.Assert(MyListSetOnSpawn.Count == 1);
                Debug.Assert(MyListSetOnSpawn[0] == 45);
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (ClientTargetedNetworkObjects.Contains(this))
            {
                ClientTargetedNetworkObjects.Remove(this);
            }
            base.OnNetworkDespawn();
        }

        public NetworkVariable<int> MyNetworkVariable;
        public NetworkList<int> MyListSetOnSpawn;

        private void Awake()
        {
            MyNetworkVariable = new NetworkVariable<int>();
            MyNetworkVariable.OnValueChanged += Changed;

            MyListSetOnSpawn = new NetworkList<int>();
        }

        public void Changed(int before, int after)
        {
            if (!Silent)
            {
                Debug.Log($"Value changed from {before} to {after}");
            }
        }
    }

    public class NetworkShowHideTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private ulong m_ClientId0;
        private GameObject m_PrefabToSpawn;

        private NetworkObject m_NetSpawnedObject1;
        private NetworkObject m_NetSpawnedObject2;
        private NetworkObject m_NetSpawnedObject3;
        private NetworkObject m_Object1OnClient0;
        private NetworkObject m_Object2OnClient0;
        private NetworkObject m_Object3OnClient0;

        protected override void OnCreatePlayerPrefab()
        {
            var networkTransform = m_PlayerPrefab.AddComponent<NetworkShowHideTestComponent>();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("ShowHideObject");
            m_PrefabToSpawn.AddComponent<ShowHideObject>();
        }

        // Check that the first client see them, or not, as expected
        private IEnumerator CheckVisible(bool isVisible)
        {
            int count = 0;
            do
            {
                yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 5);
                count++;

                if (count > 20)
                {
                    // timeout waiting for object to reach the expect visibility
                    Assert.Fail("timeout waiting for object to reach the expect visibility");
                    break;
                }
            } while (m_NetSpawnedObject1.IsNetworkVisibleTo(m_ClientId0) != isVisible ||
                     m_NetSpawnedObject2.IsNetworkVisibleTo(m_ClientId0) != isVisible ||
                     m_NetSpawnedObject3.IsNetworkVisibleTo(m_ClientId0) != isVisible ||
                     m_Object1OnClient0.IsSpawned != isVisible ||
                     m_Object2OnClient0.IsSpawned != isVisible ||
                     m_Object3OnClient0.IsSpawned != isVisible
                     );

            Debug.Assert(m_NetSpawnedObject1.IsNetworkVisibleTo(m_ClientId0) == isVisible);
            Debug.Assert(m_NetSpawnedObject2.IsNetworkVisibleTo(m_ClientId0) == isVisible);
            Debug.Assert(m_NetSpawnedObject3.IsNetworkVisibleTo(m_ClientId0) == isVisible);

            Debug.Assert(m_Object1OnClient0.IsSpawned == isVisible);
            Debug.Assert(m_Object2OnClient0.IsSpawned == isVisible);
            Debug.Assert(m_Object3OnClient0.IsSpawned == isVisible);

            var clientNetworkManager = m_ClientNetworkManagers.Where((c) => c.LocalClientId == m_ClientId0).First();
            if (isVisible)
            {
                Assert.True(ShowHideObject.ClientTargetedNetworkObjects.Count == 3, $"Client-{clientNetworkManager.LocalClientId} should have 3 instances visible but only has {ShowHideObject.ClientTargetedNetworkObjects.Count}!");
            }
            else
            {
                Assert.True(ShowHideObject.ClientTargetedNetworkObjects.Count == 0, $"Client-{clientNetworkManager.LocalClientId} should have no visible instances but still has {ShowHideObject.ClientTargetedNetworkObjects.Count}!");
            }
        }

        // Set the 3 objects visibility
        private void Show(bool individually, bool visibility)
        {
            if (individually)
            {
                if (!visibility)
                {
                    m_NetSpawnedObject1.NetworkHide(m_ClientId0);
                    m_NetSpawnedObject2.NetworkHide(m_ClientId0);
                    m_NetSpawnedObject3.NetworkHide(m_ClientId0);
                }
                else
                {
                    m_NetSpawnedObject1.NetworkShow(m_ClientId0);
                    m_NetSpawnedObject2.NetworkShow(m_ClientId0);
                    m_NetSpawnedObject3.NetworkShow(m_ClientId0);
                }
            }
            else
            {
                var list = new List<NetworkObject>();
                list.Add(m_NetSpawnedObject1);
                list.Add(m_NetSpawnedObject2);
                list.Add(m_NetSpawnedObject3);

                if (!visibility)
                {
                    NetworkObject.NetworkHide(list, m_ClientId0);
                }
                else
                {
                    NetworkObject.NetworkShow(list, m_ClientId0);
                }
            }
        }

        private bool RefreshNetworkObjects()
        {
            m_Object1OnClient0 = ShowHideObject.GetNetworkObjectById(m_NetSpawnedObject1.NetworkObjectId);
            m_Object2OnClient0 = ShowHideObject.GetNetworkObjectById(m_NetSpawnedObject2.NetworkObjectId);
            m_Object3OnClient0 = ShowHideObject.GetNetworkObjectById(m_NetSpawnedObject3.NetworkObjectId);
            if (m_Object1OnClient0 == null || m_Object2OnClient0 == null || m_Object3OnClient0 == null)
            {
                return false;
            }
            Assert.True(m_Object1OnClient0.NetworkManagerOwner == m_ClientNetworkManagers[0]);
            Assert.True(m_Object2OnClient0.NetworkManagerOwner == m_ClientNetworkManagers[0]);
            Assert.True(m_Object3OnClient0.NetworkManagerOwner == m_ClientNetworkManagers[0]);
            return true;
        }


        [UnityTest]
        public IEnumerator NetworkShowHideTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;
            ShowHideObject.ClientTargetedNetworkObjects.Clear();
            ShowHideObject.ClientIdToTarget = m_ClientId0;


            // create 3 objects
            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject2 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject3 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();
            m_NetSpawnedObject2 = spawnedObject2.GetComponent<NetworkObject>();
            m_NetSpawnedObject3 = spawnedObject3.GetComponent<NetworkObject>();

            for (int mode = 0; mode < 2; mode++)
            {
                // get the NetworkObject on a client instance
                yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
                AssertOnTimeout($"Could not refresh all NetworkObjects!");

                // check object start visible
                yield return CheckVisible(true);

                // hide them on one client
                Show(mode == 0, false);

                yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 5);

                m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyNetworkVariable.Value = 3;

                yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 5);

                // verify they got hidden
                yield return CheckVisible(false);

                // show them to that client
                Show(mode == 0, true);
                yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
                AssertOnTimeout($"Could not refresh all NetworkObjects!");

                // verify they become visible
                yield return CheckVisible(true);
            }
        }

        [UnityTest]
        public IEnumerator NetworkShowHideQuickTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;
            ShowHideObject.ClientTargetedNetworkObjects.Clear();
            ShowHideObject.ClientIdToTarget = m_ClientId0;

            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject2 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject3 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();
            m_NetSpawnedObject2 = spawnedObject2.GetComponent<NetworkObject>();
            m_NetSpawnedObject3 = spawnedObject3.GetComponent<NetworkObject>();

            for (int mode = 0; mode < 2; mode++)
            {
                // get the NetworkObject on a client instance
                yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
                AssertOnTimeout($"Could not refresh all NetworkObjects!");

                // check object start visible
                yield return CheckVisible(true);

                // hide and show them on one client, during the same frame
                Show(mode == 0, false);
                Show(mode == 0, true);

                yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 5);
                yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
                AssertOnTimeout($"Could not refresh all NetworkObjects!");
                yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 5);

                // verify they become visible
                yield return CheckVisible(true);
            }
        }

        [UnityTest]
        public IEnumerator NetworkHideDespawnTest()
        {
            m_ClientId0 = m_ClientNetworkManagers[0].LocalClientId;
            ShowHideObject.ClientTargetedNetworkObjects.Clear();
            ShowHideObject.ClientIdToTarget = m_ClientId0;
            ShowHideObject.Silent = true;

            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject2 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            var spawnedObject3 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();
            m_NetSpawnedObject2 = spawnedObject2.GetComponent<NetworkObject>();
            m_NetSpawnedObject3 = spawnedObject3.GetComponent<NetworkObject>();

            m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyNetworkVariable.Value++;
            m_NetSpawnedObject1.NetworkHide(m_ClientId0);
            m_NetSpawnedObject1.Despawn();

            yield return NetcodeIntegrationTestHelpers.WaitForTicks(m_ServerNetworkManager, 5);

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NetworkHideChangeOwnership()
        {
            ShowHideObject.ClientTargetedNetworkObjects.Clear();
            ShowHideObject.ClientIdToTarget = m_ClientNetworkManagers[1].LocalClientId;
            ShowHideObject.Silent = true;

            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();

            m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyNetworkVariable.Value++;
            // Hide an object to a client
            m_NetSpawnedObject1.NetworkHide(m_ClientNetworkManagers[1].LocalClientId);

            yield return WaitForConditionOrTimeOut(() => ShowHideObject.ClientTargetedNetworkObjects.Count == 0);

            // Change ownership while the object is hidden to some
            m_NetSpawnedObject1.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // The two-second wait is actually needed as there's a potential warning of unhandled message after 1 second
            yield return new WaitForSeconds(1.25f);

            LogAssert.NoUnexpectedReceived();

            // Show the object again to check nothing unexpected happens
            m_NetSpawnedObject1.NetworkShow(m_ClientNetworkManagers[1].LocalClientId);

            yield return WaitForConditionOrTimeOut(() => ShowHideObject.ClientTargetedNetworkObjects.Count == 1);

            Assert.True(ShowHideObject.ClientTargetedNetworkObjects[0].OwnerClientId == m_ClientNetworkManagers[0].LocalClientId);
        }
    }
}
