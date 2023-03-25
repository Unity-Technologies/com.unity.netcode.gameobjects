using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace TestProject.RuntimeTests
{
    public struct TemplatedType<T1> : INetworkSerializeByMemcpy where T1 : unmanaged
    {
        public enum Enum
        {
            One,
            Two,
            Three
        }

        public struct NestedTemplatedType<T2> : INetworkSerializeByMemcpy where T2 : unmanaged
        {
            public T1 Value1;
            public T2 Value2;
        }

        public T1 Value;
    }
    public struct NetworkSerializableTemplatedType<T1> : INetworkSerializable where T1 : unmanaged, IComparable, IConvertible, IComparable<T1>, IEquatable<T1>
    {
        public struct NestedTemplatedType<T2> : INetworkSerializable where T2 : unmanaged, IComparable, IConvertible, IComparable<T2>, IEquatable<T2>
        {
            public T1 Value1;
            public T2 Value2;

            public void NetworkSerialize<TImplementation>(BufferSerializer<TImplementation> serializer) where TImplementation : IReaderWriter
            {
                serializer.SerializeValue(ref Value1);
                serializer.SerializeValue(ref Value2);
            }
        }

        public T1 Value;

        public void NetworkSerialize<TImplementation>(BufferSerializer<TImplementation> serializer) where TImplementation : IReaderWriter
        {
            serializer.SerializeValue(ref Value);
        }
    }
    public class RpcUserSerializableTypesTest : NetcodeIntegrationTest
    {
        private UserSerializableClass m_UserSerializableClass;
        private UserSerializableStruct m_UserSerializableStruct;

        private TemplatedType<int> m_T1Val;
        private TemplatedType<int>.NestedTemplatedType<int> m_T2Val;
        private TemplatedType<int>.Enum m_EnumVal;
        private NetworkSerializableTemplatedType<int> m_NetworkSerializableT1Val;
        private NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> m_NetworkSerializableT2Val;

        private TemplatedType<int>[] m_T1Vals;
        private TemplatedType<int>.NestedTemplatedType<int>[] m_T2Vals;
        private TemplatedType<int>.Enum[] m_EnumVals;
        private NetworkSerializableTemplatedType<int>[] m_NetworkSerializableT1Vals;
        private NetworkSerializableTemplatedType<int>.NestedTemplatedType<int>[] m_NetworkSerializableT2Vals;

        private List<UserSerializableClass> m_UserSerializableClassArray;
        private List<UserSerializableStruct> m_UserSerializableStructArray;

        private bool m_FinishedTest;
        private bool m_FinishedStructTest;
        private bool m_FinishedClassTest;
        private bool m_FinishedTemplateStructTest;
        private bool m_FinishedNetworkSerializableTemplateStructTest;

        private bool m_IsSendingNull;
        private bool m_IsArrayEmpty;

        protected override int NumberOfClients => 1;

        protected override NetworkManagerInstatiationMode OnSetIntegrationTestMode()
        {
            return NetworkManagerInstatiationMode.DoNotCreate;
        }

        /// <summary>
        /// Tests that INetworkSerializable can be used through RPCs by a user
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator NetworkSerializableTest()
        {
            m_FinishedTest = false;
            m_FinishedStructTest = false;
            m_FinishedClassTest = false;
            m_FinishedTemplateStructTest = false;
            m_FinishedNetworkSerializableTemplateStructTest = false;
            var startTime = Time.realtimeSinceStartup;

            CreateServerAndClients();
            m_PlayerPrefab.AddComponent<TestSerializationComponent>();

            yield return StartServerAndClients();

            // [Client-Side] We only need to get the client side Player's NetworkObject so we can grab that instance of the TestSerializationComponent
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult);
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestSerializationComponent>();
            clientSideNetworkBehaviourClass.OnSerializableClassUpdated = OnClientReceivedUserSerializableClassUpdated;
            clientSideNetworkBehaviourClass.OnSerializableStructUpdated = OnClientReceivedUserSerializableStructUpdated;
            clientSideNetworkBehaviourClass.OnTemplateStructUpdated = OnClientReceivedUserSerializableTemplateStructUpdated;
            clientSideNetworkBehaviourClass.OnNetworkSerializableTemplateStructUpdated = OnClientReceivedUserSerializableNetworkSerializableTemplateStructUpdated; clientSideNetworkBehaviourClass.OnTemplateStructsUpdated = OnClientReceivedUserSerializableTemplateStructsUpdated;
            clientSideNetworkBehaviourClass.OnNetworkSerializableTemplateStructsUpdated = OnClientReceivedUserSerializableNetworkSerializableTemplateStructsUpdated;


            var userSerializableClass = new UserSerializableClass();
            for (int i = 0; i < 32; i++)
            {
                userSerializableClass.MyByteListValues.Add((byte)i);
            }

            userSerializableClass.MyintValue = 1;
            userSerializableClass.MyulongValue = 100;

            var userSerializableStruct = new UserSerializableStruct
            {
                MyintValue = 1,
                MyulongValue = 100
            };

            var t1val = new TemplatedType<int> { Value = 1 };
            var t2val = new TemplatedType<int>.NestedTemplatedType<int> { Value1 = 1, Value2 = 2 };
            var networkSerializableT1val = new NetworkSerializableTemplatedType<int> { Value = 1 };
            var networkSerializableT2val = new NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> { Value1 = 1, Value2 = 2 };
            var enumVal = TemplatedType<int>.Enum.One;

            var t1vals = new[] { t1val };
            var t2vals = new[] { t2val };
            var networkSerializableT1vals = new[] { networkSerializableT1val };
            var networkSerializableT2vals = new[] { networkSerializableT2val };
            var enumVals = new[] { enumVal };

            clientSideNetworkBehaviourClass.ClientStartTest(userSerializableClass);
            clientSideNetworkBehaviourClass.ClientStartTest(userSerializableStruct);
            clientSideNetworkBehaviourClass.ClientStartTest(t1val, t2val, enumVal);
            clientSideNetworkBehaviourClass.ClientStartTest(networkSerializableT1val, networkSerializableT2val);
            clientSideNetworkBehaviourClass.ClientStartTest(t1vals, t2vals, enumVals);
            clientSideNetworkBehaviourClass.ClientStartTest(networkSerializableT1vals, networkSerializableT2vals);

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

            Assert.AreEqual(m_UserSerializableStruct.MyintValue, userSerializableStruct.MyintValue + 1);
            Assert.AreEqual(m_UserSerializableStruct.MyulongValue, userSerializableStruct.MyulongValue + 1);

            Assert.AreEqual(m_T1Val.Value, t1val.Value + 1);
            Assert.AreEqual(m_T2Val.Value1, t2val.Value1 + 1);
            Assert.AreEqual(m_T2Val.Value2, t2val.Value2 + 1);
            Assert.AreEqual(m_NetworkSerializableT1Val.Value, networkSerializableT1val.Value + 1);
            Assert.AreEqual(m_NetworkSerializableT2Val.Value1, networkSerializableT2val.Value1 + 1);
            Assert.AreEqual(m_NetworkSerializableT2Val.Value2, networkSerializableT2val.Value2 + 1);
            Assert.AreEqual(m_EnumVal, enumVal + 1);

            Assert.AreEqual(m_T1Vals[0].Value, t1val.Value + 1);
            Assert.AreEqual(m_T2Vals[0].Value1, t2val.Value1 + 1);
            Assert.AreEqual(m_T2Vals[0].Value2, t2val.Value2 + 1);
            Assert.AreEqual(m_NetworkSerializableT1Vals[0].Value, networkSerializableT1val.Value + 1);
            Assert.AreEqual(m_NetworkSerializableT2Vals[0].Value1, networkSerializableT2val.Value1 + 1);
            Assert.AreEqual(m_NetworkSerializableT2Vals[0].Value2, networkSerializableT2val.Value2 + 1);
            Assert.AreEqual(m_EnumVals[0], enumVal + 1);

            // End of test
            ShutdownAndCleanUp();
        }

        /// <summary>
        /// Tests that INetworkSerializable can be used through RPCs by a user
        /// </summary>
        /// <returns></returns>
        [UnityTest]
        public IEnumerator ExtensionMethodRpcTest()
        {
            m_FinishedTest = false;
            m_FinishedStructTest = false;
            m_FinishedClassTest = false;
            m_FinishedTemplateStructTest = false;
            m_FinishedNetworkSerializableTemplateStructTest = false;
            var startTime = Time.realtimeSinceStartup;
            CreateServerAndClients();
            m_PlayerPrefab.AddComponent<TestSerializationComponent>();
            yield return StartServerAndClients();

            // [Client-Side] We only need to get the client side Player's NetworkObject so we can grab that instance of the TestSerializationComponent
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult);
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestSerializationComponent>();

            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult);
            var serverSideNetworkBehaviourClass = serverClientPlayerResult.Result.gameObject.GetComponent<TestSerializationComponent>();

            var obj = new MyObject(256);
            var obj2 = new MySharedObjectReferencedById(256);
            var obj3 = new MyObjectPassedWithThisRef(256);
            var intList = new List<int> { 5, 10, 15, 5, 1 };
            var strList = new List<string> { "foo", "bar", "baz", "qux" };
            bool clientMyObjCalled = false;
            bool clientMyObjPassedWithThisRefCalled = false;
            bool clientMySharedObjCalled = false;
            bool serverMyObjCalled = false;
            bool serverMyObjPassedWithThisRefCalled = false;
            bool serverMySharedObjCalled = false;
            bool serverIntListCalled = false;
            bool serverStrListCalled = false;
            clientSideNetworkBehaviourClass.OnMyObjectUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                clientMyObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnMyObjectUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                serverMyObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            clientSideNetworkBehaviourClass.OnMyObjectPassedWithThisRefUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                clientMyObjPassedWithThisRefCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnMyObjectPassedWithThisRefUpdated = (receivedObj) =>
            {
                Assert.AreEqual(obj.I, receivedObj.I);
                Assert.AreNotSame(obj, receivedObj);
                serverMyObjPassedWithThisRefCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            clientSideNetworkBehaviourClass.OnMySharedObjectReferencedByIdUpdated = (receivedObj) =>
            {
                Assert.AreSame(obj2, receivedObj);
                clientMySharedObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnMySharedObjectReferencedByIdUpdated = (receivedObj) =>
            {
                Assert.AreSame(obj2, receivedObj);
                serverMySharedObjCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnIntListUpdated = (receivedList) =>
            {
                Assert.AreEqual(intList.Count, receivedList.Count);
                for (var i = 0; i < receivedList.Count; ++i)
                {
                    Assert.AreEqual(intList[i], receivedList[i]);
                }
                serverIntListCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnStringListUpdated = (receivedList) =>
            {
                Assert.AreEqual(strList.Count, receivedList.Count);
                for (var i = 0; i < receivedList.Count; ++i)
                {
                    Assert.AreEqual(strList[i], receivedList[i]);
                }
                serverStrListCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };

            clientSideNetworkBehaviourClass.SendMyObjectServerRpc(obj);
            clientSideNetworkBehaviourClass.SendMySharedObjectReferencedByIdServerRpc(obj2);
            clientSideNetworkBehaviourClass.SendMyObjectPassedWithThisRefServerRpc(obj3);
            clientSideNetworkBehaviourClass.SendIntListServerRpc(intList);
            clientSideNetworkBehaviourClass.SendStringListServerRpc(strList);

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
            ShutdownAndCleanUp();
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

            CreateServerAndClients();
            m_PlayerPrefab.AddComponent<TestCustomTypesArrayComponent>();
            yield return StartServerAndClients();

            // [Client-Side] We only need to get the client side Player's NetworkObject so we can grab that instance of the TestSerializationComponent
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult);
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();

            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult);
            var serverSideNetworkBehaviourClass = serverClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();

            var objs = new[] { new MyObject(256), new MyObject(512) };
            var objs2 = new[] { new MySharedObjectReferencedById(256), new MySharedObjectReferencedById(512) };
            var objs3 = new[] { new MyObjectPassedWithThisRef(256), new MyObjectPassedWithThisRef(512) };
            var intList = new[] { new List<int> { 5, 10, 15 }, new List<int> { 5, 1 } };
            var strList = new[] { new List<string> { "foo", "bar" }, new List<string> { "baz", "qux" }, new List<string> { "quuz" } };
            bool clientMyObjCalled = false;
            bool clientMyObjPassedWithThisRefCalled = false;
            bool clientMySharedObjCalled = false;
            bool serverMyObjCalled = false;
            bool serverMyObjPassedWithThisRefCalled = false;
            bool serverMySharedObjCalled = false;
            bool serverIntListCalled = false;
            bool serverStrListCalled = false;
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
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
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
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
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
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
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
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
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
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
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
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnIntListUpdated = (receivedLists) =>
            {
                Assert.AreEqual(receivedLists.Length, intList.Length);
                for (var i = 0; i < receivedLists.Length; ++i)
                {
                    Assert.AreEqual(receivedLists[i].Count, intList[i].Count);
                    for (var j = 0; j < receivedLists[i].Count; ++j)
                    {
                        Assert.AreEqual(intList[i][j], receivedLists[i][j]);
                    }
                }
                serverIntListCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };
            serverSideNetworkBehaviourClass.OnStringListUpdated = (receivedLists) =>
            {
                Assert.AreEqual(receivedLists.Length, strList.Length);
                for (var i = 0; i < receivedLists.Length; ++i)
                {
                    Assert.AreEqual(receivedLists[i].Count, strList[i].Count);
                    for (var j = 0; j < receivedLists[i].Count; ++j)
                    {
                        Assert.AreEqual(strList[i][j], receivedLists[i][j]);
                    }
                }
                serverStrListCalled = true;
                m_FinishedTest = clientMyObjCalled && clientMySharedObjCalled && clientMyObjPassedWithThisRefCalled &&
                                 serverMyObjCalled && serverMySharedObjCalled && serverMyObjPassedWithThisRefCalled &&
                                 serverIntListCalled && serverStrListCalled;
            };

            clientSideNetworkBehaviourClass.SendMyObjectServerRpc(objs);
            clientSideNetworkBehaviourClass.SendMySharedObjectReferencedByIdServerRpc(objs2);
            clientSideNetworkBehaviourClass.SendMyObjectPassedWithThisRefServerRpc(objs3);
            clientSideNetworkBehaviourClass.SendIntListServerRpc(intList);
            clientSideNetworkBehaviourClass.SendStringListServerRpc(strList);

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
            ShutdownAndCleanUp();
        }

        /// <summary>
        /// Delegate handler invoked towards the end of the when the NetworkSerializableTest
        /// </summary>
        /// <param name="userSerializableClass"></param>
        private void OnClientReceivedUserSerializableClassUpdated(UserSerializableClass userSerializableClass)
        {
            m_UserSerializableClass = userSerializableClass;
            m_FinishedClassTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest && m_FinishedTemplateStructTest && m_FinishedNetworkSerializableTemplateStructTest;
        }

        /// <summary>
        /// Delegate handler invoked towards the end of the when the NetworkSerializableTest
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        private void OnClientReceivedUserSerializableStructUpdated(UserSerializableStruct userSerializableStruct)
        {
            m_UserSerializableStruct = userSerializableStruct;
            m_FinishedStructTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest && m_FinishedTemplateStructTest && m_FinishedNetworkSerializableTemplateStructTest;
        }

        private void OnClientReceivedUserSerializableTemplateStructUpdated(TemplatedType<int> t1val, TemplatedType<int>.NestedTemplatedType<int> t2val, TemplatedType<int>.Enum enumVal)
        {
            m_T1Val = t1val;
            m_T2Val = t2val;
            m_EnumVal = enumVal;
            m_FinishedTemplateStructTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest && m_FinishedTemplateStructTest && m_FinishedNetworkSerializableTemplateStructTest;
        }

        private void OnClientReceivedUserSerializableNetworkSerializableTemplateStructUpdated(NetworkSerializableTemplatedType<int> t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> t2val)
        {
            m_NetworkSerializableT1Val = t1val;
            m_NetworkSerializableT2Val = t2val;
            m_FinishedNetworkSerializableTemplateStructTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest && m_FinishedTemplateStructTest && m_FinishedNetworkSerializableTemplateStructTest;
        }

        private void OnClientReceivedUserSerializableTemplateStructsUpdated(TemplatedType<int>[] t1val, TemplatedType<int>.NestedTemplatedType<int>[] t2val, TemplatedType<int>.Enum[] enumVal)
        {
            m_T1Vals = t1val;
            m_T2Vals = t2val;
            m_EnumVals = enumVal;
            m_FinishedTemplateStructTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest && m_FinishedTemplateStructTest && m_FinishedNetworkSerializableTemplateStructTest;
        }

        private void OnClientReceivedUserSerializableNetworkSerializableTemplateStructsUpdated(NetworkSerializableTemplatedType<int>[] t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int>[] t2val)
        {
            m_NetworkSerializableT1Vals = t1val;
            m_NetworkSerializableT2Vals = t2val;
            m_FinishedNetworkSerializableTemplateStructTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest && m_FinishedTemplateStructTest && m_FinishedNetworkSerializableTemplateStructTest;
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
            CreateServerAndClients();
            m_PlayerPrefab.AddComponent<TestCustomTypesArrayComponent>();
            yield return StartServerAndClients();

            // [Host-Side] Get the host-server side Player's NetworkObject so we can grab that instance of the TestCustomTypesArrayComponent
            var serverClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ServerNetworkManager, serverClientPlayerResult);
            var serverSideNetworkBehaviourClass = serverClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();
            serverSideNetworkBehaviourClass.OnSerializableClassesUpdatedServerRpc = OnServerReceivedUserSerializableClassesUpdated;
            serverSideNetworkBehaviourClass.OnSerializableStructsUpdatedServerRpc = OnServerReceivedUserSerializableStructsUpdated;

            // [Client-Side] Get the client side Player's NetworkObject so we can grab that instance of the TestCustomTypesArrayComponent
            var clientClientPlayerResult = new NetcodeIntegrationTestHelpers.ResultWrapper<NetworkObject>();
            yield return NetcodeIntegrationTestHelpers.GetNetworkObjectByRepresentation((x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId), m_ClientNetworkManagers[0], clientClientPlayerResult);
            var clientSideNetworkBehaviourClass = clientClientPlayerResult.Result.gameObject.GetComponent<TestCustomTypesArrayComponent>();
            clientSideNetworkBehaviourClass.OnSerializableClassesUpdatedClientRpc = OnClientReceivedUserSerializableClassesUpdated;
            clientSideNetworkBehaviourClass.OnSerializableStructsUpdatedClientRpc = OnClientReceivedUserSerializableStructsUpdated;

            m_UserSerializableClassArray = new List<UserSerializableClass>();
            m_UserSerializableStructArray = new List<UserSerializableStruct>();

            if (!m_IsSendingNull)
            {
                // Create an array of userSerializableClass instances
                for (int i = 0; i < arraySize; i++)
                {
                    var userSerializableClass = new UserSerializableClass
                    {
                        //Used for testing order of the array
                        MyintValue = i
                    };
                    m_UserSerializableClassArray.Add(userSerializableClass);

                    var userSerializableStruct = new UserSerializableStruct
                    {
                        //Used for testing order of the array
                        MyintValue = i
                    };
                    m_UserSerializableStructArray.Add(userSerializableStruct);
                }

                clientSideNetworkBehaviourClass.ClientStartTest(m_UserSerializableClassArray.ToArray());
                clientSideNetworkBehaviourClass.ClientStartStructTest(m_UserSerializableStructArray.ToArray());
            }
            else
            {
                clientSideNetworkBehaviourClass.ClientStartTest(null);
                clientSideNetworkBehaviourClass.ClientStartStructTest(null);
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
            ShutdownAndCleanUp();
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
            m_FinishedClassTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest;
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

        /// <summary>
        /// Verifies that the UserSerializableStruct array is in the same order
        /// that it was sent.
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        private void ValidateUserSerializableStructs(UserSerializableStruct[] userSerializableStruct)
        {
            if (m_IsSendingNull)
            {
                Assert.IsNull(userSerializableStruct);
            }
            else if (m_IsArrayEmpty)
            {
                Assert.AreEqual(userSerializableStruct.Length, 0);
            }
            else
            {
                var indexCount = 0;
                // Check the order of the array
                foreach (var customTypeEntry in userSerializableStruct)
                {
                    Assert.AreEqual(customTypeEntry.MyintValue, indexCount);
                    indexCount++;
                }
            }
        }

        /// <summary>
        /// Delegate handler invoked when the server sends the client
        /// the UserSerializableStruct array during the NetworkSerializableArrayTest
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        private void OnClientReceivedUserSerializableStructsUpdated(UserSerializableStruct[] userSerializableStruct)
        {
            ValidateUserSerializableStructs(userSerializableStruct);
            m_FinishedStructTest = true;
            m_FinishedTest = m_FinishedClassTest && m_FinishedStructTest;
        }

        /// <summary>
        /// Delegate handler invoked when the client sends the server
        /// the UserSerializableStruct array during the NetworkSerializableArrayTest
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        private void OnServerReceivedUserSerializableStructsUpdated(UserSerializableStruct[] userSerializableStruct)
        {
            ValidateUserSerializableStructs(userSerializableStruct);
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

        public delegate void OnSerializableStructUpdatedDelgateHandler(UserSerializableStruct userSerializableStruct);
        public OnSerializableStructUpdatedDelgateHandler OnSerializableStructUpdated;

        public delegate void OnTemplateStructUpdatedDelgateHandler(TemplatedType<int> t1val, TemplatedType<int>.NestedTemplatedType<int> t2val, TemplatedType<int>.Enum enumVal);
        public OnTemplateStructUpdatedDelgateHandler OnTemplateStructUpdated;

        public delegate void OnNetworkSerializableTemplateStructUpdatedDelgateHandler(NetworkSerializableTemplatedType<int> t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> t2val);
        public OnNetworkSerializableTemplateStructUpdatedDelgateHandler OnNetworkSerializableTemplateStructUpdated;

        public delegate void OnTemplateStructsUpdatedDelgateHandler(TemplatedType<int>[] t1val, TemplatedType<int>.NestedTemplatedType<int>[] t2val, TemplatedType<int>.Enum[] enumVal);
        public OnTemplateStructsUpdatedDelgateHandler OnTemplateStructsUpdated;

        public delegate void OnNetworkSerializableTemplateStructsUpdatedDelgateHandler(NetworkSerializableTemplatedType<int>[] t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int>[] t2val);
        public OnNetworkSerializableTemplateStructsUpdatedDelgateHandler OnNetworkSerializableTemplateStructsUpdated;

        public delegate void OnMySharedObjectReferencedByIdUpdatedDelgateHandler(MySharedObjectReferencedById obj);
        public OnMySharedObjectReferencedByIdUpdatedDelgateHandler OnMySharedObjectReferencedByIdUpdated;

        public delegate void OnMyObjectUpdatedDelgateHandler(MyObject obj);
        public OnMyObjectUpdatedDelgateHandler OnMyObjectUpdated;

        public delegate void OnIntListUpdatedDelgateHandler(List<int> lst);
        public OnIntListUpdatedDelgateHandler OnIntListUpdated;
        public delegate void OnStringListUpdatedDelgateHandler(List<string> lst);
        public OnStringListUpdatedDelgateHandler OnStringListUpdated;

        public delegate void OnMyObjectPassedWithThisRefUpdatedDelgateHandler(MyObjectPassedWithThisRef obj);
        public OnMyObjectPassedWithThisRefUpdatedDelgateHandler OnMyObjectPassedWithThisRefUpdated;

        /// <summary>
        /// Starts the unit test and passes the UserSerializableClass from the client to the server
        /// </summary>
        /// <param name="userSerializableClass"></param>
        public void ClientStartTest(UserSerializableClass userSerializableClass)
        {
            SendServerSerializedDataClassServerRpc(userSerializableClass);
        }

        /// <summary>
        /// Server receives the UserSerializableClass, modifies it, and sends it back
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataClassServerRpc(UserSerializableClass userSerializableClass)
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
            SendClientSerializedDataClassClientRpc(userSerializableClass);
        }

        /// <summary>
        /// Client receives the UserSerializableClass and then invokes the OnSerializableClassUpdated (if set)
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ClientRpc]
        private void SendClientSerializedDataClassClientRpc(UserSerializableClass userSerializableClass)
        {
            OnSerializableClassUpdated?.Invoke(userSerializableClass);
        }

        public void ClientStartTest(TemplatedType<int> t1val, TemplatedType<int>.NestedTemplatedType<int> t2val, TemplatedType<int>.Enum enumVal)
        {
            SendTemplateStructServerRpc(t1val, t2val, enumVal);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendTemplateStructServerRpc(TemplatedType<int> t1val, TemplatedType<int>.NestedTemplatedType<int> t2val, TemplatedType<int>.Enum enumVal)
        {
            Debug.Log($"Received server RPC values {t1val.Value} {t2val.Value1} {t2val.Value2} {enumVal}");
            t1val.Value++;
            t2val.Value1++;
            t2val.Value2++;
            enumVal += 1;

            SendTemplateStructClientRpc(t1val, t2val, enumVal);
        }

        [ClientRpc]
        private void SendTemplateStructClientRpc(TemplatedType<int> t1val, TemplatedType<int>.NestedTemplatedType<int> t2val, TemplatedType<int>.Enum enumVal)
        {
            Debug.Log($"Received client RPC values {t1val.Value} {t2val.Value1} {t2val.Value2}");
            OnTemplateStructUpdated?.Invoke(t1val, t2val, enumVal);
        }

        public void ClientStartTest(NetworkSerializableTemplatedType<int> t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> t2val)
        {
            SendNetworkSerializableTemplateStructServerRpc(t1val, t2val);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendNetworkSerializableTemplateStructServerRpc(NetworkSerializableTemplatedType<int> t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> t2val)
        {
            Debug.Log($"Received NetworkSerializable server RPC values {t1val.Value} {t2val.Value1} {t2val.Value2}");
            t1val.Value++;
            t2val.Value1++;
            t2val.Value2++;

            SendNetworkSerializableTemplateStructClientRpc(t1val, t2val);
        }

        [ClientRpc]
        private void SendNetworkSerializableTemplateStructClientRpc(NetworkSerializableTemplatedType<int> t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int> t2val)
        {
            Debug.Log($"Received NetworkSerializable client RPC values {t1val.Value} {t2val.Value1} {t2val.Value2}");
            OnNetworkSerializableTemplateStructUpdated?.Invoke(t1val, t2val);
        }


        public void ClientStartTest(TemplatedType<int>[] t1val, TemplatedType<int>.NestedTemplatedType<int>[] t2val, TemplatedType<int>.Enum[] enumVal)
        {
            SendTemplateStructServerRpc(t1val, t2val, enumVal);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendTemplateStructServerRpc(TemplatedType<int>[] t1val, TemplatedType<int>.NestedTemplatedType<int>[] t2val, TemplatedType<int>.Enum[] enumVal)
        {
            Debug.Log($"Received server RPC values {t1val[0].Value} {t2val[0].Value1} {t2val[0].Value2} {enumVal[0]}");
            t1val[0].Value++;
            t2val[0].Value1++;
            t2val[0].Value2++;
            enumVal[0] += 1;

            SendTemplateStructClientRpc(t1val, t2val, enumVal);
        }

        [ClientRpc]
        private void SendTemplateStructClientRpc(TemplatedType<int>[] t1val, TemplatedType<int>.NestedTemplatedType<int>[] t2val, TemplatedType<int>.Enum[] enumVal)
        {
            Debug.Log($"Received client RPC values {t1val[0].Value} {t2val[0].Value1} {t2val[0].Value2}");
            OnTemplateStructsUpdated?.Invoke(t1val, t2val, enumVal);
        }

        public void ClientStartTest(NetworkSerializableTemplatedType<int>[] t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int>[] t2val)
        {
            SendNetworkSerializableTemplateStructServerRpc(t1val, t2val);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendNetworkSerializableTemplateStructServerRpc(NetworkSerializableTemplatedType<int>[] t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int>[] t2val)
        {
            Debug.Log($"Received NetworkSerializable server RPC values {t1val[0].Value} {t2val[0].Value1} {t2val[0].Value2}");
            t1val[0].Value++;
            t2val[0].Value1++;
            t2val[0].Value2++;

            SendNetworkSerializableTemplateStructClientRpc(t1val, t2val);
        }

        [ClientRpc]
        private void SendNetworkSerializableTemplateStructClientRpc(NetworkSerializableTemplatedType<int>[] t1val, NetworkSerializableTemplatedType<int>.NestedTemplatedType<int>[] t2val)
        {
            Debug.Log($"Received NetworkSerializable client RPC values {t1val[0].Value} {t2val[0].Value1} {t2val[0].Value2}");
            OnNetworkSerializableTemplateStructsUpdated?.Invoke(t1val, t2val);
        }

        /// <summary>
        /// Starts the unit test and passes the UserSerializableStruct from the client to the server
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        public void ClientStartTest(UserSerializableStruct userSerializableStruct)
        {
            SendServerSerializedDataStructServerRpc(userSerializableStruct);
        }

        /// <summary>
        /// Server receives the UserSerializableStruct, modifies it, and sends it back
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataStructServerRpc(UserSerializableStruct userSerializableStruct)
        {
            userSerializableStruct.MyintValue++;
            userSerializableStruct.MyulongValue++;

            SendClientSerializedDataStructClientRpc(userSerializableStruct);
        }

        /// <summary>
        /// Client receives the UserSerializableStruct and then invokes the OnSerializableStructUpdated (if set)
        /// </summary>
        /// <param name="userSerializableStruct"></param>
        [ClientRpc]
        private void SendClientSerializedDataStructClientRpc(UserSerializableStruct userSerializableStruct)
        {
            OnSerializableStructUpdated?.Invoke(userSerializableStruct);
        }

        [ClientRpc]
        public void SendMyObjectClientRpc(MyObject obj)
        {
            OnMyObjectUpdated?.Invoke(obj);
        }
        [ClientRpc]
        public void SendIntListClientRpc(List<int> lst)
        {
            OnIntListUpdated?.Invoke(lst);
        }
        [ClientRpc]
        public void SendStringListClientRpc(List<string> lst)
        {
            OnStringListUpdated?.Invoke(lst);
        }
        [ClientRpc]
        public void SendMyObjectPassedWithThisRefClientRpc(MyObjectPassedWithThisRef obj)
        {
            OnMyObjectPassedWithThisRefUpdated?.Invoke(obj);
        }

        [ClientRpc]
        public void SendMySharedObjectReferencedByIdClientRpc(MySharedObjectReferencedById obj)
        {
            OnMySharedObjectReferencedByIdUpdated?.Invoke(obj);
        }

        [ServerRpc]
        public void SendMyObjectServerRpc(MyObject obj)
        {
            OnMyObjectUpdated?.Invoke(obj);
            SendMyObjectClientRpc(obj);
        }

        [ServerRpc]
        public void SendIntListServerRpc(List<int> lst)
        {
            OnIntListUpdated?.Invoke(lst);
            SendIntListClientRpc(lst);
        }

        [ServerRpc]
        public void SendStringListServerRpc(List<string> lst)
        {
            OnStringListUpdated?.Invoke(lst);
            SendStringListClientRpc(lst);
        }

        [ServerRpc]
        public void SendMyObjectPassedWithThisRefServerRpc(MyObjectPassedWithThisRef obj)
        {
            OnMyObjectPassedWithThisRefUpdated?.Invoke(obj);
            SendMyObjectPassedWithThisRefClientRpc(obj);
        }

        [ServerRpc]
        public void SendMySharedObjectReferencedByIdServerRpc(MySharedObjectReferencedById obj)
        {
            OnMySharedObjectReferencedByIdUpdated?.Invoke(obj);
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
        public delegate void OnSerializableStructsUpdatedDelgateHandler(UserSerializableStruct[] userSerializableStructs);

        public delegate void OnMySharedObjectReferencedByIdUpdatedDelgateHandler(MySharedObjectReferencedById[] obj);
        public OnMySharedObjectReferencedByIdUpdatedDelgateHandler OnMySharedObjectReferencedByIdUpdated;

        public delegate void OnMyObjectUpdatedDelgateHandler(MyObject[] obj);
        public OnMyObjectUpdatedDelgateHandler OnMyObjectUpdated;
        public delegate void OnIntListUpdatedDelgateHandler(List<int>[] obj);
        public OnIntListUpdatedDelgateHandler OnIntListUpdated;
        public delegate void OnStringListUpdatedDelgateHandler(List<string>[] obj);
        public OnStringListUpdatedDelgateHandler OnStringListUpdated;
        public delegate void OnMyObjectPassedWithThisRefUpdatedDelgateHandler(MyObjectPassedWithThisRef[] obj);
        public OnMyObjectPassedWithThisRefUpdatedDelgateHandler OnMyObjectPassedWithThisRefUpdated;

        public OnSerializableClassesUpdatedDelgateHandler OnSerializableClassesUpdatedServerRpc;
        public OnSerializableClassesUpdatedDelgateHandler OnSerializableClassesUpdatedClientRpc;
        public OnSerializableStructsUpdatedDelgateHandler OnSerializableStructsUpdatedServerRpc;
        public OnSerializableStructsUpdatedDelgateHandler OnSerializableStructsUpdatedClientRpc;

        /// <summary>
        /// Starts the unit test and passes the userSerializableClasses array
        /// from the client to the server
        /// </summary>
        /// <param name="userSerializableClass"></param>
        public void ClientStartTest(UserSerializableClass[] userSerializableClasses)
        {
            SendServerSerializedDataClassArryServerRpc(userSerializableClasses);
        }

        /// <summary>
        /// Server receives the UserSerializableClasses array, invokes the callback
        /// that checks the order, and then passes it back to the client
        /// </summary>
        /// <param name="userSerializableClass"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataClassArryServerRpc(UserSerializableClass[] userSerializableClasses)
        {
            OnSerializableClassesUpdatedServerRpc?.Invoke(userSerializableClasses);
            SendClientSerializedDataClassArrayClientRpc(userSerializableClasses);
        }

        /// <summary>
        /// Client receives the UserSerializableClass array and invokes the callback
        /// for verification and signaling the test is complete.
        /// </summary>
        /// <param name="userSerializableClasses"></param>
        [ClientRpc]
        private void SendClientSerializedDataClassArrayClientRpc(UserSerializableClass[] userSerializableClasses)
        {
            OnSerializableClassesUpdatedClientRpc?.Invoke(userSerializableClasses);
        }

        /// <summary>
        /// Starts the unit test and passes the userSerializableStructs array
        /// from the client to the server
        /// </summary>
        /// <param name="userSerializableStructs"></param>
        public void ClientStartStructTest(UserSerializableStruct[] userSerializableStructs)
        {
            SendServerSerializedDataStructArrayServerRpc(userSerializableStructs);
        }

        /// <summary>
        /// Server receives the UserSerializableStructs array, invokes the callback
        /// that checks the order, and then passes it back to the client
        /// </summary>
        /// <param name="userSerializableStructs"></param>
        [ServerRpc(RequireOwnership = false)]
        private void SendServerSerializedDataStructArrayServerRpc(UserSerializableStruct[] userSerializableStructs)
        {
            OnSerializableStructsUpdatedServerRpc?.Invoke(userSerializableStructs);
            SendClientSerializedDataStructArrayClientRpc(userSerializableStructs);
        }

        /// <summary>
        /// Client receives the userSerializableStructs array and invokes the callback
        /// for verification and signaling the test is complete.
        /// </summary>
        /// <param name="userSerializableStructs"></param>
        [ClientRpc]
        private void SendClientSerializedDataStructArrayClientRpc(UserSerializableStruct[] userSerializableStructs)
        {
            OnSerializableStructsUpdatedClientRpc?.Invoke(userSerializableStructs);
        }

        [ClientRpc]
        public void SendMyObjectClientRpc(MyObject[] objs)
        {
            OnMyObjectUpdated?.Invoke(objs);
        }

        [ClientRpc]
        public void SendIntListClientRpc(List<int>[] lists)
        {
            OnIntListUpdated?.Invoke(lists);
        }

        [ClientRpc]
        public void SendStringListClientRpc(List<string>[] lists)
        {
            OnStringListUpdated?.Invoke(lists);
        }

        [ClientRpc]
        public void SendMyObjectPassedWithThisRefClientRpc(MyObjectPassedWithThisRef[] objs)
        {
            OnMyObjectPassedWithThisRefUpdated?.Invoke(objs);
        }

        [ClientRpc]
        public void SendMySharedObjectReferencedByIdClientRpc(MySharedObjectReferencedById[] objs)
        {
            OnMySharedObjectReferencedByIdUpdated?.Invoke(objs);
        }

        [ServerRpc]
        public void SendMyObjectServerRpc(MyObject[] objs)
        {
            OnMyObjectUpdated?.Invoke(objs);
            SendMyObjectClientRpc(objs);
        }

        [ServerRpc]
        public void SendIntListServerRpc(List<int>[] lists)
        {
            OnIntListUpdated?.Invoke(lists);
            SendIntListClientRpc(lists);
        }

        [ServerRpc]
        public void SendStringListServerRpc(List<string>[] lists)
        {
            OnStringListUpdated?.Invoke(lists);
            SendStringListClientRpc(lists);
        }

        [ServerRpc]
        public void SendMyObjectPassedWithThisRefServerRpc(MyObjectPassedWithThisRef[] objs)
        {
            OnMyObjectPassedWithThisRefUpdated?.Invoke(objs);
            SendMyObjectPassedWithThisRefClientRpc(objs);
        }


        [ServerRpc]
        public void SendMySharedObjectReferencedByIdServerRpc(MySharedObjectReferencedById[] objs)
        {
            OnMySharedObjectReferencedByIdUpdated?.Invoke(objs);
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

    /// <summary>
    /// The test version of a custom user-defined struct that implements INetworkSerializable
    /// </summary>
    public struct UserSerializableStruct : INetworkSerializable
    {
        public int MyintValue;
        public ulong MyulongValue;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MyintValue);
            serializer.SerializeValue(ref MyulongValue);
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

        public static void ReadValueSafe(this FastBufferReader reader, out List<int> value)
        {
            reader.ReadValueSafe(out int length);
            value = new List<int>();
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out int val);
                value.Add(val);
            }
        }

        //Serialization write for a List of ints
        public static void WriteValueSafe(this FastBufferWriter writer, in List<int> value)
        {
            writer.WriteValueSafe(value.Count);
            foreach (var item in value)
            {
                writer.WriteValueSafe(item);
            }
        }

        //Serialization read for a List of strings
        public static void ReadValueSafe(this FastBufferReader reader, out List<string> value)
        {
            reader.ReadValueSafe(out int length);
            value = new List<string>();
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out string val);
                value.Add(val);
            }
        }

        //Serialization write for a List of strings
        public static void WriteValueSafe(this FastBufferWriter writer, in List<string> value)
        {
            writer.WriteValueSafe(value.Count);
            foreach (var item in value)
            {
                writer.WriteValueSafe(item);
            }
        }

        public static void ReadValueSafe(this FastBufferReader reader, out List<int>[] value)
        {
            reader.ReadValueSafe(out int length);
            value = new List<int>[length];
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out int oneLength);

                value[i] = new List<int>();
                for (var j = 0; j < oneLength; ++j)
                {
                    reader.ReadValueSafe(out int val);
                    value[i].Add(val);
                }
            }
        }

        //Serialization write for a List of ints
        public static void WriteValueSafe(this FastBufferWriter writer, in List<int>[] value)
        {
            writer.WriteValueSafe(value.Length);
            foreach (var item in value)
            {
                writer.WriteValueSafe(item.Count);
                foreach (var subItem in item)
                {
                    writer.WriteValueSafe(subItem);
                }
            }
        }

        //Serialization read for a List of strings
        public static void ReadValueSafe(this FastBufferReader reader, out List<string>[] value)
        {
            reader.ReadValueSafe(out int length);
            value = new List<string>[length];
            for (var i = 0; i < length; ++i)
            {
                reader.ReadValueSafe(out int oneLength);

                value[i] = new List<string>();
                for (var j = 0; j < oneLength; ++j)
                {
                    reader.ReadValueSafe(out string val);
                    value[i].Add(val);
                }
            }
        }

        //Serialization write for a List of strings
        public static void WriteValueSafe(this FastBufferWriter writer, in List<string>[] value)
        {
            writer.WriteValueSafe(value.Length);
            foreach (var item in value)
            {
                writer.WriteValueSafe(item.Count);
                foreach (var subItem in item)
                {
                    writer.WriteValueSafe(subItem);
                }
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

