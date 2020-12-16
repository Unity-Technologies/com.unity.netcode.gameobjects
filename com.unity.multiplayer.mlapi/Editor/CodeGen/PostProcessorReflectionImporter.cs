using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace MLAPI.Editor.CodeGen
{
    internal class PostProcessorReflectionImporter : DefaultReflectionImporter
    {
        private const string SystemPrivateCoreLib = "System.Private.CoreLib";
        private readonly AssemblyNameReference _correctCorlib;

        public PostProcessorReflectionImporter(ModuleDefinition module) : base(module)
        {
            _correctCorlib = module.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "netstandard" || a.Name == SystemPrivateCoreLib);
        }

        public override AssemblyNameReference ImportReference(AssemblyName reference)
        {
            return _correctCorlib != null && reference.Name == SystemPrivateCoreLib ? _correctCorlib : base.ImportReference(reference);
        }
    }
}
