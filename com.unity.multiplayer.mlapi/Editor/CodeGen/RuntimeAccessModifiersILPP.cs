using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MLAPI.Editor.CodeGen
{
    internal sealed class RuntimeAccessModifiersILPP : ILPostProcessor
    {
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName;

        private readonly List<DiagnosticMessage> _diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly)) return null;
            _diagnostics.Clear();


            // read
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);
            if (assemblyDefinition == null)
            {
                _diagnostics.AddError("todo: informative error message -> assemblyDefinition == null");
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
            else _diagnostics.AddError("todo: informative error message -> mainModule != null");


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

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), _diagnostics);
        }

        private void ProcessNetworkManager(TypeDefinition typeDefinition)
        {
            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == "__ntable")
                {
                    fieldDefinition.IsPublic = true;
                }
            }
        }

        private void ProcessNetworkBehaviour(TypeDefinition typeDefinition)
        {
            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                if (nestedType.Name == "NExec")
                {
                    nestedType.IsNestedFamily = true;
                }
            }

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.Name == "__nexec")
                {
                    fieldDefinition.IsFamily = true;
                }
            }

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                switch (methodDefinition.Name)
                {
                    case "BeginServerRPC":
                    case "BeginClientRPC":
                    case "EndServerRPC":
                    case "EndClientRPC":
                        methodDefinition.IsFamily = true;
                        break;
                }
            }
        }
    }
}
