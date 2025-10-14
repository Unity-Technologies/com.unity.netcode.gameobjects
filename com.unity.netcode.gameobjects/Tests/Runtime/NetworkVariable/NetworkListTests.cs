using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkListTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 3;

        public NetworkListTests(HostOrServer host) : base(host) { }

        private GameObject m_ListObjectPrefab;

        private List<int> m_ExpectedValues = new();

        private ulong m_TestObjectId;

        protected override void OnServerAndClientsCreated()
        {
            m_ListObjectPrefab = CreateNetworkObjectPrefab("ListObject");
            m_ListObjectPrefab.AddComponent<NetworkListTest>();

            base.OnServerAndClientsCreated();
        }

        private bool OnVerifyData(StringBuilder errorLog)
        {
            foreach (var manager in m_NetworkManagers)
            {
                if (!manager.SpawnManager.SpawnedObjects.TryGetValue(m_TestObjectId, out NetworkObject networkObject))
                {
                    errorLog.Append($"[Client-{manager.LocalClientId}] Test object was not spawned");
                    return false;
                }

                var listComponent = networkObject.GetComponent<NetworkListTest>();
                if (listComponent == null)
                {
                    errorLog.Append($"[Client-{manager.LocalClientId}] List component was not found");
                    return false;
                }

                if (m_ExpectedValues.Count != listComponent.TheList.Count)
                {
                    errorLog.Append($"[Client-{manager.LocalClientId}] List component has the incorrect number of items. Expected: {m_ExpectedValues.Count}, Have: {listComponent.TheList.Count}");
                    return false;
                }

                for (int i = 0; i < m_ExpectedValues.Count; i++)
                {
                    var expectedValue = m_ExpectedValues[i];
                    var actual = listComponent.TheList[i];

                    if (expectedValue != actual)
                    {
                        errorLog.Append($"[Client-{manager.LocalClientId}] Incorrect value at index {i}, expected: {expectedValue}, actual: {actual}");
                        return false;
                    }
                }
            }

            return true;
        }

        [UnityTest]
        public IEnumerator ValidateNetworkListSynchronization()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();
            var instantiatedObject = SpawnObject(m_ListObjectPrefab, authority).GetComponent<NetworkObject>();
            m_TestObjectId = instantiatedObject.NetworkObjectId;

            yield return WaitForSpawnedOnAllOrTimeOut(instantiatedObject);
            AssertOnTimeout("[Setup] Failed to spawn list object");

            Assert.IsTrue(nonAuthority.SpawnManager.SpawnedObjects.TryGetValue(instantiatedObject.NetworkObjectId, out var nonAuthorityObject));

            var authorityInstance = instantiatedObject.GetComponent<NetworkListTest>();
            var nonAuthorityInstance = nonAuthorityObject.GetComponent<NetworkListTest>();

            m_ExpectedValues.Clear();

            // NetworkList.Add
            for (int i = 0; i < 10; i++)
            {
                var val = Random.Range(0, 1234);
                m_ExpectedValues.Add(val);

                authorityInstance.TheList.Add(val);
            }

            yield return WaitForConditionOrTimeOut(OnVerifyData);
            AssertOnTimeout("[Add] Failed to add items to list");

            // NetworkList.Contains
            foreach (var expectedValue in m_ExpectedValues)
            {
                Assert.IsTrue(authorityInstance.TheList.Contains(expectedValue), $"[Contains][Client-{authority.LocalClientId}] Does not contain {expectedValue}");
                Assert.IsTrue(nonAuthorityInstance.TheList.Contains(expectedValue), $"[Contains][Client-{nonAuthority.LocalClientId}] Does not contain {expectedValue}");
            }

            // NetworkList.Insert
            for (int i = 0; i < 5; i++)
            {
                var indexToInsert = Random.Range(0, m_ExpectedValues.Count);
                var valToInsert = Random.Range(1, 99);
                m_ExpectedValues.Insert(indexToInsert, valToInsert);

                authorityInstance.TheList.Insert(indexToInsert, valToInsert);
            }

            yield return WaitForConditionOrTimeOut(OnVerifyData);
            AssertOnTimeout("[Insert] Failed to insert items to list");


            // NetworkList.IndexOf
            foreach (var testValue in Shuffle(m_ExpectedValues))
            {
                var expectedIndex = m_ExpectedValues.IndexOf(testValue);

                Assert.AreEqual(expectedIndex, authorityInstance.TheList.IndexOf(testValue), $"[IndexOf][Client-{authority.LocalClientId}] Has incorrect index for {testValue}");
                Assert.AreEqual(expectedIndex, nonAuthorityInstance.TheList.IndexOf(testValue), $"[IndexOf][Client-{nonAuthority.LocalClientId}] Has incorrect index for {testValue}");
            }

            // NetworkList[index] getter and setter
            foreach (var testValue in Shuffle(m_ExpectedValues))
            {
                var testIndex = m_ExpectedValues.IndexOf(testValue);

                // Set up our original and previous
                var previousValue = testValue;
                var updatedValue = previousValue + 10;

                m_ExpectedValues[testIndex] = updatedValue;

                Assert.AreEqual(testValue, authorityInstance.TheList[testIndex], $"[Get][Client-{authority.LocalClientId}] incorrect index get");
                Assert.AreEqual(testValue, nonAuthorityInstance.TheList[testIndex], $"[Get][Client-{nonAuthority.LocalClientId}] incorrect index get");

                var callbackSucceeded = false;

                // Callback that verifies the changed event occurred and that the original and new values are correct
                void TestValueUpdatedCallback(NetworkListEvent<int> changedEvent)
                {
                    nonAuthorityInstance.TheList.OnListChanged -= TestValueUpdatedCallback;

                    callbackSucceeded = changedEvent.PreviousValue == previousValue &&
                                    changedEvent.Value == updatedValue;
                }

                // Subscribe to the OnListChanged event on the client side and
                nonAuthorityInstance.TheList.OnListChanged += TestValueUpdatedCallback;
                authorityInstance.TheList[testIndex] = updatedValue;

                yield return WaitForConditionOrTimeOut(() => callbackSucceeded);
                AssertOnTimeout($"[OnListChanged][Client-{nonAuthority.LocalClientId}] client callback was not called");
            }

            yield return WaitForConditionOrTimeOut(OnVerifyData);
            AssertOnTimeout("[NetworkList[index]] Failed to get/set items at index in list");

            /*
             * NetworkList.Set with same value (forced and non-forced updates)
             */
            var expectedUpdateCount = 0;
            var actualUpdateCount = 0;

            // Callback that verifies the changed event occurred and that the original and new values are correct
            void TestForceUpdateCallback(NetworkListEvent<int> _)
            {
                actualUpdateCount++;
            }
            nonAuthorityInstance.TheList.OnListChanged += TestForceUpdateCallback;

            foreach (var testValue in Shuffle(m_ExpectedValues))
            {
                var testIndex = m_ExpectedValues.IndexOf(testValue);
                var forceUpdate = testIndex % 2 == 0;
                if (forceUpdate)
                {
                    expectedUpdateCount++;
                }
                // Subscribe to the OnListChanged event on the client side and
                authorityInstance.TheList.Set(testIndex, testValue, forceUpdate);
            }

            yield return WaitForConditionOrTimeOut(() => actualUpdateCount == expectedUpdateCount);
            AssertOnTimeout($"[OnListChanged][Client-{nonAuthority.LocalClientId}] OnListChanged update was called an incorrect number of times");
            nonAuthorityInstance.TheList.OnListChanged -= TestForceUpdateCallback;

            /*
             * NetworkList.Remove and NetworkList.RemoveAt
             */
            foreach (var testValue in Shuffle(m_ExpectedValues))
            {
                var testIndex = m_ExpectedValues.IndexOf(testValue);

                // Add a new value to the end to ensure the list isn't depleted
                var newValue = Random.Range(0, 99);
                m_ExpectedValues.Add(newValue);
                authorityInstance.TheList.Add(newValue);

                var removeAt = testIndex % 2 == 0;
                if (removeAt)
                {
                    m_ExpectedValues.RemoveAt(testIndex);
                    authorityInstance.TheList.RemoveAt(testIndex);
                }
                else
                {
                    m_ExpectedValues.Remove(testValue);
                    authorityInstance.TheList.Remove(testValue);
                }
            }
            yield return WaitForConditionOrTimeOut(OnVerifyData);
            AssertOnTimeout($"[Remove] List is incorrect after removing items");

            /*
             * NetworkList.Clear
             */
            m_ExpectedValues.Clear();
            authorityInstance.TheList.Clear();

            yield return WaitForConditionOrTimeOut(OnVerifyData);
            AssertOnTimeout($"[Clear] List is incorrect after clearing items");
        }

        // don't extend this please
        [UnityTest]
        public IEnumerator LegacyPredicateTesting()
        {
            var authority = GetAuthorityNetworkManager();
            var nonAuthority = GetNonAuthorityNetworkManager();

            var instantiatedObject = SpawnObject(m_ListObjectPrefab, authority).GetComponent<NetworkObject>();

            yield return WaitForSpawnedOnAllOrTimeOut(instantiatedObject);
            AssertOnTimeout("Failed to spawn list object");

            Assert.IsTrue(nonAuthority.SpawnManager.SpawnedObjects.TryGetValue(instantiatedObject.NetworkObjectId, out var nonAuthorityObject));

            var authorityInstance = instantiatedObject.GetComponent<NetworkListTest>();
            var nonAuthorityInstance = nonAuthorityObject.GetComponent<NetworkListTest>();

            // WhenListContainsManyLargeValues_OverflowExceptionIsNotThrown
            var overflowPredicate = new NetworkListTestPredicate(authorityInstance, nonAuthorityInstance, 20);
            yield return WaitForConditionOrTimeOut(overflowPredicate);
            AssertOnTimeout("Overflow exception shouldn't be thrown when adding many large values");

            /*
             * NetworkList Struct
             */
            bool VerifyList()
            {
                return nonAuthorityInstance.TheStructList.Count == authorityInstance.TheStructList.Count &&
                       nonAuthorityInstance.TheStructList[0].Value == authorityInstance.TheStructList[0].Value &&
                       nonAuthorityInstance.TheStructList[1].Value == authorityInstance.TheStructList[1].Value;
            }

            authorityInstance.TheStructList.Add(new StructUsedOnlyInNetworkList { Value = 1 });
            authorityInstance.TheStructList.Add(new StructUsedOnlyInNetworkList { Value = 2 });
            authorityInstance.TheStructList.SetDirty(true);

            // Wait for the client-side to notify it is finished initializing and spawning.
            yield return WaitForConditionOrTimeOut(VerifyList);
            AssertOnTimeout("All list values should match between clients");
        }

        private int[] Shuffle(List<int> list)
        {
            var rng = new System.Random();

            // Order the list by a progression of random numbers
            // This will do a shuffle of the list
            return list.OrderBy(_ => rng.Next()).ToArray();
        }
    }

    internal class NetworkListTest : NetworkBehaviour
    {
        public readonly NetworkList<int> TheList = new();
        public readonly NetworkList<StructUsedOnlyInNetworkList> TheStructList = new();
        public readonly NetworkList<FixedString128Bytes> TheLargeList = new();

        private void ListChanged(NetworkListEvent<int> e)
        {
            ListDelegateTriggered = true;
        }

        public void Awake()
        {
            TheList.OnListChanged += ListChanged;
        }

        public override void OnDestroy()
        {
            TheList.OnListChanged -= ListChanged;
            base.OnDestroy();
        }

        public bool ListDelegateTriggered;
    }


    /// <summary>
    /// Handles the more generic conditional logic for NetworkList tests
    /// which can be used with the NetcodeIntegrationTest.WaitForConditionOrTimeOut
    /// that accepts anything derived from the <see cref="ConditionalPredicateBase"/> class
    /// as a parameter.
    /// </summary>
    internal class NetworkListTestPredicate : ConditionalPredicateBase
    {
        private readonly NetworkListTest m_AuthorityInstance;

        private readonly NetworkListTest m_NonAuthorityInstance;

        private string m_TestStageFailedMessage;

        /// <summary>
        /// Determines if the condition has been reached for the current NetworkListTestState
        /// </summary>
        protected override bool OnHasConditionBeenReached()
        {
            return OnContainsLarge() && OnVerifyData();
        }

        /// <summary>
        /// Provides all information about the players for both sides for simplicity and informative sake.
        /// </summary>
        /// <returns></returns>
        private string ConditionFailedInfo()
        {
            return $"[ContainsLarge] condition test failed:\n Server List Count: {m_AuthorityInstance.TheList.Count} vs  Client List Count: {m_NonAuthorityInstance.TheList.Count}\n" +
                $"Server List Count: {m_AuthorityInstance.TheLargeList.Count} vs  Client List Count: {m_NonAuthorityInstance.TheLargeList.Count}\n" +
                $"Server Delegate Triggered: {m_AuthorityInstance.ListDelegateTriggered} | Client Delegate Triggered: {m_NonAuthorityInstance.ListDelegateTriggered}\n";
        }

        /// <summary>
        /// When finished, check if a time out occurred and if so assert and provide meaningful information to troubleshoot why
        /// </summary>
        protected override void OnFinished()
        {
            Assert.IsFalse(TimedOut, $"{nameof(NetworkListTestPredicate)} timed out waiting for the ContainsLarge condition to be reached! \n" + ConditionFailedInfo());
        }

        // Uses the ArrayOperator and validates that on both sides the count and values are the same
        private bool OnVerifyData()
        {
            // Wait until both sides have the same number of elements
            if (m_AuthorityInstance.TheLargeList.Count != m_NonAuthorityInstance.TheLargeList.Count)
            {
                return false;
            }

            // Check the client values against the server values to make sure they match
            for (int i = 0; i < m_AuthorityInstance.TheLargeList.Count; i++)
            {
                if (m_AuthorityInstance.TheLargeList[i] != m_NonAuthorityInstance.TheLargeList[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The current version of this test only verified the count of the large list, so that is what this does
        /// </summary>
        private bool OnContainsLarge()
        {
            return m_AuthorityInstance.TheLargeList.Count == m_NonAuthorityInstance.TheLargeList.Count && OnVerifyData();
        }

        private const string k_CharSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static string GenerateRandomString(int length)
        {
            var charArray = k_CharSet.Distinct().ToArray();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = charArray[RandomNumberGenerator.GetInt32(charArray.Length)];
            }

            return new string(result);
        }

        public NetworkListTestPredicate(NetworkListTest authorityInstance, NetworkListTest nonAuthorityInstance, int elementCount)
        {
            m_AuthorityInstance = authorityInstance;
            m_NonAuthorityInstance = nonAuthorityInstance;

            for (var i = 0; i < elementCount; ++i)
            {
                m_AuthorityInstance.TheLargeList.Add(new FixedString128Bytes(GenerateRandomString(Random.Range(0, 99))));
            }
        }
    }

}
