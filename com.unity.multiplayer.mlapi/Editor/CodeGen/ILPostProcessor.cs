#if !UNITY_2020_2_OR_NEWER
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MLAPI.Editor.CodeGen
{
    public abstract class ILPostProcessor
    {
        public abstract bool WillProcess(ICompiledAssembly compiledAssembly);
        public abstract ILPostProcessResult Process(ICompiledAssembly compiledAssembly);
        public abstract ILPostProcessor GetInstance();
    }
}
#endif