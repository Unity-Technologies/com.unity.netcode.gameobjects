using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DocumentationCodeSamples
{
    internal static class FastBufferExtensions
    {
        internal static void WriteValueSafe(this FastBufferWriter writer, in TestSerializationDocs.Health health)
        {
            writer.WriteValueSafe(health.MaxHealth);
            writer.WriteValueSafe(health.CurrentHealth);
        }

        internal static void ReadValueSafe(this FastBufferReader reader, out TestSerializationDocs.Health health)
        {
            reader.ReadValueSafe(out uint max);
            reader.ReadValueSafe(out int current);
            health = new TestSerializationDocs.Health { MaxHealth = max, CurrentHealth = current };
        }
    }


    internal class TestSerializationDocs : NetcodeIntegrationTest
    {
        #region HealthExample

        public struct Health
        {
            public uint MaxHealth;
            public int CurrentHealth;

            // Register our custom serialization on load
            [InitializeOnLoadMethod]
            public static void RegisterHealthSerialization()
            {
                // You can reuse the FastBufferWriter and FastBufferReader extension methods we wrote above
                UserNetworkVariableSerialization<Health>.WriteValue = FastBufferExtensions.WriteValueSafe;
                UserNetworkVariableSerialization<Health>.ReadValue = FastBufferExtensions.ReadValueSafe;

                // Here is where you register your custom delta handling.
                UserNetworkVariableSerialization<Health>.WriteDelta = WriteDelta;
                UserNetworkVariableSerialization<Health>.ReadDelta = ReadDelta;

                // You can also use lambda expressions to register functions
                UserNetworkVariableSerialization<Health>.DuplicateValue = (in Health value, ref Health duplicatedValue) => { duplicatedValue = value; };
            }

            // We can use an enum to indicate which field has changed for the delta change.
            // This lets us save bandwidth when only one value has changed.
            // In the case of Health, we expect CurrentHealth to change much more often than MaxHealth
            // Implementing WriteDelta saves us bandwidth on not sending the MaxHealth every time CurrentHealth changes.
            internal enum ChangeType : byte
            {
                MaxHealth,
                CurrentHealth,
                All,
            }

            public static void WriteDelta(FastBufferWriter writer, in Health value, in Health previousValue)
            {
                if (value.MaxHealth == previousValue.MaxHealth && value.CurrentHealth != previousValue.CurrentHealth)
                {
                    // If only our CurrentHealth has changed, we can send the CurrentHealth enum with only the updated CurrentHealth value
                    writer.WriteValueSafe(ChangeType.CurrentHealth);
                    writer.WriteValueSafe(value.CurrentHealth);
                }
                else if (value.CurrentHealth == previousValue.CurrentHealth && value.MaxHealth != previousValue.MaxHealth)
                {
                    // If only our MaxHealth has changed, we can send the MaxHealth enum with only the updated MaxHealth value
                    writer.WriteValueSafe(ChangeType.MaxHealth);
                    writer.WriteValueSafe(value.MaxHealth);
                }
                else
                {
                    // If both values have changed, we need to serialize both values.
                    writer.WriteValueSafe(ChangeType.All);
                    writer.WriteValueSafe(value.MaxHealth);
                    writer.WriteValueSafe(value.CurrentHealth);
                }
            }

            public static void ReadDelta(FastBufferReader reader, ref Health value)
            {
                // First we read what type of change we've received
                reader.ReadValueSafe(out ChangeType changeType);

                // Then we read the data in our delta message, based on what type of change we've received.
                switch (changeType)
                {
                    case ChangeType.CurrentHealth:
                    {
                        reader.ReadValueSafe(out value.CurrentHealth);
                        break;
                    }
                    case ChangeType.MaxHealth:
                    {
                        reader.ReadValueSafe(out value.MaxHealth);
                        break;
                    }
                    case ChangeType.All:
                    {
                        reader.ReadValueSafe(out value.MaxHealth);
                        reader.ReadValueSafe(out value.CurrentHealth);
                        break;
                    }
                }
            }
        }

        #endregion

        internal class TestHealthBehaviour : NetworkBehaviour
        {
            internal readonly NetworkVariable<Health> HealthVar = new();

            internal Health ReceivedFromRpc;

            [Rpc(SendTo.Everyone)]
            public void SendHealthRpc(Health health)
            {
                ReceivedFromRpc = health;
            }
        }

        protected override int NumberOfClients => 1;
        private GameObject m_PrefabToSpawn;

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab(nameof(TestHealthBehaviour));
            m_PrefabToSpawn.AddComponent<TestHealthBehaviour>();
        }

        private Health m_ExpectedHealth;
        private ulong m_NetworkObjectIdToTest;

        private bool m_TestingRpc;

        private bool ValidateAllAreEqual(StringBuilder errorLog)
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(m_NetworkObjectIdToTest, out NetworkObject localInstance))
                {
                    errorLog.Append($"[Client-{networkManager.LocalClientId}] SpawnedObject not found!");
                    return false;
                }

                var healthInstance = localInstance.GetComponent<TestHealthBehaviour>();
                if (healthInstance == null)
                {
                    errorLog.Append($"[Client-{networkManager.LocalClientId}] Health instance is null!");
                    return false;
                }

                var received = m_TestingRpc ? healthInstance.ReceivedFromRpc : healthInstance.HealthVar.Value;
                if (m_ExpectedHealth.MaxHealth != received.MaxHealth)
                {
                    errorLog.Append($"[Client-{networkManager.LocalClientId}] MaxHealth values don't match! Expected {m_ExpectedHealth.MaxHealth}, Received {received.MaxHealth}");
                    return false;
                }

                if (m_ExpectedHealth.CurrentHealth != received.CurrentHealth)
                {
                    errorLog.Append($"[Client-{networkManager.LocalClientId}] CurrentHealth values don't match! Expected {m_ExpectedHealth.CurrentHealth}, Received {received.CurrentHealth}");
                    return false;
                }
            }

            return true;
        }

        [UnityTest]
        public IEnumerator TestHealthCode()
        {
            var authority = GetAuthorityNetworkManager();
            var authorityInstance = SpawnObject(m_PrefabToSpawn, authority).GetComponent<TestHealthBehaviour>();
            m_NetworkObjectIdToTest = authorityInstance.NetworkObjectId;

            yield return WaitForSpawnedOnAllOrTimeOut(authorityInstance.NetworkObjectId);
            AssertOnTimeout("Failed to spawn network object");

            var healthToTest = new Health { MaxHealth = 456, CurrentHealth = 23 };
            m_ExpectedHealth = healthToTest;
            m_TestingRpc = true;

            authorityInstance.SendHealthRpc(healthToTest);

            yield return WaitForConditionOrTimeOut(ValidateAllAreEqual);
            AssertOnTimeout("RPC send failed");

            m_TestingRpc = false;
            healthToTest = new Health { MaxHealth = 123, CurrentHealth = 45 };
            m_ExpectedHealth = healthToTest;

            authorityInstance.HealthVar.Value = healthToTest;

            yield return WaitForConditionOrTimeOut(ValidateAllAreEqual);
            AssertOnTimeout("NetworkVariable assignment failed");

            var current = authorityInstance.HealthVar.Value;
            current.CurrentHealth -= 10;
            authorityInstance.HealthVar.Value = current;

            m_ExpectedHealth = current;

            yield return WaitForConditionOrTimeOut(ValidateAllAreEqual);
            AssertOnTimeout("NetworkVariable update failed");
        }
    }
}
