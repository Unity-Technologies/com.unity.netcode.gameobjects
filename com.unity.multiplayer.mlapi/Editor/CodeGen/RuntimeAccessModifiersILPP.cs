#if UNITY_2020_2_OR_NEWER
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;

namespace MLAPI.Editor.CodeGen
{
    internal sealed class RuntimeAccessModifiersILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName;

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly)) return null;
            m_Diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly);
            if (assemblyDefinition == null)
            {
                m_Diagnostics.AddError($"Cannot read MLAPI Runtime assembly definition: {compiledAssembly.Name}");
                return null;
            }

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                foreach (var typeDefinition in mainModule.Types)
                {
                    if (!typeDefinition.IsClass) continue;

                    switch (typeDefinition.Name)
                    {
                        case nameof(NetworkingManager):
                            ProcessNetworkManager(typeDefinition);
                            break;
                        case nameof(NetworkedBehaviour):
                            ProcessNetworkBehaviour(typeDefinition);
                            break;
                    }
                }
            }
            else m_Diagnostics.AddError($"Cannot get main module from MLAPI Runtime assembly definition: {compiledAssembly.Name}");

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

        private void ProcessNetworkManager(TypeDefinition typeDefinition)
        {
            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == nameof(NetworkingManager.__ntable))
                {
                    fieldDefinition.IsPublic = true;
                }
            }
        }

        private void ProcessNetworkBehaviour(TypeDefinition typeDefinition)
        {
            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                if (nestedType.Name == nameof(NetworkedBehaviour.__NExec))
                {
                    nestedType.IsNestedFamily = true;
                }
            }

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == nameof(NetworkedBehaviour.__nexec))
                {
                    fieldDefinition.IsFamily = true;
                }
            }

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                switch (methodDefinition.Name)
                {
                    case nameof(NetworkedBehaviour.__beginSendServerRpc):
                    case nameof(NetworkedBehaviour.__endSendServerRpc):
                    case nameof(NetworkedBehaviour.__beginSendClientRpc):
                    case nameof(NetworkedBehaviour.__endSendClientRpc):
                        methodDefinition.IsFamily = true;
                        break;
                }
            }
        }
    }
}
#endif