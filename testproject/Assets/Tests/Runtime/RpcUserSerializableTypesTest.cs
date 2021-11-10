using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode;
using Unity.Netcode.RuntimeTests;

namespace TestProject.RuntimeTests
{
    public class RpcUserSerializableTypesTest : BaseMultiInstanceTest
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
        /// Tests that INetworkSerializable can be used through RPCs by a user
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ExtensionMethodRpcTest()
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

            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));
            var serverSideNetworkBehaviourClass = serverClientPlayerResult.Result.gameObject.GetComponent<TestSerializationComponent>();

            var obj = new MyObject(256);
            var obj2 = new MySharedObjectReferencedById(256);
            var obj3 = new MyObjectPassedWithThisRef(256);
            bool clientMyObjCalled = false;
            bool clientMyObjPassedWithThisRefCalled = false;
            bool clientMySharedObjCalled = true;
            bool serverMyObjCalled = false;
            bool serverMyObjPassedWithThisRefCalled = false;
            bool serverMySharedObjCalled = true;
            clientSideNetworkBehaviourClass.OnMyObjectUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                clientMyObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            serverSideNetworkBehaviourClass.OnMyObjectUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                serverMyObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            clientSideNetworkBehaviourClass.OnMyObjectPassedWithThisRefUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                clientMyObjPassedWithThisRefCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            serverSideNetworkBehaviourClass.OnMyObjectPassedWithThisRefUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                serverMyObjPassedWithThisRefCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            clientSideNetworkBehaviourClass.OnMySharedObjectReferencedByIdUpdated = (receivedObj) =>
            {
                Assert.AreSame(obj2, receivedObj);
                clientMySharedObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            serverSideNetworkBehaviourClass.OnMySharedObjectReferencedByIdUpdated = (receivedObj) =>
            {
                Assert.AreSame(obj2, receivedObj);
                serverMySharedObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };

            clientSideNetworkBehaviourClass.SendMyObjectServerRpc(obj);
            clientSideNetworkBehaviourClass.SendMySharedObjectReferencedByIdServerRpc(obj2);
            clientSideNetworkBehaviourClass.SendMyObjectPassedWithThisRefServerRpc(obj3);

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
        /// Tests that INetworkSerializable can be used through RPCs by a user
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ExtensionMethodArrayRpcTest()
        {
            m_FinishedTest = false;
            var startTime = Time.realtimeSinceStartup;

            yield return StartSomeClientsAndServerWithPlayers(true, NbClients, playerPrefab =>
            {
                playerPrefab.AddComponent<TestCustomTypesArrayComponent>();
            });

            // [Client-Side] We only need to get the client side Player's NetworkObject so we can grab that instance of the TestSerializationComponent
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult));
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();

            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult));
            var serverSideNetworkBehaviourClass = serverClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();

            var objs = new[] { new MyObject(256), new MyObject(512) };
            var objs2 = new[] { new MySharedObjectReferencedById(256), new MySharedObjectReferencedById(512) };
            var objs3 = new[] { new MyObjectPassedWithThisRef(256), new MyObjectPassedWithThisRef(512) };
            bool clientMyObjCalled = false;
            bool clientMyObjPassedWithThisRefCalled = false;
            bool clientMySharedObjCalled = true;
            bool serverMyObjCalled = false;
            bool serverMyObjPassedWithThisRefCalled = false;
            bool serverMySharedObjCalled = true;
            clientSideNetworkBehaviourClass.OnMyObjectUpdated = (receivedObjs) =>
            {
                Assert.AreEqual(receivedObjs.Length, objs2.Length);
                for (var i = 0; i < receivedObjs.Length; ++i)
                {
                    Assert.AreEqual(objs[i].I, receivedObjs[i].I);
                    Assert.AreNotSame(objs[i], receivedObjs[i]);
                }
                clientMyObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            serverSideNetworkBehaviourClass.OnMyObjectUpdated = (receivedObjs) =>
            {
                Assert.AreEqual(receivedObjs.Length, objs2.Length);
                for (var i = 0; i < receivedObjs.Length; ++i)
                {
                    Assert.AreEqual(objs[i].I, receivedObjs[i].I);
                    Assert.AreNotSame(objs[i], receivedObjs[i]);
                }
                serverMyObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            clientSideNetworkBehaviourClass.OnMyObjectPassedWithThisRefUpdated = (receivedObjs) =>
            {
                Assert.AreEqual(receivedObjs.Length, objs2.Length);
                for (var i = 0; i < receivedObjs.Length; ++i)
                {
                    Assert.AreEqual(objs[i].I, receivedObjs[i].I);
                    Assert.AreNotSame(objs[i], receivedObjs[i]);
                }
                clientMyObjPassedWithThisRefCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            serverSideNetworkBehaviourClass.OnMyObjectPassedWithThisRefUpdated = (receivedObjs) =>
            {
                Assert.AreEqual(receivedObjs.Length, objs2.Length);
                for (var i = 0; i < receivedObjs.Length; ++i)
                {
                    Assert.AreEqual(objs[i].I, receivedObjs[i].I);
                    Assert.AreNotSame(objs[i], receivedObjs[i]);
                }
                serverMyObjPassedWithThisRefCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            clientSideNetworkBehaviourClass.OnMySharedObjectReferencedByIdUpdated = (receivedObjs) =>
            {
                Assert.AreEqual(receivedObjs.Length, objs2.Length);
                for (var i = 0; i < receivedObjs.Length; ++i)
                {
                    Assert.AreSame(objs2[i], receivedObjs[i]);
                }
                clientMySharedObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };
            serverSideNetworkBehaviourClass.OnMySharedObjectReferencedByIdUpdated = (receivedObjs) =>
            {
                Assert.AreEqual(receivedObjs.Length, objs2.Length);
                for (var i = 0; i < receivedObjs.Length; ++i)
                {
                    Assert.AreSame(objs2[i], receivedObjs[i]);
                }
                serverMySharedObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled;
            };

            clientSideNetworkBehaviourClass.SendMyObjectServerRpc(objs);
            clientSideNetworkBehaviourClass.SendMySharedObjectReferencedByIdServerRpc(objs2);
            clientSideNetworkBehaviourClass.SendMyObjectPassedWithThisRefServerRpc(objs3);

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

        public delegate void OnMySharedObjectReferencedByIdUpdatedDelgateHandler(MySharedObjectReferencedById obj);
        public OnMySharedObjectReferencedByIdUpdatedDelgateHandler OnMySharedObjectReferencedByIdUpdated;

        public delegate void OnMyObjectUpdatedDelgateHandler(MyObject obj);
        public OnMyObjectUpdatedDelgateHandler OnMyObjectUpdated;

        public delegate void OnMyObjectPassedWithThisRefUpdatedDelgateHandler(MyObjectPassedWithThisRef obj);
        public OnMyObjectPassedWithThisRefUpdatedDelgateHandler OnMyObjectPassedWithThisRefUpdated;

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

        [ClientRpc]
        public void SendMyObjectClientRpc(MyObject obj)
        {
            if (OnMyObjectUpdated != null)
            {
                OnMyObjectUpdated.Invoke(obj);
            }
        }
        [ClientRpc]
        public void SendMyObjectPassedWithThisRefClientRpc(MyObjectPassedWithThisRef obj)
        {
            if (OnMyObjectPassedWithThisRefUpdated != null)
            {
                OnMyObjectPassedWithThisRefUpdated.Invoke(obj);
            }
        }

        [ClientRpc]
        public void SendMySharedObjectReferencedByIdClientRpc(MySharedObjectReferencedById obj)
        {
            if (OnMySharedObjectReferencedByIdUpdated != null)
            {
                OnMySharedObjectReferencedByIdUpdated.Invoke(obj);
            }
        }

        [ServerRpc]
        public void SendMyObjectServerRpc(MyObject obj)
        {
            if (OnMyObjectUpdated != null)
            {
                OnMyObjectUpdated.Invoke(obj);
            }
            SendMyObjectClientRpc(obj);
        }

        [ServerRpc]
        public void SendMyObjectPassedWithThisRefServerRpc(MyObjectPassedWithThisRef obj)
        {
            if (OnMyObjectPassedWithThisRefUpdated != null)
            {
                OnMyObjectPassedWithThisRefUpdated.Invoke(obj);
            }
            SendMyObjectPassedWithThisRefClientRpc(obj);
        }

        [ServerRpc]
        public void SendMySharedObjectReferencedByIdServerRpc(MySharedObjectReferencedById obj)
        {
            if (OnMySharedObjectReferencedByIdUpdated != null)
            {
                OnMySharedObjectReferencedByIdUpdated.Invoke(obj);
            }
            SendMySharedObjectReferencedByIdClientRpc(obj);
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

        public delegate void OnMySharedObjectReferencedByIdUpdatedDelgateHandler(MySharedObjectReferencedById[] obj);
        public OnMySharedObjectReferencedByIdUpdatedDelgateHandler OnMySharedObjectReferencedByIdUpdated;

        public delegate void OnMyObjectUpdatedDelgateHandler(MyObject[] obj);
        public OnMyObjectUpdatedDelgateHandler OnMyObjectUpdated;
        public delegate void OnMyObjectPassedWithThisRefUpdatedDelgateHandler(MyObjectPassedWithThisRef[] obj);
        public OnMyObjectPassedWithThisRefUpdatedDelgateHandler OnMyObjectPassedWithThisRefUpdated;

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

        [ClientRpc]
        public void SendMyObjectClientRpc(MyObject[] objs)
        {
            if (OnMyObjectUpdated != null)
            {
                OnMyObjectUpdated.Invoke(objs);
            }
        }

        [ClientRpc]
        public void SendMyObjectPassedWithThisRefClientRpc(MyObjectPassedWithThisRef[] objs)
        {
            if (OnMyObjectPassedWithThisRefUpdated != null)
            {
                OnMyObjectPassedWithThisRefUpdated.Invoke(objs);
            }
        }

        [ClientRpc]
        public void SendMySharedObjectReferencedByIdClientRpc(MySharedObjectReferencedById[] objs)
        {
            if (OnMySharedObjectReferencedByIdUpdated != null)
            {
                OnMySharedObjectReferencedByIdUpdated.Invoke(objs);
            }
        }

        [ServerRpc]
        public void SendMyObjectServerRpc(MyObject[] objs)
        {
            if (OnMyObjectUpdated != null)
            {
                OnMyObjectUpdated.Invoke(objs);
            }
            SendMyObjectClientRpc(objs);
        }

        [ServerRpc]
        public void SendMyObjectPassedWithThisRefServerRpc(MyObjectPassedWithThisRef[] objs)
        {
            if (OnMyObjectPassedWithThisRefUpdated != null)
            {
                OnMyObjectPassedWithThisRefUpdated.Invoke(objs);
            }
            SendMyObjectPassedWithThisRefClientRpc(objs);
        }


        [ServerRpc]
        public void SendMySharedObjectReferencedByIdServerRpc(MySharedObjectReferencedById[] objs)
        {
            if (OnMySharedObjectReferencedByIdUpdated != null)
            {
                OnMySharedObjectReferencedByIdUpdated.Invoke(objs);
            }
            SendMySharedObjectReferencedByIdClientRpc(objs);
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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
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

    public class MyObject
    {
        public int I;

        public MyObject(int i)
        {
            I = i;
        }
    }

    public class MySharedObjectReferencedById
    {
        public static Dictionary<int, MySharedObjectReferencedById> Values =
            new Dictionary<int, MySharedObjectReferencedById>();
        public int I;

        public MySharedObjectReferencedById(int i)
        {
            I = i;
            Values[I] = this;
        }
    }

    public class MyObjectPassedWithThisRef
    {
        public int I;

        public MyObjectPassedWithThisRef(int i)
        {
            I = i;
        }
    }

    public static class TestSerializationExtensions
    {
        public static void ReadValueSafe(this ref FastBufferReader reader, out MyObjectPassedWithThisRef value)
        {
            reader.ReadValueSafe(out int i);
            value = new MyObjectPassedWithThisRef(i);
        }

        public static void WriteValueSafe(this ref FastBufferWriter writer, in MyObjectPassedWithThisRef value)
        {
            writer.WriteValueSafe(value.I);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out MyObject value)
        {
            reader.ReadValueSafe(out int i);
            value = new MyObject(i);
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in MyObject value)
        {
            writer.WriteValueSafe(value.I);
        }

        public static void ReadValueSafe(this FastBufferReader reader, out MySharedObjectReferencedById value)
        {
            reader.ReadValueSafe(out int i);
            value = MySharedObjectReferencedById.Values[i];
        }

        public static void WriteValueSafe(this FastBufferWriter writer, MySharedObjectReferencedById value)
        {
            writer.WriteValueSafe(value.I);
        }
        public static void ReadValueSafe(this FastBufferReader reader, out MyObject[] values)
        {
            reader.ReadValueSafe(out int length);
            values = new MyObject[length];
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out values[i]);
            }
        }

        public static void WriteValueSafe(this FastBufferWriter writer, in MyObject[] values)
        {
            writer.WriteValueSafe(values.Length);
            for (var i = 0; i < values.Length; ++i)
            {
                writer.WriteValueSafe(values[i]);
            }
        }

        public static void ReadValueSafe(this ref FastBufferReader reader, out MyObjectPassedWithThisRef[] values)
        {
            reader.ReadValueSafe(out int length);
            values = new MyObjectPassedWithThisRef[length];
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out values[i]);
            }
        }

        public static void WriteValueSafe(this ref FastBufferWriter writer, in MyObjectPassedWithThisRef[] values)
        {
            writer.WriteValueSafe(values.Length);
            for (var i = 0; i < values.Length; ++i)
            {
                writer.WriteValueSafe(values[i]);
            }
        }

        public static void ReadValueSafe(this FastBufferReader reader, out MySharedObjectReferencedById[] values)
        {
            reader.ReadValueSafe(out int length);
            values = new MySharedObjectReferencedById[length];
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out values[i]);
            }
        }

        public static void WriteValueSafe(this FastBufferWriter writer, MySharedObjectReferencedById[] values)
        {
            writer.WriteValueSafe(values.Length);
            for (var i = 0; i < values.Length; ++i)
            {
                writer.WriteValueSafe(values[i]);
            }
        }

    }
}

