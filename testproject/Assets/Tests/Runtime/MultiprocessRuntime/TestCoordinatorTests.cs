using System;
using System.Collections;
using MLAPI;
using MLAPI.MultiprocessRuntimeTests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static TestCoordinator.ExecuteStepInContext;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;

        protected override bool m_IsPerformanceTest => false;

        public static void ExecuteSimpleCoordinatorTest()
        {
            TestCoordinator.Instance.WriteTestResultsServerRpc(float.PositiveInfinity);
        }

        [UnityTest]
        public IEnumerator CheckTestCoordinator()
        {
            // Sanity check for TestCoordinator
            // Call the method
            TestCoordinator.Instance.TriggerRpc(ExecuteSimpleCoordinatorTest);

            for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.ResultIsSet());

                var nbResults = 0;
                foreach (var current in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got results, asserting, result is {current.result} from key {current.clientId}");
                    Assert.Greater(current.result, 0f);
                    nbResults++;
                }
                Assert.That(nbResults, Is.EqualTo(1));
            }
        }
    }
}
