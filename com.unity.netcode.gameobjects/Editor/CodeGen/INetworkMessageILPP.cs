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
        private TypeReference m_MessagingSystem_MessageWithHandler_TypeRef;
        private MethodReference m_MessagingSystem_MessageHandler_Constructor_TypeRef;
        private FieldReference m_ILPPMessageProvider___network_message_types_FieldRef;
        private FieldReference m_MessagingSystem_MessageWithHandler_MessageType_FieldRef;
        private FieldReference m_MessagingSystem_MessageWithHandler_Handler_FieldRef;
        private MethodReference m_Type_GetTypeFromHandle_MethodRef;

        private MethodReference m_List_Add_MethodRef;

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            m_FastBufferReader_TypeRef = moduleDefinition.ImportReference(typeof(FastBufferReader));
            m_NetworkContext_TypeRef = moduleDefinition.ImportReference(typeof(NetworkContext));
            m_MessagingSystem_MessageHandler_Constructor_TypeRef =
                moduleDefinition.ImportReference(typeof(MessagingSystem.MessageHandler).GetConstructors()[0]);

            var messageWithHandlerType = typeof(MessagingSystem.MessageWithHandler);
            m_MessagingSystem_MessageWithHandler_TypeRef =
                moduleDefinition.ImportReference(messageWithHandlerType);
            foreach (var fieldInfo in messageWithHandlerType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case nameof(MessagingSystem.MessageWithHandler.MessageType):
                        m_MessagingSystem_MessageWithHandler_MessageType_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                    case nameof(MessagingSystem.MessageWithHandler.Handler):
                        m_MessagingSystem_MessageWithHandler_Handler_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

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

            var ilppMessageProviderType = typeof(ILPPMessageProvider);
            foreach (var fieldInfo in ilppMessageProviderType.GetFields(BindingFlags.Static | BindingFlags.NonPublic))
            {
                switch (fieldInfo.Name)
                {
                    case nameof(ILPPMessageProvider.__network_message_types):
                        m_ILPPMessageProvider___network_message_types_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var listType = typeof(List<MessagingSystem.MessageWithHandler>);
            foreach (var methodInfo in listType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case nameof(List<MessagingSystem.MessageWithHandler>.Add):
                        m_List_Add_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }


            return true;
        }

        private MethodReference GetNetworkMessageRecieveHandler(TypeDefinition typeDefinition)
        {
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
                    return method;
                }
            }

            m_Diagnostics.AddError(typeSequence, $"Class {typeDefinition.FullName} does not implement required method: `public static void Receive(FastBufferReader, in NetworkContext)`");
            return null;
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

        private void CreateInstructionsToRegisterType(ILProcessor processor, List<Instruction> instructions, TypeReference type, MethodReference receiveMethod)
        {
            // MessagingSystem.__network_message_types.Add(new MessagingSystem.MessageWithHandler{MessageType=typeof(type), Handler=type.Receive});
            processor.Body.Variables.Add(new VariableDefinition(m_MessagingSystem_MessageWithHandler_TypeRef));
            int messageWithHandlerLocIdx = processor.Body.Variables.Count - 1;

            instructions.Add(processor.Create(OpCodes.Ldsfld, m_ILPPMessageProvider___network_message_types_FieldRef));
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Initobj, m_MessagingSystem_MessageWithHandler_TypeRef));

            // tmp.MessageType = typeof(type);
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Ldtoken, type));
            instructions.Add(processor.Create(OpCodes.Call, m_Type_GetTypeFromHandle_MethodRef));
            instructions.Add(processor.Create(OpCodes.Stfld, m_MessagingSystem_MessageWithHandler_MessageType_FieldRef));

            // tmp.Handler = type.Receive
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Ldnull));

            instructions.Add(processor.Create(OpCodes.Ldftn, receiveMethod));
            instructions.Add(processor.Create(OpCodes.Newobj, m_MessagingSystem_MessageHandler_Constructor_TypeRef));
            instructions.Add(processor.Create(OpCodes.Stfld, m_MessagingSystem_MessageWithHandler_Handler_FieldRef));

            // ILPPMessageProvider.__network_message_types.Add(tmp);
            instructions.Add(processor.Create(OpCodes.Ldloc, messageWithHandlerLocIdx));
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
                        var receiveMethod = GetNetworkMessageRecieveHandler(type);
                        if (receiveMethod == null)
                        {
                            continue;
                        }
                        CreateInstructionsToRegisterType(processor, instructions, type, receiveMethod);
                    }

                    instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));
                    break;
                }
            }
        }
    }
}
