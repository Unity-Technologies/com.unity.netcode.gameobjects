using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
            var buildReport = BuildPipeline.BuildPlayer(
                new[] { Path.Combine(packagePath, DefaultBuildScenePath) },
                Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", nameof(BuildTests)),
                EditorUserBuildSettings.activeBuildTarget,
                BuildOptions.None
            );
            Assert.AreEqual(BuildResult.Succeeded, buildReport.summary.result);
        }
    }
}
