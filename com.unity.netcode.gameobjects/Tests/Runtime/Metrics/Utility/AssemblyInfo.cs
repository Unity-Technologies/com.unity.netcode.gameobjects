using System.Runtime.CompilerServices;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("TestProject.EditorTests")]
[assembly: InternalsVisibleTo("TestProject.RuntimeTests")]
[assembly: InternalsVisibleTo("TestProject.ToolsIntegration.RuntimeTests")]
#endif
