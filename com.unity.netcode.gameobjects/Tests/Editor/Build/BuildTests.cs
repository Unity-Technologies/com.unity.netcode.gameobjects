#if !NGO_EXCLUDE_HEAVY_TESTS
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.Netcode.GameObjects.EditorTests
{
    internal class BuildTests
    {
        public const string DefaultBuildScenePath = "Tests/Editor/Build/BuildTestScene.unity";

        // Increased the Build test timeout from 3 to 10 minutes.
        [Timeout(900000)]
        [Test]
#if UNITY_EDITOR_LINUX
        // Temporary: BuildPlayer hangs past the job timeout on the ubuntu CI agents (6000.7 and trunk), while Windows and macOS pass.
        [Ignore("BuildPlayer hangs on the ubuntu CI agents; temporarily disabled on Linux editors until the ubuntu CI issue is resolved.")]
#endif
        public void BasicBuildTest()
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var buildTargetSupported = BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget);

            if (buildTargetSupported)
            {
                // Netcode for Entities' build preprocessor writes an asset under this folder and errors if the parent does not already exist.
                if (!AssetDatabase.IsValidFolder("Assets/netcode-build-assets-temp"))
                {
                    AssetDatabase.CreateFolder("Assets", "netcode-build-assets-temp");
                    AssetDatabase.Refresh();
                }

                var buildReport = BuildPipeline.BuildPlayer(
                    new[] { Path.Combine(packagePath, DefaultBuildScenePath) },
                    Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", nameof(BuildTests)),
                    buildTarget,
                    BuildOptions.None
                );

                Assert.AreEqual(BuildResult.Succeeded, buildReport.summary.result);
            }
            else
            {
                Debug.Log($"Skipped building player due to Unsupported Build Target");
            }
        }
    }
}
#endif
