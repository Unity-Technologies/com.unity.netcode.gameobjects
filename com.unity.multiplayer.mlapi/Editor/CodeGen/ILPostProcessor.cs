#if !USE_UNITY_ILPP

using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MLAPI.Editor.CodeGen
{
    public abstract class ILPostProcessor
    {
        public virtual bool WillProcess(ICompiledAssembly compiledAssembly) => false;
        public virtual ILPostProcessResult Process(ICompiledAssembly compiledAssembly) => null;
        public virtual ILPostProcessor GetInstance() => null;
    }
}

#endif
