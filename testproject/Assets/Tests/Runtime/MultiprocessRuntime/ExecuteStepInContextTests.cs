using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ExecuteStepInContext;

namespace MLAPI.MultiprocessRuntimeTests
{
    /// <summary>
    /// Smoke tests for ExecuteStepInContext, to make sure it's working properly before being used in other tests
    /// </summary>
    [TestFixture(1)]
    [TestFixture(2)]
    public class ExecuteStepInContextTests : BaseMultiprocessTests
    {
        private int m_NbWorkersToTest;
        public ExecuteStepInContextTests(int nbWorkersToTest)
        {
            m_NbWorkersToTest = nbWorkersToTest;
        }
        protected override int NbWorkers => m_NbWorkersToTest;
        protected override bool m_IsPerformanceTest => false;

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator TestWithSameName([Values(1)]int a)
        {
            // ExecuteStepInContext bases itself on method name to identify steps. We need to make sure that methods with
            // same names, but different signatures behave correctly
            InitContextSteps();
            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Assert.That(a, Is.EqualTo(1));
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                Assert.That(BitConverter.ToInt32(bytes, 0), Is.EqualTo(1));
            }, paramToPass: BitConverter.GetBytes(a));
        }

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator TestWithSameName([Values(2)]int a, [Values(3)]int b)
        {
            InitContextSteps();
            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Assert.That(b, Is.EqualTo(3));
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                Assert.That(BitConverter.ToInt32(bytes, 0), Is.EqualTo(3));
            }, paramToPass: BitConverter.GetBytes(b));
        }

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator TestWithParameters([Values(1, 2, 3)] int a)
        {
            InitContextSteps();

            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Assert.Less(a, 4);
                Assert.Greater(a, 0);
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                var clientA = BitConverter.ToInt32(bytes, 0);
                Assert.True(!NetworkManager.Singleton.IsServer);
                Assert.Less(clientA, 4);
                Assert.Greater(clientA, 0);
            }, paramToPass: BitConverter.GetBytes(a));
        }

        [UnityTest, MultiprocessContextBasedTest]
        [TestCase(1, 2, ExpectedResult = null)]
        [TestCase(2, 3, ExpectedResult = null)]
        [TestCase(3, 4, ExpectedResult = null)]
        public IEnumerator TestWithParameters(int a, int b)
        {
            InitContextSteps();

            yield return new ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Assert.Less(a, 4);
                Assert.Greater(a, 0);
                Assert.Less(b, 5);
                Assert.Greater(b, 1);
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                var clientB = BitConverter.ToInt32(bytes, 0);
                Assert.True(!NetworkManager.Singleton.IsServer);
                Assert.Less(clientB, 5);
                Assert.Greater(clientB, 1);
            }, paramToPass: BitConverter.GetBytes(b));
        }

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator TestExceptionClientSide()
        {
            InitContextSteps();

            const string exceptionMessageToTest = "This is an exception for TestCoordinator that's expected";
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                throw new Exception(exceptionMessageToTest);
            }, ignoreTimeoutException: true);
            yield return new ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                for (int i = 0; i < m_NbWorkersToTest; i++)
                {
                    LogAssert.Expect(LogType.Error, new Regex($".*{exceptionMessageToTest}.*"));
                }
            });

            const string exceptionUpdateMessageToTest = "This is an exception for update loop client side that's expected";
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                void Update(float __)
                {
                    NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                    throw new Exception(exceptionUpdateMessageToTest);
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;

            }, ignoreTimeoutException: true);
            yield return new ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                for (int i = 0; i < m_NbWorkersToTest; i++)
                {
                    LogAssert.Expect(LogType.Error, new Regex($".*{exceptionUpdateMessageToTest}.*"));
                }
            });
        }

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator ContextTestWithAdditionalWait()
        {
            InitContextSteps();

            const int maxValue = 10;
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                int count = 0;

                void Update(float __)
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
                int nbFinished = 0;
                for (int i = 0; i < m_NbWorkersToTest; i++)
                {
                    if (TestCoordinator.PeekLatestResult(TestCoordinator.AllClientIdsExceptMine[i]) == maxValue)
                    {
                        nbFinished++;
                    }
                }
                return nbFinished == m_NbWorkersToTest;
            });
            yield return new ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                Assert.That(TestCoordinator.AllClientIdsExceptMine.Count, Is.EqualTo(m_NbWorkersToTest));
                foreach (var clientID in TestCoordinator.AllClientIdsExceptMine)
                {
                    var current = 0;
                    foreach (var res in TestCoordinator.ConsumeCurrentResult(clientID))
                    {
                        Assert.That(res, Is.EqualTo(current++));
                    }
                    Assert.That(current - 1, Is.EqualTo(maxValue));
                }
            });
        }

        [UnityTest, MultiprocessContextBasedTest]
        public IEnumerator TestExecuteInContext()
        {
            InitContextSteps();

            int stepCountExecuted = 0;
            yield return new ExecuteStepInContext(StepExecutionContext.Server, args =>
            {
                stepCountExecuted++;
                int count = BitConverter.ToInt32(args, 0);
                Assert.That(count, Is.EqualTo(1));
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new ExecuteStepInContext(StepExecutionContext.Clients, args =>
            {
                int count = BitConverter.ToInt32(args, 0);
                Assert.That(count, Is.EqualTo(2));
                TestCoordinator.Instance.WriteTestResultsServerRpc(12345);
#if UNITY_EDITOR
                Assert.Fail("Should not be here!! This should only execute on client!!");
#endif
            }, paramToPass: BitConverter.GetBytes(2));

            yield return new ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                stepCountExecuted++;
                int resultCountFromWorkers = 0;
                foreach (var res in TestCoordinator.ConsumeCurrentResult())
                {
                    resultCountFromWorkers++;
                    Assert.AreEqual(12345, res.result);
                }

                Assert.That(resultCountFromWorkers, Is.EqualTo(NbWorkers));
            });

            const int timeToWait = 4;
            yield return new ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                void Update(float __)
                {
                    if (Time.time > timeToWait)
                    {
                        NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                        TestCoordinator.Instance.WriteTestResultsServerRpc(Time.time);

                        TestCoordinator.Instance.ClientFinishedServerRpc(); // since finishOnInvoke is false, we need to do this manually
                    }
                }

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, waitMultipleUpdates: true); // waits multiple frames before allowing the next action to continue.

            yield return new ExecuteStepInContext(StepExecutionContext.Server, args =>
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

            if (!isRegistering)
            {
                Assert.AreEqual(3, stepCountExecuted);
            }
        }
    }
}
