using System.Linq;
using UnityEditor;

internal class AndroidBuilder
{
    private static void TestBuild()
    {
        // Build the player.\
        var buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = EditorBuildSettings.scenes.Select(x => x.path).ToArray();
        buildPlayerOptions.locationPathName = "Build/AndroidTestBuild.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.IncludeTestAssemblies;
        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
