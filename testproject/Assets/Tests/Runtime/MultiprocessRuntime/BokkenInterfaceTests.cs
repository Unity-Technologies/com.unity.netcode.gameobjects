using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(1, new string[] { "default-win:test-win" })]
    [TestFixture(2, new string[] { "default-mac:test-mac" })]
    [TestFixture(3, new string[] { "default-win:test-win", "default-mac:test-mac" })]
    // [TestFixture(4, new string[] { "default-win:test-win", "default-win:test-win2" })]
    // [TestFixture(5, new string[] { "default-mac:test-mac2", "default-mac:test-mac" })]
    public class BokkenInterfaceTests : BaseMultiprocessTests
    {
        private int m_WorkerCount;
        protected override int WorkerCount => m_WorkerCount;

        private string[] m_Platforms;
        protected override string[] platformList => m_Platforms;

        protected override bool LaunchRemotely => true;
        protected override bool IsPerformanceTest => false;

        public BokkenInterfaceTests(int workerCount, string[] platformList)
        {
            m_WorkerCount = platformList.Length;
            m_Platforms = platformList;
        }
        /*
        public override void SetupTestSuite()
        {
            MultiprocessLogger.Log("BokkenInterfaceTests - SetupTestSuite - override from BaseMultiprocessTests");
        }
        */

        [UnityTest]
        public IEnumerator CheckPreconditions()
        {
            MultiprocessLogger.Log($"Are Clients Connected: {WorkerCount}, {m_ConnectedClientsList.Count}");

            var pathTodll = new FileInfo(BokkenMachine.PathToDll);
            MultiprocessLogger.Log("The Bokken API Dll exists");
            Assert.True(pathTodll.Exists, "The Bokken API Dll exists");

            var externalProcess = BokkenMachine.ExecuteCommand("--help", true);
            MultiprocessLogger.Log("The help command process should have exited");
            Assert.True(externalProcess.HasExited, "The process should have exited");

            string externalProcessStdOut = externalProcess.StandardOutput.ReadToEnd();
            MultiprocessLogger.Log("Help stdout");
            Assert.IsNotNull(externalProcessStdOut, "The help output should not be null");

            string externalProcessStdErr = externalProcess.StandardError.ReadToEnd();
            MultiprocessLogger.Log("The help command stderr");
            Assert.True(string.IsNullOrEmpty(externalProcessStdErr), $"The help command error stream should be null but was {externalProcessStdErr}");

            MultiprocessLogger.Log("Before yield");
            yield return new WaitForSeconds(0.1f);
            MultiprocessLogger.Log("after yield");
        }
    }
}
