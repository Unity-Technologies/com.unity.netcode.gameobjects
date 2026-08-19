using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Unit tests to test:
    /// - Serializing NetworkBehaviour to NetworkBehaviourReference
    /// - Deserializing NetworkBehaviourReference to NetworkBehaviour
    /// - Implicit operators of NetworkBehaviourReference
    /// </summary>
    internal class NetworkBehaviourReferenceTests : BaseReferenceTests
    {
        public NetworkBehaviourReferenceTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        #region Tests using non-null NetworkBehaviours and RPCs

        [UnityTest]
        [UnityCoreClrExplicitDisabled("https://jira.unity3d.com/browse/UUM-149592", "NGO NetworkVariable serialization codegen not generated for some types on CoreCLR (falls back to FallbackSerializer)")]
        public IEnumerator TestRpc()
        {
            yield return SpawnTestPrefabInstance();

            // Explicitly send the NetworkBehaviour as a reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkBehaviourReferenceRpc(new NetworkBehaviourReference(m_ReferenceToUse.TestNetworkBehaviour));

            // Validated the reference
            yield return WaitForConditionOrTimeOut(RpcWasReceivedAndBehaviourValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");
        }


        [UnityTest]
        [UnityCoreClrExplicitDisabled("https://jira.unity3d.com/browse/UUM-149592", "NGO NetworkVariable serialization codegen not generated for some types on CoreCLR (falls back to FallbackSerializer)")]
        public IEnumerator TestRpcImplicitNetworkBehaviour()
        {
            yield return SpawnTestPrefabInstance();

            // Implicitly send the NetworkBehaviour as a reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkBehaviourReferenceRpc(m_ReferenceToUse.TestNetworkBehaviour);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(RpcWasReceivedAndBehaviourValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");
        }
        #endregion

        #region Tests using non-null NetworkBehaviours and NetworkVariable
        [UnityTest]
        public IEnumerator TestNetworkVariable()
        {
            yield return SpawnTestPrefabInstance();

            // Assure the authority instance's value is the default (null) value
            Assert.IsNull((NetworkBehaviour)m_ValidatingInstance.TestNetworkBehaviour.NetworkBehaviourVariable.Value);

            // Implicitly assign the NetworkBehaviourReference by assigning the NetworkBehaviour to the NetworkVariable.
            m_ValidatingInstance.TestNetworkBehaviour.NetworkBehaviourVariable.Value = m_ReferenceToUse.TestNetworkBehaviour;

            // Validated the NetworkVariable reference propogates to clients
            yield return WaitForConditionOrTimeOut(NetworkVariableChangedAndBehaviourValidated);
            AssertOnTimeout($"[{nameof(TestNetworkVariable)}] Failed to validate reference!");
        }
        #endregion

        #region Validating using NULL as a NetworkBehaviourReference
        [UnityTest]
        public IEnumerator TestSerializeNull()
        {
            yield return SpawnTestPrefabInstance(true);

            // Initialize with NULL parameter
            var initializeWithNull = new NetworkBehaviourReference(null);
            // Initialize with no parameter
            var initializeWithNothing = new NetworkBehaviourReference();

            // Initialized with NULL parameter
            // Explicitly send the NetworkBehaviour as a reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkBehaviourReferenceRpc(initializeWithNull);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(RpcSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");

            // Reset the RPC NetworkBehaviourReference to the local instance for all spawned instances.
            foreach (var networkManager in m_NetworkManagers)
            {
                var testBehaviour = networkManager.SpawnManager.SpawnedObjects[m_ValidatingInstance.NetworkObject.NetworkObjectId].GetComponent<TestNetworkBehaviour>();
                testBehaviour.RpcReceivedBehaviour = testBehaviour;
            }

            // Initialized with no parameter
            // Explicitly send the NetworkBehaviour as a reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkBehaviourReferenceRpc(initializeWithNothing);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(RpcSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestRpc)}] Failed to validate reference!");


            // Initialize NetworkBehaviourVariable with NULL parameter
            m_ValidatingInstance.TestNetworkBehaviour.NetworkBehaviourVariable.Value = initializeWithNull;
            yield return WaitForConditionOrTimeOut(NetworkVariableSerializingNullValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNull)}][Initialize with null parameter] Failed to validate null {nameof(NetworkBehaviour)} reference!");

            // Reset the NetworkVaraible NetworkBehaviourReference to the local instance for all spawned instances.
            foreach (var networkManager in m_NetworkManagers)
            {
                var testBehaviour = networkManager.SpawnManager.SpawnedObjects[m_ValidatingInstance.NetworkObject.NetworkObjectId].GetComponent<TestNetworkBehaviour>();
                testBehaviour.TestVariableBehaviour = testBehaviour;
                testBehaviour.TestVariableChanged = false;
            }

            // Initialize NetworkBehaviourVariable with no parameter
            m_ValidatingInstance.TestNetworkBehaviour.NetworkBehaviourVariable.Value = initializeWithNothing;
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
