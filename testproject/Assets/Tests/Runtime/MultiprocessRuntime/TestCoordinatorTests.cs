using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(1)]
    [TestFixture(2)]
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        private int m_WorkerCount;
        //TODO: Figure out how manage local workercounts vs. remote worker counts
        // Since remote worker counts should not be changed within the test
        // and is not supported due to performance and stability reasons
        protected override int WorkerCount => m_WorkerCount;

        protected override bool IsPerformanceTest => false;

        public TestCoordinatorTests(int workerCount)
        {
            m_WorkerCount = workerCount;
        }

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

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            // Sanity check for TestCoordinator
            // Call the method
            MultiprocessLogger.Log("CheckTestCoordinator - Calling ExecuteSimpleCoordinatorTest");
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteSimpleCoordinatorTest);

            var nbResults = 0;
            for (int i = 0; i < GetWorkerCount(); i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Assert.Greater(result, 0f);
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(GetWorkerCount()));
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinatorWithArgs()
        {
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteWithArgs, 99);
            var nbResults = 0;

            for (int i = 0; i < GetWorkerCount(); i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Assert.That(result, Is.EqualTo(99));
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(GetWorkerCount()));
        }
    }
}
