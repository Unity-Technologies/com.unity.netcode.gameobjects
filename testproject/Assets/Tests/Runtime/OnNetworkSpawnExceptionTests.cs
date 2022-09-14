using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace TestProject.RuntimeTests
{

    public class OnNetworkSpawnNoExceptionComponent : NetworkBehaviour
    {
        public static int NumClientSpawns = 0;
        public static int NumServerSpawns = 0;
        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                ++NumClientSpawns;
            }
            else
            {
                ++NumServerSpawns;
            }
        }
    }

    public class OnNetworkSpawnThrowsExceptionComponent : NetworkBehaviour
    {
        public static int NumClientSpawns = 0;
        public static int NumServerSpawns = 0;
        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                ++NumClientSpawns;
            }
            else
            {
                ++NumServerSpawns;
            }
            throw new Exception("Exception thrown in OnNetworkSpawn");
        }
    }

    public class OnNetworkDespawnNoExceptionComponent : NetworkBehaviour
    {
        public static int NumClientDespawns = 0;
        public static int NumServerDespawns = 0;
        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                ++NumClientDespawns;
            }
            else
            {
                ++NumServerDespawns;
            }
        }
    }

    public class OnNetworkDespawnThrowsExceptionComponent : NetworkBehaviour
    {
        public static int NumClientDespawns = 0;
        public static int NumServerDespawns = 0;
        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                ++NumClientDespawns;
            }
            else
            {
                ++NumServerDespawns;
            }
            throw new Exception("Exception thrown in OnNetworkDespawn");
        }
    }

    public class OnNetworkSpawnExceptionTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_SpawnExceptionPrefab;
        private GameObject m_NoSpawnExceptionPrefab;
        private GameObject m_SpawnWithAndWithoutExceptionPrefab;
        private GameObject m_DespawnExceptionPrefab;
        private GameObject m_NoDespawnExceptionPrefab;
        private GameObject m_DespawnWithAndWithoutExceptionPrefab;

        private const int k_NumObjects = 10;
        [UnityTest]
        public IEnumerator WhenOnNetworkSpawnThrowsException_FutureOnNetworkSpawnsAreNotPrevented()
        {
            int numExceptionsExpected = 0;
            int numExceptionFreeSpawnsExpected = 0;

            var messageHookEntriesForSpawn = new List<MessageHookEntry>();

            for (var i = 0; i < k_NumObjects; ++i)
            {
                GameObject instance;
                // Randomly create some different objects, but the first will always be with exception,
                // the second will always be with both an exception behaviour and a non-exception behaviour,
                // and the last will always be without exception, with random choices in between.
                // Random.Range with int values 0 and 3 will always return 0, 1, or 2.
                var rand = Random.Range(0, 3);
                if (i == 0 || (i != 1 && i != k_NumObjects - 1 && rand == 0))
                {
                    instance = UnityEngine.Object.Instantiate(m_SpawnExceptionPrefab);
                    ++numExceptionsExpected;
                    // One for server, one for client.
                    LogAssert.Expect(LogType.Exception, new Regex("Exception thrown in OnNetworkSpawn"));
                    LogAssert.Expect(LogType.Exception, new Regex("Exception thrown in OnNetworkSpawn"));
                }
                else if (i == 1 || (i != k_NumObjects - 1 && rand == 1))
                {
                    instance = UnityEngine.Object.Instantiate(m_SpawnWithAndWithoutExceptionPrefab);
                    ++numExceptionsExpected;
                    // One for server, one for client.
                    LogAssert.Expect(LogType.Exception, new Regex("Exception thrown in OnNetworkSpawn"));
                    LogAssert.Expect(LogType.Exception, new Regex("Exception thrown in OnNetworkSpawn"));
                    ++numExceptionFreeSpawnsExpected;
                }
                else
                {
                    instance = UnityEngine.Object.Instantiate(m_NoSpawnExceptionPrefab);
                    ++numExceptionFreeSpawnsExpected;
                }
                var networkObject = instance.GetComponent<NetworkObject>();
                networkObject.NetworkManager = m_ServerNetworkManager;
                networkObject.Spawn();

                var messageHook = new MessageHookEntry(m_ClientNetworkManagers[0]);
                messageHook.AssignMessageType<CreateObjectMessage>();
                messageHookEntriesForSpawn.Add(messageHook);
            }

            var condition = new MessageHooksConditional(messageHookEntriesForSpawn);

            yield return WaitForConditionOrTimeOut(condition);

            // Assert that all objects had their OnNetworkSpawn called whether they threw exceptions or not
            Assert.AreEqual(numExceptionsExpected, OnNetworkSpawnThrowsExceptionComponent.NumClientSpawns);
            Assert.AreEqual(numExceptionsExpected, OnNetworkSpawnThrowsExceptionComponent.NumServerSpawns);
            Assert.AreEqual(numExceptionFreeSpawnsExpected, OnNetworkSpawnNoExceptionComponent.NumClientSpawns);
            Assert.AreEqual(numExceptionFreeSpawnsExpected, OnNetworkSpawnNoExceptionComponent.NumServerSpawns);
        }

        [UnityTest]
        public IEnumerator WhenOnNetworkDespawnThrowsException_FutureOnNetworkDespawnsAreNotPrevented()
        {
            int numExceptionsExpected = 0;
            int numExceptionFreeDespawnsExpected = 0;

            var messageHookEntriesForSpawn = new List<MessageHookEntry>();

            var allObjects = new List<NetworkObject>();

            for (var i = 0; i < k_NumObjects; ++i)
            {
                GameObject instance;
                // Randomly create some different objects, but the first will always be with exception,
                // the second will always be with both an exception behaviour and a non-exception behaviour,
                // and the last will always be without exception, with random choices in between.
                // Random.Range with int values 0 and 3 will always return 0, 1, or 2.
                var rand = Random.Range(0, 3);
                if (i == 0 || (i != 1 && i != k_NumObjects - 1 && rand == 0))
                {
                    instance = UnityEngine.Object.Instantiate(m_DespawnExceptionPrefab);
                    ++numExceptionsExpected;
                }
                else if (i == 1 || (i != k_NumObjects - 1 && rand == 1))
                {
                    instance = UnityEngine.Object.Instantiate(m_DespawnWithAndWithoutExceptionPrefab);
                    ++numExceptionsExpected;
                    ++numExceptionFreeDespawnsExpected;
                }
                else
                {
                    instance = UnityEngine.Object.Instantiate(m_NoDespawnExceptionPrefab);
                    ++numExceptionFreeDespawnsExpected;
                }
                var networkObject = instance.GetComponent<NetworkObject>();
                networkObject.NetworkManager = m_ServerNetworkManager;
                networkObject.Spawn();

                allObjects.Add(networkObject);

                var messageHook = new MessageHookEntry(m_ClientNetworkManagers[0]);
                messageHook.AssignMessageType<CreateObjectMessage>();
                messageHookEntriesForSpawn.Add(messageHook);
            }

            var spawnCondition = new MessageHooksConditional(messageHookEntriesForSpawn);

            yield return WaitForConditionOrTimeOut(spawnCondition);

            // Make sure no exceptions were thrown in the spawn
            LogAssert.NoUnexpectedReceived();

            var messageHookEntriesForDespawn = new List<MessageHookEntry>();
            for (var i = 0; i < numExceptionsExpected; ++i)
            {
                // One for server, one for client
                LogAssert.Expect(LogType.Exception, new Regex("Exception thrown in OnNetworkDespawn"));
                LogAssert.Expect(LogType.Exception, new Regex("Exception thrown in OnNetworkDespawn"));
            }
            foreach (var networkObject in allObjects)
            {
                networkObject.Despawn();
                var messageHook = new MessageHookEntry(m_ClientNetworkManagers[0]);
                messageHook.AssignMessageType<DestroyObjectMessage>();
                messageHookEntriesForDespawn.Add(messageHook);
            }
            var despawnCondition = new MessageHooksConditional(messageHookEntriesForDespawn);

            yield return WaitForConditionOrTimeOut(despawnCondition);

            // Assert that all objects had their OnNetworkSpawn called whether they threw exceptions or not
            Assert.AreEqual(numExceptionsExpected, OnNetworkDespawnThrowsExceptionComponent.NumClientDespawns);
            Assert.AreEqual(numExceptionsExpected, OnNetworkDespawnThrowsExceptionComponent.NumServerDespawns);
            Assert.AreEqual(numExceptionFreeDespawnsExpected, OnNetworkDespawnNoExceptionComponent.NumClientDespawns);
            Assert.AreEqual(numExceptionFreeDespawnsExpected, OnNetworkDespawnNoExceptionComponent.NumServerDespawns);
        }

        protected override IEnumerator OnSetup()
        {
            m_UseHost = false;
            OnNetworkSpawnThrowsExceptionComponent.NumClientSpawns = 0;
            OnNetworkSpawnThrowsExceptionComponent.NumServerSpawns = 0;
            OnNetworkSpawnNoExceptionComponent.NumClientSpawns = 0;
            OnNetworkSpawnNoExceptionComponent.NumServerSpawns = 0;
            OnNetworkDespawnThrowsExceptionComponent.NumClientDespawns = 0;
            OnNetworkDespawnThrowsExceptionComponent.NumServerDespawns = 0;
            OnNetworkDespawnNoExceptionComponent.NumClientDespawns = 0;
            OnNetworkDespawnNoExceptionComponent.NumServerDespawns = 0;
            yield return null;
        }

        protected override void OnServerAndClientsCreated()
        {
            m_SpawnExceptionPrefab = new GameObject("Spawn Exception Object");
            m_NoSpawnExceptionPrefab = new GameObject("Spawn Normal Object");
            m_SpawnWithAndWithoutExceptionPrefab = new GameObject("Spawn Hybrid Object");
            m_DespawnExceptionPrefab = new GameObject("Despawn Exception Object");
            m_NoDespawnExceptionPrefab = new GameObject("Despawn Normal Object");
            m_DespawnWithAndWithoutExceptionPrefab = new GameObject("Despawn Hybrid Object");

            m_SpawnExceptionPrefab.AddComponent<OnNetworkSpawnThrowsExceptionComponent>();
            m_NoSpawnExceptionPrefab.AddComponent<OnNetworkSpawnNoExceptionComponent>();

            // Note: Unity does not actually guarantee that GetComponenetsInChildren() will return the components in the
            // same order they are added, however in practice it *seems* to do so. This is an attempt to test that
            // an exception thrown in one NetworkBehaviour won't prevent another NetworkBehaviour on the same object
            // from executing its events... but this only works because that behaviour in Unity happens to work that way.
            // If a future version of Unity changes that, then this test won't actually test that, but there doesn't
            // seem to be any way to actually make this reliable, so this test is just doing the best it can with
            // current Unity behavior to test this.
            m_SpawnWithAndWithoutExceptionPrefab.AddComponent<OnNetworkSpawnThrowsExceptionComponent>();
            m_SpawnWithAndWithoutExceptionPrefab.AddComponent<OnNetworkSpawnNoExceptionComponent>();

            m_DespawnExceptionPrefab.AddComponent<OnNetworkDespawnThrowsExceptionComponent>();
            m_NoDespawnExceptionPrefab.AddComponent<OnNetworkDespawnNoExceptionComponent>();
            m_DespawnWithAndWithoutExceptionPrefab.AddComponent<OnNetworkDespawnThrowsExceptionComponent>();
            m_DespawnWithAndWithoutExceptionPrefab.AddComponent<OnNetworkDespawnNoExceptionComponent>();

            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_SpawnExceptionPrefab.AddComponent<NetworkObject>());
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_NoSpawnExceptionPrefab.AddComponent<NetworkObject>());
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_SpawnWithAndWithoutExceptionPrefab.AddComponent<NetworkObject>());
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_DespawnExceptionPrefab.AddComponent<NetworkObject>());
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_NoDespawnExceptionPrefab.AddComponent<NetworkObject>());
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(m_DespawnWithAndWithoutExceptionPrefab.AddComponent<NetworkObject>());

            m_ServerNetworkManager.AddNetworkPrefab(m_SpawnExceptionPrefab);
            m_ServerNetworkManager.AddNetworkPrefab(m_NoSpawnExceptionPrefab);
            m_ServerNetworkManager.AddNetworkPrefab(m_SpawnWithAndWithoutExceptionPrefab);
            m_ServerNetworkManager.AddNetworkPrefab(m_DespawnExceptionPrefab);
            m_ServerNetworkManager.AddNetworkPrefab(m_NoDespawnExceptionPrefab);
            m_ServerNetworkManager.AddNetworkPrefab(m_DespawnWithAndWithoutExceptionPrefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.AddNetworkPrefab(m_SpawnExceptionPrefab);
                client.AddNetworkPrefab(m_NoSpawnExceptionPrefab);
                client.AddNetworkPrefab(m_SpawnWithAndWithoutExceptionPrefab);
                client.AddNetworkPrefab(m_DespawnExceptionPrefab);
                client.AddNetworkPrefab(m_NoDespawnExceptionPrefab);
                client.AddNetworkPrefab(m_DespawnWithAndWithoutExceptionPrefab);
            }
        }
    }
}
