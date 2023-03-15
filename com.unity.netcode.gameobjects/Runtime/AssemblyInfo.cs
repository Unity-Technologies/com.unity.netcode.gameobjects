using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
#endif
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.TestHelpers.Runtime")]
[assembly: InternalsVisibleTo("Unity.Netcode.Adapter.UTP")]
[assembly: InternalsVisibleTo("Unity.Multiplayer.Tools.Adapters.Ngo1WithUtp2")]
