using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MLAPI.Editor.CodeGen.ILPP
{
    internal class MLAPIRuntimeILPP : ILPostProcessor
    {
        private static MLAPIRuntimeILPP _instance;

        public override ILPostProcessor GetInstance()
        {
            return _instance ??= new MLAPIRuntimeILPP();
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.Name == ILPP.MLAPI_RUNTIME_ASSEMBLY_NAME;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly)) return null;

            var diagnostics = new List<DiagnosticMessage>();


            // read

            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            Console.WriteLine($"{nameof(NetworkBehaviourILPP)}.{nameof(Process)}: {compiledAssembly.Name}");
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);
            if (assemblyDefinition == null)
            {
                // todo: warning!
                return null;
            }


            // process

            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                var networkBehaviourTypeDef = mainModule.GetType(ILPP.NetworkBehaviour_FullName);
                if (networkBehaviourTypeDef != null)
                {
                    var ntableServerRPCFieldDef = networkBehaviourTypeDef.Fields.FirstOrDefault(f => f.Name == ILPP.MLAPI_ntableServerRPC_FieldName);
                    if (ntableServerRPCFieldDef != null) ntableServerRPCFieldDef.IsPublic = true;

                    var ntableClientRPCFieldDef = networkBehaviourTypeDef.Fields.FirstOrDefault(f => f.Name == ILPP.MLAPI_ntableClientRPC_FieldName);
                    if (ntableClientRPCFieldDef != null) ntableClientRPCFieldDef.IsPublic = true;

                    var nheadServerRPCMethodDef = networkBehaviourTypeDef.Methods.FirstOrDefault(m => m.Name == ILPP.MLAPI_nheadServerRPC_MethodName);
                    if (nheadServerRPCMethodDef != null) nheadServerRPCMethodDef.IsPublic = true;

                    var nheadClientRPCMethodDef = networkBehaviourTypeDef.Methods.FirstOrDefault(m => m.Name == ILPP.MLAPI_nheadClientRPC_MethodName);
                    if (nheadClientRPCMethodDef != null) nheadClientRPCMethodDef.IsPublic = true;

                    var nwriteServerRPCMethodDef = networkBehaviourTypeDef.Methods.FirstOrDefault(m => m.Name == ILPP.MLAPI_nwriteServerRPC_MethodName);
                    if (nwriteServerRPCMethodDef != null) nwriteServerRPCMethodDef.IsPublic = true;

                    var nwriteClientRPCMethodDef = networkBehaviourTypeDef.Methods.FirstOrDefault(m => m.Name == ILPP.MLAPI_nwriteClientRPC_MethodName);
                    if (nwriteClientRPCMethodDef != null) nwriteClientRPCMethodDef.IsPublic = true;

                    var ncallServerRPCMethodDef = networkBehaviourTypeDef.Methods.FirstOrDefault(m => m.Name == ILPP.MLAPI_ncallServerRPC_MethodName);
                    if (ncallServerRPCMethodDef != null) ncallServerRPCMethodDef.IsPublic = true;

                    var ncallClientRPCMethodDef = networkBehaviourTypeDef.Methods.FirstOrDefault(m => m.Name == ILPP.MLAPI_ncallClientRPC_MethodName);
                    if (ncallClientRPCMethodDef != null) ncallClientRPCMethodDef.IsPublic = true;
                }
            }
            else
            {
                // todo: warning!
                Console.WriteLine($"XXX Cannot find MainModule -> {assemblyDefinition.Name}");
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

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), diagnostics);
        }
    }
}
