using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
#endif // UNITY_EDITOR

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif // UNITY_EDITOR
#endif // UNITY_INCLUDE_TESTS
