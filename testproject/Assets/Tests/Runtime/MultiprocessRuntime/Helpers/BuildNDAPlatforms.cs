using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif


namespace Unity.Netcode.MultiprocessRuntimeTests
{
	public static class BuildNDAPlatforms
	{
#if UNITY_EDITOR
        [MenuItem(BuildMultiprocessTestPlayer.MultiprocessBaseMenuName + "/XboxOne Player")]
        public static void BuildGameCoreXboxOne()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.GameCoreXboxOne, ScriptingImplementation.IL2CPP);

            // XboxBuildSubtarget.Debug
            EditorUserBuildSettings.xboxBuildSubtarget |= XboxBuildSubtarget.Development;
            EditorUserBuildSettings.xboxOneDeployMethod = XboxOneDeployMethod.Push;

            var report = BuildMultiprocessTestPlayer.BuildPlayerUtility(BuildTarget.GameCoreXboxOne, "");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }
		
        [MenuItem(BuildMultiprocessTestPlayer.MultiprocessBaseMenuName + "/PS4 Player")]
        public static void BuildPS4()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.PS4, ScriptingImplementation.IL2CPP);
            var report = BuildMultiprocessTestPlayer.BuildPlayerUtility(BuildTarget.PS4, "");
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed! {report.summary.totalErrors} errors");
            }
        }
#endif
    }
}
