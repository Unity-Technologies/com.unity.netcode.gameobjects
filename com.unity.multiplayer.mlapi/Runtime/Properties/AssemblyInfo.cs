using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.EditorTests")]
#endif

#if UNITY_2020_2_OR_NEWER && UNITY_EDITOR
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Unity.Multiplayer.MLAPI.Editor.CodeGen")]
#endif