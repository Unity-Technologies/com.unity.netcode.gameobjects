using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(1)]
    //[TestFixture(2)]
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        private int m_WorkerCount;
        protected override int WorkerCount => m_WorkerCount;

        protected override bool IsPerformanceTest => false;

        public TestCoordinatorTests(int workerCount)
        {
            m_WorkerCount = workerCount;
        }

        private static void ExecuteSimpleCoordinatorTest()
        {
            TestCoordinator.Instance.WriteTestResultsServerRpc(float.PositiveInfinity);
        }

        private static void ExecuteWithArgs(byte[] args)
        {
            TestCoordinator.Instance.WriteTestResultsServerRpc(args[0]);
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            // Sanity check for TestCoordinator
            // Call the method
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteSimpleCoordinatorTest);

            var nbResults = 0;
            for (int i = 0; i < WorkerCount; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Assert.Greater(result, 0f);
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(WorkerCount));
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinatorWithArgs()
        {
            TestCoordinator.Instance.InvokeFromMethodActionRpc(ExecuteWithArgs, 99);
            var nbResults = 0;

            for (int i = 0; i < WorkerCount; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Assert.That(result, Is.EqualTo(99));
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(WorkerCount));
        }
    }
}
