using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;



namespace Unity.Netcode.MultiprocessRuntimeTests
{
    /// <summary>
    /// Smoke tests for ExecuteStepInContext, to make sure it's working properly before being used in other tests
    /// </summary>
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        protected override bool IsPerformanceTest => false;

        static private float s_ValueToValidateAgainst;
        private static void ValidateSimpleCoordinatorTestValue(float resultReceived)
        {
            Assert.AreEqual(s_ValueToValidateAgainst, resultReceived);
        }

        private static void ExecuteSimpleCoordinatorTest()
        {
            s_ValueToValidateAgainst = float.PositiveInfinity;
            TestCoordinator.Instance.WriteTestResultsServerRpc(s_ValueToValidateAgainst);
        }

        private static void ExecuteWithArgs(byte[] args)
        {
            s_ValueToValidateAgainst = args[0];
            TestCoordinator.Instance.WriteTestResultsServerRpc(s_ValueToValidateAgainst);
        }

        [SetUp]
        public void TestCoordinatorTestsSetUp()
        {
            MultiprocessLogger.Log($"NUnit Level Setup TestCoordinatorTestsSetUp - Connected Clients: {m_ConnectedClientsList.Count}");
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            // Sanity check for TestCoordinator
            // Call the method
            MultiprocessLogger.Log("Before sending message, let's see that we have the right number of connected clients");
            MultiprocessLogger.Log($"{GetWorkerCount()}, {m_ConnectedClientsList.Count}");
            MultiprocessLogger.Log("CheckTestCoordinator test in TestCoordinatorTests about to call InvokeFromMethodActionRpc");
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteSimpleCoordinatorTest);

            var nbResults = 0;
            MultiprocessLogger.Log($"WorkerCount is {GetWorkerCount()}");
            for (int i = 0; i < GetWorkerCount(); i++) // wait and test for the two clients
            {
                MultiprocessLogger.Log("Waiting for result to be set on TestCoordinator");
                yield return new WaitUntil(TestCoordinator.ResultIsSet());
                MultiprocessLogger.Log("Returning from wait");
                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                MultiprocessLogger.Log($"Check if {result} is greater than 0");
                Assert.Greater(result, 0f);
                nbResults++;
            }
            MultiprocessLogger.Log($"Check that {nbResults} is equal to {GetWorkerCount()}");
            Assert.That(nbResults, Is.EqualTo(GetWorkerCount()));
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinatorWithArgs()
        {
            MultiprocessLogger.Log($"CheckTestCoordinatorWithArgs - Start");
            MultiprocessLogHandler.Flush();
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteWithArgs, 99);
            var nbResults = 0;

            for (int i = 0; i < GetWorkerCount(); i++) // wait and test for the two clients
            {
                MultiprocessLogger.Log($"CheckTestCoordinatorWithArgs - WaitUntil ResultIsSet, or timeout {i} of {GetWorkerCount()}");
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                MultiprocessLogger.Log($"CheckTestCoordinatorWithArgs - ConsumeCurrentResult {clientId} {result}");
                Assert.That(result, Is.EqualTo(99));
                nbResults++;
            }
            MultiprocessLogger.Log($"CheckTestCoordinatorWithArgs - End for loop");
            Assert.That(nbResults, Is.EqualTo(GetWorkerCount()));
            MultiprocessLogger.Log($"CheckTestCoordinatorWithArgs - End of test");
            MultiprocessLogHandler.Flush();
        }
    }
}
