using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class ShowHideObject : NetworkBehaviour
    {
        public static List<ShowHideObject> ClientTargetedNetworkObjects = new List<ShowHideObject>();
        public static ulong ClientIdToTarget;
        public static bool Silent;
        public static int ValueAfterOwnershipChange = 0;
        public static Dictionary<ulong, ShowHideObject> ObjectsPerClientId = new Dictionary<ulong, ShowHideObject>();
        public static List<ulong> ClientIdsRpcCalledOn;

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

            if (ObjectsPerClientId.ContainsKey(NetworkManager.LocalClientId))
            {
                ObjectsPerClientId[NetworkManager.LocalClientId] = this;
            }
            else
            {
                ObjectsPerClientId.Add(NetworkManager.LocalClientId, this);
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
        public NetworkVariable<int> MyOwnerReadNetworkVariable;
        public NetworkList<int> MyList;
        public static NetworkManager NetworkManagerOfInterest;

        internal static int GainOwnershipCount = 0;

        private void Awake()
        {
            // Debug.Log($"Awake {NetworkManager.LocalClientId}");
            MyNetworkVariable = new NetworkVariable<int>();
            MyNetworkVariable.OnValueChanged += Changed;

            MyListSetOnSpawn = new NetworkList<int>();
            MyList = new NetworkList<int>();

            MyOwnerReadNetworkVariable = new NetworkVariable<int>(readPerm: NetworkVariableReadPermission.Owner);
            MyOwnerReadNetworkVariable.OnValueChanged += OwnerReadChanged;
        }

        public override void OnGainedOwnership()
        {
            GainOwnershipCount++;
            base.OnGainedOwnership();
        }

        public void OwnerReadChanged(int before, int after)
        {
            if (NetworkManager == NetworkManagerOfInterest)
            {
                ValueAfterOwnershipChange = after;
            }
        }

        public void Changed(int before, int after)
        {
            if (!Silent)
            {
                Debug.Log($"Value changed from {before} to {after}");
            }
        }

        [ClientRpc]
        public void SomeRandomClientRPC()
        {
            Debug.Log($"RPC called {NetworkManager.LocalClientId}");
            ClientIdsRpcCalledOn?.Add(NetworkManager.LocalClientId);
        }

        public void TriggerRpc()
        {
            SomeRandomClientRPC();
        }
    }

    public class NetworkShowHideTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 4;

        private ulong m_ClientId0;
        private GameObject m_PrefabToSpawn;

        private NetworkObject m_NetSpawnedObject1;
        private NetworkObject m_NetSpawnedObject2;
        private NetworkObject m_NetSpawnedObject3;
        private NetworkObject m_Object1OnClient0;
        private NetworkObject m_Object2OnClient0;
        private NetworkObject m_Object3OnClient0;

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
                yield return WaitForTicks(m_ServerNetworkManager, 5);
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
                var list = new List<NetworkObject>
                {
                    m_NetSpawnedObject1,
                    m_NetSpawnedObject2,
                    m_NetSpawnedObject3
                };

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

                yield return WaitForTicks(m_ServerNetworkManager, 5);

                m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyNetworkVariable.Value = 3;

                yield return WaitForTicks(m_ServerNetworkManager, 5);

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
        public IEnumerator ConcurrentShowAndHideOnDifferentObjects()
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

            // get the NetworkObject on a client instance
            yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
            AssertOnTimeout($"Could not refresh all NetworkObjects!");

            m_NetSpawnedObject1.NetworkHide(m_ClientId0);

            yield return WaitForTicks(m_ServerNetworkManager, 5);

            m_NetSpawnedObject1.NetworkShow(m_ClientId0);
            m_NetSpawnedObject2.NetworkHide(m_ClientId0);

            yield return WaitForTicks(m_ServerNetworkManager, 5);
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

                yield return WaitForTicks(m_ServerNetworkManager, 5);
                yield return WaitForConditionOrTimeOut(RefreshNetworkObjects);
                AssertOnTimeout($"Could not refresh all NetworkObjects!");
                yield return WaitForTicks(m_ServerNetworkManager, 5);

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

            yield return WaitForTicks(m_ServerNetworkManager, 5);

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

        [UnityTest]
        public IEnumerator NetworkHideChangeOwnershipNotHidden()
        {
            ShowHideObject.ClientTargetedNetworkObjects.Clear();
            ShowHideObject.ClientIdToTarget = m_ClientNetworkManagers[1].LocalClientId;
            ShowHideObject.Silent = true;

            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();

            // wait for host to have spawned and gained ownership
            while (ShowHideObject.GainOwnershipCount == 0)
            {
                yield return new WaitForSeconds(0.0f);
            }

            // change the value
            m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyOwnerReadNetworkVariable.Value++;

            // wait for three ticks
            yield return WaitForTicks(m_ServerNetworkManager, 3);

            // check we'll actually be changing owners
            Assert.False(ShowHideObject.ClientTargetedNetworkObjects[0].OwnerClientId == m_ClientNetworkManagers[0].LocalClientId);

            // only check for value change on one specific client
            ShowHideObject.NetworkManagerOfInterest = m_ClientNetworkManagers[0];

            // change ownership
            m_NetSpawnedObject1.ChangeOwnership(m_ClientNetworkManagers[0].LocalClientId);

            // wait three ticks
            yield return WaitForTicks(m_ServerNetworkManager, 3);
            yield return WaitForTicks(m_ClientNetworkManagers[0], 3);

            // verify ownership changed
            Assert.True(ShowHideObject.ClientTargetedNetworkObjects[0].OwnerClientId == m_ClientNetworkManagers[0].LocalClientId);

            // verify the expected client got the OnValueChanged. (Only client 1 sets this value)
            Assert.True(ShowHideObject.ValueAfterOwnershipChange == 1);
        }

        private string Display(NetworkList<int> list)
        {
            string message = "";
            foreach (var i in list)
            {
                message += $"{i}, ";
            }

            return message;
        }

        private void Compare(NetworkList<int> list1, NetworkList<int> list2)
        {
            if (list1.Count != list2.Count)
            {
                string message = $"{Display(list1)} versus {Display(list2)}";
                Debug.Log(message);
            }
            else
            {
                for (var i = 0; i < list1.Count; i++)
                {
                    if (list1[i] != list2[i])
                    {
                        string message = $"{Display(list1)} versus {Display(list2)}";
                        Debug.Log(message);
                        break;
                    }
                }
            }

            Debug.Assert(list1.Count == list2.Count);
        }

        private IEnumerator HideThenShowAndHideThenModifyAndShow()
        {
            Debug.Log("Hiding");
            // hide
            m_NetSpawnedObject1.NetworkHide(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);
            yield return WaitForTicks(m_ClientNetworkManagers[0], 3);

            Debug.Log("Showing and Hiding");
            // show and hide
            m_NetSpawnedObject1.NetworkShow(1);
            m_NetSpawnedObject1.NetworkHide(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);
            yield return WaitForTicks(m_ClientNetworkManagers[0], 3);

            Debug.Log("Modifying and Showing");
            // modify and show
            m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyList.Add(5);
            m_NetSpawnedObject1.NetworkShow(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);
            yield return WaitForTicks(m_ClientNetworkManagers[0], 3);
        }


        private IEnumerator HideThenModifyAndShow()
        {
            // hide
            m_NetSpawnedObject1.NetworkHide(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);

            // modify
            m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyList.Add(5);
            // show
            m_NetSpawnedObject1.NetworkShow(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);
            yield return WaitForTicks(m_ClientNetworkManagers[0], 3);

        }

        private IEnumerator HideThenShowAndModify()
        {
            // hide
            m_NetSpawnedObject1.NetworkHide(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);

            // show
            m_NetSpawnedObject1.NetworkShow(1);
            // modify
            m_NetSpawnedObject1.GetComponent<ShowHideObject>().MyList.Add(5);
            yield return WaitForTicks(m_ServerNetworkManager, 3);
            yield return WaitForTicks(m_ClientNetworkManagers[0], 3);
        }

        private IEnumerator HideThenShowAndRPC()
        {
            // hide
            m_NetSpawnedObject1.NetworkHide(1);
            yield return WaitForTicks(m_ServerNetworkManager, 3);

            // show
            m_NetSpawnedObject1.NetworkShow(1);
            m_NetSpawnedObject1.GetComponent<ShowHideObject>().TriggerRpc();
            yield return WaitForTicks(m_ServerNetworkManager, 3);
        }

        [UnityTest]
        public IEnumerator NetworkShowHideAroundListModify()
        {
            ShowHideObject.ClientTargetedNetworkObjects.Clear();
            ShowHideObject.ClientIdToTarget = m_ClientNetworkManagers[1].LocalClientId;
            ShowHideObject.Silent = true;

            var spawnedObject1 = SpawnObject(m_PrefabToSpawn, m_ServerNetworkManager);
            m_NetSpawnedObject1 = spawnedObject1.GetComponent<NetworkObject>();

            // wait for host to have spawned and gained ownership
            while (ShowHideObject.GainOwnershipCount == 0)
            {
                yield return new WaitForSeconds(0.0f);
            }

            for (int i = 0; i < 4; i++)
            {
                // wait for three ticks
                yield return WaitForTicks(m_ServerNetworkManager, 3);
                yield return WaitForTicks(m_ClientNetworkManagers[0], 3);

                switch (i)
                {
                    case 0:
                        Debug.Log("Running HideThenModifyAndShow");
                        yield return HideThenModifyAndShow();
                        break;
                    case 1:
                        Debug.Log("Running HideThenShowAndModify");
                        yield return HideThenShowAndModify();
                        break;
                    case 2:
                        Debug.Log("Running HideThenShowAndHideThenModifyAndShow");
                        yield return HideThenShowAndHideThenModifyAndShow();
                        break;
                    case 3:
                        Debug.Log("Running HideThenShowAndRPC");
                        ShowHideObject.ClientIdsRpcCalledOn = new List<ulong>();
                        yield return HideThenShowAndRPC();
                        // Provide enough time for slower systems or VM systems possibly under a heavy load could fail on this test
                        yield return WaitForConditionOrTimeOut(() => ShowHideObject.ClientIdsRpcCalledOn.Count == NumberOfClients + 1);
                        AssertOnTimeout($"Timed out waiting for ClientIdsRpcCalledOn.Count ({ShowHideObject.ClientIdsRpcCalledOn.Count}) to equal ({NumberOfClients + 1})!");
                        break;

                }

                Compare(ShowHideObject.ObjectsPerClientId[0].MyList, ShowHideObject.ObjectsPerClientId[1].MyList);
                Compare(ShowHideObject.ObjectsPerClientId[0].MyList, ShowHideObject.ObjectsPerClientId[2].MyList);
            }
        }

        private GameObject m_OwnershipObject;
        private NetworkObject m_OwnershipNetworkObject;
        private NetworkManager m_NewOwner;
        private ulong m_ObjectId;


        private bool AllObjectsSpawnedOnClients()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_OwnershipNetworkObject.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool ObjectHiddenOnNonAuthorityClients()
        {
            foreach (var networkManager in m_ClientNetworkManagers)
            {
                if (networkManager.LocalClientId == m_OwnershipNetworkObject.OwnerClientId)
                {
                    continue;
                }
                if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_OwnershipNetworkObject.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool OwnershipHasChanged()
        {
            if (!m_NewOwner.SpawnManager.SpawnedObjects.ContainsKey(m_ObjectId))
            {
                return false;
            }
            return m_NewOwner.SpawnManager.SpawnedObjects[m_ObjectId].OwnerClientId == m_NewOwner.LocalClientId;
        }

        /// <summary>
        /// Validates when invoking NetworkObject.NetworkShow and NetworkObject.ChangeOwnership
        /// back-to-back it will not attempt to send a change ownership message since the visibility
        /// message (CreateObjectMessage) is deferred until the end of the frame.
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator NetworkShowAndChangeOwnership()
        {
            var authority = m_ServerNetworkManager;

            m_OwnershipObject = SpawnObject(m_PrefabToSpawn, authority);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(AllObjectsSpawnedOnClients);
            AssertOnTimeout("Timed out waiting for all clients to spawn the ownership object!");

            VerboseDebug($"Hiding object {m_OwnershipNetworkObject.NetworkObjectId} on all clients");
            foreach (var client in m_ClientNetworkManagers)
            {
                m_OwnershipNetworkObject.NetworkHide(client.LocalClientId);
            }

            yield return WaitForConditionOrTimeOut(ObjectHiddenOnNonAuthorityClients);
            AssertOnTimeout("Timed out waiting for all clients to hide the ownership object!");

            m_NewOwner = m_ClientNetworkManagers[0];
            Assert.AreNotEqual(m_OwnershipNetworkObject.OwnerClientId, m_NewOwner.LocalClientId, $"Client-{m_NewOwner.LocalClientId} should not have ownership of object {m_OwnershipNetworkObject.NetworkObjectId}!");
            Assert.False(m_NewOwner.SpawnManager.SpawnedObjects.ContainsKey(m_OwnershipNetworkObject.NetworkObjectId), $"Client-{m_NewOwner.LocalClientId} should not have object {m_OwnershipNetworkObject.NetworkObjectId} spawned!");

            // Run NetworkShow and ChangeOwnership directly after one-another
            VerboseDebug($"Calling {nameof(NetworkObject.NetworkShow)} on object {m_OwnershipNetworkObject.NetworkObjectId} for client {m_NewOwner.LocalClientId}");
            m_OwnershipNetworkObject.NetworkShow(m_NewOwner.LocalClientId);
            VerboseDebug($"Calling {nameof(NetworkObject.ChangeOwnership)} on object {m_OwnershipNetworkObject.NetworkObjectId} for client {m_NewOwner.LocalClientId}");
            m_OwnershipNetworkObject.ChangeOwnership(m_NewOwner.LocalClientId);
            m_ObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            yield return WaitForConditionOrTimeOut(OwnershipHasChanged);
            AssertOnTimeout($"Timed out waiting for clients-{m_NewOwner.LocalClientId} to gain ownership of object {m_OwnershipNetworkObject.NetworkObjectId}!");
            VerboseDebug($"Client {m_NewOwner.LocalClientId} now owns object {m_OwnershipNetworkObject.NetworkObjectId}!");
        }
    }
}
