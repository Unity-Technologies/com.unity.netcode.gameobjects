using System.Runtime.CompilerServices;

#if UNITY_2020_2_OR_NEWER && UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.Editor.CodeGen")]
#endif

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.RuntimeTests")]
#endif