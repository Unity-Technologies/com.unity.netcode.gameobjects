using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
#endif

#if UNITY_INCLUDE_TESTS
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
#endif
