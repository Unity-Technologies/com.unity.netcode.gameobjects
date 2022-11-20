using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif

#if NGO_COMPONENTS_ASSEMBLY_RUNTIME_VISIBILITY
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#endif
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
