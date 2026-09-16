#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class IOSLinkerPostProcessor
{
    [PostProcessBuild(1000)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        // Xcode 15's default linker cannot reach objc_msgSend selector stubs across the >128MB
        // IL2CPP Debug test binary; the classic linker inserts branch islands to keep the ARM64
        // B/BL calls in range.
        foreach (var guid in new[] { project.GetUnityMainTargetGuid(), project.GetUnityFrameworkTargetGuid() })
        {
            project.AddBuildProperty(guid, "OTHER_LDFLAGS", "-Wl,-ld_classic");
        }

        project.WriteToFile(projectPath);
    }
}
#endif
