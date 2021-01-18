#if !USE_UNITY_ILPP

using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MLAPI.Editor.CodeGen
{
    internal class AssemblyDefinition
    {
        public string name = "";
        public string[] references = new string[0];
        public string[] optionalUnityReferences = new string[0];
        public string[] includePlatforms = new string[0];
        public string[] excludePlatforms = new string[0];
        public string[] precompiledReferences = new string[0];
    }

    public abstract class ILPostProcessor
    {
        public virtual bool WillProcess(ICompiledAssembly compiledAssembly) => false;
        public virtual ILPostProcessResult Process(ICompiledAssembly compiledAssembly) => null;
        public virtual ILPostProcessor GetInstance() => null;
    }
}

#endif
