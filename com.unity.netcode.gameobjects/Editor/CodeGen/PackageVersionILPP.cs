using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
using MethodAttributes = Mono.Cecil.MethodAttributes;


namespace Unity.Netcode.Editor.CodeGen
{
    internal sealed class PackageVersionILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName;

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            m_Diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly, out m_AssemblyResolver);
            if (assemblyDefinition == null)
            {
                m_Diagnostics.AddError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return null;
            }

            // modules
            (_, m_NetcodeModule) = CodeGenHelpers.FindBaseModules(assemblyDefinition, m_AssemblyResolver);

            if (m_NetcodeModule == null)
            {
                m_Diagnostics.AddError($"Cannot find Netcode module: {CodeGenHelpers.NetcodeModuleName}");
                return null;
            }

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                if (ImportReferences(mainModule))
                {
                    try
                    {
                        CreateModuleInitializer(assemblyDefinition);
                    }
                    catch (Exception e)
                    {
                        m_Diagnostics.AddError((e.ToString() + e.StackTrace).Replace("\n", "|").Replace("\r", "|"));
                    }
                }
                else
                {
                    m_Diagnostics.AddError($"Cannot import references into main module: {mainModule.Name}");
                }
            }
            else
            {
                m_Diagnostics.AddError($"Cannot get main module from assembly definition: {compiledAssembly.Name}");
            }

            mainModule.RemoveRecursiveReferences();

            // write
            var pe = new MemoryStream();
            var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            assemblyDefinition.Write(pe, writerParameters);

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), m_Diagnostics);
        }

        private ModuleDefinition m_NetcodeModule;
        private PostProcessorAssemblyResolver m_AssemblyResolver;

        private FieldReference m_PackageMetadata_VersionString_FieldRef;

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            TypeDefinition packageMetadataTypeDef = null;
            foreach (var netcodeTypeDef in m_NetcodeModule.GetAllTypes())
            {
                if (netcodeTypeDef.Name == nameof(PackageMetadata))
                {
                    packageMetadataTypeDef = netcodeTypeDef;
                    break;
                }
            }

            foreach (var fieldDef in packageMetadataTypeDef.Fields)
            {
                if (fieldDef.Name == nameof(PackageMetadata.VersionString))
                {
                    m_PackageMetadata_VersionString_FieldRef = moduleDefinition.ImportReference(fieldDef);
                    break;
                }
            }
            return true;
        }

        private MethodDefinition GetOrCreateStaticConstructor(TypeDefinition typeDefinition)
        {
            var staticCtorMethodDef = typeDefinition.GetStaticConstructor();
            if (staticCtorMethodDef == null)
            {
                staticCtorMethodDef = new MethodDefinition(
                    ".cctor", // Static Constructor (constant-constructor)
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName |
                    MethodAttributes.Static,
                    typeDefinition.Module.TypeSystem.Void);
                staticCtorMethodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                typeDefinition.Methods.Add(staticCtorMethodDef);
            }

            return staticCtorMethodDef;
        }

        // Creates a static module constructor (which is executed when the module is loaded) that registers all the message types in the assembly with MessagingSystem.
        // This is the same behavior as annotating a static method with [ModuleInitializer] in standardized C# (that attribute doesn't exist in Unity, but the static module constructor still works).
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.moduleinitializerattribute?view=net-5.0
        // https://web.archive.org/web/20100212140402/http://blogs.msdn.com/junfeng/archive/2005/11/19/494914.aspx
        private void CreateModuleInitializer(AssemblyDefinition assembly, [CallerFilePath] string filePath = "")
        {
            foreach (var typeDefinition in assembly.MainModule.Types)
            {
                if (typeDefinition.FullName == "<Module>")
                {
                    var staticCtorMethodDef = GetOrCreateStaticConstructor(typeDefinition);

                    var processor = staticCtorMethodDef.Body.GetILProcessor();

                    var instructions = new List<Instruction>();

                    // This is annoying... when used within ILPP:
                    // PackageInfo.FindForAssembly - throws System.MissingMethodException: Attempted to access a missing method.
                    // PackageInfo.FindForAssetPath - throws System.MissingMethodException: Attempted to access a missing method.
                    // JsonUtiity.FromJson - throws System.MissingMethodException: Attempted to access a missing method.
                    //
                    // Non-ILPP solutions for this fail because they require an object to exist in the scene to hold the data,
                    // and we can't 100% guarantee that NetworkManager, etc, won't be created programmatically.
                    //
                    // This leaves us with only the option of finding and opening package.json ourselves and finding the version
                    // string with a regex... Let us hope that no one ever adds a double quote inside the version string.
                    var dirName = Path.GetDirectoryName(filePath);
                    var jsonLocation = Path.Combine(new[] { dirName, "..", "..", "package.json" });
                    var jsonData = File.ReadAllText(jsonLocation);
                    var versionRegex = new Regex("\"version\":\\s*\"([^\"]+)\"");

                    var match = versionRegex.Match(jsonData);
                    var version = match.Groups[1].Captures[0].Value;

                    // PackageMetadata.VersionString = "<version>"
                    instructions.Add(processor.Create(OpCodes.Ldstr, version));
                    instructions.Add(processor.Create(OpCodes.Stsfld, m_PackageMetadata_VersionString_FieldRef));

                    instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));
                    break;
                }
            }
        }
    }
}
