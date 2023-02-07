using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif // UNITY_EDITOR
#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
#endif // MULTIPLAYER_TOOLS
#endif // UNITY_INCLUDE_TESTS
