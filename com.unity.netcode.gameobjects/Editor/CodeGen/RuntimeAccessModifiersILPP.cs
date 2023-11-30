using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
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

                    switch (typeDefinition.Name)
                    {
                        case nameof(NetworkManager):
                            ProcessNetworkManager(typeDefinition, compiledAssembly.Defines);
                            break;
                        case nameof(NetworkBehaviour):
                            ProcessNetworkBehaviour(typeDefinition);
                            break;
                        case nameof(RpcAttribute):
                            foreach (var methodDefinition in typeDefinition.GetConstructors())
                            {
                                if (methodDefinition.Parameters.Count == 0)
                                {
                                    methodDefinition.IsPublic = true;
                                }
                            }
                            break;
                        case nameof(__RpcParams):
                        case nameof(RpcFallbackSerialization):
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

        // TODO: Deprecate...
        // This is changing accessibility for values that are no longer used, but since our validator runs
        // after ILPP and sees those values as public, they cannot be removed until a major version change.
        private void ProcessNetworkManager(TypeDefinition typeDefinition, string[] assemblyDefines)
        {
            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == nameof(NetworkManager.__rpc_func_table))
                {
                    fieldDefinition.IsPublic = true;
                }

                if (fieldDefinition.Name == nameof(NetworkManager.RpcReceiveHandler))
                {
                    fieldDefinition.IsPublic = true;
                }

                if (fieldDefinition.Name == nameof(NetworkManager.__rpc_name_table))
                {
                    fieldDefinition.IsPublic = true;
                }
            }

            foreach (var nestedTypeDefinition in typeDefinition.NestedTypes)
            {
                if (nestedTypeDefinition.Name == nameof(NetworkManager.RpcReceiveHandler))
                {
                    nestedTypeDefinition.IsNestedPublic = true;
                }
            }
        }

        private void ProcessNetworkBehaviour(TypeDefinition typeDefinition)
        {
            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                if (nestedType.Name == nameof(NetworkBehaviour.__RpcExecStage))
                {
                    nestedType.IsNestedFamily = true;
                }
                if (nestedType.Name == nameof(NetworkBehaviour.RpcReceiveHandler))
                {
                    nestedType.IsNestedPublic = true;
                }
            }

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == nameof(NetworkBehaviour.__rpc_exec_stage) || fieldDefinition.Name == nameof(NetworkBehaviour.NetworkVariableFields))
                {
                    fieldDefinition.IsFamilyOrAssembly = true;
                }
                if (fieldDefinition.Name == nameof(NetworkBehaviour.__rpc_func_table))
                {
                    fieldDefinition.IsFamilyOrAssembly = true;
                }

                if (fieldDefinition.Name == nameof(NetworkBehaviour.RpcReceiveHandler))
                {
                    fieldDefinition.IsFamilyOrAssembly = true;
                }

                if (fieldDefinition.Name == nameof(NetworkBehaviour.__rpc_name_table))
                {
                    fieldDefinition.IsFamilyOrAssembly = true;
                }
            }

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                if (methodDefinition.Name == nameof(NetworkBehaviour.__beginSendServerRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__endSendServerRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__beginSendClientRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__endSendClientRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__beginSendRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__endSendRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__initializeVariables) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__initializeRpcs) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__registerRpc) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__nameNetworkVariable) ||
                    methodDefinition.Name == nameof(NetworkBehaviour.__createNativeList))
                {
                    methodDefinition.IsFamily = true;
                }

                if (methodDefinition.Name == nameof(NetworkBehaviour.__getTypeName))
                {
                    methodDefinition.IsFamilyOrAssembly = true;
                }
            }
        }
    }
}
