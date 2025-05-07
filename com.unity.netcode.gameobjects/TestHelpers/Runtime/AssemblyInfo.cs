using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Netcode.Runtime.Tests")]
[assembly: InternalsVisibleTo("TestProject.Runtime.Tests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.Tests")]
[assembly: InternalsVisibleTo("TestProject.Editor.Tests")]
#endif // UNITY_EDITOR
#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("Unity.Multiplayer.Tools.GameObjects.Tests")]
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
#endif // MULTIPLAYER_TOOLS
#endif // UNITY_INCLUDE_TESTS
