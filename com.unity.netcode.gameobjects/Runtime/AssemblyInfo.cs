using System.Runtime.CompilerServices;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#endif
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
