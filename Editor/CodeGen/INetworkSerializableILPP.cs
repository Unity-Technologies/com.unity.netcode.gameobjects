using System;
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
    internal sealed class INetworkSerializableILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName ||
            compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        private TypeReference ResolveGenericType(TypeReference type, List<TypeReference> typeStack)
        {
            var genericName = type.Name;
            var lastType = (GenericInstanceType)typeStack[typeStack.Count - 1];
            var resolvedType = lastType.Resolve();
            typeStack.RemoveAt(typeStack.Count - 1);
            for (var i = 0; i < resolvedType.GenericParameters.Count; ++i)
            {
                var parameter = resolvedType.GenericParameters[i];
                if (parameter.Name == genericName)
                {
                    var underlyingType = lastType.GenericArguments[i];
                    if (underlyingType.Resolve() == null)
                    {
                        return ResolveGenericType(underlyingType, typeStack);
                    }

                    return underlyingType;
                }
            }

            return null;
        }

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
                try
                {
                    var structTypes = mainModule.GetTypes()
                        .Where(t => t.Resolve().HasInterface(CodeGenHelpers.INetworkSerializeByMemcpy_FullName) && !t.Resolve().IsAbstract && !t.Resolve().HasGenericParameters && t.Resolve().IsValueType)
                        .ToList();

                    foreach (var type in structTypes)
                    {
                        // We'll avoid some confusion by ensuring users only choose one of the two
                        // serialization schemes - by method OR by memcpy, not both. We'll also do a cursory
                        // check that INetworkSerializeByMemcpy types are unmanaged.
                        if (type.HasInterface(CodeGenHelpers.INetworkSerializeByMemcpy_FullName))
                        {
                            if (type.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                            {
                                m_Diagnostics.AddError($"{nameof(INetworkSerializeByMemcpy)} types may not implement {nameof(INetworkSerializable)} - choose one or the other.");
                            }
                            if (!type.IsValueType)
                            {
                                m_Diagnostics.AddError($"{nameof(INetworkSerializeByMemcpy)} types must be unmanaged types.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    m_Diagnostics.AddError((e.ToString() + e.StackTrace).Replace("\n", "|").Replace("\r", "|"));
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
    }
}
