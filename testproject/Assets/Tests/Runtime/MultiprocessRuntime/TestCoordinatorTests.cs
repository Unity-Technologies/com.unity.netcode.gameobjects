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

                foreach (var current in TestCoordinator.ConsumeCurrentResult())
                {
                    Debug.Log($"got results, asserting, result is {current.result} from key {current.clientId}");
                    Assert.Greater(current.result, 0f);
                }
            }
        }

        [UnityTest, TestCoordinator.ExecuteStepInContext.MultiprocessContextBasedTestAttribute]
        public IEnumerator TestWithParameters([Values(1, 2, 3)] int a)
        {
            InitContextSteps();

            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Server, bytes =>
            {
                Assert.Less(a, 4);
                Assert.Greater(a, 0);
            });
            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Clients, bytes =>
            {
                var clientA = BitConverter.ToInt32(bytes, 0);
                Assert.True(!NetworkManager.Singleton.IsServer);
                Assert.Less(clientA, 4);
                Assert.Greater(clientA, 0);
            }, paramToPass: BitConverter.GetBytes(a));
        }

        [UnityTest, TestCoordinator.ExecuteStepInContext.MultiprocessContextBasedTestAttribute]
        [TestCase(1, 2, ExpectedResult = null)]
        [TestCase(2, 3, ExpectedResult = null)]
        [TestCase(3, 4, ExpectedResult = null)]
        public IEnumerator TestWithParameters(int a, int b)
        {
            InitContextSteps();

            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Server, bytes =>
            {
                Assert.Less(a, 4);
                Assert.Greater(a, 0);
                Assert.Less(b, 5);
                Assert.Greater(b, 1);
            });
            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Clients, bytes =>
            {
                var clientB = BitConverter.ToInt32(bytes, 0);
                Assert.True(!NetworkManager.Singleton.IsServer);
                Assert.Less(clientB, 5);
                Assert.Greater(clientB, 1);
            }, paramToPass: BitConverter.GetBytes(b));
        }

        [UnityTest, TestCoordinator.ExecuteStepInContext.MultiprocessContextBasedTestAttribute]
        public IEnumerator ContextTestWithAdditionalWait()
        {
            InitContextSteps();

            int maxValue = 10;
            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Clients, _ =>
            {
                int count = 0;

                void Update(float _)
                {
                    TestCoordinator.Instance.WriteTestResultsServerRpc(count++);
                    if (count > maxValue)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                    }
                }

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, additionalIsFinishedWaiter: () =>
            {
                return TestCoordinator.PeekLatestResult(TestCoordinator.AllClientIdExceptMine[0]) == maxValue;
            });
            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Server, _ =>
            {
                var current = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    Assert.That(res.result, Is.EqualTo(current++));
                }

                Assert.That(current - 1, Is.EqualTo(maxValue));
            });
        }

        [UnityTest, TestCoordinator.ExecuteStepInContext.MultiprocessContextBasedTestAttribute]
        public IEnumerator TestExecuteInContext()
        {
            // TODO convert other tests to this format
            // todo move ExecuteInContext out of here (in test coordinator?)
            InitContextSteps();

            int stepCountExecuted = 0;
            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Server, (byte[] args) =>
            {
                stepCountExecuted++;
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something server side, count is {count}");
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Clients, (byte[] args) =>
            {
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something client side, count is {count}");
                TestCoordinator.Instance.WriteTestResultsServerRpc(12345);
#if UNITY_EDITOR
                Assert.Fail("Should not be here!! This should only execute on client!!");
#endif
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Server, _ =>
            {
                stepCountExecuted++;
                int count = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    count++;
                    Assert.AreEqual(12345, res.result);
                }

                Assert.Greater(count, 0);
            });

            int timeToWait = 4;
            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Clients, _ =>
            {
                void Update(float _)
                {
                    if (Time.time > timeToWait)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                        TestCoordinator.Instance.WriteTestResultsServerRpc(Time.time);

                        TestCoordinator.Instance.ClientFinishedServerRpc(); // since finishOnInvoke is false, we need to do this manually
                    }
                    else
                    {
                        Debug.Log($"current time on client : {Time.time}");
                    }
                }

                ;
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, spansMultipleUpdates: true); // waits multiple frames before allowing the next action to continue.

            yield return new TestCoordinator.ExecuteStepInContext(TestCoordinator.ExecuteStepInContext.StepExecutionContext.Server, (byte[] args) =>
            {
                stepCountExecuted++;
                int count = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    count++;
                    Assert.GreaterOrEqual(res.result, timeToWait);
                }

                Assert.Greater(count, 0);
            });
            if (!TestCoordinator.Instance.isRegistering)
            {
                Assert.AreEqual(3, stepCountExecuted);
            }
        }
    }
}
