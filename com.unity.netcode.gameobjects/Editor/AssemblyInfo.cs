using System.Runtime.CompilerServices;

// The Unity CLI commands (com.unity.pipeline) wrap the internal query layer in Editor/CliQueries.
[assembly: InternalsVisibleTo("Unity.Netcode.GameObjects.CLI.Editor")]

#if UNITY_INCLUDE_TESTS
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.GameObjects.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Netcode.GameObjects.CLI.Editor.Tests")]
[assembly: InternalsVisibleTo("TestProject.Runtime.Tests")]
#endif // UNITY_EDITOR
#endif // UNITY_INCLUDE_TESTS
