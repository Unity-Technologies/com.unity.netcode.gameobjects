using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Unit tests to test:
    /// - Serializing NetworkObject to NetworkObjectReference
    /// - Deserializing NetworkObjectReference to NetworkObject
    /// - Implicit operators of NetworkObjectReference
    /// </summary>
    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkBehaviourReferenceTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        private GameObject m_TestPrefab;

        private TestNetworkBehaviour m_AuthorityValidatingInstance;
        private TestNetworkBehaviour m_BehaviourToUseAsReference;

        public NetworkBehaviourReferenceTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("TestBehaviour");
            m_TestPrefab.AddComponent<TestNetworkBehaviour>();
            base.OnServerAndClientsCreated();
        }

        #region Tests using non-null NetworkBehaviours and RPCs
        private bool RpcWasReceivedAndBehaviourValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_AuthorityValidatingInstance.NetworkObjectId;
            var authorityReferenceId = m_BehaviourToUseAsReference.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            var referenceNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject)
                    && networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityReferenceId, out referenceNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();
                    var referenceBehaviour = referenceNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.ReceivedRPC)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the RPC!");
                        continue;
                    }
                    if (!validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} failed to acquire the reference!");
                        continue;
                    }
                    if (referenceBehaviour != validatingBehaviour.RpcReceivedBehaviour)
                    {
                        var currentReferenceBehaviour = validatingBehaviour.RpcReceivedBehaviour != null ? validatingBehaviour.RpcReceivedBehaviour.name : "null";
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference {referenceBehaviour.name} but was {currentReferenceBehaviour}!");
                    }
                }
                else
                {
                    var wasNotSpawned = validatingNetworkObject == null ? m_AuthorityValidatingInstance.name : m_BehaviourToUseAsReference.name;
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {wasNotSpawned}!");
                }
                validatingNetworkObject = null;
                referenceNetworkObject = null;
            }

            return stringBuilder.Length == 0;
        }


        [UnityTest]
        public IEnumerator TestRpc()
        {
            var authority = GetAuthorityNetworkManager();
            m_AuthorityValidatingInstance = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            m_BehaviourToUseAsReference = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            var objectsToSpawn = new List<NetworkObject>()
            {
                m_AuthorityValidatingInstance.NetworkObject,
                m_BehaviourToUseAsReference.NetworkObject
            };

            // Spawn the instances
            yield return WaitForSpawnedOnAllOrTimeOut(objectsToSpawn);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}] Not all clients spawned all instances of {m_AuthorityValidatingInstance.name} or {m_BehaviourToUseAsReference.name}");

            // Explicitly send the NetworkBehaviour as a reference
            m_AuthorityValidatingInstance.SendReferenceRpc(new NetworkBehaviourReference(m_BehaviourToUseAsReference));

            // Validated the reference
            yield return WaitForConditionOrTimeOut(RpcWasReceivedAndBehaviourValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");
        }


        [UnityTest]
        public IEnumerator TestRpcImplicitNetworkBehaviour()
        {
            var authority = GetAuthorityNetworkManager();
            m_AuthorityValidatingInstance = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            m_BehaviourToUseAsReference = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            var objectsToSpawn = new List<NetworkObject>()
            {
                m_AuthorityValidatingInstance.NetworkObject,
                m_BehaviourToUseAsReference.NetworkObject
            };

            // Spawn the instances
            yield return WaitForSpawnedOnAllOrTimeOut(objectsToSpawn);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}] Not all clients spawned all instances of {m_AuthorityValidatingInstance.name} or {m_BehaviourToUseAsReference.name}");

            // Implicitly send the NetworkBehaviour as a reference
            m_AuthorityValidatingInstance.SendReferenceRpc(m_BehaviourToUseAsReference);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(RpcWasReceivedAndBehaviourValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");
        }
        #endregion

        #region Tests using non-null NetworkBehaviours and NetworkVariable
        private bool NetworkVariableChangedAndBehaviourValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_AuthorityValidatingInstance.NetworkObjectId;
            var authorityReferenceId = m_BehaviourToUseAsReference.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            var referenceNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject)
                    && networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityReferenceId, out referenceNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();
                    var referenceBehaviour = referenceNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.TestVariableChanged)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the NetworkVariable update!");
                        continue;
                    }
                    if (!validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} failed to acquire a reference!");
                        continue;
                    }
                    if (referenceBehaviour != validatingBehaviour.TestVariableBehaviour)
                    {
                        var currentReferenceBehaviour = validatingBehaviour.TestVariableBehaviour != null ? validatingBehaviour.RpcReceivedBehaviour.name : "null";
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference {referenceBehaviour.name} but was {currentReferenceBehaviour}!");
                    }
                }
                else
                {
                    var wasNotSpawned = validatingNetworkObject == null ? m_AuthorityValidatingInstance.name : m_BehaviourToUseAsReference.name;
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {wasNotSpawned}!");
                }
                validatingNetworkObject = null;
                referenceNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }


        [UnityTest]
        public IEnumerator TestNetworkVariable()
        {
            var authority = GetAuthorityNetworkManager();
            m_AuthorityValidatingInstance = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            m_BehaviourToUseAsReference = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            var objectsToSpawn = new List<NetworkObject>()
            {
                m_AuthorityValidatingInstance.NetworkObject,
                m_BehaviourToUseAsReference.NetworkObject
            };

            // Spawn the instances
            yield return WaitForSpawnedOnAllOrTimeOut(objectsToSpawn);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}] Not all clients spawned all instances of {m_AuthorityValidatingInstance.name} or {m_BehaviourToUseAsReference.name}");

            // Assure the authority instance's value is the default (null) value
            Assert.IsNull((NetworkBehaviour)m_AuthorityValidatingInstance.TestVariable.Value);

            // Implicitly assign the NetworkBehaviourReference by assigning the NetworkBehaviour to the NetworkVariable.
            m_AuthorityValidatingInstance.TestVariable.Value = m_BehaviourToUseAsReference;

            // Validated the NetworkVariable reference propogates to clients
            yield return WaitForConditionOrTimeOut(NetworkVariableChangedAndBehaviourValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");
        }
        #endregion

        #region Validating using NULL as a NetworkBehaviourReference
        private bool RpcSerializingNullValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_AuthorityValidatingInstance.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.ReceivedRPC)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not received the RPC!");
                        continue;
                    }
                    if (validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} Acquired a reference when it should not have!");
                        continue;
                    }
                    if (validatingBehaviour.RpcReceivedBehaviour != null)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference NULL but was {validatingBehaviour.RpcReceivedBehaviour.name}!");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {m_AuthorityValidatingInstance.name}!");
                }
                validatingNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }

        private bool NetworkVariableSerializingNullValidated(StringBuilder stringBuilder)
        {
            var authorityValidatingId = m_AuthorityValidatingInstance.NetworkObjectId;
            var validatingNetworkObject = (NetworkObject)null;
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.TryGetValue(authorityValidatingId, out validatingNetworkObject))
                {
                    var validatingBehaviour = validatingNetworkObject.GetComponent<TestNetworkBehaviour>();

                    if (!validatingBehaviour.TestVariableChanged)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} has not detected a change in the NetworkVariable!");
                        continue;
                    }
                    if (validatingBehaviour.AcquiredReference)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} Acquired a reference when it should not have!");
                        continue;
                    }
                    if (validatingBehaviour.TestVariableBehaviour != null)
                    {
                        stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} {validatingBehaviour.name} expected behaviour reference NULL but was {validatingBehaviour.TestVariableBehaviour.name}!");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"Client-{networkManager.LocalClientId} has not yet spawned {m_AuthorityValidatingInstance.name}!");
                }
                validatingNetworkObject = null;
            }
            return stringBuilder.Length == 0;
        }


        [UnityTest]
        public IEnumerator TestSerializeNull()
        {
            var authority = GetAuthorityNetworkManager();
            m_AuthorityValidatingInstance = SpawnObject(m_TestPrefab, authority).GetComponent<TestNetworkBehaviour>();
            yield return WaitForSpawnedOnAllOrTimeOut(m_AuthorityValidatingInstance.gameObject);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}] Not all clients spawned {m_AuthorityValidatingInstance.name}");
            // Initialize with NULL parameter
            var initializeWithNull = new NetworkBehaviourReference(null);
            var initializeWithNothing = new NetworkBehaviourReference();

            m_AuthorityValidatingInstance.SendReferenceRpc(initializeWithNull);
            yield return WaitForConditionOrTimeOut(RpcSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}][Initialize with null parameter] Failed to validate null {nameof(NetworkBehaviour)} reference!");

            // Reset the RPC NetworkBehaviourReference to the local instance for all spawned instances.
            foreach (var networkManager in m_NetworkManagers)
            {
                var testBehaviour = networkManager.SpawnManager.SpawnedObjects[m_AuthorityValidatingInstance.NetworkObjectId].GetComponent<TestNetworkBehaviour>();
                testBehaviour.RpcReceivedBehaviour = testBehaviour;
            }

            // Initialize with no parameter
            m_AuthorityValidatingInstance.SendReferenceRpc(initializeWithNothing);
            yield return WaitForConditionOrTimeOut(RpcSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}][Initialize with no parameter] Failed to validate null {nameof(NetworkBehaviour)} reference!");

            m_AuthorityValidatingInstance.TestVariable.Value = initializeWithNull;
            yield return WaitForConditionOrTimeOut(NetworkVariableSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}][Initialize with null parameter] Failed to validate null {nameof(NetworkBehaviour)} reference!");

            // Reset the NetworkVaraible NetworkBehaviourReference to the local instance for all spawned instances.
            foreach (var networkManager in m_NetworkManagers)
            {
                var testBehaviour = networkManager.SpawnManager.SpawnedObjects[m_AuthorityValidatingInstance.NetworkObjectId].GetComponent<TestNetworkBehaviour>();
                testBehaviour.TestVariableBehaviour = testBehaviour;
                testBehaviour.TestVariableChanged = false;
            }

            // Initialize with no parameter
            m_AuthorityValidatingInstance.TestVariable.Value = initializeWithNothing;
            yield return WaitForConditionOrTimeOut(NetworkVariableSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}][Initialize with no parameter] Failed to validate null {nameof(NetworkBehaviour)} reference!");
        }
        #endregion

        #region Serialization Failure validation tests

        /// <summary>
        /// This test is ok to create but not spawn.
        /// </summary>
        [UnityTest]
        public IEnumerator FailSerializeNonSpawnedNetworkObject()
        {
            yield return s_DefaultWaitForTick;
            var instance = Object.Instantiate(m_TestPrefab);

            Assert.Throws<ArgumentException>(() =>
            {
                NetworkBehaviourReference outReference = instance.GetComponent<TestNetworkBehaviour>();
            });

            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator FailSerializeGameObjectWithoutNetworkObject()
        {
            yield return s_DefaultWaitForTick;
            var instance = Object.Instantiate(m_TestPrefab);
            Object.Destroy(instance.GetComponent<NetworkObject>());

            Assert.Throws<ArgumentException>(() =>
            {
                NetworkBehaviourReference outReference = instance.GetComponent<TestNetworkBehaviour>();
            });

            Object.Destroy(instance);
        }

        #endregion

        #region TestNetworkBehaviour component script

        private class TestNetworkBehaviour : NetworkBehaviour
        {
            public bool ReceivedRPC;
            public bool TestVariableChanged;
            public bool AcquiredReference;

            public NetworkVariable<NetworkBehaviourReference> TestVariable = new NetworkVariable<NetworkBehaviourReference>();

            public TestNetworkBehaviour TestVariableBehaviour;

            public TestNetworkBehaviour RpcReceivedBehaviour;

            protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
            {
                // Set it to ourself so we can validate serializing null too.
                RpcReceivedBehaviour = this;
                TestVariableBehaviour = null;
                base.OnNetworkPreSpawn(ref networkManager);
            }

            public override void OnNetworkSpawn()
            {
                TestVariable.OnValueChanged += OnTestVariableChanged;
                base.OnNetworkSpawn();
            }

            public override void OnNetworkPreDespawn()
            {
                TestVariable.OnValueChanged -= OnTestVariableChanged;
                base.OnNetworkPreDespawn();
            }

            private void OnTestVariableChanged(NetworkBehaviourReference previous, NetworkBehaviourReference current)
            {
                TestVariableChanged = true;
                AcquiredReference = current.TryGet(out TestVariableBehaviour, NetworkManager);
            }

            [Rpc(SendTo.Everyone)]
            public void SendReferenceRpc(NetworkBehaviourReference value)
            {
                AcquiredReference = value.TryGet(out RpcReceivedBehaviour, NetworkManager);
                ReceivedRPC = true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Integration tests for NetworkBehaviourReference
    /// </summary>
    internal class NetworkBehaviourReferenceIntegrationTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 1;

        internal class FakeMissingComponent : NetworkBehaviour
        {

        }

        internal class TestAddedComponent : NetworkBehaviour
        {

        }

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.AddComponent<TestAddedComponent>();
            base.OnCreatePlayerPrefab();
        }

        /// <summary>
        /// This test validates that if a component does not exist the NetworkBehaviourReference will not throw an
        /// invalid cast exception.
        /// (It is a full integration test to assure the NetworkObjects are spawned)
        /// </summary>
        [UnityTest]
        public IEnumerator TestTryGetWithAndWithOutExistingComponent()
        {
            var networkBehaviourReference = new NetworkBehaviourReference(m_ClientNetworkManagers[0].LocalClient.PlayerObject.GetComponent<TestAddedComponent>());
            var missingComponent = (FakeMissingComponent)null;
            var testBehaviour = (TestAddedComponent)null;
            Assert.IsFalse(networkBehaviourReference.TryGet(out missingComponent));
            Assert.IsTrue(networkBehaviourReference.TryGet(out testBehaviour));
            yield return null;
        }
    }
}
