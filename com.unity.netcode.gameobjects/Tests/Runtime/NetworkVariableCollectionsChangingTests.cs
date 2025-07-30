using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host, CollectionTypes.List)]
    [TestFixture(HostOrServer.Host, CollectionTypes.Dictionary)]
    [TestFixture(HostOrServer.Server, CollectionTypes.List)]
    [TestFixture(HostOrServer.Server, CollectionTypes.Dictionary)]
    public class NetworkVariableCollectionsChangingTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 2;
        public enum CollectionTypes
        {
            Dictionary,
            List,
        }
        private StringBuilder m_ErrorLog = new StringBuilder();
        private CollectionTypes m_CollectionType;
        private GameObject m_TestPrefab;
        private NetworkObject m_Instance;

        public NetworkVariableCollectionsChangingTests(HostOrServer hostOrServer, CollectionTypes collectionType) : base(hostOrServer)
        {
            m_CollectionType = collectionType;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("TestObject");
            if (m_CollectionType == CollectionTypes.Dictionary)
            {
                m_TestPrefab.AddComponent<DictionaryCollectionUpdateHelper>();
            }
            else
            {
                m_TestPrefab.AddComponent<ListCollectionUpdateHelper>();
            }
            base.OnServerAndClientsCreated();
        }

        private bool AllInstancesSpawned()
        {
            if (!m_ServerNetworkManager.SpawnManager.SpawnedObjects.ContainsKey(m_Instance.NetworkObjectId))
            {
                return false;
            }

            foreach (var client in m_ClientNetworkManagers)
            {
                if (!client.SpawnManager.SpawnedObjects.ContainsKey(m_Instance.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        private Dictionary<ulong, NetworkManager> m_NetworkManagers = new Dictionary<ulong, NetworkManager>();

        private bool ValidateAllInstances()
        {
            if (!m_NetworkManagers.ContainsKey(m_Instance.OwnerClientId))
            {
                return false;
            }

            if (!m_NetworkManagers[m_Instance.OwnerClientId].SpawnManager.SpawnedObjects.ContainsKey(m_Instance.NetworkObjectId))
            {
                return false;
            }

            var ownerNetworkManager = m_NetworkManagers[m_Instance.OwnerClientId];

            var ownerClientInstance = m_NetworkManagers[m_Instance.OwnerClientId].SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();

            foreach (var client in m_NetworkManagers)
            {
                if (client.Value == ownerNetworkManager)
                {
                    continue;
                }

                var otherInstance = client.Value.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();
                if (!ownerClientInstance.ValidateAgainst(otherInstance))
                {
                    return false;
                }
            }
            return true;
        }

        private bool ValidateAllValueChangedEqual()
        {
            if (!m_NetworkManagers.TryGetValue(m_Instance.OwnerClientId, out var ownerNetworkManager))
            {
                return false;
            }

            if (!ownerNetworkManager.SpawnManager.SpawnedObjects.TryGetValue(m_Instance.NetworkObjectId, out var ownerObject))
            {
                return false;
            }


            var ownerClientInstance = ownerObject.GetComponent<BaseCollectionUpdateHelper>();

            foreach (var client in m_NetworkManagers)
            {
                if (client.Value == ownerNetworkManager)
                {
                    continue;
                }

                var otherInstance = client.Value.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();
                if (ownerClientInstance.OnValueChangedCount() != otherInstance.OnValueChangedCount())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// With CheckDirtyState(true), a change back to the original value should trigger an extra OnValueChanged call on the authority instance.
        /// </summary>
        private bool ValidateOwnerHasExtraValueChangedCall()
        {
            if (!m_NetworkManagers.TryGetValue(m_Instance.OwnerClientId, out var ownerNetworkManager))
            {
                return false;
            }

            if (!ownerNetworkManager.SpawnManager.SpawnedObjects.TryGetValue(m_Instance.NetworkObjectId, out var ownerObject))
            {
                return false;
            }


            var ownerClientInstance = ownerObject.GetComponent<BaseCollectionUpdateHelper>();

            // With forced check, owner should have an extra OnValueChanged callback
            var expectedClientCallbackCount = ownerClientInstance.OnValueChangedCount() - 1;

            foreach (var client in m_NetworkManagers)
            {
                if (client.Value == ownerNetworkManager)
                {
                    continue;
                }

                var otherInstance = client.Value.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();
                if (otherInstance.OnValueChangedCount() != expectedClientCallbackCount)
                {
                    return false;
                }
            }
            return true;
        }


        private bool OwnershipChangedOnAllClients(ulong expectedOwner)
        {
            m_ErrorLog.Clear();
            foreach (var client in m_NetworkManagers)
            {
                var otherInstance = client.Value.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();
                if (otherInstance.OwnerClientId != expectedOwner)
                {
                    m_ErrorLog.AppendLine($"Client-{client.Value.LocalClientId} instance of {m_Instance.name} still shows the owner is Client-{otherInstance.OwnerClientId} when it should be Client-{expectedOwner}!");
                    return false;
                }
            }
            return true;
        }

        private BaseCollectionUpdateHelper GetOwnerInstance()
        {
            var ownerNetworkManager = m_NetworkManagers[m_Instance.OwnerClientId];
            return m_NetworkManagers[m_Instance.OwnerClientId].SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();
        }

        /// <summary>
        /// Gets the authority instance.
        /// Client-Server: will always return the server-side instance
        /// Distributed Authority: will always return the owner
        /// </summary>
        /// <returns>authority instance</returns>
        private BaseCollectionUpdateHelper GetAuthorityInstance()
        {
            return m_ServerNetworkManager.SpawnManager.SpawnedObjects[m_Instance.NetworkObjectId].GetComponent<BaseCollectionUpdateHelper>();
        }

        [UnityTest]
        public IEnumerator CollectionAndOwnershipChangingTest()
        {
            BaseCollectionUpdateHelper.VerboseMode = m_EnableVerboseDebug;
            var runWaitPeriod = new WaitForSeconds(0.5f);
            m_NetworkManagers.Clear();
            if (m_UseHost)
            {
                m_NetworkManagers.Add(m_ServerNetworkManager.LocalClientId, m_ServerNetworkManager);
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                m_NetworkManagers.Add(client.LocalClientId, client);
            }

            var authorityNetworkManager = !m_UseHost ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;

            var instance = SpawnObject(m_TestPrefab, authorityNetworkManager);
            m_Instance = instance.GetComponent<NetworkObject>();
            var helper = instance.GetComponent<BaseCollectionUpdateHelper>();
            var currentOwner = helper.OwnerClientId;
            yield return WaitForConditionOrTimeOut(AllInstancesSpawned);
            AssertOnTimeout($"[Pre][1st Phase] Timed out waiting for all clients to spawn {m_Instance.name}!");
            helper.SetState(BaseCollectionUpdateHelper.HelperStates.Start);
            yield return runWaitPeriod;

            // Update values, validate values, change owner, updates values, and repeat until all clients have been the owner at least once
            for (int i = 0; i < 4; i++)
            {
                helper.SetState(BaseCollectionUpdateHelper.HelperStates.Pause);
                yield return WaitForConditionOrTimeOut(ValidateAllInstances);
                AssertOnTimeout($"[1st Phase] Timed out waiting for all clients to validdate their values!");
                helper.SetState(BaseCollectionUpdateHelper.HelperStates.Start);
                yield return s_DefaultWaitForTick;

                currentOwner = GetAuthorityInstance().ChangeOwner();
                Assert.IsFalse(currentOwner == ulong.MaxValue, "A non-authority instance attempted to change ownership!");

                yield return WaitForConditionOrTimeOut(() => OwnershipChangedOnAllClients(currentOwner));
                AssertOnTimeout($"[1st Phase] Timed out waiting for all clients to change ownership!\n {m_ErrorLog.ToString()}");
                helper = GetOwnerInstance();
                yield return runWaitPeriod;
            }

            // Now reset the values
            helper.SetState(BaseCollectionUpdateHelper.HelperStates.Pause);
            helper.Clear();

            // Validate all instances are reset
            yield return WaitForConditionOrTimeOut(ValidateAllInstances);
            AssertOnTimeout($"[Pre][2nd Phase]Timed out waiting for all clients to validdate their values!");
            helper.SetState(BaseCollectionUpdateHelper.HelperStates.Start);

            // Update, change ownership, and repeat until all clients have been the owner at least once
            for (int i = 0; i < 4; i++)
            {
                yield return runWaitPeriod;
                currentOwner = GetAuthorityInstance().ChangeOwner();
                Assert.IsFalse(currentOwner == ulong.MaxValue, "A non-authority instance attempted to change ownership!");
                yield return WaitForConditionOrTimeOut(() => OwnershipChangedOnAllClients(currentOwner));
                AssertOnTimeout($"[2nd Phase] Timed out waiting for all clients to change ownership!");
                helper = GetOwnerInstance();
            }

            helper.SetState(BaseCollectionUpdateHelper.HelperStates.Pause);
            yield return WaitForConditionOrTimeOut(ValidateAllInstances);
            AssertOnTimeout($"[Last Validate] Timed out waiting for all clients to validdate their values!");
        }

        [UnityTest]
        public IEnumerator CollectionFastChangingTest()
        {
            BaseCollectionUpdateHelper.VerboseMode = m_EnableVerboseDebug;
            var runWaitPeriod = new WaitForSeconds(0.5f);
            m_NetworkManagers.Clear();
            if (m_UseHost)
            {
                m_NetworkManagers.Add(m_ServerNetworkManager.LocalClientId, m_ServerNetworkManager);
            }
            foreach (var client in m_ClientNetworkManagers)
            {
                m_NetworkManagers.Add(client.LocalClientId, client);
            }

            var authorityNetworkManager = !m_UseHost ? m_ClientNetworkManagers[0] : m_ServerNetworkManager;

            var instance = SpawnObject(m_TestPrefab, authorityNetworkManager);
            m_Instance = instance.GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(AllInstancesSpawned);
            AssertOnTimeout($"[Pre][1st Phase] Timed out waiting for all clients to spawn {m_Instance.name}!");

            var helper = GetOwnerInstance();

            helper.SetState(BaseCollectionUpdateHelper.HelperStates.Start);
            yield return runWaitPeriod;

            helper.SetState(BaseCollectionUpdateHelper.HelperStates.Pause);
            yield return WaitForConditionOrTimeOut(ValidateAllValueChangedEqual);
            AssertOnTimeout($"[1st Phase] Timed out waiting for all clients to have OnValueChanged an equal number of times!");

            // Clear the collection
            helper.Clear();

            yield return WaitForConditionOrTimeOut(ValidateAllInstances);
            AssertOnTimeout($"[1st Phase] Timed out waiting for all clients to validate their values!");

            // Change the collection and then change back in the same frame without forcing a dirty check
            // VerboseDebug("Doing fast change test without a forced dirty check");
            // helper.AddItem();
            // helper.Clear();
            //
            // yield return WaitForConditionOrTimeOut(ValidateAllValueChangedEqual);
            // AssertOnTimeout($"[1st Phase] Timed out waiting for all clients to have OnValueChanged an equal number of times!");
            //
            // // Change the collection and then change back in the same frame with a forced dirty check
            // VerboseDebug("Doing fast change test with a forced dirty check");
            // helper.AddItem();
            // helper.Clear(true);
            //
            // yield return WaitForConditionOrTimeOut(ValidateOwnerHasExtraValueChangedCall);
            // AssertOnTimeout($"[1st Phase] Timed out waiting for all clients to have OnValueChanged an equal number of times!");
            //
            // yield return WaitForConditionOrTimeOut(ValidateAllInstances);
            // AssertOnTimeout($"[Last Validate] Timed out waiting for all clients to validate their values!");
        }
    }

    #region COLLECTION CHANGING COMPONENTS
    /// <summary>
    /// Helper class to test adding <see cref="NetworkVariable{T}"/> dictionary entries rapidly with frequent ownership changes.
    /// This includes a companion <see cref="NetworkVariable{T}"/> integer that is continually incremented and used as the key value for each entry.
    /// </summary>
    public class DictionaryCollectionUpdateHelper : BaseCollectionUpdateHelper
    {
        private NetworkVariable<Dictionary<int, int>> m_DictionaryCollection = new NetworkVariable<Dictionary<int, int>>(new Dictionary<int, int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<int> m_CurrentKeyValue = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        protected override bool OnValidateAgainst(BaseCollectionUpdateHelper otherHelper)
        {
            var otherListHelper = otherHelper as DictionaryCollectionUpdateHelper;
            var localValues = m_DictionaryCollection.Value;
            var otherValues = otherListHelper.m_DictionaryCollection.Value;

            if (localValues.Count != otherValues.Count)
            {
                return false;
            }

            foreach (var entry in m_DictionaryCollection.Value)
            {
                if (!otherValues.ContainsKey(entry.Key))
                {
                    return false;
                }

                if (entry.Value != otherValues[entry.Key])
                {
                    return false;
                }
            }
            return true;
        }
        protected override void OnClear(bool force)
        {
            m_DictionaryCollection.Value.Clear();
            m_DictionaryCollection.CheckDirtyState(force);
            base.OnClear(force);
        }

        public override void AddItem()
        {
            m_DictionaryCollection.Value.Add(m_CurrentKeyValue.Value, m_CurrentKeyValue.Value);
            m_DictionaryCollection.CheckDirtyState();
            m_CurrentKeyValue.Value++;
        }

        protected override void OnNetworkPostSpawn()
        {
            m_DictionaryCollection.OnValueChanged += OnValueChanged;
            base.OnNetworkPostSpawn();
        }
        public override void OnNetworkDespawn()
        {
            m_DictionaryCollection.OnValueChanged -= OnValueChanged;
            base.OnNetworkDespawn();
        }

        private int m_ValueChangeCount;
        private void OnValueChanged(Dictionary<int, int> previous, Dictionary<int, int> newValue)
        {
            Log($"[Client-{NetworkManager.LocalClientId}] OnValueChanged: previousKey {previous.Count}, newKey {newValue.Count}");
            m_ValueChangeCount++;
        }

        public override int OnValueChangedCount()
        {
            return m_ValueChangeCount;
        }
    }

    /// <summary>
    /// Helper class to test adding list entries rapidly with frequent ownership changes
    /// </summary>
    public class ListCollectionUpdateHelper : BaseCollectionUpdateHelper
    {
        private NetworkVariable<List<int>> m_ListCollection = new NetworkVariable<List<int>>(new List<int>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


        protected override bool OnValidateAgainst(BaseCollectionUpdateHelper otherHelper)
        {
            var otherListHelper = otherHelper as ListCollectionUpdateHelper;
            var localValues = m_ListCollection.Value;
            var otherValues = otherListHelper.m_ListCollection.Value;

            if (localValues.Count != otherValues.Count)
            {
                return false;
            }

            for (int i = 0; i < localValues.Count - 1; i++)
            {
                if (localValues[i] != i)
                {
                    return false;
                }

                if (localValues[i] != otherValues[i])
                {
                    return false;
                }
            }
            return true;
        }

        protected override void OnClear(bool force)
        {
            m_ListCollection.Value.Clear();
            m_ListCollection.CheckDirtyState(force);
            base.OnClear(force);
        }

        public override void AddItem()
        {
            m_ListCollection.Value.Add(m_ListCollection.Value.Count);
            m_ListCollection.CheckDirtyState();
        }

        protected override void OnNetworkPostSpawn()
        {
            m_ListCollection.OnValueChanged += OnValueChanged;
            base.OnNetworkPostSpawn();
        }
        public override void OnNetworkDespawn()
        {
            m_ListCollection.OnValueChanged -= OnValueChanged;
            base.OnNetworkDespawn();
        }

        private int m_ValueChangeCount;
        private void OnValueChanged(List<int> previous, List<int> newValue)
        {
            Log($"[Client-{NetworkManager.LocalClientId}] OnValueChanged: previousKey {previous.Count}, newKey {newValue.Count}");
            m_ValueChangeCount++;
        }

        public override int OnValueChangedCount()
        {
            return m_ValueChangeCount;
        }
    }

    /// <summary>
    /// The base class to test rapidly adding items to a collection type
    /// </summary>
    public class BaseCollectionUpdateHelper : NetworkBehaviour
    {
        public static bool VerboseMode;
        private const int k_OwnershipTickDelay = 1;

        public enum HelperStates
        {
            Stop,
            Start,
            Pause,
            ClearToChangeOwner,
            ChangingOwner
        }
        public HelperStates HelperState { get; private set; }

        private int m_SendClearForOwnershipOnTick;
        private ulong m_NextClient = 0;
        private ulong m_ClientToSendClear = 0;

        public void SetState(HelperStates helperState)
        {
            Log($"[Client-{NetworkManager.LocalClientId}] Setting state. Previous: {HelperState}, New: {helperState}");
            HelperState = helperState;
        }

        protected virtual bool OnValidateAgainst(BaseCollectionUpdateHelper otherHelper)
        {
            return true;
        }

        public bool ValidateAgainst(BaseCollectionUpdateHelper otherHelper)
        {
            return OnValidateAgainst(otherHelper);
        }

        public override void OnNetworkSpawn()
        {
            // Register for tick updates
            NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;

            base.OnNetworkSpawn();
        }
        public override void OnNetworkDespawn()
        {
            NetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
            base.OnNetworkDespawn();
        }

        protected virtual void OnClear(bool force)
        {
        }

        /// <summary>
        /// Clears the underlying list state.
        /// Use force to force the isDirty check
        /// </summary>
        /// <param name="force">Value passed into the <see cref="NetworkVariable{T}.CheckDirtyState"/> call after clearing the list.</param>
        public void Clear(bool force = false)
        {
            OnClear(force);
        }

        public virtual void AddItem()
        {
        }

        private bool CanUpdate()
        {
            return HelperState == HelperStates.Start;
        }

        public virtual int OnValueChangedCount()
        {
            return 0;
        }

        private void Update()
        {
            // Exit early if not spawn, updating is not enabled, or is not the owner
            Log($"[Client-{NetworkManager.LocalClientId}] Updating. spawned: {IsSpawned}, can update: {CanUpdate()}, isOwner: {IsOwner}");
            if (!IsSpawned || !CanUpdate() || !IsOwner)
            {
                return;
            }

            AddItem();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            // When the ownership changes and the client is the owner, then immediately add an item to the collection
            if (NetworkManager.LocalClientId == current)
            {
                AddItem();
            }
            base.OnOwnershipChanged(previous, current);
        }


        /// <summary>
        /// Sets the tick delay period of time to provide all in-flight deltas to be processed.
        /// </summary>
        private void SetTickDelay()
        {
            m_SendClearForOwnershipOnTick = NetworkManager.ServerTime.Tick + k_OwnershipTickDelay;
        }

        /// <summary>
        /// Changes the ownership
        /// </summary>
        /// <returns>next owner or ulong.MaxValue that means the authority did not invoke this method</returns>
        public ulong ChangeOwner()
        {
            if (IsServer && !IsOwnershipChanging())
            {
                var index = NetworkManager.ConnectedClientsIds.ToList().IndexOf(OwnerClientId);
                index++;
                index = index % NetworkManager.ConnectedClientsIds.Count;
                m_NextClient = NetworkManager.ConnectedClientsIds[index];

                if (IsOwnedByServer && NetworkManager.IsServer)
                {
                    HelperState = HelperStates.ChangingOwner;
                    SetTickDelay();
                    Log($"Locally changing ownership to Client-{m_NextClient}");
                }

                if (NetworkManager.IsServer && !IsOwnedByServer)
                {
                    // If we are transitioning between a client to the host or client to client,
                    // send a "heads-up" Rpc to the client prior to changing ownership. The client
                    // will stop updating for the tick delay period and then send a confirmation
                    // to the host that it is clear to change ownership.
                    ChangingOwnershipRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
                    Log($"Remotely changing ownership to Client-{m_NextClient}");
                }

                return m_NextClient;
            }

            return ulong.MaxValue;
        }

        /// <summary>
        /// Sent by the host to a client when ownership is transitioning from a client to
        /// the host or to another client.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void ChangingOwnershipRpc(RpcParams rpcParams = default)
        {
            // The sender is who we respond to that it is clear to change ownership
            m_ClientToSendClear = rpcParams.Receive.SenderClientId;
            HelperState = HelperStates.ClearToChangeOwner;
            SetTickDelay();
        }

        /// <summary>
        /// Notification that the current owner has stopped updating and ownership
        /// updates can occur without missed updates.
        /// </summary>
        /// <param name="rpcParams"></param>
        [Rpc(SendTo.SpecifiedInParams)]
        private void ChangingOwnershipClearRpc(RpcParams rpcParams = default)
        {
            HelperState = HelperStates.ChangingOwner;
            SetTickDelay();
            Log($"Changing ownership to Client-{m_NextClient} based on ready request.");
        }

        private bool IsOwnershipChanging()
        {
            return HelperState == HelperStates.ClearToChangeOwner || HelperState == HelperStates.ChangingOwner;
        }

        private void OnNetworkTick()
        {
            if (!IsSpawned || !IsOwnershipChanging() || m_SendClearForOwnershipOnTick > NetworkManager.ServerTime.Tick)
            {
                return;
            }

            if (HelperState == HelperStates.ChangingOwner)
            {
                NetworkObject.ChangeOwnership(m_NextClient);
                Log($"Local Change ownership to Client-{m_NextClient} complete! New Owner is {NetworkObject.OwnerClientId} | Expected {m_NextClient}");
            }
            else
            {
                ChangingOwnershipClearRpc(RpcTarget.Single(m_ClientToSendClear, RpcTargetUse.Temp));
            }
            HelperState = HelperStates.Stop;
        }

        protected void Log(string msg)
        {
            if (VerboseMode)
            {
                Debug.Log($"[Client-{NetworkManager.LocalClientId}] {msg}");
            }
        }
    }
    #endregion

}
