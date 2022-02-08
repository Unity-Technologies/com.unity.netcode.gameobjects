using System.Runtime.CompilerServices;
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.UTP.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.Editor")]
#endif
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.Netcode.UTP.RuntimeTests")]





