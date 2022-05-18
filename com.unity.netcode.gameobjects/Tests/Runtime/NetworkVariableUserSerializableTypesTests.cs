using System;
using System.Collections;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public struct MyTypeOne
    {
        public int i;
    }
    public struct MyTypeTwo
    {
        public int i;
    }
    public struct MyTypeThree
    {
        public int i;
    }
    public class WorkingUserNetworkVariableComponent : NetworkBehaviour
    {
        public NetworkVariable<MyTypeOne> NetworkVariable = new NetworkVariable<MyTypeOne>();
    }
    public class WorkingUserNetworkVariableComponentUsingExtensionMethod : NetworkBehaviour
    {
        public NetworkVariable<MyTypeTwo> NetworkVariable = new NetworkVariable<MyTypeTwo>();
    }
    public class NonWorkingUserNetworkVariableComponent : NetworkBehaviour
    {
        public NetworkVariable<MyTypeThree> NetworkVariable = new NetworkVariable<MyTypeThree>();
    }

    internal static class NetworkVariableUserSerializableTypesTestsExtensionMethods
    {
        public static void WriteValueSafe(this FastBufferWriter writer, in MyTypeTwo value)
        {
            writer.WriteValueSafe(value.i);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out MyTypeTwo value)
        {
            value = new MyTypeTwo();
            reader.ReadValueSafe(out value.i);
        }
    }

    public class NetworkVariableUserSerializableTypesTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        public NetworkVariableUserSerializableTypesTests()
            : base(HostOrServer.Server)
        {
        }

        private GameObject m_WorkingPrefab;
        private GameObject m_ExtensionMethodPrefab;
        private GameObject m_NonWorkingPrefab;

        protected override void OnServerAndClientsCreated()
        {
            m_WorkingPrefab = CreateNetworkObjectPrefab($"[{nameof(NetworkVariableUserSerializableTypesTests)}.{nameof(m_WorkingPrefab)}]");
            m_ExtensionMethodPrefab = CreateNetworkObjectPrefab($"[{nameof(NetworkVariableUserSerializableTypesTests)}.{nameof(m_ExtensionMethodPrefab)}]");
            m_NonWorkingPrefab = CreateNetworkObjectPrefab($"[{nameof(NetworkVariableUserSerializableTypesTests)}.{nameof(m_NonWorkingPrefab)}]");
            m_WorkingPrefab.AddComponent<WorkingUserNetworkVariableComponent>();
            m_ExtensionMethodPrefab.AddComponent<WorkingUserNetworkVariableComponentUsingExtensionMethod>();
            m_NonWorkingPrefab.AddComponent<NonWorkingUserNetworkVariableComponent>();
        }

        private T GetComponentForClient<T>(ulong clientId) where T : NetworkBehaviour
        {
            foreach (var component in Object.FindObjectsOfType<T>())
            {
                if (component.IsSpawned && component.NetworkManager.LocalClientId == clientId)
                {
                    return component;
                }
            }

            return null;
        }

        [UnityTest]
        public IEnumerator WhenUsingAUserSerializableNetworkVariableWithUserSerialization_ReplicationWorks()
        {
            UserNetworkVariableSerialization<MyTypeOne>.WriteValue = (FastBufferWriter writer, in MyTypeOne value) =>
            {
                writer.WriteValueSafe(value.i);
            };
            UserNetworkVariableSerialization<MyTypeOne>.ReadValue = (FastBufferReader reader, out MyTypeOne value) =>
            {
                value = new MyTypeOne();
                reader.ReadValueSafe(out value.i);
            };
            var serverObject = Object.Instantiate(m_WorkingPrefab);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverNetworkObject.Spawn();

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponent>().NetworkVariable.Value = new MyTypeOne { i = 20 };

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<NetworkVariableDeltaMessage>(m_ClientNetworkManagers[0]);
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<NetworkVariableDeltaMessage>(m_ClientNetworkManagers[0]);

            var clientObject = GetComponentForClient<WorkingUserNetworkVariableComponent>(m_ClientNetworkManagers[0].LocalClientId);

            Assert.AreEqual(serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponent>().NetworkVariable.Value.i, clientObject.NetworkVariable.Value.i);
            Assert.AreEqual(20, clientObject.NetworkVariable.Value.i);
        }

        [UnityTest]
        public IEnumerator WhenUsingAUserSerializableNetworkVariableWithUserSerializationViaExtensionMethod_ReplicationWorks()
        {
            UserNetworkVariableSerialization<MyTypeTwo>.WriteValue = NetworkVariableUserSerializableTypesTestsExtensionMethods.WriteValueSafe;
            UserNetworkVariableSerialization<MyTypeTwo>.ReadValue = NetworkVariableUserSerializableTypesTestsExtensionMethods.ReadValueSafe;

            var serverObject = Object.Instantiate(m_ExtensionMethodPrefab);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            serverNetworkObject.Spawn();

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponentUsingExtensionMethod>().NetworkVariable.Value = new MyTypeTwo { i = 20 };

            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<NetworkVariableDeltaMessage>(m_ClientNetworkManagers[0]);
            yield return NetcodeIntegrationTestHelpers.WaitForMessageOfTypeReceived<NetworkVariableDeltaMessage>(m_ClientNetworkManagers[0]);

            var clientObject = GetComponentForClient<WorkingUserNetworkVariableComponentUsingExtensionMethod>(m_ClientNetworkManagers[0].LocalClientId);

            Assert.AreEqual(serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponentUsingExtensionMethod>().NetworkVariable.Value.i, clientObject.NetworkVariable.Value.i);
            Assert.AreEqual(20, clientObject.NetworkVariable.Value.i);
        }

        [Test]
        public void WhenUsingAUserSerializableNetworkVariableWithoutUserSerialization_ReplicationFails()
        {
            var serverObject = Object.Instantiate(m_NonWorkingPrefab);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();
            serverNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            Assert.Throws<ArgumentException>(
                () =>
                {
                    serverNetworkObject.Spawn();
                }
            );
        }
    }
}
