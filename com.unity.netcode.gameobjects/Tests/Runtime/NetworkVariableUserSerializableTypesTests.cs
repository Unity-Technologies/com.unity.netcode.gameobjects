using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    public struct MyTypeOne
    {
        public int Value;
    }
    public struct MyTypeTwo
    {
        public int Value;
    }
    public struct MyTypeThree
    {
        public int Value;
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
            writer.WriteValueSafe(value.Value);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out MyTypeTwo value)
        {
            value = new MyTypeTwo();
            reader.ReadValueSafe(out value.Value);
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
            return s_GlobalNetworkObjects[clientId].Values.Where((c) => c.GetComponent<T>() != null).FirstOrDefault().GetComponent<T>();
        }

        [UnityTest]
        public IEnumerator WhenUsingAUserSerializableNetworkVariableWithUserSerialization_ReplicationWorks()
        {
            UserNetworkVariableSerialization<MyTypeOne>.WriteValue = (FastBufferWriter writer, in MyTypeOne value) =>
            {
                writer.WriteValueSafe(value.Value);
            };
            UserNetworkVariableSerialization<MyTypeOne>.ReadValue = (FastBufferReader reader, out MyTypeOne value) =>
            {
                value = new MyTypeOne();
                reader.ReadValueSafe(out value.Value);
            };

            var serverObject = SpawnObject(m_WorkingPrefab, m_ServerNetworkManager);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();

            yield return WaitForMessageOfType<CreateObjectMessage>(m_ClientNetworkManagers[0]);

            var serverComponent = serverObject.GetComponent<WorkingUserNetworkVariableComponent>();
            serverComponent.NetworkVariable.Value = new MyTypeOne { Value = 20 };
            var clientObject = GetComponentForClient<WorkingUserNetworkVariableComponent>(m_ClientNetworkManagers[0].LocalClientId);

            yield return WaitForMessageOfType<NetworkVariableDeltaMessage>(m_ClientNetworkManagers[0], 2);

            yield return WaitForConditionOrTimeOut(() => serverComponent.NetworkVariable.Value.Value == clientObject.NetworkVariable.Value.Value);
            AssertOnTimeout($"Timed out waiting for the client side value to match the server side!  Server Side ({serverComponent.NetworkVariable.Value.Value}) | Client Side ({clientObject.NetworkVariable.Value.Value})");

            Assert.AreEqual(serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponent>().NetworkVariable.Value.Value, clientObject.NetworkVariable.Value.Value);
            Assert.AreEqual(20, clientObject.NetworkVariable.Value.Value);
        }

        [UnityTest]
        public IEnumerator WhenUsingAUserSerializableNetworkVariableWithUserSerializationViaExtensionMethod_ReplicationWorks()
        {
            UserNetworkVariableSerialization<MyTypeTwo>.WriteValue = NetworkVariableUserSerializableTypesTestsExtensionMethods.WriteValueSafe;
            UserNetworkVariableSerialization<MyTypeTwo>.ReadValue = NetworkVariableUserSerializableTypesTestsExtensionMethods.ReadValueSafe;

            var serverObject = SpawnObject(m_ExtensionMethodPrefab, m_ServerNetworkManager);
            var serverNetworkObject = serverObject.GetComponent<NetworkObject>();

            yield return WaitForMessageOfType<CreateObjectMessage>(m_ClientNetworkManagers[0]);
            serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponentUsingExtensionMethod>().NetworkVariable.Value = new MyTypeTwo { Value = 20 };

            yield return WaitForMessageOfType<NetworkVariableDeltaMessage>(m_ClientNetworkManagers[0], 2);
            var serverComponent = serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponentUsingExtensionMethod>();
            var clientObject = GetComponentForClient<WorkingUserNetworkVariableComponentUsingExtensionMethod>(m_ClientNetworkManagers[0].LocalClientId);
            yield return WaitForConditionOrTimeOut(() => serverComponent.NetworkVariable.Value.Value == clientObject.NetworkVariable.Value.Value);
            AssertOnTimeout($"Timed out waiting for the client side value to match the server side!  Server Side ({serverComponent.NetworkVariable.Value.Value}) | Client Side ({clientObject.NetworkVariable.Value.Value})");

            Assert.AreEqual(serverNetworkObject.GetComponent<WorkingUserNetworkVariableComponentUsingExtensionMethod>().NetworkVariable.Value.Value, clientObject.NetworkVariable.Value.Value);
            Assert.AreEqual(20, clientObject.NetworkVariable.Value.Value);
        }

        /// <summary>
        /// Waits for (count) message(s) of type T to be received by the networkManager client
        /// </summary>
        private IEnumerator WaitForMessageOfType<T>(NetworkManager networkManager, int count = 1) where T : INetworkMessage
        {
            var messageHookEntries = new List<MessageHookEntry>();
            for (int i = 0; i < count; i++)
            {
                var messageHookEntry = new MessageHookEntry(networkManager);
                messageHookEntry.AssignMessageType<T>();
                messageHookEntries.Add(messageHookEntry);
            }
            var messageHookConditional = new MessageHooksConditional(messageHookEntries);
            yield return WaitForConditionOrTimeOut(messageHookConditional);
            AssertOnTimeout($"Timed out waiting for message type {nameof(T)} to be received by client!\n Message Hooks Still Waiting:\n {messageHookConditional.GetHooksStillWaiting()}");
            yield return s_DefaultWaitForTick;
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
