using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class ParentChildDistributionTests : IntegrationTestWithApproximation
    {
        protected override int NumberOfClients => 4;

        private GameObject m_GenericPrefab;
        private ulong m_OriginalOwnerId;
        private List<NetworkObject> m_TargetSpawnedObjects = new List<NetworkObject>();
        private List<NetworkObject> m_AltTargetSpawnedObjects = new List<NetworkObject>();
        private Dictionary<ulong, List<NetworkObject>> m_AncillarySpawnedObjects = new Dictionary<ulong, List<NetworkObject>>();
        private StringBuilder m_ErrorMsg = new StringBuilder();

        // TODO: [CmbServiceTesting] Update UponDisconnect tests to work with the rust service
        protected override bool UseCMBService()
        {
            return false;
        }

        public ParentChildDistributionTests() : base(HostOrServer.DAHost)
        {
        }

        protected override IEnumerator OnTearDown()
        {
            m_TargetSpawnedObjects.Clear();
            m_AncillarySpawnedObjects.Clear();
            m_AltTargetSpawnedObjects.Clear();
            return base.OnTearDown();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_GenericPrefab = CreateNetworkObjectPrefab("GenPrefab");
            var networkObject = m_GenericPrefab.GetComponent<NetworkObject>();
            networkObject.DontDestroyWithOwner = true;
            base.OnServerAndClientsCreated();
        }

        private bool AllTargetedInstancesSpawned()
        {
            m_ErrorMsg.Clear();
            foreach (var client in m_NetworkManagers)
            {
                foreach (var spawnedObject in m_TargetSpawnedObjects)
                {
                    if (!client.SpawnManager.SpawnedObjects.ContainsKey(spawnedObject.NetworkObjectId))
                    {
                        m_ErrorMsg.AppendLine($"{client.name} has not spawned {spawnedObject.name}!");
                    }
                }
            }

            return m_ErrorMsg.Length == 0;
        }

        private bool AllAncillaryInstancesSpawned()
        {
            m_ErrorMsg.Clear();
            foreach (var client in m_NetworkManagers)
            {
                foreach (var clientObjects in m_AncillarySpawnedObjects)
                {
                    foreach (var spawnedObject in clientObjects.Value)
                    {
                        if (!client.SpawnManager.SpawnedObjects.ContainsKey(spawnedObject.NetworkObjectId))
                        {
                            m_ErrorMsg.AppendLine($"{client.name} has not spawned {spawnedObject.name}!");
                        }
                    }
                }
            }

            return m_ErrorMsg.Length == 0;
        }

        /// <summary>
        /// Validates that a new owner is assigned to all of the targeted objects
        /// </summary>
        private bool TargetedObjectsChangedOwnership()
        {
            m_ErrorMsg.Clear();
            var newOwnerId = m_OriginalOwnerId;
            foreach (var spawnedObject in m_AltTargetSpawnedObjects)
            {
                var isParentLocked = spawnedObject.transform.parent != null ? spawnedObject.transform.parent.GetComponent<NetworkObject>().IsOwnershipLocked : false;
                if (!isParentLocked && !spawnedObject.IsOwnershipLocked && spawnedObject.OwnerClientId == m_OriginalOwnerId)
                {
                    m_ErrorMsg.AppendLine($"{spawnedObject.name} still is owned by Client-{m_OriginalOwnerId}!");
                }
                else if (!isParentLocked && !spawnedObject.IsOwnershipLocked && m_OriginalOwnerId == newOwnerId)
                {
                    newOwnerId = spawnedObject.OwnerClientId;
                }
                else if ((isParentLocked || spawnedObject.IsOwnershipLocked) && spawnedObject.OwnerClientId != m_OriginalOwnerId)
                {
                    if (isParentLocked)
                    {
                        m_ErrorMsg.AppendLine($"{spawnedObject.name}'s parent was locked but its owner changed to Client-{m_OriginalOwnerId}!");
                    }
                    else
                    {
                        m_ErrorMsg.AppendLine($"{spawnedObject.name} was locked but its owner changed to Client-{m_OriginalOwnerId}!");
                    }
                }

                if (spawnedObject.OwnerClientId != newOwnerId)
                {
                    m_ErrorMsg.AppendLine($"{spawnedObject.name} is not owned by Client-{newOwnerId}!");
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        private bool AncillaryObjectsKeptOwnership()
        {
            m_ErrorMsg.Clear();
            foreach (var clientObjects in m_AncillarySpawnedObjects)
            {
                foreach (var spawnedObject in clientObjects.Value)
                {
                    if (spawnedObject.OwnerClientId != clientObjects.Key)
                    {
                        m_ErrorMsg.AppendLine($"{spawnedObject.name} changed ownership to Client-{spawnedObject.OwnerClientId}!");
                    }
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        public enum DistributionTypes
        {
            UponConnect,
            UponDisconnect
        }

        public enum OwnershipLocking
        {
            NoLocking,
            LockRootParent,
            LockTargetChild,
        }


        [UnityTest]
        public IEnumerator DistributeOwnerHierarchy([Values] DistributionTypes distributionType, [Values] OwnershipLocking ownershipLock)
        {
            m_TargetSpawnedObjects.Clear();
            m_AncillarySpawnedObjects.Clear();
            m_AltTargetSpawnedObjects.Clear();

            var clientToReconnect = m_ClientNetworkManagers[3];
            if (distributionType == DistributionTypes.UponConnect)
            {
                yield return StopOneClient(clientToReconnect);
            }

            // When testing connect redistribution
            var instances = distributionType == DistributionTypes.UponDisconnect ? 1 : 2;
            var rootObject = (GameObject)null;
            var childOne = (GameObject)null;
            var childTwo = (GameObject)null;
            var networkObject = (NetworkObject)null;

            for (int i = 0; i < instances; i++)
            {
                rootObject = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[0]);
                networkObject = rootObject.GetComponent<NetworkObject>();
                networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
                if (ownershipLock == OwnershipLocking.LockRootParent && distributionType == DistributionTypes.UponConnect)
                {
                    networkObject.SetOwnershipLock(true);
                }
                m_TargetSpawnedObjects.Add(networkObject);

                // Used to validate nested transferable transfers to the same owner
                childOne = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[0]);
                networkObject = childOne.GetComponent<NetworkObject>();
                networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
                m_TargetSpawnedObjects.Add(networkObject);

                // Used to validate nested distributable transfers to the same owner
                childTwo = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[0]);
                networkObject = childTwo.GetComponent<NetworkObject>();
                networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
                if (ownershipLock == OwnershipLocking.LockTargetChild && distributionType == DistributionTypes.UponConnect)
                {
                    networkObject.SetOwnershipLock(true);
                }
                m_TargetSpawnedObjects.Add(childTwo.GetComponent<NetworkObject>());

                childOne.transform.parent = rootObject.transform;
                childTwo.transform.parent = rootObject.transform;
            }
            yield return WaitForConditionOrTimeOut(AllTargetedInstancesSpawned);
            AssertOnTimeout($"Timed out waiting for all targeted cloned instances to spawn!\n {m_ErrorMsg}");

            // Used to validate that other children do not transfer ownership when redistributing.

            var altAchildOne = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[1]);
            var altAchildTwo = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[1]);
            m_AncillarySpawnedObjects.Add(m_ClientNetworkManagers[1].LocalClientId, new List<NetworkObject>());
            networkObject = altAchildOne.GetComponent<NetworkObject>();
            networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
            m_AncillarySpawnedObjects[m_ClientNetworkManagers[1].LocalClientId].Add(networkObject);
            altAchildOne.transform.parent = rootObject.transform;

            networkObject = altAchildTwo.GetComponent<NetworkObject>();
            networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            m_AncillarySpawnedObjects[m_ClientNetworkManagers[1].LocalClientId].Add(networkObject);
            altAchildTwo.transform.parent = rootObject.transform;

            var altBchildOne = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[2]);
            var altBchildTwo = SpawnObject(m_GenericPrefab, m_ClientNetworkManagers[2]);
            m_AncillarySpawnedObjects.Add(m_ClientNetworkManagers[2].LocalClientId, new List<NetworkObject>());
            networkObject = altBchildOne.GetComponent<NetworkObject>();
            networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable);
            m_AncillarySpawnedObjects[m_ClientNetworkManagers[2].LocalClientId].Add(networkObject);
            altBchildOne.transform.parent = rootObject.transform;

            networkObject = altBchildTwo.GetComponent<NetworkObject>();
            networkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            m_AncillarySpawnedObjects[m_ClientNetworkManagers[2].LocalClientId].Add(networkObject);
            altBchildTwo.transform.parent = rootObject.transform;

            yield return WaitForConditionOrTimeOut(AllAncillaryInstancesSpawned);
            AssertOnTimeout($"Timed out waiting for all ancillary cloned instances to spawn!\n {m_ErrorMsg}");

            // Now disconnect the client that owns the rootObject
            // Get the original clientId
            m_OriginalOwnerId = m_ClientNetworkManagers[0].LocalClientId;

            if (distributionType == DistributionTypes.UponDisconnect)
            {
                // Swap out the original owner's NetworkObject with one of the other client's since those instances will
                // be destroyed when the client disconnects.
                foreach (var entry in m_TargetSpawnedObjects)
                {
                    m_AltTargetSpawnedObjects.Add(m_ClientNetworkManagers[1].SpawnManager.SpawnedObjects[entry.NetworkObjectId]);
                }
                // Disconnect the client to trigger object redistribution
                yield return StopOneClient(m_ClientNetworkManagers[0]);
            }
            else
            {
                yield return StartClient(clientToReconnect);
                yield return WaitForConditionOrTimeOut(() => clientToReconnect.IsConnectedClient);
                AssertOnTimeout($"{clientToReconnect.name} failed to reconnect!");
            }

            // Verify all of the targeted objects changed ownership to the same client
            yield return WaitForConditionOrTimeOut(TargetedObjectsChangedOwnership);
            AssertOnTimeout($"All targeted objects did not get distributed to the same owner!\n {m_ErrorMsg}");

            // When enabled, you should see one of the two root instances that have children get distributed to
            // the reconnected client.
            if (m_EnableVerboseDebug && distributionType == DistributionTypes.UponConnect)
            {
                m_ErrorMsg.Clear();
                m_ErrorMsg.AppendLine($"Original targeted objects owner: {m_OriginalOwnerId}");
                foreach (var spawnedObject in m_TargetSpawnedObjects)
                {
                    m_ErrorMsg.AppendLine($"{spawnedObject.name} new owner: {spawnedObject.OwnerClientId}");
                }
                Debug.Log($"{m_ErrorMsg}");
            }

            // We only want to make sure no other children owned by still connected clients change ownership
            if (distributionType == DistributionTypes.UponDisconnect)
            {
                // Verify the ancillary objects kept the same ownership
                yield return WaitForConditionOrTimeOut(AncillaryObjectsKeptOwnership);
                AssertOnTimeout($"All ancillary objects did not get distributed to the same owner!\n {m_ErrorMsg}");
            }
        }
    }
}
