using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            Console.WriteLine($"check {compiledAssembly.Name} == {CodeGenHelpers.RuntimeAssemblyName};");
            return compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName;
        }

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                Console.WriteLine($"Not processing {compiledAssembly.Name}");
                return null;
            }
            Console.WriteLine("Running...");

            m_Diagnostics.Clear();

            Console.WriteLine("Diagnostics Cleared");

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly, out m_AssemblyResolver);
            if (assemblyDefinition == null)
            {

                Console.WriteLine("No Assembly Definition");
                m_Diagnostics.AddError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return null;
            }

            Console.WriteLine("Got assembly definition");

            // modules
            (m_DotnetModule, _, m_NetcodeModule) = CodeGenHelpers.FindBaseModules(assemblyDefinition, m_AssemblyResolver);

            Console.WriteLine("After FindBaseModules");

            if (m_DotnetModule == null)
            {
                Console.WriteLine("No .NET module");
                m_Diagnostics.AddError($"Cannot find .NET module: {CodeGenHelpers.DotnetModuleName}");
                return null;
            }
            Console.WriteLine("Got .NET module");

            if (m_NetcodeModule == null)
            {
                Console.WriteLine("No netcode module");
                m_Diagnostics.AddError($"Cannot find Netcode module: {CodeGenHelpers.NetcodeModuleName}");
                return null;
            }
            Console.WriteLine("Got netcode module");

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                Console.WriteLine("Got main module");
                if (ImportReferences(mainModule))
                {
                    Console.WriteLine("Imported references");
                    var types = mainModule.GetTypes()
                        .Where(t => t.Resolve().HasInterface(CodeGenHelpers.INetworkMessage_FullName) && !t.Resolve().IsAbstract)
                        .ToList();
                    // process `INetworkMessage` types
                    if (types.Count == 0)
                    {
                        Console.WriteLine("Couldn't find any messages to process.");
                        return null;
                    }

                    try
                    {
                        Console.WriteLine("Creating module initializer");
                        CreateModuleInitializer(assemblyDefinition, types);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to create module initializer!");
                        m_Diagnostics.AddError((e.ToString() + e.StackTrace).Replace("\n", "|").Replace("\r", "|"));
                    }
                }
                else
                {
                    Console.WriteLine("Could not import references!");
                    m_Diagnostics.AddError($"Cannot import references into main module: {mainModule.Name}");
                }
            }
            else
            {
                Console.WriteLine("Could not get main module!");
                m_Diagnostics.AddError($"Cannot get main module from assembly definition: {compiledAssembly.Name}");
            }
            Console.WriteLine("Created output");

            mainModule.RemoveRecursiveReferences();

            Console.WriteLine("Removed recursive references");
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
            Console.WriteLine("Wrote new assembly definition");

            return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), m_Diagnostics);
        }

        private ModuleDefinition m_DotnetModule;
        private ModuleDefinition m_NetcodeModule;
        private PostProcessorAssemblyResolver m_AssemblyResolver;

        private MethodReference m_MessagingSystem_ReceiveMessage_MethodRef;
        private TypeReference m_MessagingSystem_MessageWithHandler_TypeRef;
        private MethodReference m_MessagingSystem_MessageHandler_Constructor_TypeRef;
        private FieldReference m_ILPPMessageProvider___network_message_types_FieldRef;
        private FieldReference m_MessagingSystem_MessageWithHandler_MessageType_FieldRef;
        private FieldReference m_MessagingSystem_MessageWithHandler_Handler_FieldRef;
        private MethodReference m_Type_GetTypeFromHandle_MethodRef;
        private MethodReference m_List_Add_MethodRef;

        private const string k_ReceiveMessageName = nameof(MessagingSystem.ReceiveMessage);

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            TypeDefinition typeTypeDef = null;
            TypeDefinition listTypeDef = null;
            foreach (var dotnetTypeDef in m_DotnetModule.GetAllTypes())
            {
                if (typeTypeDef == null && dotnetTypeDef.Name == typeof(Type).Name)
                {
                    typeTypeDef = dotnetTypeDef;
                    continue;
                }

                if (listTypeDef == null && dotnetTypeDef.Name == typeof(List<>).Name)
                {
                    listTypeDef = dotnetTypeDef;
                    continue;
                }
            }

            TypeDefinition messageHandlerTypeDef = null;
            TypeDefinition messageWithHandlerTypeDef = null;
            TypeDefinition ilppMessageProviderTypeDef = null;
            TypeDefinition messagingSystemTypeDef = null;
            foreach (var netcodeTypeDef in m_NetcodeModule.GetAllTypes())
            {
                if (messageHandlerTypeDef == null && netcodeTypeDef.Name == nameof(MessagingSystem.MessageHandler))
                {
                    messageHandlerTypeDef = netcodeTypeDef;
                    continue;
                }

                if (messageWithHandlerTypeDef == null && netcodeTypeDef.Name == nameof(MessagingSystem.MessageWithHandler))
                {
                    messageWithHandlerTypeDef = netcodeTypeDef;
                    continue;
                }

                if (ilppMessageProviderTypeDef == null && netcodeTypeDef.Name == nameof(ILPPMessageProvider))
                {
                    ilppMessageProviderTypeDef = netcodeTypeDef;
                    continue;
                }

                if (messagingSystemTypeDef == null && netcodeTypeDef.Name == nameof(MessagingSystem))
                {
                    messagingSystemTypeDef = netcodeTypeDef;
                    continue;
                }
            }

            m_MessagingSystem_MessageHandler_Constructor_TypeRef = moduleDefinition.ImportReference(messageHandlerTypeDef.GetConstructors().First());

            m_MessagingSystem_MessageWithHandler_TypeRef = moduleDefinition.ImportReference(messageWithHandlerTypeDef);
            foreach (var fieldDef in messageWithHandlerTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case nameof(MessagingSystem.MessageWithHandler.MessageType):
                        m_MessagingSystem_MessageWithHandler_MessageType_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                    case nameof(MessagingSystem.MessageWithHandler.Handler):
                        m_MessagingSystem_MessageWithHandler_Handler_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                }
            }

            foreach (var methodDef in typeTypeDef.Methods)
            {
                switch (methodDef.Name)
                {
                    case nameof(Type.GetTypeFromHandle):
                        m_Type_GetTypeFromHandle_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                }
            }

            foreach (var fieldDef in ilppMessageProviderTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case nameof(ILPPMessageProvider.__network_message_types):
                        m_ILPPMessageProvider___network_message_types_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                }
            }

            foreach (var methodDef in listTypeDef.Methods)
            {
                switch (methodDef.Name)
                {
                    case "Add":
                        m_List_Add_MethodRef = methodDef;
                        m_List_Add_MethodRef.DeclaringType = listTypeDef.MakeGenericInstanceType(messageWithHandlerTypeDef);
                        m_List_Add_MethodRef = moduleDefinition.ImportReference(m_List_Add_MethodRef);
                        break;
                }
            }

            foreach (var methodDef in messagingSystemTypeDef.Methods)
            {
                switch (methodDef.Name)
                {
                    case k_ReceiveMessageName:
                        m_MessagingSystem_ReceiveMessage_MethodRef = moduleDefinition.ImportReference(methodDef);
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

        // Creates a static module constructor (which is executed when the module is loaded) that registers all the message types in the assembly with MessagingSystem.
        // This is the same behavior as annotating a static method with [ModuleInitializer] in standardized C# (that attribute doesn't exist in Unity, but the static module constructor still works).
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
                        Console.WriteLine($"Creating initializer for {type}");
                        var receiveMethod = new GenericInstanceMethod(m_MessagingSystem_ReceiveMessage_MethodRef);
                        receiveMethod.GenericArguments.Add(type);
                        CreateInstructionsToRegisterType(processor, instructions, type, receiveMethod);
                    }

                    instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));
                    break;
                }
            }
        }
    }
}
