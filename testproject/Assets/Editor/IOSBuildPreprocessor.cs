#if UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class IOSBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
        {
            return;
        }

        // The Debug IL2CPP test binary otherwise exceeds the 128MB ARM64 B/BL branch range at link
        // time; full generic sharing keeps GameAssembly small enough for the default linker.
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, Il2CppCodeGeneration.OptimizeSize);
    }
}
#endif
