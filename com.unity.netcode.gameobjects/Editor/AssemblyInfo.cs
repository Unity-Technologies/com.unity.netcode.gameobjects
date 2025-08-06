using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.Tests")]
[assembly: InternalsVisibleTo("TestProject.Runtime.Tests")]
#endif // UNITY_EDITOR
#endif // UNITY_INCLUDE_TESTS
