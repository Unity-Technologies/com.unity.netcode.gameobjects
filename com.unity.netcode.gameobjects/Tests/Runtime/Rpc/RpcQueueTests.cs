using System.Collections;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// The RpcQueue unit tests validate:
    ///     - Maximum buffer size that can be sent (currently 1MB is the default maximum `MessageQueueHistoryFrame` size)
    ///     - That all RPCs invoke at the appropriate `NetworkUpdateStage` (Client and Server)
    ///     - A lower level `MessageQueueContainer` test that validates `MessageQueueFrameItems` after they have been put into the queue
    /// </summary>
    internal class RpcQueueTests : NetcodeIntegrationTest
    {
        protected override int NumberOfClients => 0;

        private GameObject m_TestPrefab;

        protected override void OnOneTimeSetup()
        {
            // TODO: [CmbServiceTests] if this test is deemed needed to test against the CMB server then update this test.
            NetcodeIntegrationTestHelpers.IgnoreIfServiceEnviromentVariableSet();
            // Excluding from unified tests. If deemed needed, update test, then  remove.
            NetcodeIntegrationTestHelpers.IgnoreIfUnifiedTestsEnvironmentVariableSet();
            base.OnOneTimeSetup();
        }

        protected override void OnServerAndClientsCreated()
        {
            m_TestPrefab = CreateNetworkObjectPrefab("RpcQueueTest");
            m_TestPrefab.AddComponent<BufferDataValidationComponent>();
            base.OnServerAndClientsCreated();
        }

        /// <summary>
        /// This tests the RPC Queue outbound and inbound buffer capabilities.
        /// </summary>
        /// <returns>IEnumerator</returns>
        [UnityTest]
        public IEnumerator BufferDataValidation()
        {
            var authority = GetAuthorityNetworkManager();
            var instance = SpawnObject(m_TestPrefab, authority);

            yield return WaitForSpawnedOnAllOrTimeOut(instance);
            AssertOnTimeout($"Not all clients spawned {instance.name}!");

            var bufferDataValidationComponent = instance.GetComponent<BufferDataValidationComponent>();

            // Start Testing
            bufferDataValidationComponent.EnableTesting = true;

            yield return WaitForConditionOrTimeOut(() => bufferDataValidationComponent.IsTestComplete());
            AssertOnTimeout($"Timed out waiting for the {nameof(BufferDataValidationComponent)} tests to complete!");

        }
    }
}
