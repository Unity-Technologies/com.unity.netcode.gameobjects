using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Unity.Netcode.RuntimeTests
{
    public struct TestStruct : INetworkSerializable
    {
        public uint SomeInt;
        public bool SomeBool;

        public void NetworkSerialize(NetworkSerializer serializer)
        {
            serializer.Serialize(ref SomeInt);
            serializer.Serialize(ref SomeBool);
        }
    }
    public class NetworkVariableTest : NetworkBehaviour
    {
        public readonly ClientNetworkVariable<int> ClientVar = new ClientNetworkVariable<int>();

        public readonly NetworkVariable<int> TheScalar = new NetworkVariable<int>();
        public readonly NetworkList<int> TheList = new NetworkList<int>();
        public readonly NetworkSet<int> TheSet = new NetworkSet<int>();
        public readonly NetworkDictionary<int, int> TheDictionary = new NetworkDictionary<int, int>();

        private void ListChanged(NetworkListEvent<int> e)
        {
            ListDelegateTriggered = true;
        }
        private void SetChanged(NetworkSetEvent<int> e)
        {
            SetDelegateTriggered = true;
        }
        private void DictionaryChanged(NetworkDictionaryEvent<int, int> e)
        {
            DictionaryDelegateTriggered = true;
        }
        public void Awake()
        {
            TheList.OnListChanged += ListChanged;
            TheSet.OnSetChanged += SetChanged;
            TheDictionary.OnDictionaryChanged += DictionaryChanged;
        }

    public readonly NetworkVariable<TestStruct> TheStruct = new NetworkVariable<TestStruct>();

        public bool ListDelegateTriggered;
        public bool SetDelegateTriggered;
        public bool DictionaryDelegateTriggered;
    }

    public class NetworkVariableTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private const uint k_TestUInt = 0xdeadbeef;

        private const int k_TestVal1 = 111;
        private const int k_TestVal2 = 222;
        private const int k_TestVal3 = 333;
        private const int k_TestVal4 = 444;

        private const int k_TestKey1 = 0x0f0f;
        private const int k_TestKey2 = 0xf0f0;

        private NetworkVariableTest m_ServersPlayer1Comp;
        private NetworkVariableTest m_ServersPlayer2Comp;
        private NetworkVariableTest m_ClientComp;
        private NetworkVariableTest m_ClientComp2;

        private bool m_TestWithHost = false;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: m_TestWithHost, nbClients: NbClients,
                updatePlayerPrefab: playerPrefab =>
                {
                    var networkTransform = playerPrefab.AddComponent<NetworkVariableTest>();
                });

            // These are the *SERVER VERSIONS* of the *CLIENT PLAYER 1 & 2*
            var result1 = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            var result2 = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ServerNetworkManager, result1));
            m_ServersPlayer1Comp = result1.Result.GetComponent<NetworkVariableTest>();

            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[1].LocalClientId,
                m_ServerNetworkManager, result2));
            m_ServersPlayer2Comp = result2.Result.GetComponent<NetworkVariableTest>();

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER 1*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId,
                m_ClientNetworkManagers[0], clientClientPlayerResult));

            var clientSideClientPlayer = clientClientPlayerResult.Result;
            m_ClientComp = clientSideClientPlayer.GetComponent<NetworkVariableTest>();

            var clientClientPlayerResult2 = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(
                x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[1].LocalClientId,
                m_ClientNetworkManagers[1], clientClientPlayerResult2));

            var clientSideClientPlayer2 = clientClientPlayerResult2.Result;
            m_ClientComp2 = clientSideClientPlayer2.GetComponent<NetworkVariableTest>();

            m_ServersPlayer1Comp.TheList.Clear();
            m_ServersPlayer1Comp.TheSet.Clear();
            m_ServersPlayer1Comp.TheDictionary.Clear();

            if (m_ServersPlayer1Comp.TheList.Count > 0 || m_ServersPlayer1Comp.TheSet.Count > 0 || m_ServersPlayer1Comp.TheDictionary.Count > 0)
            {
                throw new Exception("at least one server network container not empty at start");
            }
            if (m_ClientComp.TheList.Count > 0 || m_ClientComp.TheSet.Count > 0 || m_ClientComp.TheDictionary.Count > 0)
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

            // server must not be allowed to write to a client auth variable
            Assert.Throws<InvalidOperationException>(() =>  m_ClientComp.TheScalar.Value = k_TestVal1);
        }

        [Test]
        public void ServerWritePermissionTest([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // server must not be allowed to write to a client auth variable
            Assert.Throws<InvalidOperationException>(() =>  m_ServersPlayer1Comp.ClientVar.Value = k_TestVal1);
        }

        [UnityTest]
        public IEnumerator ClientNetvarTest([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ClientComp.ClientVar.Value = k_TestVal2;
                    m_ClientComp2.ClientVar.Value = k_TestVal3;
                },
                () =>
                {
            // the client's values should win on the objects it owns
                    return
                        m_ServersPlayer1Comp.ClientVar.Value == k_TestVal2 &&
                        m_ServersPlayer2Comp.ClientVar.Value == k_TestVal3 &&
                        m_ClientComp.ClientVar.Value == k_TestVal2 &&
                        m_ClientComp2.ClientVar.Value == k_TestVal3;
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
                    m_ServersPlayer1Comp.TheList.Add(k_TestVal1);
                    m_ServersPlayer1Comp.TheList.Add(k_TestVal2);
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheList.Count == 2 &&
                        m_ClientComp.TheList.Count == 2 &&
                        m_ServersPlayer1Comp.ListDelegateTriggered &&
                        m_ClientComp.ListDelegateTriggered &&
                        m_ServersPlayer1Comp.TheList[0] == k_TestVal1 &&
                        m_ClientComp.TheList[0] == k_TestVal1 &&
                        m_ServersPlayer1Comp.TheList[1] == k_TestVal2 &&
                        m_ClientComp.TheList[1] == k_TestVal2;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkListRemove([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;
            // first put some stuff in; re-use the add test
            yield return NetworkListAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () => m_ServersPlayer1Comp.TheList.RemoveAt(0),
                () =>
                {
                    return m_ServersPlayer1Comp.TheList.Count == 1 &&
                           m_ClientComp.TheList.Count == 1 &&
                           m_ServersPlayer1Comp.ListDelegateTriggered &&
                           m_ClientComp.ListDelegateTriggered &&
                           m_ServersPlayer1Comp.TheList[0] == k_TestVal2 &&
                           m_ClientComp.TheList[0] == k_TestVal2;
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
                () => m_ServersPlayer1Comp.TheList.Clear(),
                () =>
                {
                    return
                        m_ServersPlayer1Comp.ListDelegateTriggered &&
                        m_ClientComp.ListDelegateTriggered &&
                        m_ServersPlayer1Comp.TheList.Count == 0 &&
                        m_ClientComp.TheList.Count == 0;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkSetAdd([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheSet.Add(k_TestVal1);
                    m_ServersPlayer1Comp.TheSet.Add(k_TestVal2);
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheSet.Count == 2 &&
                           m_ClientComp.TheSet.Count == 2 &&
                           m_ServersPlayer1Comp.SetDelegateTriggered &&
                           m_ClientComp.SetDelegateTriggered &&
                           m_ServersPlayer1Comp.TheSet.Contains(k_TestVal1) &&
                           m_ClientComp.TheSet.Contains(k_TestVal1) &&
                           m_ServersPlayer1Comp.TheSet.Contains(k_TestVal2) &&
                           m_ClientComp.TheSet.Contains(k_TestVal2);
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkSetRemove([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // first put some stuff in; re-use the add test
            yield return NetworkSetAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheSet.Remove(k_TestVal1);
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheSet.Count == 1 &&
                           m_ClientComp.TheSet.Count == 1 &&
                           m_ServersPlayer1Comp.SetDelegateTriggered &&
                           m_ClientComp.SetDelegateTriggered &&
                           m_ServersPlayer1Comp.TheSet.Contains(k_TestVal2) &&
                           m_ClientComp.TheSet.Contains(k_TestVal2);
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkSetClear([Values(true, false)] bool useHost)
        {
            // first put some stuff in; re-use the add test
            yield return NetworkSetAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheSet.Clear();
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheSet.Count == 0 &&
                           m_ClientComp.TheSet.Count == 0 &&
                           m_ServersPlayer1Comp.SetDelegateTriggered &&
                           m_ClientComp.SetDelegateTriggered;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkDictionaryAdd([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheDictionary.Add(k_TestKey1, k_TestVal1);
                    m_ServersPlayer1Comp.TheDictionary.Add(k_TestKey2, k_TestVal2);
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheDictionary.Count == 2 &&
                           m_ClientComp.TheDictionary.Count == 2 &&
                           m_ServersPlayer1Comp.DictionaryDelegateTriggered &&
                           m_ClientComp.DictionaryDelegateTriggered &&
                           m_ServersPlayer1Comp.TheDictionary[k_TestKey1] == k_TestVal1 &&
                           m_ClientComp.TheDictionary[k_TestKey1] == k_TestVal1 &&
                           m_ServersPlayer1Comp.TheDictionary[k_TestKey2] == k_TestVal2 &&
                           m_ClientComp.TheDictionary[k_TestKey2] == k_TestVal2;
                }
            );
        }

        /* Note, not adding coverage for RemovePair, because we plan to remove
         *  this in the next PR
         */
        [UnityTest]
        public IEnumerator NetworkDictionaryRemoveByKey([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // first put some stuff in; re-use the add test
            yield return NetworkDictionaryAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheDictionary.Remove(k_TestKey2);
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheDictionary.Count == 1 &&
                           m_ClientComp.TheDictionary.Count == 1 &&
                           m_ServersPlayer1Comp.DictionaryDelegateTriggered &&
                           m_ClientComp.DictionaryDelegateTriggered &&
                           m_ServersPlayer1Comp.TheDictionary[k_TestKey1] == k_TestVal1 &&
                           m_ClientComp.TheDictionary[k_TestKey1] == k_TestVal1;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkDictionaryChangeValue([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // first put some stuff in; re-use the add test
            yield return NetworkDictionaryAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheDictionary[k_TestKey1] = k_TestVal3;
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheDictionary.Count == 2 &&
                           m_ClientComp.TheDictionary.Count == 2 &&
                           m_ServersPlayer1Comp.DictionaryDelegateTriggered &&
                           m_ClientComp.DictionaryDelegateTriggered &&
                           m_ServersPlayer1Comp.TheDictionary[k_TestKey1] == k_TestVal3 &&
                           m_ClientComp.TheDictionary[k_TestKey1] == k_TestVal3;
                }
            );
        }

        [UnityTest]
        public IEnumerator NetworkDictionaryClear([Values(true, false)] bool useHost)
        {
            m_TestWithHost = useHost;

            // first put some stuff in; re-use the add test
            yield return NetworkDictionaryAdd(useHost);

            yield return MultiInstanceHelpers.RunAndWaitForCondition(
                () =>
                {
                    m_ServersPlayer1Comp.TheDictionary.Clear();
                },
                () =>
                {
                    return m_ServersPlayer1Comp.TheDictionary.Count == 0 &&
                           m_ClientComp.TheDictionary.Count == 0 &&
                           m_ServersPlayer1Comp.DictionaryDelegateTriggered &&
                           m_ClientComp.DictionaryDelegateTriggered;
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
                    m_ServersPlayer1Comp.TheStruct.Value =
                        new TestStruct() { SomeInt = k_TestUInt, SomeBool = false };
                    m_ServersPlayer1Comp.TheStruct.SetDirty(true);
                },
                () =>
                {
                    return
                        m_ClientComp.TheStruct.Value.SomeBool == false &&
                        m_ClientComp.TheStruct.Value.SomeInt == k_TestUInt;
                }
            );
        }

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return base.Teardown();
            UnityEngine.Object.Destroy(m_PlayerPrefab);
        }
    }
}
