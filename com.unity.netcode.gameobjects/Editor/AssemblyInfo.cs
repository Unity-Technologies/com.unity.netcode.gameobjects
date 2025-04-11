using System.Runtime.CompilerServices;

#if UNITY_INCLUDE_TESTS
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#endif // UNITY_EDITOR
#endif // UNITY_INCLUDE_TESTS
