using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Netcode.RuntimeTests
{
    public struct TestStruct : INetworkSerializable
    {
        public uint SomeInt;
        public bool SomeBool;
        public static bool NetworkSerializeCalledOnWrite;
        public static bool NetworkSerializeCalledOnRead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsReader)
            {
                NetworkSerializeCalledOnRead = true;
            }
            else
            {
                NetworkSerializeCalledOnWrite = true;
            }
            serializer.SerializeValue(ref SomeInt);
            serializer.SerializeValue(ref SomeBool);
        }
    }

    public class NetworkVariableTest : NetworkBehaviour
    {
        public readonly NetworkVariable<int> TheScalar = new NetworkVariable<int>();
        public readonly NetworkList<int> TheList = new NetworkList<int>();
        public readonly NetworkList<FixedString128Bytes> TheLargeList = new NetworkList<FixedString128Bytes>();

        public readonly NetworkVariable<FixedString32Bytes> FixedString32 = new NetworkVariable<FixedString32Bytes>();

        private void ListChanged(NetworkListEvent<int> e)
        {
            ListDelegateTriggered = true;
        }

        public void Awake()
        {
            TheList.OnListChanged += ListChanged;
        }

        public readonly NetworkVariable<TestStruct> TheStruct = new NetworkVariable<TestStruct>();

        public bool ListDelegateTriggered;

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                NetworkVariableTests.ClientNetworkVariableTestSpawned(this);
            }
            base.OnNetworkSpawn();
        }
    }

    [TestFixture(true)]
    [TestFixture(false)]
    public class NetworkVariableTests : BaseMultiInstanceTest
    {
        private const string k_FixedStringTestValue = "abcdefghijklmnopqrstuvwxyz";
        protected override int NbClients => 2;

        private const uint k_TestUInt = 0x12345678;

        private const int k_TestVal1 = 111;
        private const int k_TestVal2 = 222;
        private const int k_TestVal3 = 333;

        private const int k_TestKey1 = 0x0f0f;

        private static List<NetworkVariableTest> s_ClientNetworkVariableTestInstances = new List<NetworkVariableTest>();
        public static void ClientNetworkVariableTestSpawned(NetworkVariableTest networkVariableTest)
        {
            s_ClientNetworkVariableTestInstances.Add(networkVariableTest);
        }

        // Player1 component on the server
        private NetworkVariableTest m_Player1OnServer;

        // Player1 component on client1
        private NetworkVariableTest m_Player1OnClient1;

        private bool m_EnsureLengthSafety;

        public NetworkVariableTests(bool ensureLengthSafety)
        {
            m_EnsureLengthSafety = ensureLengthSafety;
        }

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            m_BypassStartAndWaitForClients = true;

            yield return base.Setup();
        }

        /// <summary>
        /// This is an adjustment to how the server and clients are started in order
        /// to avoid timing issues when running in a stand alone test runner build.
        /// </summary>
        private IEnumerator InitializeServerAndClients(bool useHost)
        {
            s_ClientNetworkVariableTestInstances.Clear();
            m_PlayerPrefab.AddComponent<NetworkVariableTest>();

            m_ServerNetworkManager.NetworkConfig.EnsureNetworkVariableLengthSafety = m_EnsureLengthSafety;
            m_ServerNetworkManager.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            foreach (var client in m_ClientNetworkManagers)
            {
                client.NetworkConfig.EnsureNetworkVariableLengthSafety = m_EnsureLengthSafety;
                client.NetworkConfig.PlayerPrefab = m_PlayerPrefab;
            }

            Assert.True(MultiInstanceHelpers.Start(useHost, m_ServerNetworkManager, m_ClientNetworkManagers), "Failed to start server and client instances");

            RegisterSceneManagerHandler();

            // Wait for connection on client side
            yield return MultiInstanceHelpers.WaitForClientsConnected(m_ClientNetworkManagers);

            yield return m_DefaultWaitForTick;

            // Wait for connection on server side
            var clientsToWaitFor = useHost ? NbClients + 1 : NbClients;
            yield return MultiInstanceHelpers.WaitForClientsConnectedToServer(m_ServerNetworkManager, clientsToWaitFor);

            // These are the *SERVER VERSIONS* of the *CLIENT PLAYER 1 & 2*
            var result = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();

            yield return MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, result);

            // Assign server-side client's player
            m_Player1OnServer = result.Result.GetComponent<NetworkVariableTest>();

            // This is client1's view of itself
            yield return MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ClientNetworkManagers[0], result);

            // Assign client-side local player
            m_Player1OnClient1 = result.Result.GetComponent<NetworkVariableTest>();

            m_Player1OnServer.TheList.Clear();

            if (m_Player1OnServer.TheList.Count > 0)
            {
                throw new Exception("at least one server network container not empty at start");
            }
            if (m_Player1OnClient1.TheList.Count > 0)
            {
                throw new Exception("at least one client network container not empty at start");
            }

            var instanceCount = useHost ? NbClients * 3 : NbClients * 2;
            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut((c) => s_ClientNetworkVariableTestInstances.Count == c, instanceCount);

            Assert.False(s_GloabalTimeOutHelper.TimedOut, "Timed out waiting for all client NetworkVariableTest instances to register they have spawned!");

            yield return m_DefaultWaitForTick;
        }

        /// <summary>
        /// Runs generalized tests on all predefined NetworkVariable types
        /// </summary>
        [UnityTest]
        public IEnumerator AllNetworkVariableTypes([Values(true, false)] bool useHost)
        {
            NetworkManager server;
            // Create, instantiate, and host
            // This would normally go in Setup, but since every other test but this one
            //  uses MultiInstanceHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out server, useHost ? NetworkManagerHelper.NetworkManagerOperatingMode.Host : NetworkManagerHelper.NetworkManagerOperatingMode.Server));

            Assert.IsTrue(server.IsHost == useHost, $"{nameof(useHost)} does not match the server.IsHost value!");

            Guid gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");

            var networkVariableTestComponent = NetworkManagerHelper.AddComponentToObject<NetworkVariableTestComponent>(gameObjectId);

            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);

            // Start Testing
            networkVariableTestComponent.EnableTesting = true;

            yield return WaitForConditionOrTimeOut((c) => c == networkVariableTestComponent.IsTestComplete(), true);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, "Timed out waiting for the test to complete!");

            // Stop Testing
            networkVariableTestComponent.EnableTesting = false;

            Assert.IsTrue(networkVariableTestComponent.DidAllValuesChange());

            // Disable this once we are done.
            networkVariableTestComponent.gameObject.SetActive(false);

            // This would normally go in Teardown, but since every other test but this one
            //  uses MultiInstanceHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [UnityTest]
        public IEnumerator ClientWritePermissionTest([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            // client must not be allowed to write to a server auth variable
            Assert.Throws<InvalidOperationException>(() => m_Player1OnClient1.TheScalar.Value = k_TestVal1);
        }

        [UnityTest]
        public IEnumerator FixedString32Test([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_Player1OnServer.FixedString32.Value = k_FixedStringTestValue;

            // Wait a tick
            yield return m_DefaultWaitForTick;

            // Now wait for the client side version to be updated to k_FixedStringTestValue
            yield return WaitForConditionOrTimeOut((c) => m_Player1OnClient1.FixedString32.Value == c, k_FixedStringTestValue);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, "Timed out waiting for client-side NetworkVariable to update!");
        }

        [UnityTest]
        public IEnumerator NetworkListAdd([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_Player1OnServer.TheList.Add(k_TestVal1);
            m_Player1OnServer.TheList.Add(k_TestVal2);

            bool TestCompleted(int listCount)
            {
                return m_Player1OnServer.TheList.Count == listCount &&
                    m_Player1OnClient1.TheList.Count == listCount &&
                    m_Player1OnServer.ListDelegateTriggered &&
                    m_Player1OnClient1.ListDelegateTriggered &&
                    m_Player1OnServer.TheList[0] == k_TestVal1 &&
                    m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                    m_Player1OnServer.TheList[1] == k_TestVal2 &&
                    m_Player1OnClient1.TheList[1] == k_TestVal2;
            }

            yield return WaitForConditionOrTimeOut(TestCompleted, 2);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(NetworkListAdd)} to complete its test!");
        }

        [UnityTest]
        public IEnumerator WhenListContainsManyLargeValues_OverflowExceptionIsNotThrown([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            for (var i = 0; i < 20; ++i)
            {
                m_Player1OnServer.TheLargeList.Add(new FixedString128Bytes());
            }

            bool TestCompleted(int listCount)
            {
                return m_Player1OnServer.TheLargeList.Count == listCount &&
                       m_Player1OnClient1.TheLargeList.Count == listCount;
            }

            yield return WaitForConditionOrTimeOut(TestCompleted, 20);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(WhenListContainsManyLargeValues_OverflowExceptionIsNotThrown)} to complete its test!");
        }

        [UnityTest]
        public IEnumerator NetworkListContains([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_Player1OnServer.TheList.Add(k_TestVal1);

            bool TestCompleted(int listCount)
            {
                return m_Player1OnServer.TheList.Count == listCount &&
                       m_Player1OnClient1.TheList.Count == listCount &&
                       m_Player1OnServer.TheList.Contains(k_TestVal1) &&
                       m_Player1OnClient1.TheList.Contains(k_TestVal1);
            }

            yield return WaitForConditionOrTimeOut(TestCompleted, m_Player1OnServer.TheList.Count);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(NetworkListContains)} to complete its test!");
        }

        [UnityTest]
        public IEnumerator NetworkListRemoveValue([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            m_Player1OnServer.TheList.Add(k_TestVal1);
            m_Player1OnServer.TheList.Add(k_TestVal2);
            m_Player1OnServer.TheList.Add(k_TestVal3);

            // Check to verify the client has 3 elements in the list
            yield return WaitForConditionOrTimeOut((c) => m_Player1OnServer.TheList.Count == c && m_Player1OnClient1.TheList.Count == c, m_Player1OnServer.TheList.Count);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(NetworkListRemoveValue)} to check that " +
                $"client has {m_Player1OnServer.TheList.Count} elements before removing one!");

            // Now remove an element from the list
            m_Player1OnServer.TheList.Remove(k_TestVal2);

            // Test to assure the element was removed from the list on the client side
            // and that the values are correct
            bool TestCompleted(int listCount)
            {
                return m_Player1OnServer.TheList.Count == listCount &&
                       m_Player1OnClient1.TheList.Count == listCount &&
                       m_Player1OnServer.TheList[0] == k_TestVal1 &&
                       m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                       m_Player1OnServer.TheList[1] == k_TestVal3 &&
                       m_Player1OnClient1.TheList[1] == k_TestVal3;
            }

            yield return WaitForConditionOrTimeOut(TestCompleted, m_Player1OnServer.TheList.Count);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(NetworkListRemoveValue)} to check that " +
                $"the client removed the element from its list!");
        }

        [UnityTest]
        public IEnumerator NetworkListInsert([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            m_Player1OnServer.TheList.Add(k_TestVal1);
            m_Player1OnServer.TheList.Add(k_TestVal2);

            // Tests pre-insertion count and values as well
            // as post-insertion count and values
            bool TestCompleted(int listCount)
            {
                bool hasCorrectCountAndValues = m_Player1OnServer.TheList.Count == listCount &&
                       m_Player1OnClient1.TheList.Count == listCount;

                if (listCount == 2)
                {
                    hasCorrectCountAndValues = hasCorrectCountAndValues &&
                        m_Player1OnServer.ListDelegateTriggered &&
                        m_Player1OnClient1.ListDelegateTriggered &&
                        m_Player1OnServer.TheList[0] == k_TestVal1 &&
                        m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                        m_Player1OnServer.TheList[1] == k_TestVal2 &&
                        m_Player1OnClient1.TheList[1] == k_TestVal2;
                }
                else
                if (listCount == 3)
                {
                    hasCorrectCountAndValues = hasCorrectCountAndValues &&
                        m_Player1OnServer.ListDelegateTriggered &&
                        m_Player1OnClient1.ListDelegateTriggered &&
                        m_Player1OnServer.TheList[0] == k_TestVal1 &&
                        m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                        m_Player1OnServer.TheList[1] == k_TestVal3 &&
                        m_Player1OnClient1.TheList[1] == k_TestVal3 &&
                        m_Player1OnServer.TheList[2] == k_TestVal2 &&
                        m_Player1OnClient1.TheList[2] == k_TestVal2;
                }

                return hasCorrectCountAndValues;
            }

            // Wait for the client to have the initial number of entries and have their values validated
            yield return WaitForConditionOrTimeOut(TestCompleted, m_Player1OnServer.TheList.Count);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"{nameof(NetworkListInsert)} timed out waiting for client to have the initial number " +
                $"of entries ({m_Player1OnServer.TheList.Count}) in its list an the values validated!");

            // Now insert a new entry to the list on the server
            m_Player1OnServer.TheList.Insert(1, k_TestVal3);

            // Wait for the client to get updated and its values validated
            yield return WaitForConditionOrTimeOut(TestCompleted, m_Player1OnServer.TheList.Count);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"{nameof(NetworkListInsert)} timed out waiting for client to have the final number " +
                $"of entries ({m_Player1OnServer.TheList.Count}) in its list an the values validated!");
        }

        [UnityTest]
        public IEnumerator NetworkListIndexOf([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            m_Player1OnServer.TheList.Add(k_TestVal1);
            m_Player1OnServer.TheList.Add(k_TestVal2);
            m_Player1OnServer.TheList.Add(k_TestVal3);

            bool TestCompleted(int listCount)
            {
                return m_Player1OnServer.TheList.Count == listCount &&
                       m_Player1OnClient1.TheList.Count == listCount &&
                       m_Player1OnServer.TheList.IndexOf(k_TestVal1) == 0 &&
                       m_Player1OnClient1.TheList.IndexOf(k_TestVal1) == 0 &&
                       m_Player1OnServer.TheList.IndexOf(k_TestVal2) == 1 &&
                       m_Player1OnClient1.TheList.IndexOf(k_TestVal2) == 1 &&
                       m_Player1OnServer.TheList.IndexOf(k_TestVal3) == 2 &&
                       m_Player1OnClient1.TheList.IndexOf(k_TestVal3) == 2;
            }

            yield return WaitForConditionOrTimeOut(TestCompleted, 3);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(NetworkListIndexOf)} to complete its test!");
        }

        [UnityTest]
        public IEnumerator NetworkListArrayOperator([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            m_Player1OnServer.TheList.Add(k_TestVal3);
            m_Player1OnServer.TheList.Add(k_TestVal3);
            m_Player1OnServer.TheList[0] = k_TestVal1;
            m_Player1OnServer.TheList[1] = k_TestVal2;

            bool TestCompleted(int listCount)
            {
                return m_Player1OnServer.TheList.Count == listCount &&
                       m_Player1OnClient1.TheList.Count == listCount &&
                       m_Player1OnServer.TheList[0] == k_TestVal1 &&
                       m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                       m_Player1OnServer.TheList[1] == k_TestVal2 &&
                       m_Player1OnClient1.TheList[1] == k_TestVal2;
            }

            yield return WaitForConditionOrTimeOut(TestCompleted, 2);
            Assert.IsFalse(s_GloabalTimeOutHelper.TimedOut, $"Timed out waiting for {nameof(NetworkListArrayOperator)} to complete its test!");
        }


        [UnityTest]
        public IEnumerator NetworkListValueUpdate([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                },
                () =>
                {
                    return m_Player1OnServer.TheList.Count == 1 &&
                           m_Player1OnClient1.TheList.Count == 1 &&
                           m_Player1OnServer.TheList[0] == k_TestVal1 &&
                           m_Player1OnClient1.TheList[0] == k_TestVal1;
                }
            );

            var testSucceeded = false;

            void TestValueUpdatedCallback(NetworkListEvent<int> changedEvent)
            {
                testSucceeded = changedEvent.PreviousValue == k_TestVal1 &&
                                changedEvent.Value == k_TestVal3;
            }

            try
            {
                yield return MultiInstanceHelpers.RunAndWaitForCondition(
                    () =>
                    {
                        m_Player1OnServer.TheList[0] = k_TestVal3;
                        m_Player1OnClient1.TheList.OnListChanged += TestValueUpdatedCallback;
                    },
                    () =>
                    {
                        return m_Player1OnServer.TheList.Count == 1 &&
                               m_Player1OnClient1.TheList.Count == 1 &&
                               m_Player1OnServer.TheList[0] == k_TestVal3 &&
                               m_Player1OnClient1.TheList[0] == k_TestVal3;
                    }
                );
            }
            finally
            {
                m_Player1OnClient1.TheList.OnListChanged -= TestValueUpdatedCallback;
            }

            Assert.That(testSucceeded);
        }

        [UnityTest]
        public IEnumerator NetworkListIEnumerator([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            var correctVals = new int[3];
            correctVals[0] = k_TestVal1;
            correctVals[1] = k_TestVal2;
            correctVals[2] = k_TestVal3;

            m_Player1OnServer.TheList.Add(correctVals[0]);
            m_Player1OnServer.TheList.Add(correctVals[1]);
            m_Player1OnServer.TheList.Add(correctVals[2]);

            Assert.IsTrue(m_Player1OnServer.TheList.Count == 3);

            int index = 0;
            foreach (var val in m_Player1OnServer.TheList)
            {
                if (val != correctVals[index++])
                {
                    Assert.Fail();
                }
            }
        }

        [UnityTest]
        public IEnumerator NetworkListRemoveAt([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                    m_Player1OnServer.TheList.Add(k_TestVal2);
                    m_Player1OnServer.TheList.Add(k_TestVal3);
                    m_Player1OnServer.TheList.RemoveAt(1);
                },
                () =>
                {
                    return m_Player1OnServer.TheList.Count == 2 &&
                           m_Player1OnClient1.TheList.Count == 2 &&
                           m_Player1OnServer.TheList[0] == k_TestVal1 &&
                           m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                           m_Player1OnServer.TheList[1] == k_TestVal3 &&
                           m_Player1OnClient1.TheList[1] == k_TestVal3;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListClear([Values(true, false)] bool useHost)
        {
            // first put some stuff in; re-use the add test
            yield return NetworkListAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () => m_Player1OnServer.TheList.Clear(),
                () =>
                {
                    return
                        m_Player1OnServer.ListDelegateTriggered &&
                        m_Player1OnClient1.ListDelegateTriggered &&
                        m_Player1OnServer.TheList.Count == 0 &&
                        m_Player1OnClient1.TheList.Count == 0;
                }
            );
        }

        [UnityTest]
        public IEnumerator TestNetworkVariableStruct([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheStruct.Value =
                        new TestStruct() { SomeInt = k_TestUInt, SomeBool = false };
                    m_Player1OnServer.TheStruct.SetDirty(true);
                },
                () =>
                {
                    return
                        m_Player1OnClient1.TheStruct.Value.SomeBool == false &&
                        m_Player1OnClient1.TheStruct.Value.SomeInt == k_TestUInt;
                }
            );
        }

        [UnityTest]
        public IEnumerator TestINetworkSerializableCallsNetworkSerialize([Values(true, false)] bool useHost)
        {
            yield return InitializeServerAndClients(useHost);
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    TestStruct.NetworkSerializeCalledOnWrite = false;
                    TestStruct.NetworkSerializeCalledOnRead = false;
                    m_Player1OnServer.TheStruct.Value =
                        new TestStruct() { SomeInt = k_TestUInt, SomeBool = false };
                    m_Player1OnServer.TheStruct.SetDirty(true);
                },
                () =>
                {
                    return
                        TestStruct.NetworkSerializeCalledOnWrite &&
                        TestStruct.NetworkSerializeCalledOnRead;
                }
            );
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return base.Teardown();
        }
    }
}
