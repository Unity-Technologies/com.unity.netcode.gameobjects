using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Unity.Netcode.Components")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
#endif // UNITY_EDITOR
#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("Unity.Multiplayer.Tools.Adapters.Ngo1WithUtp2")]
#endif // MULTIPLAYER_TOOLS
#if COM_UNITY_NETCODE_ADAPTER_UTP
[assembly: InternalsVisibleTo("Unity.Netcode.Adapter.UTP")]
#endif // COM_UNITY_NETCODE_ADAPTER_UTP

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Netcode.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.TestHelpers.Runtime")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
#endif // UNITY_EDITOR
#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
#endif // MULTIPLAYER_TOOLS
#endif // UNITY_INCLUDE_TESTS
