using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Unity.Netcode.Components")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.CodeGen")]
#endif // UNITY_EDITOR

#if COM_UNITY_NETCODE_ADAPTER_UTP
[assembly: InternalsVisibleTo("Unity.Netcode.Adapter.UTP")]
#endif // COM_UNITY_NETCODE_ADAPTER_UTP

#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Netcode.Runtime.Tests")]
[assembly: InternalsVisibleTo("Unity.Netcode.TestHelpers.Runtime")]
[assembly: InternalsVisibleTo("TestProject.Runtime.Tests")]
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Netcode.Editor.Tests")]
[assembly: InternalsVisibleTo("TestProject.Editor.Tests")]
#endif // UNITY_EDITOR

#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("Unity.Multiplayer.Tools.GameObjects.Tests")]
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
[assembly: InternalsVisibleTo("TestProject.Netcode.GameObjejct.Runtime.Tests")]
#endif // MULTIPLAYER_TOOLS
#endif // UNITY_INCLUDE_TESTS
// Should always be visible when multiplayer tools package is installed.
#if MULTIPLAYER_TOOLS
[assembly: InternalsVisibleTo("Unity.Multiplayer.Tools.Adapters.Ngo1WithUtp2")]
#endif
