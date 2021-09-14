using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;

namespace TestProject.RuntimeTests
{
    public class RpcINetworkSerializable : BaseMultiInstanceTest
    {
        private UserSerializableClass m_UserSerializableClass;
        private List<UserSerializableClass> m_UserSerializableClassArray;

        private bool m_FinishedTest;

        private bool m_IsSendingNull;
        private bool m_IsArrayEmpty;

        protected override int NbClients => 1;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield break; // ignore
        }

        /// <summary>
        /// Tests that INetworkSerializable can be used through RPCs by a user
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator NetworkSerializableTest()
        {
            m_FinishedTest = false;
            var startTime = Time.realtimeSinceStartup;

            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                playerPrefab.AddComponent<TestSerializationComponent>();
            });

            // [Client-Side] We only need to get the client side Player's NetworkObject so we can grab that instance of the TestSerializationComponent
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestSerializationComponent>();
            clientSideNetworkBehaviourClass.OnSerializableClassUpdated = OnClientReceivedUserSerializableClassUpdated;


            var userSerializableClass = new UserSerializableClass();
            for (int i = 0; i < 32; i++)
            {
                userSerializableClass.MyByteListValues.Add((byte)i);
            }

            userSerializableClass.MyintValue = 1;
            userSerializableClass.MyulongValue = 100;

            clientSideNetworkBehaviourClass.ClientStartTest(userSerializableClass);

            // Wait until the test has finished or we time out
            var timeOutPeriod = Time.realtimeSinceStartup + 5;
            var timedOut = false;
            while (!m_FinishedTest)
            {
                if (Time.realtimeSinceStartup > timeOutPeriod)
                {
                    timedOut = true;
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Verify the test passed
            Assert.False(timedOut);
            Assert.IsNotNull(m_UserSerializableClass);
            Assert.AreEqual(m_UserSerializableClass.MyintValue, userSerializableClass.MyintValue + 1);
            Assert.AreEqual(m_UserSerializableClass.MyulongValue, userSerializableClass.MyulongValue + 1);
            Assert.AreEqual(m_UserSerializableClass.MyByteListValues.Count, 64);

            // Validate the list is being sent in order on both sides.
            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(m_UserSerializableClass.MyByteListValues[i], i);
            }

            // End of test
            m_ClientNetworkManagers[0].Shutdown();
            m_ServerNetworkManager.Shutdown();
        }

        /// <summary>
        /// Delegate handler invoked towards the end of the when the NetworkSerializableTest
        /// </summary>
        /// <param name="userSerializableClass"></param>
        private void OnClientReceivedUserSerializableClassUpdated(UserSerializableClass userSerializableClass)
        {
            m_UserSerializableClass = userSerializableClass;
            m_FinishedTest = true;
        }

        /// <summary>
        /// Tests that an array of the same type of class that implements the
        /// INetworkSerializable interface will be received in the same order
        /// that it was sent.
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator NetworkSerializableArrayTest()
        {
            return NetworkSerializableArrayTestHandler(32);
        }

        /// <summary>
        /// Tests that an array of the same type of class that implements the
        /// INetworkSerializable interface can send an empty array
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator NetworkSerializableEmptyArrayTest()
        {
            return NetworkSerializableArrayTestHandler(0);
        }

        /// <summary>
        /// Tests that an array of the same type of class that implements the
        /// INetworkSerializable interface can send a null value for the array
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator NetworkSerializableNULLArrayTest()
        {
            return NetworkSerializableArrayTestHandler(0, true);
        }

        /// <summary>
        /// Handles the various tests for INetworkSerializable arrays
        /// </summary>
        /// <param name="arraySize">how many elements</param>
        /// <param name="sendNullArray">force to send a null as the array value</param>
        /// <returns></returns>
        public IEnumerator NetworkSerializableArrayTestHandler(int arraySize, bool sendNullArray = false)
        {
            m_IsSendingNull = sendNullArray;
            m_FinishedTest = false;
            m_IsArrayEmpty = false;

            if (arraySize == 0)
            {
                m_IsArrayEmpty = true;
            }

            var startTime = Time.realtimeSinceStartup;

            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                playerPrefab.AddComponent<TestCustomTypesArrayComponent>();
            });

            // [Host-Side] Get the host-server side Player's NetworkObject so we can grab that instance of the TestCustomTypesArrayComponent
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));
            var serverSideNetworkBehaviourClass = serverClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();
            serverSideNetworkBehaviourClass.OnSerializableClassesUpdatedServerRpc = OnServerReceivedUserSerializableClassesUpdated;

            // [Client-Side] Get the client side Player's NetworkObject so we can grab that instance of the TestCustomTypesArrayComponent
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();
            clientSideNetworkBehaviourClass.OnSerializableClassesUpdatedClientRpc = OnClientReceivedUserSerializableClassesUpdated;

            m_UserSerializableClassArray = new List<UserSerializableClass>();

            if (!m_IsSendingNull)
            {
                // Create an array of userSerializableClass instances
                for (int i = 0; i < arraySize; i++)
                {
                    var userSerializableClass = new UserSerializableClass();
                    //Used for testing order of the array
                    userSerializableClass.MyintValue = i;
                    m_UserSerializableClassArray.Add(userSerializableClass);
                }

                clientSideNetworkBehaviourClass.ClientStartTest(m_UserSerializableClassArray.ToArray());
            }
            else
            {
                clientSideNetworkBehaviourClass.ClientStartTest(null);
            }

            // Wait until the test has finished or we time out
            var timeOutPeriod = Time.realtimeSinceStartup + 5;
            var timedOut = false;
            while (!m_FinishedTest)
            {
                if (Time.realtimeSinceStartup > timeOutPeriod)
                {
                    timedOut = true;
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Verify the test passed
            Assert.False(timedOut);

            // End of test
            m_ClientNetworkManagers[0].Shutdown();
            m_ServerNetworkManager.Shutdown();

        }

        /// <summary>
        /// Verifies that the UserSerializableClass array is in the same order
        /// that it was sent.
        /// </summary>
        /// <param name="userSerializableClass"></param>
        private void ValidateUserSerializableClasses(UserSerializableClass[] userSerializableClass)
        {
            if (m_IsSendingNull)
            {
                Assert.IsNull(userSerializableClass);
            }
            else if (m_IsArrayEmpty)
            {
                Assert.AreEqual(userSerializableClass.Length, 0);
            }
            else
            {
                var indexCount = 0;
                // Check the order of the array
                foreach (var customTypeEntry in userSerializableClass)
                {
                    Assert.AreEqual(customTypeEntry.MyintValue, indexCount);
                    indexCount++;
                }
            }
        }

        /// <summary>
        /// Delegate handler invoked when the server sends the client
        /// the UserSerializableClass array during the NetworkSerializableArrayTest
        /// </summary>
        /// <param name="userSerializableClass"></param>
        private void OnClientReceivedUserSerializableClassesUpdated(UserSerializableClass[] userSerializableClass)
        {
            ValidateUserSerializableClasses(userSerializableClass);
            m_FinishedTest = true;
        }

        /// <summary>
        /// Delegate handler invoked when the client sends the server
        /// the UserSerializableClass array during the NetworkSerializableArrayTest
        /// </summary>
        /// <param name="userSerializableClass"></param>
        private void OnServerReceivedUserSerializableClassesUpdated(UserSerializableClass[] userSerializableClass)
        {
            ValidateUserSerializableClasses(userSerializableClass);
        }

    }

    /// <summary>
    /// Component used with NetworkSerializableTest that houses the
    /// client and server RPC calls.
    /// </summary>
    public class TestSerializationComponent : NetworkBehaviour
    {
        public delegate void OnSerializableClassUpdatedDelgateHandler(UserSerializableClass userSerializableClass);

        public OnSerializableClassUpdatedDelgateHandler OnSerializableClassUpdated;

        /// <summary>
        /// Starts the unit test and passes the UserSerializableClass from the client to the server
        /// </summary>
        /// <param name="userSerializableClass"></param>
        public void ClientStartTest(UserSerializableClass userSerializableClass)
        {
            SendServerSerializedDataServerRpc(userSerializableClass);
        }

        /// <summary>
        /// Server receives the UserSerializableClass, modifies it, and sends it back
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataServerRpc(UserSerializableClass userSerializableClass)
        {
            userSerializableClass.MyintValue++;
            userSerializableClass.MyulongValue++;

            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(userSerializableClass.MyByteListValues[i], i);
            }

            for (int i = 32; i < 64; i++)
            {
                userSerializableClass.MyByteListValues.Add((byte)i);
            }
            SendClientSerializedDataClientRpc(userSerializableClass);
        }

        /// <summary>
        /// Client receives the UserSerializableClass and then invokes the OnSerializableClassUpdated (if set)
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ClientRpc]
        private void SendClientSerializedDataClientRpc(UserSerializableClass userSerializableClass)
        {
            if (OnSerializableClassUpdated != null)
            {
                OnSerializableClassUpdated.Invoke(userSerializableClass);
            }
        }
    }

    /// <summary>
    /// Component used with NetworkSerializableArrayTest that
    /// houses the client and server RPC calls that pass an
    /// array of UserSerializableClass between the client and
    /// the server.
    /// </summary>
    public class TestCustomTypesArrayComponent : NetworkBehaviour
    {
        public delegate void OnSerializableClassesUpdatedDelgateHandler(UserSerializableClass[] userSerializableClasses);

        public OnSerializableClassesUpdatedDelgateHandler OnSerializableClassesUpdatedServerRpc;
        public OnSerializableClassesUpdatedDelgateHandler OnSerializableClassesUpdatedClientRpc;

        /// <summary>
        /// Starts the unit test and passes the userSerializableClasses array
        /// from the client to the server
        /// </summary>
        /// <param name="userSerializableClass"></param>
        public void ClientStartTest(UserSerializableClass[] userSerializableClasses)
        {
            SendServerSerializedDataServerRpc(userSerializableClasses);
        }

        /// <summary>
        /// Server receives the UserSerializableClasses array, invokes the callback
        /// that checks the order, and then passes it back to the client
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataServerRpc(UserSerializableClass[] userSerializableClasses)
        {
            if (OnSerializableClassesUpdatedServerRpc != null)
            {
                OnSerializableClassesUpdatedServerRpc.Invoke(userSerializableClasses);
            }
            SendClientSerializedDataClientRpc(userSerializableClasses);
        }

        /// <summary>
        /// Client receives the UserSerializableClasses array and invokes the callback
        /// for verification and signaling the test is complete.
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ClientRpc]
        private void SendClientSerializedDataClientRpc(UserSerializableClass[] userSerializableClasses)
        {
            if (OnSerializableClassesUpdatedClientRpc != null)
            {
                OnSerializableClassesUpdatedClientRpc.Invoke(userSerializableClasses);
            }
        }
    }

    /// <summary>
    /// The test version of a custom user-defined class that implements INetworkSerializable
    /// </summary>
    public class UserSerializableClass : INetworkSerializable
    {
        public int MyintValue;
        public ulong MyulongValue;
        public List<byte> MyByteListValues;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IBufferSerializerImplementation
        {
            serializer.SerializeValue(ref MyintValue);
            serializer.SerializeValue(ref MyulongValue);
            int size = MyByteListValues.Count;
            serializer.SerializeValue(ref size);
            if (serializer.IsReader)
            {
                var b = new byte[size];
                serializer.GetFastBufferReader().ReadBytesSafe(ref b, size);
                MyByteListValues = new List<byte>(b);
            }
            else
            {

                serializer.GetFastBufferWriter().WriteBytesSafe(MyByteListValues.ToArray());
            }
        }

        public UserSerializableClass()
        {
            MyByteListValues = new List<byte>();
        }
    }
}

