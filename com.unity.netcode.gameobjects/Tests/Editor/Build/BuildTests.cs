using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class BuildTests
    {
        public const string DefaultBuildScenePath = "Tests/Editor/Build/BuildTestScene.unity";

        [Test]
        public void BasicBuildTest()
        {
            var execAssembly = Assembly.GetExecutingAssembly();
            var packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(execAssembly).assetPath;
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var buildTargetSupported = BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget);

            var buildReport = BuildPipeline.BuildPlayer(
                new[] { Path.Combine(packagePath, DefaultBuildScenePath) },
                Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", nameof(BuildTests)),
                buildTarget,
                BuildOptions.None
            );

            if (buildTargetSupported)
            {
                Assert.AreEqual(BuildResult.Succeeded, buildReport.summary.result);
            }
            else
            {
                LogAssert.Expect(LogType.Error, "Error building player because build target was unsupported");
            }
        }
    }
}
