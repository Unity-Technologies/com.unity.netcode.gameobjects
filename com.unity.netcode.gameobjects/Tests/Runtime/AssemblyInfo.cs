using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("TestProject.Runtime.Tests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("TestProject.Editor.Tests")]
#endif // UNITY_EDITOR
#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
#endif // MULTIPLAYER_TOOLS
#endif // UNITY_INCLUDE_TESTS
