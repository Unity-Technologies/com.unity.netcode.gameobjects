using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    [TestFixture(1, new string[] { "default-win:test-win" })]
    [TestFixture(3, new string[] { "default-win:test-win", "default-mac:test-mac-2", "default-win:test-win-2" })]
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
            m_WorkerCount = workerCount;
            m_Platforms = platformList;
        }

        [UnityTest]
        public IEnumerator CheckPreconditions()
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
            MultiprocessLogger.Log("Before yield");
            yield return new WaitForSeconds(0.1f);
            MultiprocessLogger.Log("after yield");
        }
    }
}
