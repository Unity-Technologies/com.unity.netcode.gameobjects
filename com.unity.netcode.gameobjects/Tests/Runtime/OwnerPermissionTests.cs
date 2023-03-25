using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class OwnerPermissionObject : NetworkBehaviour
    {
        // indexed by [object, machine]
        public static OwnerPermissionObject[,] Objects = new OwnerPermissionObject[3, 3];
        public static int CurrentlySpawning = 0;

        public static List<OwnerPermissionObject> ClientTargetedNetworkObjects = new List<OwnerPermissionObject>();
        // a client-owned NetworkVariable
        public NetworkVariable<int> MyNetworkVariableOwner;
        // a server-owned NetworkVariable
        public NetworkVariable<int> MyNetworkVariableServer;

        // a client-owned NetworkVariable
        public NetworkList<int> MyNetworkListOwner;
        // a server-owned NetworkVariable
        public NetworkList<int> MyNetworkListServer;

        // verifies two lists are identical
        public static void CheckLists(NetworkList<int> listA, NetworkList<int> listB)
        {
            Debug.Assert(listA.Count == listB.Count);
            for (var i = 0; i < listA.Count; i++)
            {
                Debug.Assert(listA[i] == listB[i]);
            }
        }

        // verifies all objects have consistent lists on all clients
        public static void VerifyConsistency()
        {
            for (var objectIndex = 0; objectIndex < 3; objectIndex++)
            {
                CheckLists(Objects[objectIndex, 0].MyNetworkListOwner, Objects[objectIndex, 1].MyNetworkListOwner);
                CheckLists(Objects[objectIndex, 0].MyNetworkListOwner, Objects[objectIndex, 2].MyNetworkListOwner);

                CheckLists(Objects[objectIndex, 0].MyNetworkListServer, Objects[objectIndex, 1].MyNetworkListServer);
                CheckLists(Objects[objectIndex, 0].MyNetworkListServer, Objects[objectIndex, 2].MyNetworkListServer);
            }
        }

        public override void OnNetworkSpawn()
        {
            Objects[CurrentlySpawning, NetworkManager.LocalClientId] = GetComponent<OwnerPermissionObject>();
            Debug.Log($"Object index ({CurrentlySpawning}) spawned on client {NetworkManager.LocalClientId}");
        }

        private void Awake()
        {
            MyNetworkVariableOwner = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
            MyNetworkVariableOwner.OnValueChanged += OwnerChanged;

            MyNetworkVariableServer = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
            MyNetworkVariableServer.OnValueChanged += ServerChanged;

            MyNetworkListOwner = new NetworkList<int>(writePerm: NetworkVariableWritePermission.Owner);
            MyNetworkListOwner.OnListChanged += ListOwnerChanged;

            MyNetworkListServer = new NetworkList<int>(writePerm: NetworkVariableWritePermission.Server);
            MyNetworkListServer.OnListChanged += ListServerChanged;
        }

        public void OwnerChanged(int before, int after)
        {
        }

        public void ServerChanged(int before, int after)
        {
        }

        public void ListOwnerChanged(NetworkListEvent<int> listEvent)
        {
        }

        public void ListServerChanged(NetworkListEvent<int> listEvent)
        {
        }
    }

    public class OwnerPermissionHideTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;

        private GameObject m_PrefabToSpawn;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("OwnerPermissionObject");
            m_PrefabToSpawn.AddComponent<OwnerPermissionObject>();
        }

        [UnityTest]
        public IEnumerator OwnerPermissionTest()
        {
            // create 3 objects
            for (var objectIndex = 0; objectIndex < 3; objectIndex++)
            {
                OwnerPermissionObject.CurrentlySpawning = objectIndex;

                NetworkManager ownerManager = m_ServerNetworkManager;
                if (objectIndex != 0)
                {
                    ownerManager = m_ClientNetworkManagers[objectIndex - 1];
                }
                SpawnObject(m_PrefabToSpawn, ownerManager);

                // wait for each object to spawn on each client
                for (var clientIndex = 0; clientIndex < 3; clientIndex++)
                {
                    while (OwnerPermissionObject.Objects[objectIndex, clientIndex] == null)
                    {
                        yield return new WaitForSeconds(0.0f);
                    }
                }
            }

            var nextValueToWrite = 1;
            var serverIndex = 0;

            for (var objectIndex = 0; objectIndex < 3; objectIndex++)
            {
                for (var clientWriting = 0; clientWriting < 3; clientWriting++)
                {
                    // ==== Server-writable NetworkVariable ====
                    var gotException = false;
                    Debug.Log($"Writing to server-write variable on object {objectIndex} on client {clientWriting}");

                    try
                    {
                        nextValueToWrite++;
                        OwnerPermissionObject.Objects[objectIndex, clientWriting].MyNetworkVariableServer.Value = nextValueToWrite;
                    }
                    catch (Exception)
                    {
                        gotException = true;
                    }

                    // Verify server-owned netvar can only be written by server
                    Debug.Assert(gotException == (clientWriting != serverIndex));

                    // ==== Owner-writable NetworkVariable ====
                    gotException = false;
                    Debug.Log($"Writing to owner-write variable on object {objectIndex} on client {clientWriting}");

                    try
                    {
                        nextValueToWrite++;
                        OwnerPermissionObject.Objects[objectIndex, clientWriting].MyNetworkVariableOwner.Value = nextValueToWrite;
                    }
                    catch (Exception)
                    {
                        gotException = true;
                    }

                    // Verify client-owned netvar can only be written by owner
                    Debug.Assert(gotException == (clientWriting != objectIndex));

                    // ==== Server-writable NetworkList ====
                    gotException = false;
                    Debug.Log($"Writing to server-write list on object {objectIndex} on client {clientWriting}");

                    try
                    {
                        nextValueToWrite++;
                        OwnerPermissionObject.Objects[objectIndex, clientWriting].MyNetworkListServer.Add(nextValueToWrite);
                    }
                    catch (Exception)
                    {
                        gotException = true;
                    }

                    // Verify server-owned networkList can only be written by server
                    Debug.Assert(gotException == (clientWriting != serverIndex));

                    // ==== Owner-writable NetworkList ====
                    gotException = false;
                    Debug.Log($"Writing to owner-write list on object {objectIndex} on client {clientWriting}");

                    try
                    {
                        nextValueToWrite++;
                        OwnerPermissionObject.Objects[objectIndex, clientWriting].MyNetworkListOwner.Add(nextValueToWrite);
                    }
                    catch (Exception)
                    {
                        gotException = true;
                    }

                    // Verify client-owned networkList can only be written by owner
                    Debug.Assert(gotException == (clientWriting != objectIndex));

                    yield return WaitForTicks(m_ServerNetworkManager, 5);
                    yield return WaitForTicks(m_ClientNetworkManagers[0], 5);
                    yield return WaitForTicks(m_ClientNetworkManagers[1], 5);

                    OwnerPermissionObject.VerifyConsistency();
                }
            }
        }
    }
}
