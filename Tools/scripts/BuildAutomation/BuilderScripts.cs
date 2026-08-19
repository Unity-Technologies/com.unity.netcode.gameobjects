using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

// This script automates the build process for different platforms.
// Can be triggered from scripts for the given configuration.
// Note: It's possible to have these as buttons in the editor (see example in comments).
// Ideally we would pass scripting backend and platform as parameters, but calling via -executeMethod
// requires static methods and parameters aren't passed in a usable way.
// TODO: add iOS support
public class BuilderScripts : MonoBehaviour
{
    static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => { Debug.Log($"Adding scene to build: {s.path}"); return s.path; })
            .ToArray();
    }

    static void BuildPlayer(BuildConfig config)
    {
        PlayerSettings.SetScriptingBackend(config.namedBuildTarget, config.scriptingBackend);

        if (config.graphicsAPIs != null && config.graphicsAPIs.Length > 0)
        {
            // We need to specify the graphics API for Android builds to ensure proper shader compilation. Vulkan is recommended for modern devices.
            PlayerSettings.SetUseDefaultGraphicsAPIs(config.buildTarget, false);
            PlayerSettings.SetGraphicsAPIs(config.buildTarget, config.graphicsAPIs);
        }

        if (config.applicationId != null)
            // This is needed only for mobiles since by default the application identifier quite often contains invalid characters like spaces so we wan't to make sure that this has a valid value. It's needed only for mobile since that's an app store requirement
            PlayerSettings.SetApplicationIdentifier(config.namedBuildTarget, config.applicationId);

        if (config.architecture.HasValue)
            // An integer value associated with the architecture of the build target. 0 - None, 1 - ARM64, 2 - Universal. Most modern Android devices use the ARM64 architecture.
            PlayerSettings.SetArchitecture(config.namedBuildTarget, config.architecture.Value);

        AssetDatabase.SaveAssets();

        var buildOptions = new BuildPlayerOptions
        {
            locationPathName = config.outputPath,
            target = config.buildTarget,
            options = BuildOptions.Development,
            scenes = GetEnabledScenes()
        };

        BuildPipeline.BuildPlayer(buildOptions);
    }

    struct BuildConfig
    {
        public NamedBuildTarget namedBuildTarget;
        public ScriptingImplementation scriptingBackend;
        public BuildTarget buildTarget;
        public GraphicsDeviceType[] graphicsAPIs;
        public string outputPath;
        public string applicationId;
        public int? architecture;
    }

    [MenuItem("Tools/Builder/Build Development Windows Il2cpp")]
    static void BuildWinIl2cpp() => BuildPlayer(new BuildConfig
    {
        namedBuildTarget = NamedBuildTarget.Standalone,
        scriptingBackend = ScriptingImplementation.IL2CPP,
        buildTarget = BuildTarget.StandaloneWindows64,
        graphicsAPIs = new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12 },
        outputPath = "./build/Windows_Il2cpp/PlaytestBuild.exe"
    });

    [MenuItem("Tools/Builder/Build Development Windows Mono")]
    static void BuildWinMono() => BuildPlayer(new BuildConfig
    {
        namedBuildTarget = NamedBuildTarget.Standalone,
        scriptingBackend = ScriptingImplementation.Mono2x,
        buildTarget = BuildTarget.StandaloneWindows64,
        graphicsAPIs = new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12 },
        outputPath = "./build/Windows_Mono/PlaytestBuild.exe"
    });

    [MenuItem("Tools/Builder/Build Development Mac Mono")]
    static void BuildMacMono() => BuildPlayer(new BuildConfig
    {
        namedBuildTarget = NamedBuildTarget.Standalone,
        scriptingBackend = ScriptingImplementation.Mono2x,
        buildTarget = BuildTarget.StandaloneOSX,
        graphicsAPIs = new[] { GraphicsDeviceType.Metal },
        outputPath = "./build/macOS_Mono/PlaytestBuild.app"
    });

    [MenuItem("Tools/Builder/Build Development Mac Il2cpp")]
    static void BuildMacIl2cpp() => BuildPlayer(new BuildConfig
    {
        namedBuildTarget = NamedBuildTarget.Standalone,
        scriptingBackend = ScriptingImplementation.IL2CPP,
        buildTarget = BuildTarget.StandaloneOSX,
        graphicsAPIs = new[] { GraphicsDeviceType.Metal },
        outputPath = "./build/macOS_Il2cpp/PlaytestBuild.app"
    });

    [MenuItem("Tools/Builder/Build Development Android Il2cpp")]
    static void BuildAndroidIl2cpp() => BuildPlayer(new BuildConfig
    {
        namedBuildTarget = NamedBuildTarget.Android,
        scriptingBackend = ScriptingImplementation.IL2CPP,
        buildTarget = BuildTarget.Android,
        graphicsAPIs = new[] { GraphicsDeviceType.Vulkan },
        outputPath = "./build/Android_Il2cpp_Vulkan/PlaytestBuild.apk",
        applicationId = "com.UnityTestRunner.UnityTestRunner",
        architecture = 1 // ARM64
    });
}
