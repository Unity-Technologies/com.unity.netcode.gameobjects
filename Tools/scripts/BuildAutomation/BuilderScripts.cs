using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// This script is used to automate the build process for different platforms.
// When included in the project, it can be triggered from the script the game for teh given configuration.
// Note that it's possible to have those as a button in the editor (see https://github.cds.internal.unity3d.com/unity/Megacity-Metro/blob/c3b1b16ff1f04f96fbfbcc3267696679ad4e8396/Megacity-Metro/Assets/Scripts/Utils/Editor/BuilderScript.cs)
// Ideally we would like to pass scripting backend and platform as parameters instead of having different script per each combintation but the nature of calling this nmethod via script (via -executeMethod) is that it needs to be static and the parameters are not passed in a way that we can use them.
// TODO: add iOS support
public class BuilderScripts : MonoBehaviour
{
    [MenuItem("Tools/Builder/Build Development Windows Il2cpp")]
    static void BuildWinIl2cpp()
    {
        // This part modifies Player Settings. We only use it (for our case) to modify scripting backend
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false); // disable auto graphic to use our own custom list
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12}); // We need to specify the graphics API for Android builds to ensure proper shader compilation. Vulkan is recommended for modern devices.

        // Below you can see additional settings that you can apply to the build:
        //PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Direct3D12});
        //PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,0);

        // The settings that we applied above need to be saved.
        AssetDatabase.SaveAssets();

        // We want to build all scenes in build settings, so we collect them here.
        // If you want to build only specific scenes, then you could just use something like buildPlayerOptions.scenes = new[] { "Assets/Scenes/Menu.unity","Assets/Scenes/Main.unity" }; below.
        List<string> scenesToAdd = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                Debug.Log("Adding scene to build: " + scene.path);
                scenesToAdd.Add(scene.path);
            }
        }

        // This is an equivalent of BuildPlayerOptions in the Unity Editor.
        // We want to choose development build, what platform are we targetting, where to save the build and which scenes to include.
        // Some of those options can be omitted when triggering this script from withing GUI since more implicit context is provided (targetGroup, subtarget)
        // subtarget = (int)StandaloneBuildSubtarget.Player
        // targetGroup = BuildTargetGroup.Standalone
        // extraScriptingDefines = new[] { "NETCODE_DEBUG", "UNITY_CLIENT" },
        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = "./build/Windows_Il2cpp/PlaytestBuild.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
            scenes = scenesToAdd.ToArray()
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("Tools/Builder/Build Development Windows Mono")]
    static void BuildWinMono()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false); // disable auto graphic to use our own custom list
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new []{GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12}); // We need to specify the graphics API for Android builds to ensure proper shader compilation. Vulkan is recommended for modern devices.

        AssetDatabase.SaveAssets();

        List<string> scenesToAdd = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                Debug.Log("Adding scene to build: " + scene.path);
                scenesToAdd.Add(scene.path);
            }
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = "./build/Windows_Mono/PlaytestBuild.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
            scenes = scenesToAdd.ToArray()
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("Tools/Builder/Build Development Mac Mono")]
    static void BuildMacMono()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false); // disable auto graphic to use our own custom list
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new []{GraphicsDeviceType.Metal}); // enforcing Metal Graphics API. Without this there will be shader errors in the final build.

        AssetDatabase.SaveAssets();

        List<string> scenesToAdd = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                Debug.Log("Adding scene to build: " + scene.path);
                scenesToAdd.Add(scene.path);
            }
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = "./build/macOS_Mono/PlaytestBuild.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development,
            scenes = scenesToAdd.ToArray()
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("Tools/Builder/Build Development Mac Il2cpp")]
    static void BuildMacIl2cpp()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new []{GraphicsDeviceType.Metal}); // enforcing Metal Graphics API. Without this there will be shader errors in the final build.

        AssetDatabase.SaveAssets();

        List<string> scenesToAdd = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                Debug.Log("Adding scene to build: " + scene.path);
                scenesToAdd.Add(scene.path);
            }
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = "./build/macOS_Il2cpp/PlaytestBuild.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development,
            scenes = scenesToAdd.ToArray()
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("Tools/Builder/Build Development Android Il2cpp")]
    static void BuildAndroidIl2cpp()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.UnityTestRunner.UnityTestRunner"); // This is needed only for mobiles since by default the application identifier quite often contains invalid characters like spaces so we wan't to make sure that this has a valid value. It's needed only for mobile since that's an app store requirement
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false); // disable auto graphic to use our own custom list
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{GraphicsDeviceType.Vulkan}); // We need to specify the graphics API for Android builds to ensure proper shader compilation. Vulkan is recommended for modern devices.
        PlayerSettings.SetArchitecture(BuildTargetGroup.Android,1); // An integer value associated with the architecture of the build target. 0 - None, 1 - ARM64, 2 - Universal. most modern Android devices use the ARM64 architecture

        AssetDatabase.SaveAssets();

        List<string> scenesToAdd = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                Debug.Log("Adding scene to build: " + scene.path);
                scenesToAdd.Add(scene.path);
            }
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            locationPathName = "./build/Android_Il2cpp_Vulkan/PlaytestBuild.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development,
            scenes = scenesToAdd.ToArray()
        };

        BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
