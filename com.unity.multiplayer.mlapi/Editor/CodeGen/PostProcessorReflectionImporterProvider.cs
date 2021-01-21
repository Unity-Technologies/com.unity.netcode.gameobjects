using Mono.Cecil;

namespace MLAPI.Editor.CodeGen
{
    internal class PostProcessorReflectionImporterProvider : IReflectionImporterProvider
    {
        public IReflectionImporter GetReflectionImporter(ModuleDefinition moduleDefinition)
        {
            return new PostProcessorReflectionImporter(moduleDefinition);
        }
    }
}