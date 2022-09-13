#if UNITY_EDITOR_OSX
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class WebGLBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 1;
    public void OnPreprocessBuild(BuildReport report)
    {
        System.Environment.SetEnvironmentVariable("EMSDK_PYTHON", "python3");
    }
}
#endif // UNITY_EDITOR_OSX
