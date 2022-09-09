using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.Netcode.Editor.CodeGen.Import;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;

namespace Unity.Netcode.Editor.CodeGen
{
    internal sealed class RuntimeAccessModifiersILPP : ILPPInterface
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
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly, out var unused);
            if (assemblyDefinition == null)
            {
                m_Diagnostics.AddError($"Cannot read Netcode Runtime assembly definition: {compiledAssembly.Name}");
                return null;
            }

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                foreach (var typeDefinition in mainModule.Types)
                {
                    if (!typeDefinition.IsClass)
                    {
                        continue;
                    }

                    switch (typeDefinition.FullName)
                    {
                        case Types.NetworkManager:
                            ProcessNetworkManager(typeDefinition, compiledAssembly.Defines);
                            break;
                        case Types.NetworkBehaviour:
                            ProcessNetworkBehaviour(typeDefinition);
                            break;
                        case Types.__RpcParams:
                            typeDefinition.IsPublic = true;
                            break;
                    }
                }
            }
            else
            {
                m_Diagnostics.AddError($"Cannot get main module from Netcode Runtime assembly definition: {compiledAssembly.Name}");
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

        private void ProcessNetworkManager(TypeDefinition typeDefinition, string[] assemblyDefines)
        {
            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == Fields.NetworkManager___rpc_func_table)
                {
                    fieldDefinition.IsPublic = true;
                }

                if (fieldDefinition.Name == Fields.NetworkManager_RpcReceiveHandler)
                {
                    fieldDefinition.IsPublic = true;
                }

                if (fieldDefinition.Name == Fields.NetworkManager___rpc_name_table)
                {
                    fieldDefinition.IsPublic = true;
                }
            }
        }

        private void ProcessNetworkBehaviour(TypeDefinition typeDefinition)
        {
            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                if (nestedType.Name == Types.NetworkBehaviour___RpcExecStage)
                {
                    nestedType.IsNestedFamily = true;
                }
            }

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == Fields.NetworkBehaviour___rpc_exec_stage)
                {
                    fieldDefinition.IsFamily = true;
                }
            }

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                if (methodDefinition.Name == Methods.NetworkBehaviour___beginSendServerRpc ||
                    methodDefinition.Name == Methods.NetworkBehaviour___endSendServerRpc ||
                    methodDefinition.Name == Methods.NetworkBehaviour___beginSendClientRpc ||
                    methodDefinition.Name == Methods.NetworkBehaviour___endSendClientRpc)
                {
                    methodDefinition.IsFamily = true;
                }
            }
        }
    }
}
