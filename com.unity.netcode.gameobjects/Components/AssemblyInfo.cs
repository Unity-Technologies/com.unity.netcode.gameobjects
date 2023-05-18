using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.GameObjects.Editor")]
[assembly: InternalsVisibleTo("Unity.Netcode.GameObjects.Editor.CodeGen")]
#endif // UNITY_EDITOR

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.GameObjects.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif // UNITY_EDITOR
#endif // UNITY_INCLUDE_TESTS
