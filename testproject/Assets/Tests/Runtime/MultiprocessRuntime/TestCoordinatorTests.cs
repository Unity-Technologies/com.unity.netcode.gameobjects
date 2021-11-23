using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(1, new string[] { "default-win:test-win" })]
    [TestFixture(2, new string[] { "default-win:test-win", "default-win:test-win-2" })]
    [TestFixture(3, new string[] { "default-win:test-win", "default-win:test-win-2", "default-win:test-win-3" })]
    public class TestCoordinatorTests : BaseMultiprocessTests
    {
        private int m_WorkerCount;
        protected override int WorkerCount => m_WorkerCount;

        private string[] m_Platforms;
        protected override string[] platformList => m_Platforms;

        protected override bool IsPerformanceTest => false;

        public TestCoordinatorTests(int workerCount, string[] platformList)
        {
            m_WorkerCount = workerCount;
            m_Platforms = platformList;            
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

        
        public IEnumerator CheckPreconditions()
        {
            if (platformList != null)
            {
                var dll = new FileInfo(BokkenMachine.PathToDll);
                MultiprocessLogger.Log("The Bokken API Dll exists");
                Assert.True(dll.Exists, "The Bokken API Dll exists");
                var p = BokkenMachine.ExecuteCommand("--help", true);
                MultiprocessLogger.Log("The help command process should have exited");
                Assert.True(p.HasExited, "The process should have exited");
                string s = p.StandardOutput.ReadToEnd();
                MultiprocessLogger.Log("Help stdout");
                Assert.IsNotNull(s, "The help output should not be null");
                string e = p.StandardError.ReadToEnd();
                MultiprocessLogger.Log("The help command stderr");
                Assert.True(string.IsNullOrEmpty(e), $"The help command error stream should be null but was {e}");
            }
            MultiprocessLogger.Log("Before yield");
            yield return new WaitForSeconds(0.1f);
            MultiprocessLogger.Log("after yield");
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
