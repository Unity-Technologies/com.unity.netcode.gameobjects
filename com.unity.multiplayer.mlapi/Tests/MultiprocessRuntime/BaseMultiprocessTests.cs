using System;
using System.Collections;
using System.Threading;
using MLAPI;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MLAPI.MultiprocessRuntimeTests
{
    public class MultiprocessTests : CategoryAttribute
    {
        public const string categoryName = "Multiprocess";
        public MultiprocessTests(params string[] nodesRequired) : base(categoryName){}
    }

    [MultiprocessTests]
    public abstract class BaseMultiprocessTests
    {
        public const string mainSceneName = "MultiprocessTestingScene";

        protected abstract int NbWorkers { get; }

//         public static bool Build(string buildPath)
//         {
// #if UNITY_EDITOR
//
//             PlayerSettings.SetScriptingBackend(BuildPipeline.GetBuildTargetGroup(BuildTarget.StandaloneOSX), PlayerSettings.GetDefaultScriptingBackend(BuildPipeline.GetBuildTargetGroup(BuildTarget.StandaloneOSX)));
//
//             var buildOptions = BuildOptions.IncludeTestAssemblies;
//             buildOptions |= BuildOptions.Development | BuildOptions.ConnectToHost | BuildOptions.IncludeTestAssemblies | BuildOptions.StrictMode;
//             // buildOptions |= BuildOptions.AllowDebugging;
//
//             buildOptions &= ~BuildOptions.AutoRunPlayer;
//             bool shouldContinue = true;
//             {
//                 var buildReport = BuildPipeline.BuildPlayer(
//                     new string[] { "Assets/Scenes/SampleScene.unity" },
//                     buildPath,
//                     BuildTarget.StandaloneOSX,
//                     buildOptions);
//                 Debug.Log("Building done !!!!!!");
//                 shouldContinue = buildReport.summary.result == BuildResult.Succeeded;
//             }
//
//             return shouldContinue;
// #endif
//         }

        [OneTimeSetUp]
        public void SetupSuite()
        {
            // Build(TestCoordinator.buildPath);

            SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                Debug.Log("starting MLAPI host");
                NetworkManager.Singleton.StartHost();
            };

            Debug.Log("starting processes");
            for (int i = 0; i < NbWorkers; i++)
            {
                TestCoordinator.StartWorkerNode(); // will automatically start as clients
            }

            Debug.Log("processes started");
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
            var startTime = Time.time;
            while (NetworkManager.Singleton.ConnectedClients.Count <= NbWorkers)
            {
                yield return new WaitForSeconds(0.2f);
                if (Time.time - startTime > TestCoordinator.maxWaitTimeout)
                {
                    throw new Exception($"waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {NbWorkers}, failing");
                }
            }
        }

        [TearDown]
        public void TeardownSingleTest()
        {
            TestCoordinator.Instance.TestRunTeardown();
        }

        [OneTimeTearDown]
        public void TeardownSuite()
        {
            // if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("Teardown, closing remote clients and stopping host");
                TestCoordinator.Instance.CloseRemoteClientRpc();
                NetworkManager.Singleton.StopHost();
            }
            // var startTime = Time.time;
            // // wait to run next tests until this test is completely torn down
            // while (Time.time - startTime < TestCoordinator.maxWaitTimeout && NetworkManager.Singleton.ConnectedClients.Count > 0)
            // {
            //     Thread.Sleep(10);
            // }
        }
    }


    public class TestCoordinatorSmokeTests : BaseMultiprocessTests
    {
        protected override int NbWorkers { get; } = 1;

        public static void ExecuteSimpleCoordinatorTest()
        {
            TestCoordinator.WriteResults(Time.time);
        }

        [UnityTest, Order(0)]
        public IEnumerator CheckTestCoordinator()
        {
            //make sure the test coordinator works
            // Call the method
            TestCoordinator.Instance.TriggerTestClientRpc(TestCoordinator.GetMethodInfo(ExecuteSimpleCoordinatorTest));

            for (int i = 0; i < NbWorkers; i++) // wait and test for the two clients
            {
                yield return new WaitUntil(TestCoordinator.SetResults());
                var resKey = TestCoordinator.Instance.CurrentResultClient;

                Debug.Log($"got results, asserting, result is {TestCoordinator.GetCurrentResult()} from key {resKey}");
                Assert.True(TestCoordinator.GetCurrentResult() > 0f);
            }
        }

    }
}

