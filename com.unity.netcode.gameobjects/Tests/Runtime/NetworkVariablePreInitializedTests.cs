using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Random = UnityEngine.Random;
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    public class PreInitializedOnAwake : NetworkBehaviour
    {
        public readonly static List<PreInitializedOnAwake> Instances = new List<PreInitializedOnAwake>();
        private static Vector3 s_InitValue1 = new Vector3(Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f));
        private static Vector3 s_InitValue2 = new Vector3(Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f));
        private static Vector3 s_InitValue3 = new Vector3(Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f));
        public static void Clean()
        {
            Instances.Clear();
        }

        public NetworkVariable<Vector3> OwnerWritable_Position = new NetworkVariable<Vector3>(default, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Owner);
        public NetworkVariable<Vector3> ServerWritable_Position = new NetworkVariable<Vector3>(default, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Server);
        public NetworkVariable<Vector3> OwnerReadWrite_Position = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            // Everyone initializes to these values
            OwnerWritable_Position.Value = s_InitValue1;
            ServerWritable_Position.Value = s_InitValue2;
            OwnerReadWrite_Position.Value = s_InitValue3;
        }

        public bool AllValuesMatch()
        {
            return OwnerWritable_Position.Value == s_InitValue1 && ServerWritable_Position.Value == s_InitValue2 && OwnerReadWrite_Position.Value == s_InitValue3;
        }
        public override void OnNetworkSpawn()
        {
            Instances.Add(this);
            base.OnNetworkSpawn();
        }
    }

    public class PreInitializedByMethod : NetworkBehaviour
    {
        public readonly static List<PreInitializedByMethod> Instances = new List<PreInitializedByMethod>();
        public NetworkVariable<Vector3> OwnerPosition = new NetworkVariable<Vector3>(default, NetworkVariableBase.DefaultReadPerm, NetworkVariableWritePermission.Owner);

        public void InitializeValue(Vector3 position)
        {
            // Everyone initializes to these values
            OwnerPosition.Value = position;
        }

        public bool ValueMatches(Vector3 position)
        {
            return OwnerPosition.Value == position;
        }

        public static void Clean()
        {
            Instances.Clear();
        }

        public override void OnNetworkSpawn()
        {
            Instances.Add(this);
            base.OnNetworkSpawn();
        }
    }

    /// <summary>
    /// Initial set of integration tests to validate that setting a NetworkVariable before it has
    /// been initialized works correctly
    /// </summary>
    public class NetworkVariablePreInitializedTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 4;

        private GameObject m_ObjectToSpawn;

        protected override IEnumerator OnSetup()
        {
            PreInitializedOnAwake.Clean();
            PreInitializedByMethod.Clean();
            return base.OnSetup();
        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<PreInitializedOnAwake>();
            base.OnCreatePlayerPrefab();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_ObjectToSpawn = CreateNetworkObjectPrefab("PreInitNetVar");
            m_ObjectToSpawn.AddComponent<PreInitializedByMethod>();
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// Validates that all instances have matching values
        /// </summary>
        /// <returns></returns>
        private bool AllNetworkVariablesOnPlayersInitializedCorrectly()
        {
            foreach(var instance in PreInitializedOnAwake.Instances)
            {
                if (!instance.AllValuesMatch())
                {
                    return false;
                }
            }
            return true;
        }


        [UnityTest]
        public IEnumerator TestPreInitOnAwake()
        {
            // All players should be spawned at this point
            // Validate that the pre-initialized values match
            yield return WaitForConditionOrTimeOut(AllNetworkVariablesOnPlayersInitializedCorrectly);
            AssertOnTimeout($"Timed out waiting for all instances of {nameof(PreInitializedOnAwake)} to have their NetworkVariables match!");
        }


        private Vector3 m_ValueToInitializeWith;
        /// <summary>
        /// Validates that all instances have matching values
        /// </summary>
        /// <returns></returns>
        private bool AllNetworkVariablesOnSpawnedObjectInitializedCorrectly()
        {
            foreach (var instance in PreInitializedByMethod.Instances)
            {
                if (!instance.ValueMatches(m_ValueToInitializeWith))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Set the value of the NetworkVariable before spawning it
        /// </summary>
        protected override void OnObjectInstantiatedBeforeSpawn(GameObject gameObject)
        {
            var preInitializedByMethod = gameObject.GetComponent<PreInitializedByMethod>();
            preInitializedByMethod.InitializeValue(m_ValueToInitializeWith);
            base.OnObjectInstantiatedBeforeSpawn(gameObject);
        }

        [UnityTest]
        public IEnumerator TestInitByMethod()
        {
            // Generate a random position value
            m_ValueToInitializeWith = new Vector3(Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f), Random.Range(0.0f, 1000.0f));

            // Spawn the object
            var spawnedObjectServerSide = SpawnObject(m_ObjectToSpawn, m_ServerNetworkManager);

            // Validate that the value set before spawning matches on all client instances
            yield return WaitForConditionOrTimeOut(AllNetworkVariablesOnSpawnedObjectInitializedCorrectly);
            AssertOnTimeout($"Timed out waiting for all instances of {nameof(PreInitializedByMethod)} to have their NetworkVariables match!");
        }
    }
}
