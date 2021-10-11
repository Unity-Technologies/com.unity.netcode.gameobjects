using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Unity.Netcode.Editor.CodeGen
{

    internal sealed class INetworkMessageILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName ||
            compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

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
                    var types = mainModule.GetTypes()
                        .Where(t => t.Resolve().HasInterface(CodeGenHelpers.INetworkMessage_FullName) && !t.Resolve().IsAbstract)
                        .ToList();
                    // process `INetworkMessage` types
                    if (types.Count == 0)
                    {
                        return null;
                    }

                    try
                    {
                        types.ForEach(b => ProcessINetworkMessage(b));
                        CreateModuleInitializer(assemblyDefinition, types);
                    }
                    catch (Exception e)
                    {
                        m_Diagnostics.AddError((e.ToString() + e.StackTrace.ToString()).Replace("\n", "|").Replace("\r", "|"));
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


        private TypeReference m_FastBufferReader_TypeRef;
        private TypeReference m_NetworkContext_TypeRef;
        private FieldReference m_MessagingSystem___network_message_types_FieldRef;
        private MethodReference m_Type_GetTypeFromHandle_MethodRef;

        private MethodReference m_List_Add_MethodRef;

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            m_FastBufferReader_TypeRef = moduleDefinition.ImportReference(typeof(FastBufferReader));
            m_NetworkContext_TypeRef = moduleDefinition.ImportReference(typeof(NetworkContext));

            var typeType = typeof(Type);
            foreach (var methodInfo in typeType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case nameof(Type.GetTypeFromHandle):
                        m_Type_GetTypeFromHandle_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }

            var messagingSystemType = typeof(MessagingSystem);
            foreach (var fieldInfo in messagingSystemType.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                switch (fieldInfo.Name)
                {
                    case nameof(MessagingSystem.__network_message_types):
                        m_MessagingSystem___network_message_types_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var listType = typeof(List<Type>);
            foreach (var methodInfo in listType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case nameof(List<Type>.Add):
                        m_List_Add_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }


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

        private void CreateInstructionsToRegisterType(ILProcessor processor, List<Instruction> instructions, TypeReference type)
        {
            // MessagingSystem.__network_message_types.Add(typeof(type));
            instructions.Add(processor.Create(OpCodes.Ldsfld, m_MessagingSystem___network_message_types_FieldRef));
            instructions.Add(processor.Create(OpCodes.Ldtoken, type));
            instructions.Add(processor.Create(OpCodes.Call, m_Type_GetTypeFromHandle_MethodRef));
            instructions.Add(processor.Create(OpCodes.Callvirt, m_List_Add_MethodRef));
        }

        // Creates a static module constructor (which is executed when the module is loaded) that registers all the
        // message types in the assembly with MessagingSystem.
        // This is the same behavior as annotating a static method with [ModuleInitializer] in standardized
        // C# (that attribute doesn't exist in Unity, but the static module constructor still works)
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.moduleinitializerattribute?view=net-5.0
        // https://web.archive.org/web/20100212140402/http://blogs.msdn.com/junfeng/archive/2005/11/19/494914.aspx
        private void CreateModuleInitializer(AssemblyDefinition assembly, List<TypeDefinition> networkMessageTypes)
        {
            foreach (var typeDefinition in assembly.MainModule.Types)
            {
                if (typeDefinition.FullName == "<Module>")
                {
                    var staticCtorMethodDef = GetOrCreateStaticConstructor(typeDefinition);

                    var processor = staticCtorMethodDef.Body.GetILProcessor();

                    var instructions = new List<Instruction>();

                    foreach (var type in networkMessageTypes)
                    {
                        CreateInstructionsToRegisterType(processor, instructions, type);
                    }

                    instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));
                    break;
                }
            }
        }
    }
}
