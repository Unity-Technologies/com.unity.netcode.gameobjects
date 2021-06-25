using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MLAPI.MultiprocessRuntimeTests
{
    [TestFixture(1)]
    [TestFixture(2)]
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        private int m_NbWorkers;
        protected override int NbWorkers => m_NbWorkers;

        protected override bool m_IsPerformanceTest => false;

        public TestCoordinatorTests(int nbWorkers)
        {
            m_NbWorkers = nbWorkers;
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
            TestCoordinator.Instance.TriggerRpc(ExecuteSimpleCoordinatorTest);

            var nbResults = 0;
            for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Debug.Log($"got results, asserting, result is {result} from key {clientId}");
                Assert.Greater(result, 0f);
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(NbWorkers));
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinatorWithArgs()
        {
            TestCoordinator.Instance.TriggerRpc(ExecuteWithArgs, 99);
            var nbResults = 0;

            for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var (clientId, result) = TestCoordinator.ConsumeCurrentResult().Take(1).Single();
                Debug.Log($"got results, asserting, result is {result} from key {clientId}");
                Assert.That(result, Is.EqualTo(99));
                nbResults++;
            }
            Assert.That(nbResults, Is.EqualTo(NbWorkers));
        }
    }
}
