using System;
using System.Collections;
using System.Text.RegularExpressions;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{

    public class OnNetworkSpawnThrowsExceptionComponent : NetworkBehaviour
    {
        public static int NumClientSpawns = 0;
        public override void OnNetworkSpawn()
        {
            if (IsClient)
            {
                ++NumClientSpawns;
                if (NumClientSpawns > 2)
                {
                    throw new Exception("I'm misbehaving");
                }
            }
        }
    }
    public class OnNetworkDespawnThrowsExceptionComponent : NetworkBehaviour
    {
        public static int NumClientDespawns = 0;
        public override void OnNetworkDespawn()
        {
            if (IsClient)
            {
                ++NumClientDespawns;
                if (NumClientDespawns > 2)
                {
                    throw new Exception("I'm misbehaving");
                }
            }
        }

    }
    public class OnNetworkSpawnExceptionTests : NetcodeIntegrationTest
    {
        private GameObject m_Prefab;
        private GameObject[] m_Objects = new GameObject[5];

        [UnityTest]
        public IEnumerator WhenOnNetworkSpawnThrowsException_FutureOnNetworkSpawnsAreNotPrevented()
        {
            //Spawning was done during setup
            Assert.AreEqual(5, OnNetworkSpawnThrowsExceptionComponent.NumClientSpawns);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WhenOnNetworkDespawnThrowsException_FutureOnNetworkDespawnsAreNotPrevented()
        {
            for (var i = 0; i < 3; ++i)
            {
                LogAssert.Expect(LogType.Exception, new Regex("I'm misbehaving"));
            }
            //Spawning was done during setup. Now we despawn.
            for (var i = 0; i < 5; ++i)
            {
                m_Objects[i].GetComponent<NetworkObject>().Despawn();
            }

            var result = new TimeoutHelper();
            yield return WaitForConditionOrTimeOut(
                    () => OnNetworkDespawnThrowsExceptionComponent.NumClientDespawns == 5, result);
            Assert.IsFalse(result.TimedOut);
        }

        protected override IEnumerator OnSetup()
        {
            m_UseHost = false;

            for (var i = 0; i < 3; ++i)
            {
                LogAssert.Expect(LogType.Exception, new Regex("I'm misbehaving"));
            }
            OnNetworkSpawnThrowsExceptionComponent.NumClientSpawns = 0;
            OnNetworkDespawnThrowsExceptionComponent.NumClientDespawns = 0;
            yield return null;
        }

        protected override IEnumerator OnServerAndClientsConnected()
        {
            m_Prefab = new GameObject();
            var networkObject = m_Prefab.AddComponent<NetworkObject>();
            m_Prefab.AddComponent<OnNetworkSpawnThrowsExceptionComponent>();
            m_Prefab.AddComponent<OnNetworkDespawnThrowsExceptionComponent>();
            NetcodeIntegrationTestHelpers.MakeNetworkObjectTestPrefab(networkObject);

            m_ServerNetworkManager.NetworkConfig.ForceSamePrefabs = false;
            m_ServerNetworkManager.AddNetworkPrefab(m_Prefab);
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.ForceSamePrefabs = false;
                client.AddNetworkPrefab(m_Prefab);
            }

            for (var i = 0; i < 5; ++i)
            {
                var obj = UnityEngine.Object.Instantiate(m_Prefab);
                m_Objects[i] = obj;
                obj.GetComponent<NetworkObject>().NetworkManagerOwner = m_ServerNetworkManager;
                obj.GetComponent<NetworkObject>().Spawn();
            }

            var result = new TimeoutHelper();
            yield return WaitForConditionOrTimeOut(
                () => OnNetworkSpawnThrowsExceptionComponent.NumClientSpawns == 5, result);
            Assert.IsFalse(result.TimedOut);
        }

        protected override int NumberOfClients => 1;
    }
}
