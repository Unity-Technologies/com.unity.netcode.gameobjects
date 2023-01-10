using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
#endif
