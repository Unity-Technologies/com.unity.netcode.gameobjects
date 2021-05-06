using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.Editor.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.Editor")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.TestProject.UnitTests.Runtime")]
#endif
