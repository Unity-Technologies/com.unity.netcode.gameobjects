using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static TestCoordinator.ExecuteStepInContext;

namespace MLAPI.MultiprocessRuntimeTests
{
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

        [UnityTest, MultiprocessContextBasedTestAttribute]
        public IEnumerator TestWithParameters([Values(1, 2, 3)] int a)
        {
            InitContextSteps();

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Assert.Less(a, 4);
                Assert.Greater(a, 0);
            });
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                var clientA = BitConverter.ToInt32(bytes, 0);
                Assert.True(!NetworkManager.Singleton.IsServer);
                Assert.Less(clientA, 4);
                Assert.Greater(clientA, 0);
            }, paramToPass: BitConverter.GetBytes(a));
        }

        [UnityTest, MultiprocessContextBasedTestAttribute]
        [TestCase(1, 2, ExpectedResult = null)]
        [TestCase(2, 3, ExpectedResult = null)]
        [TestCase(3, 4, ExpectedResult = null)]
        public IEnumerator TestWithParameters(int a, int b)
        {
            InitContextSteps();

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, bytes =>
            {
                Assert.Less(a, 4);
                Assert.Greater(a, 0);
                Assert.Less(b, 5);
                Assert.Greater(b, 1);
            });
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, bytes =>
            {
                var clientB = BitConverter.ToInt32(bytes, 0);
                Assert.True(!NetworkManager.Singleton.IsServer);
                Assert.Less(clientB, 5);
                Assert.Greater(clientB, 1);
            }, paramToPass: BitConverter.GetBytes(b));
        }

        [UnityTest, MultiprocessContextBasedTestAttribute]
        public IEnumerator TestExceptionClientSide()
        {
            InitContextSteps();

            var exceptionMessageToTest = "This is an exception for TestCoordinator that's expected";

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                throw new Exception(exceptionMessageToTest);
            }, timeoutExpected: true);
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                for (int i = 0; i < m_NbWorkersToTest; i++)
                {
                    LogAssert.Expect(LogType.Error, new Regex($".*{exceptionMessageToTest}.*"));
                }
            });

            var exceptionUpdateMessageToTest = "This is an exception for update loop client side that's expected";
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
            {
                void Update(float _)
                {
                    NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate -= Update;
                    throw new Exception(exceptionUpdateMessageToTest);
                }
                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;

            }, timeoutExpected: true);
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                for (int i = 0; i < m_NbWorkersToTest; i++)
                {
                    LogAssert.Expect(LogType.Error, new Regex($".*{exceptionUpdateMessageToTest}.*"));
                }
            });
        }

        [UnityTest, MultiprocessContextBasedTestAttribute]
        public IEnumerator ContextTestWithAdditionalWait()
        {
            InitContextSteps();

            int maxValue = 10;
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
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
                int nbFinished = 0;
                for (int i = 0; i < m_NbWorkersToTest; i++)
                {
                    if (TestCoordinator.PeekLatestResult(TestCoordinator.AllClientIdExceptMine[i]) == maxValue)
                    {
                        nbFinished++;
                    }
                }
                return nbFinished == m_NbWorkersToTest;
            });
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
            {
                Assert.That(TestCoordinator.AllClientIdExceptMine.Count, Is.EqualTo(m_NbWorkersToTest));
                foreach (var clientID in TestCoordinator.AllClientIdExceptMine)
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

        [UnityTest, MultiprocessContextBasedTestAttribute]
        public IEnumerator TestExecuteInContext()
        {
            InitContextSteps();

            int stepCountExecuted = 0;
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, (byte[] args) =>
            {
                stepCountExecuted++;
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something server side, count is {count}");
                Assert.That(count, Is.EqualTo(1));
            }, paramToPass: BitConverter.GetBytes(1));

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, (byte[] args) =>
            {
                int count = BitConverter.ToInt32(args, 0);
                Debug.Log($"something client side, count is {count}");
                Assert.That(count, Is.EqualTo(2));
                TestCoordinator.Instance.WriteTestResultsServerRpc(12345);
#if UNITY_EDITOR
                Assert.Fail("Should not be here!! This should only execute on client!!");
#endif
            }, paramToPass: BitConverter.GetBytes(2));

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, _ =>
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

            int timeToWait = 4;
            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Clients, _ =>
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

                NetworkManager.Singleton.gameObject.GetComponent<CallbackComponent>().OnUpdate += Update;
            }, waitMultipleUpdates: true); // waits multiple frames before allowing the next action to continue.

            yield return new TestCoordinator.ExecuteStepInContext(StepExecutionContext.Server, (byte[] args) =>
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
