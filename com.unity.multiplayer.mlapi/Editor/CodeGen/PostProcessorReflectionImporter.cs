using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace MLAPI.Editor.CodeGen
{
    internal class PostProcessorReflectionImporter : DefaultReflectionImporter
    {
        private const string SystemPrivateCoreLib = "System.Private.CoreLib";
        private readonly AssemblyNameReference m_CorrectCorlib;

        public PostProcessorReflectionImporter(ModuleDefinition module) : base(module)
        {
            m_CorrectCorlib = module.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "netstandard" || a.Name == SystemPrivateCoreLib);
        }

        public override AssemblyNameReference ImportReference(AssemblyName reference)
        {
            return m_CorrectCorlib != null && reference.Name == SystemPrivateCoreLib ? m_CorrectCorlib : base.ImportReference(reference);
        }
    }
}