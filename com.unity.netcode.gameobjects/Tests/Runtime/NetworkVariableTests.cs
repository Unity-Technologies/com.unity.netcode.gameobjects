using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Netcode.RuntimeTests
{
    public struct TestStruct : INetworkSerializable
    {
        public uint SomeInt;
        public bool SomeBool;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SomeInt);
            serializer.SerializeValue(ref SomeBool);
        }
    }

    public class NetworkVariableTest : NetworkBehaviour
    {
        public readonly NetworkVariable<int> TheScalar = new NetworkVariable<int>();
        public readonly NetworkList<int> TheList = new NetworkList<int>();

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
    }

    public class NetworkVariableTests : BaseMultiInstanceTest
    {
        private const string k_FixedStringTestValue = "abcdefghijklmnopqrstuvwxyz";
        protected override int NbClients => 2;

        private const uint k_TestUInt = 0x12345678;

        private const int k_TestVal1 = 111;
        private const int k_TestVal2 = 222;
        private const int k_TestVal3 = 333;

        private const int k_TestKey1 = 0x0f0f;

        // Player1 component on the server
        private NetworkVariableTest m_Player1OnServer;

        // Player1 component on client1
        private NetworkVariableTest m_Player1OnClient1;

        private bool m_TestWithHost;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: m_TestWithHost, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    playerPrefab.AddComponent<NetworkVariableTest>();
                });

            // These are the *SERVER VERSIONS* of the *CLIENT PLAYER 1 & 2*
            var result = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, result));
            m_Player1OnServer = result.Result.GetComponent<NetworkVariableTest>();

            // This is client1's view of itself
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ClientNetworkManagers[0], result));

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
        }

        /// <summary>
        /// Runs generalized tests on all predefined NetworkVariable types
        /// </summary>
        [UnityTest]
        public IEnumerator AllNetworkVariableTypes([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // Create, instantiate, and host
            // This would normally go in Setup, but since every other test but this one
            //  uses MultiInstanceHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            Assert.IsTrue(NetworkManagerHelper.StartNetworkManager(out _));

            Guid gameObjectId = NetworkManagerHelper.AddGameNetworkObject("NetworkVariableTestComponent");

            var networkVariableTestComponent = NetworkManagerHelper.AddComponentToObject<NetworkVariableTestComponent>(gameObjectId);

            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);

            // Start Testing
            networkVariableTestComponent.EnableTesting = true;

            var testsAreComplete = networkVariableTestComponent.IsTestComplete();

            // Wait for the NetworkVariable tests to complete
            while (!testsAreComplete)
            {
                yield return new WaitForSeconds(0.003f);
                testsAreComplete = networkVariableTestComponent.IsTestComplete();
            }

            // Stop Testing
            networkVariableTestComponent.EnableTesting = false;

            Assert.IsTrue(networkVariableTestComponent.DidAllValuesChange());

            // Disable this once we are done.
            networkVariableTestComponent.gameObject.SetActive(false);

            Assert.IsTrue(testsAreComplete);

            // This would normally go in Teardown, but since every other test but this one
            //  uses MultiInstanceHelper, and it does its own NetworkManager setup / teardown,
            //  for now we put this within this one test until we migrate it to MIH
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [Test]
        public void ClientWritePermissionTest([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // client must not be allowed to write to a server auth variable
            Assert.Throws<InvalidOperationException>(() => m_Player1OnClient1.TheScalar.Value = k_TestVal1);
        }

        [UnityTest]
        public IEnumerator FixedString32Test([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.FixedString32.Value = k_FixedStringTestValue;

                    // we are writing to the private and public variables on player 1's object...
                },
                () =>
                {

                    // ...and we should see the writes to the private var only on the server & the owner,
                    //  but the public variable everywhere
                    return
                        m_Player1OnClient1.FixedString32.Value == k_FixedStringTestValue;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListAdd([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                    m_Player1OnServer.TheList.Add(k_TestVal2);
                },
                () =>
                {
                    return m_Player1OnServer.TheList.Count == 2 &&
                        m_Player1OnClient1.TheList.Count == 2 &&
                        m_Player1OnServer.ListDelegateTriggered &&
                        m_Player1OnClient1.ListDelegateTriggered &&
                        m_Player1OnServer.TheList[0] == k_TestVal1 &&
                        m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                        m_Player1OnServer.TheList[1] == k_TestVal2 &&
                        m_Player1OnClient1.TheList[1] == k_TestVal2;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListContains([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                },
                () =>
                {
                    return m_Player1OnServer.TheList.Count == 1 &&
                           m_Player1OnClient1.TheList.Count == 1 &&
                           m_Player1OnServer.TheList.Contains(k_TestKey1) &&
                           m_Player1OnClient1.TheList.Contains(k_TestKey1);
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListRemoveValue([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                    m_Player1OnServer.TheList.Add(k_TestVal2);
                    m_Player1OnServer.TheList.Add(k_TestVal3);
                    m_Player1OnServer.TheList.Remove(k_TestVal2);
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
        public IEnumerator NetworkListInsert([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                    m_Player1OnServer.TheList.Add(k_TestVal2);
                    m_Player1OnServer.TheList.Insert(1, k_TestVal3);
                },
                () =>
                {
                    return m_Player1OnServer.TheList.Count == 3 &&
                           m_Player1OnClient1.TheList.Count == 3 &&
                           m_Player1OnServer.ListDelegateTriggered &&
                           m_Player1OnClient1.ListDelegateTriggered &&
                           m_Player1OnServer.TheList[0] == k_TestVal1 &&
                           m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                           m_Player1OnServer.TheList[1] == k_TestVal3 &&
                           m_Player1OnClient1.TheList[1] == k_TestVal3 &&
                           m_Player1OnServer.TheList[2] == k_TestVal2 &&
                           m_Player1OnClient1.TheList[2] == k_TestVal2;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListIndexOf([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal1);
                    m_Player1OnServer.TheList.Add(k_TestVal2);
                    m_Player1OnServer.TheList.Add(k_TestVal3);
                },
                () =>
                {
                    return m_Player1OnServer.TheList.IndexOf(k_TestVal1) == 0 &&
                           m_Player1OnClient1.TheList.IndexOf(k_TestVal1) == 0 &&
                           m_Player1OnServer.TheList.IndexOf(k_TestVal2) == 1 &&
                           m_Player1OnClient1.TheList.IndexOf(k_TestVal2) == 1 &&
                           m_Player1OnServer.TheList.IndexOf(k_TestVal3) == 2 &&
                           m_Player1OnClient1.TheList.IndexOf(k_TestVal3) == 2;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListArrayOperator([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_Player1OnServer.TheList.Add(k_TestVal3);
                    m_Player1OnServer.TheList.Add(k_TestVal3);
                    m_Player1OnServer.TheList[0] = k_TestVal1;
                    m_Player1OnServer.TheList[1] = k_TestVal2;
                },
                () =>
                {
                    return m_Player1OnServer.TheList.Count == 2 &&
                           m_Player1OnClient1.TheList.Count == 2 &&
                           m_Player1OnServer.TheList[0] == k_TestVal1 &&
                           m_Player1OnClient1.TheList[0] == k_TestVal1 &&
                           m_Player1OnServer.TheList[1] == k_TestVal2 &&
                           m_Player1OnClient1.TheList[1] == k_TestVal2;
                }
            );
        }

        [Test]
        public void NetworkListIEnumerator([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
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
            m_TestWithHost = useHost;

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
            m_TestWithHost = useHost;

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
            m_TestWithHost = useHost;
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

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return base.Teardown();
        }
    }
}
