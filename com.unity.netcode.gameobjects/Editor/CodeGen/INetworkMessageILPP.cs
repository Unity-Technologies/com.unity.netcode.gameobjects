using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Unity.Netcode.Editor.CodeGen
{
    internal sealed class INetworkMessageILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName || compiledAssembly.Name == CodeGenHelpers.ComponentsAssemblyName;

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            m_Diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly, out m_AssemblyResolver);
            if (assemblyDefinition == null)
            {
                m_Diagnostics.AddError($"Cannot read assembly definition: {compiledAssembly.Name}");
                return null;
            }

            // modules
            (_, m_NetcodeModule) = CodeGenHelpers.FindBaseModules(assemblyDefinition, m_AssemblyResolver);

            if (m_NetcodeModule == null)
            {
                m_Diagnostics.AddError($"Cannot find Netcode module: {CodeGenHelpers.NetcodeModuleName}");
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
                        m_Diagnostics.AddError((e.ToString() + e.StackTrace).Replace("\n", "|").Replace("\r", "|"));
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

        private ModuleDefinition m_NetcodeModule;
        private PostProcessorAssemblyResolver m_AssemblyResolver;

        private MethodReference m_RuntimeInitializeOnLoadAttribute_Ctor;

        private MethodReference m_MessageManager_ReceiveMessage_MethodRef;
        private MethodReference m_MessageManager_CreateMessageAndGetVersion_MethodRef;
        private TypeReference m_MessageManager_MessageWithHandler_TypeRef;
        private MethodReference m_MessageManager_MessageHandler_Constructor_TypeRef;
        private MethodReference m_MessageManager_VersionGetter_Constructor_TypeRef;
        private FieldReference m_ILPPMessageProvider___network_message_types_FieldRef;
        private FieldReference m_MessageManager_MessageWithHandler_MessageType_FieldRef;
        private FieldReference m_MessageManager_MessageWithHandler_Handler_FieldRef;
        private FieldReference m_MessageManager_MessageWithHandler_GetVersion_FieldRef;
        private MethodReference m_Type_GetTypeFromHandle_MethodRef;
        private MethodReference m_List_Add_MethodRef;

        private const string k_ReceiveMessageName = nameof(NetworkMessageManager.ReceiveMessage);
        private const string k_CreateMessageAndGetVersionName = nameof(NetworkMessageManager.CreateMessageAndGetVersion);

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            // Different environments seem to have different situations...
            // Some have these definitions in netstandard.dll, some seem to have them elsewhere...
            // Since they're standard .net classes they're not going to cause the same issues as referencing other assemblies,
            // in theory, since the definitions should be standard and consistent across platforms
            // (i.e., there's no #if UNITY_EDITOR in them that could create invalid IL code)
            TypeDefinition typeTypeDef = moduleDefinition.ImportReference(typeof(Type)).Resolve();
            TypeDefinition listTypeDef = moduleDefinition.ImportReference(typeof(List<>)).Resolve();
            m_RuntimeInitializeOnLoadAttribute_Ctor = moduleDefinition.ImportReference(typeof(RuntimeInitializeOnLoadMethodAttribute).GetConstructor(new Type[] { }));

            TypeDefinition messageHandlerTypeDef = null;
            TypeDefinition versionGetterTypeDef = null;
            TypeDefinition messageWithHandlerTypeDef = null;
            TypeDefinition ilppMessageProviderTypeDef = null;
            TypeDefinition messageManagerSystemTypeDef = null;
            foreach (var netcodeTypeDef in m_NetcodeModule.GetAllTypes())
            {
                if (messageHandlerTypeDef == null && netcodeTypeDef.Name == nameof(NetworkMessageManager.MessageHandler))
                {
                    messageHandlerTypeDef = netcodeTypeDef;
                    continue;
                }

                if (versionGetterTypeDef == null && netcodeTypeDef.Name == nameof(NetworkMessageManager.VersionGetter))
                {
                    versionGetterTypeDef = netcodeTypeDef;
                    continue;
                }

                if (messageWithHandlerTypeDef == null && netcodeTypeDef.Name == nameof(NetworkMessageManager.MessageWithHandler))
                {
                    messageWithHandlerTypeDef = netcodeTypeDef;
                    continue;
                }

                if (ilppMessageProviderTypeDef == null && netcodeTypeDef.Name == nameof(ILPPMessageProvider))
                {
                    ilppMessageProviderTypeDef = netcodeTypeDef;
                    continue;
                }

                if (messageManagerSystemTypeDef == null && netcodeTypeDef.Name == nameof(NetworkMessageManager))
                {
                    messageManagerSystemTypeDef = netcodeTypeDef;
                    continue;
                }
            }

            m_MessageManager_MessageHandler_Constructor_TypeRef = moduleDefinition.ImportReference(messageHandlerTypeDef.GetConstructors().First());
            m_MessageManager_VersionGetter_Constructor_TypeRef = moduleDefinition.ImportReference(versionGetterTypeDef.GetConstructors().First());

            m_MessageManager_MessageWithHandler_TypeRef = moduleDefinition.ImportReference(messageWithHandlerTypeDef);
            foreach (var fieldDef in messageWithHandlerTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case nameof(NetworkMessageManager.MessageWithHandler.MessageType):
                        m_MessageManager_MessageWithHandler_MessageType_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                    case nameof(NetworkMessageManager.MessageWithHandler.Handler):
                        m_MessageManager_MessageWithHandler_Handler_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                    case nameof(NetworkMessageManager.MessageWithHandler.GetVersion):
                        m_MessageManager_MessageWithHandler_GetVersion_FieldRef = moduleDefinition.ImportReference(fieldDef);
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

            foreach (var methodDef in messageManagerSystemTypeDef.Methods)
            {
                switch (methodDef.Name)
                {
                    case k_ReceiveMessageName:
                        m_MessageManager_ReceiveMessage_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                    case k_CreateMessageAndGetVersionName:
                        m_MessageManager_CreateMessageAndGetVersion_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                }
            }

            return true;
        }

        private void CreateInstructionsToRegisterType(ILProcessor processor, List<Instruction> instructions, TypeReference type, MethodReference receiveMethod, MethodReference versionMethod)
        {
            // NetworkMessageManager.__network_message_types.Add(new NetworkMessageManager.MessageWithHandler{MessageType=typeof(type), Handler=type.Receive});
            processor.Body.Variables.Add(new VariableDefinition(m_MessageManager_MessageWithHandler_TypeRef));
            int messageWithHandlerLocIdx = processor.Body.Variables.Count - 1;

            instructions.Add(processor.Create(OpCodes.Ldsfld, m_ILPPMessageProvider___network_message_types_FieldRef));
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Initobj, m_MessageManager_MessageWithHandler_TypeRef));

            // tmp.MessageType = typeof(type);
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Ldtoken, type));
            instructions.Add(processor.Create(OpCodes.Call, m_Type_GetTypeFromHandle_MethodRef));
            instructions.Add(processor.Create(OpCodes.Stfld, m_MessageManager_MessageWithHandler_MessageType_FieldRef));

            // tmp.Handler = MessageHandler.ReceveMessage<type>
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Ldnull));

            instructions.Add(processor.Create(OpCodes.Ldftn, receiveMethod));
            instructions.Add(processor.Create(OpCodes.Newobj, m_MessageManager_MessageHandler_Constructor_TypeRef));
            instructions.Add(processor.Create(OpCodes.Stfld, m_MessageManager_MessageWithHandler_Handler_FieldRef));


            // tmp.GetVersion = MessageHandler.CreateMessageAndGetVersion<type>
            instructions.Add(processor.Create(OpCodes.Ldloca, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Ldnull));

            instructions.Add(processor.Create(OpCodes.Ldftn, versionMethod));
            instructions.Add(processor.Create(OpCodes.Newobj, m_MessageManager_VersionGetter_Constructor_TypeRef));
            instructions.Add(processor.Create(OpCodes.Stfld, m_MessageManager_MessageWithHandler_GetVersion_FieldRef));

            // ILPPMessageProvider.__network_message_types.Add(tmp);
            instructions.Add(processor.Create(OpCodes.Ldloc, messageWithHandlerLocIdx));
            instructions.Add(processor.Create(OpCodes.Callvirt, m_List_Add_MethodRef));
        }

        // Creates a static module constructor (which is executed when the module is loaded) that registers all the message types in the assembly with NetworkMessageManager.
        // This is the same behavior as annotating a static method with [ModuleInitializer] in standardized C# (that attribute doesn't exist in Unity, but the static module constructor still works).
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.moduleinitializerattribute?view=net-5.0
        // https://web.archive.org/web/20100212140402/http://blogs.msdn.com/junfeng/archive/2005/11/19/494914.aspx
        private void CreateModuleInitializer(AssemblyDefinition assembly, List<TypeDefinition> networkMessageTypes)
        {
            var typeDefinition = new TypeDefinition("__GEN", "INetworkMessageHelper", TypeAttributes.NotPublic | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, assembly.MainModule.TypeSystem.Object);

            var staticCtorMethodDef = new MethodDefinition(
                $"InitializeMessages",
                MethodAttributes.Assembly |
                MethodAttributes.Static,
                assembly.MainModule.TypeSystem.Void);
            staticCtorMethodDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            staticCtorMethodDef.CustomAttributes.Add(new CustomAttribute(m_RuntimeInitializeOnLoadAttribute_Ctor));
            typeDefinition.Methods.Add(staticCtorMethodDef);

            var instructions = new List<Instruction>();
            var processor = staticCtorMethodDef.Body.GetILProcessor();

            foreach (var type in networkMessageTypes)
            {
                var receiveMethod = new GenericInstanceMethod(m_MessageManager_ReceiveMessage_MethodRef);
                receiveMethod.GenericArguments.Add(type);
                var versionMethod = new GenericInstanceMethod(m_MessageManager_CreateMessageAndGetVersion_MethodRef);
                versionMethod.GenericArguments.Add(type);
                CreateInstructionsToRegisterType(processor, instructions, type, receiveMethod, versionMethod);
            }

            instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));

            assembly.MainModule.Types.Add(typeDefinition);
        }
    }
}
