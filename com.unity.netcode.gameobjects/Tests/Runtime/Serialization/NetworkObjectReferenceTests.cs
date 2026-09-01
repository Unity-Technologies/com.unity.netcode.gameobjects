using System;
using System.Collections;
using System.Collections.Generic;
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
    internal class NetworkObjectReferenceTests : BaseReferenceTests
    {
        protected override int NumberOfClients => 1;


        public NetworkObjectReferenceTests(HostOrServer hostOrServer) : base(hostOrServer)
        {
        }

        protected override void OnOneTimeSetup()
        {
            // TODO: [CmbServiceTests] if this test is deemed needed to test against the CMB server then update this test.
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();
            // Excluding from unified tests. If deemed needed, update test, then  remove.
            NetcodeIntegrationTestHelpers.IgnoreIfUnifiedTestsEnvironmentVariableSet();
            base.OnOneTimeSetup();
        }

        [UnityTest]
        public IEnumerator TestSerializeNetworkObject()
        {
            yield return SpawnTestPrefabInstance();

            // Explicitly send the NetworkObject as a reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkObjectReferenceRpc(new NetworkObjectReference(m_ReferenceToUse.NetworkObject));

            // Validated the reference
            yield return WaitForConditionOrTimeOut(NetworkObjectSerializedValidation);
            AssertOnTimeout($"[{nameof(TestSerializeNetworkObject)}][Explicit reference] Failed to validate {nameof(NetworkObjectReference)} serialization!");

            // Implicitly send the NetworkObject as a reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkObjectReferenceRpc(m_ReferenceToUse.NetworkObject);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(NetworkObjectSerializedValidation);
            AssertOnTimeout($"[{nameof(TestSerializeNetworkObject)}][Implicit reference] Failed to validate {nameof(NetworkObjectReference)} serialization!");

            // Use the GameObject to set the NetworkObjet reference
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkObjectReferenceRpc(new NetworkObjectReference(m_ReferenceToUse.GameObject));

            // Validated the reference
            yield return WaitForConditionOrTimeOut(NetworkObjectSerializedValidation);
            AssertOnTimeout($"[{nameof(TestSerializeNetworkObject)}][GameObject reference] Failed to validate {nameof(NetworkObjectReference)} serialization!");
        }

        [UnityTest]
        public IEnumerator TestSerializeNull()
        {
            yield return SpawnTestPrefabInstance(true);

            // Initialize with NULL parameter
            var initializeWithNullGameObject = new NetworkObjectReference((GameObject)null);
            var initializeWithNullNetworkObject = new NetworkObjectReference((NetworkObject)null);
            // Initialize with no parameter
            var initializeWithNothing = new NetworkObjectReference();

            // Initialize with NULL GameObject parameter
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkObjectReferenceRpc(initializeWithNullGameObject);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(SerializingNullNetworkObjectValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNetworkObject)}][GameObject as NULL] Failed to validate {nameof(NetworkObjectReference)} serialization!");

            // Reset the RPC NetworkObjectReference to the local instance for all spawned instances.
            foreach (var networkManager in m_NetworkManagers)
            {
                var testBehaviour = networkManager.SpawnManager.SpawnedObjects[m_ValidatingInstance.NetworkObject.NetworkObjectId].GetComponent<TestNetworkBehaviour>();
                testBehaviour.RpcReceivedNetworkObject = testBehaviour.NetworkObject;
            }

            // Initialize with NULL NetworkObject parameter
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkObjectReferenceRpc(initializeWithNullNetworkObject);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(SerializingNullNetworkObjectValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNetworkObject)}][NetworkObject as NULL] Failed to validate {nameof(NetworkObjectReference)} serialization!");

            // Reset the RPC NetworkObjectReference to the local instance for all spawned instances.
            foreach (var networkManager in m_NetworkManagers)
            {
                var testBehaviour = networkManager.SpawnManager.SpawnedObjects[m_ValidatingInstance.NetworkObject.NetworkObjectId].GetComponent<TestNetworkBehaviour>();
                testBehaviour.RpcReceivedNetworkObject = testBehaviour.NetworkObject;
            }

            // Initialize with no parameter
            m_ValidatingInstance.TestNetworkBehaviour.SendNetworkObjectReferenceRpc(initializeWithNothing);

            // Validated the reference
            yield return WaitForConditionOrTimeOut(SerializingNullNetworkObjectValidated);
            AssertOnTimeout($"[{nameof(TestSerializeNetworkObject)}][No Parameter] Failed to validate {nameof(NetworkObjectReference)} serialization!");
        }

        [UnityTest]
        public IEnumerator TestGetReferenceAndConversion()
        {
            yield return SpawnTestPrefabInstance();

            var referenceToUse = new NetworkObjectReference(m_ReferenceToUse.NetworkObject);

            Assert.True(referenceToUse.TryGet(out NetworkObject networkObject));
            Assert.NotNull(networkObject, $"TryGet succeeded but value returned is null!");

            // TODO: Revisit this to determine if this portion of the test is actually needed
            referenceToUse.TryGet(out NetworkObject result);
            Assert.AreEqual(networkObject, result);

            // Now implicitly convert from a NetworkObjectReference to a GameObject
            GameObject fromReference = referenceToUse;
            Assert.IsTrue(fromReference == m_ReferenceToUse.GameObject, $"Implicitly converting {nameof(NetworkObjectReference)} to {nameof(GameObject)} failed!");

            // Despawn
            m_ReferenceToUse.NetworkObject.Despawn();
            var referenceName = m_ReferenceToUse.GameObject.name;
            yield return WaitForDespawnedOnAllOrTimeOut(new List<NetworkObject>() { m_ReferenceToUse.NetworkObject });
            AssertOnTimeout($"Timed out waiting for {referenceName} to de-spawn!");

            // Destroy
            Object.DestroyImmediate(m_ReferenceToUse.GameObject);

            // Validate trying to implicitly convert returns a null value when the GameObject
            // no longer exists
            fromReference = referenceToUse;
            Assert.IsTrue(fromReference == null, $"Implicitly converting {nameof(NetworkObjectReference)} to {nameof(GameObject)} failed when destroyed!");
        }


        [UnityTest]
        public IEnumerator FailSerializeNonSpawnedNetworkObject()
        {
            yield return s_DefaultWaitForTick;
            var instance = Object.Instantiate(m_TestPrefab);


            Assert.Throws<ArgumentException>(() =>
            {
                NetworkObjectReference outReference = instance.GetComponent<NetworkObject>();
            });
        }

        [UnityTest]
        public IEnumerator FailSerializeGameObjectWithoutNetworkObject()
        {
            yield return s_DefaultWaitForTick;
            var instance = Object.Instantiate(m_TestPrefab);


            Assert.Throws<ArgumentException>(() =>
            {
                NetworkObjectReference outReference = instance;
            });
        }
    }
}
