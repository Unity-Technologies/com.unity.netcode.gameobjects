using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;

namespace Unity.Netcode.Editor.CodeGen
{

    internal sealed class INetworkMessageILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }


            m_Diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly, out var resolver);
            if (assemblyDefinition == null)
            {
                m_Diagnostics.AddError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return null;
            }

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                if (ImportReferences(mainModule))
                {
                    // process `INetworkMessage` types
                    mainModule.GetTypes()
                        .Where(t => t.HasInterface(CodeGenHelpers.INetworkMessage_FullName))
                        .ToList()
                        .ForEach(b => ProcessINetworkMessage(b));
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


        private TypeReference m_FastBufferReader_TypeRef;
        private TypeReference m_NetworkContext_TypeRef;

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            m_FastBufferReader_TypeRef = moduleDefinition.ImportReference(typeof(FastBufferReader));
            m_NetworkContext_TypeRef = moduleDefinition.ImportReference(typeof(NetworkContext));

            return true;
        }

        private void ProcessINetworkMessage(TypeDefinition typeDefinition)
        {
            var foundAValidMethod = false;
            SequencePoint typeSequence = null;
            foreach (var method in typeDefinition.Methods)
            {
                var resolved = method.Resolve();
                var methodSequence = resolved.DebugInformation.SequencePoints.FirstOrDefault();
                if (typeSequence == null || methodSequence.StartLine < typeSequence.StartLine)
                {
                    typeSequence = methodSequence;
                }
                if (resolved.IsStatic && resolved.IsPublic && resolved.Name == "Receive" && resolved.Parameters.Count == 2
                    && !resolved.Parameters[0].IsIn
                    && !resolved.Parameters[0].ParameterType.IsByReference
                    && resolved.Parameters[0].ParameterType.Resolve() ==
                        m_FastBufferReader_TypeRef.Resolve()
                    && resolved.Parameters[1].IsIn
                    && resolved.Parameters[1].ParameterType.IsByReference
                    && resolved.Parameters[1].ParameterType.GetElementType().Resolve() == m_NetworkContext_TypeRef.Resolve()
                    && resolved.ReturnType == resolved.Module.TypeSystem.Void)
                {
                    foundAValidMethod = true;
                    break;
                }
            }

            if (!foundAValidMethod)
            {
                m_Diagnostics.AddError(typeSequence, $"Class {typeDefinition.FullName} does not implement required function: `public static void Receive(FastBufferReader, in NetworkContext)`");
            }
        }
    }
}
