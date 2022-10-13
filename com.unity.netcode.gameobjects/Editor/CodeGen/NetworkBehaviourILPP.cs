using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;

namespace Unity.Netcode.Editor.CodeGen
{
    internal sealed class NetworkBehaviourILPP : ILPPInterface
    {
        private const string k_ReadValueMethodName = nameof(FastBufferReader.ReadValueSafe);
        private const string k_WriteValueMethodName = nameof(FastBufferWriter.WriteValueSafe);

        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

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
            (m_UnityModule, m_NetcodeModule) = CodeGenHelpers.FindBaseModules(assemblyDefinition, m_AssemblyResolver);

            if (m_UnityModule == null)
            {
                m_Diagnostics.AddError($"Cannot find Unity module: {CodeGenHelpers.UnityModuleName}");
                return null;
            }

            if (m_NetcodeModule == null)
            {
                m_Diagnostics.AddError($"Cannot find Netcode module: {CodeGenHelpers.NetcodeModuleName}");
                return null;
            }

            // process
            var mainModule = assemblyDefinition.MainModule;
            if (mainModule != null)
            {
                m_MainModule = mainModule;

                if (ImportReferences(mainModule))
                {
                    // process `NetworkBehaviour` types
                    try
                    {
                        mainModule.GetTypes()
                            .Where(t => t.IsSubclassOf(CodeGenHelpers.NetworkBehaviour_FullName))
                            .ToList()
                            .ForEach(b => ProcessNetworkBehaviour(b, compiledAssembly.Defines));

                        CreateNetworkVariableTypeInitializers(assemblyDefinition);
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

        private bool IsMemcpyableType(TypeReference type)
        {
            foreach (var supportedType in BaseSupportedTypes)
            {
                if (type.FullName == supportedType.FullName)
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateNetworkVariableTypeInitializers(AssemblyDefinition assembly)
        {
            foreach (var typeDefinition in assembly.MainModule.Types)
            {
                if (typeDefinition.FullName == "<Module>")
                {
                    var staticCtorMethodDef = GetOrCreateStaticConstructor(typeDefinition);

                    var processor = staticCtorMethodDef.Body.GetILProcessor();

                    var instructions = new List<Instruction>();

                    foreach (var type in m_WrappedNetworkVariableTypes)
                    {
                        // If a serializable type isn't found, FallbackSerializer will be used automatically, which will
                        // call into UserNetworkVariableSerialization, giving the user a chance to define their own serializaiton
                        // for types that aren't in our official supported types list.
                        GenericInstanceMethod serializeMethod = null;
                        GenericInstanceMethod equalityMethod;

                        if (type.IsValueType)
                        {
                            if (type.HasInterface(typeof(INetworkSerializeByMemcpy).FullName) || type.Resolve().IsEnum || IsMemcpyableType(type))
                            {
                                serializeMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeSerializer_UnmanagedByMemcpy_MethodRef);
                            }
                            else if (type.HasInterface(typeof(INetworkSerializable).FullName))
                            {
                                serializeMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeSerializer_UnmanagedINetworkSerializable_MethodRef);
                            }
                            else if (type.HasInterface(CodeGenHelpers.IUTF8Bytes_FullName) && type.HasInterface(k_INativeListBool_FullName))
                            {
                                serializeMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeSerializer_FixedString_MethodRef);
                            }

                            if (type.HasInterface(typeof(IEquatable<>).FullName + "<" + type.FullName + ">"))
                            {
                                equalityMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeEqualityChecker_UnmanagedIEquatable_MethodRef);
                            }
                            else
                            {
                                equalityMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeEqualityChecker_UnmanagedValueEquals_MethodRef);
                            }
                        }
                        else
                        {
                            if (type.HasInterface(typeof(INetworkSerializable).FullName))
                            {
                                serializeMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeSerializer_ManagedINetworkSerializable_MethodRef);
                            }

                            if (type.HasInterface(typeof(IEquatable<>).FullName + "<" + type.FullName + ">"))
                            {
                                equalityMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeEqualityChecker_ManagedIEquatable_MethodRef);
                            }
                            else
                            {
                                equalityMethod = new GenericInstanceMethod(m_NetworkVariableSerializationTypes_InitializeEqualityChecker_ManagedClassEquals_MethodRef);
                            }
                        }

                        if (serializeMethod != null)
                        {
                            serializeMethod.GenericArguments.Add(type);
                            instructions.Add(processor.Create(OpCodes.Call, m_MainModule.ImportReference(serializeMethod)));
                        }
                        equalityMethod.GenericArguments.Add(type);
                        instructions.Add(processor.Create(OpCodes.Call, m_MainModule.ImportReference(equalityMethod)));
                    }

                    instructions.ForEach(instruction => processor.Body.Instructions.Insert(processor.Body.Instructions.Count - 1, instruction));
                    break;
                }
            }
        }

        private ModuleDefinition m_MainModule;
        private ModuleDefinition m_UnityModule;
        private ModuleDefinition m_NetcodeModule;
        private PostProcessorAssemblyResolver m_AssemblyResolver;

        private MethodReference m_Debug_LogError_MethodRef;
        private TypeReference m_NetworkManager_TypeRef;
        private MethodReference m_NetworkManager_getLocalClientId_MethodRef;
        private MethodReference m_NetworkManager_getIsListening_MethodRef;
        private MethodReference m_NetworkManager_getIsHost_MethodRef;
        private MethodReference m_NetworkManager_getIsServer_MethodRef;
        private MethodReference m_NetworkManager_getIsClient_MethodRef;
        private FieldReference m_NetworkManager_LogLevel_FieldRef;
        private FieldReference m_NetworkManager_rpc_func_table_FieldRef;
        private MethodReference m_NetworkManager_rpc_func_table_Add_MethodRef;
        private FieldReference m_NetworkManager_rpc_name_table_FieldRef;
        private MethodReference m_NetworkManager_rpc_name_table_Add_MethodRef;
        private TypeReference m_NetworkBehaviour_TypeRef;
        private MethodReference m_NetworkBehaviour_beginSendServerRpc_MethodRef;
        private MethodReference m_NetworkBehaviour_endSendServerRpc_MethodRef;
        private MethodReference m_NetworkBehaviour_beginSendClientRpc_MethodRef;
        private MethodReference m_NetworkBehaviour_endSendClientRpc_MethodRef;
        private FieldReference m_NetworkBehaviour_rpc_exec_stage_FieldRef;
        private MethodReference m_NetworkBehaviour_getNetworkManager_MethodRef;
        private MethodReference m_NetworkBehaviour_getOwnerClientId_MethodRef;
        private MethodReference m_NetworkHandlerDelegateCtor_MethodRef;
        private TypeReference m_RpcParams_TypeRef;
        private FieldReference m_RpcParams_Server_FieldRef;
        private FieldReference m_RpcParams_Client_FieldRef;
        private TypeReference m_ServerRpcParams_TypeRef;
        private FieldReference m_ServerRpcParams_Receive_FieldRef;
        private FieldReference m_ServerRpcParams_Receive_SenderClientId_FieldRef;
        private TypeReference m_ClientRpcParams_TypeRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeSerializer_UnmanagedByMemcpy_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeSerializer_UnmanagedINetworkSerializable_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeSerializer_ManagedINetworkSerializable_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeSerializer_FixedString_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeEqualityChecker_ManagedIEquatable_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeEqualityChecker_UnmanagedIEquatable_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeEqualityChecker_UnmanagedValueEquals_MethodRef;
        private MethodReference m_NetworkVariableSerializationTypes_InitializeEqualityChecker_ManagedClassEquals_MethodRef;

        private TypeReference m_FastBufferWriter_TypeRef;
        private readonly Dictionary<string, MethodReference> m_FastBufferWriter_WriteValue_MethodRefs = new Dictionary<string, MethodReference>();
        private readonly List<MethodReference> m_FastBufferWriter_ExtensionMethodRefs = new List<MethodReference>();

        private TypeReference m_FastBufferReader_TypeRef;
        private readonly Dictionary<string, MethodReference> m_FastBufferReader_ReadValue_MethodRefs = new Dictionary<string, MethodReference>();
        private readonly List<MethodReference> m_FastBufferReader_ExtensionMethodRefs = new List<MethodReference>();

        private HashSet<TypeReference> m_WrappedNetworkVariableTypes = new HashSet<TypeReference>();

        internal static readonly Type[] BaseSupportedTypes = new[]
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(Color32),
            typeof(Ray),
            typeof(Ray2D)
        };

        private const string k_Debug_LogError = nameof(Debug.LogError);
        private const string k_NetworkManager_LocalClientId = nameof(NetworkManager.LocalClientId);
        private const string k_NetworkManager_IsListening = nameof(NetworkManager.IsListening);
        private const string k_NetworkManager_IsHost = nameof(NetworkManager.IsHost);
        private const string k_NetworkManager_IsServer = nameof(NetworkManager.IsServer);
        private const string k_NetworkManager_IsClient = nameof(NetworkManager.IsClient);
        private const string k_NetworkManager_LogLevel = nameof(NetworkManager.LogLevel);
        private const string k_NetworkManager_rpc_func_table = nameof(NetworkManager.__rpc_func_table);
        private const string k_NetworkManager_rpc_name_table = nameof(NetworkManager.__rpc_name_table);

        private const string k_NetworkBehaviour_rpc_exec_stage = nameof(NetworkBehaviour.__rpc_exec_stage);
        private const string k_NetworkBehaviour_beginSendServerRpc = nameof(NetworkBehaviour.__beginSendServerRpc);
        private const string k_NetworkBehaviour_endSendServerRpc = nameof(NetworkBehaviour.__endSendServerRpc);
        private const string k_NetworkBehaviour_beginSendClientRpc = nameof(NetworkBehaviour.__beginSendClientRpc);
        private const string k_NetworkBehaviour_endSendClientRpc = nameof(NetworkBehaviour.__endSendClientRpc);
        private const string k_NetworkBehaviour_NetworkManager = nameof(NetworkBehaviour.NetworkManager);
        private const string k_NetworkBehaviour_OwnerClientId = nameof(NetworkBehaviour.OwnerClientId);

        private const string k_RpcAttribute_Delivery = nameof(RpcAttribute.Delivery);
        private const string k_ServerRpcAttribute_RequireOwnership = nameof(ServerRpcAttribute.RequireOwnership);
        private const string k_RpcParams_Server = nameof(__RpcParams.Server);
        private const string k_RpcParams_Client = nameof(__RpcParams.Client);
        private const string k_ServerRpcParams_Receive = nameof(ServerRpcParams.Receive);
        private const string k_ServerRpcReceiveParams_SenderClientId = nameof(ServerRpcReceiveParams.SenderClientId);

        // CodeGen cannot reference the collections assembly to do a typeof() on it due to a bug that causes that to crash.
        private const string k_INativeListBool_FullName = "Unity.Collections.INativeList`1<System.Byte>";

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            TypeDefinition debugTypeDef = null;
            foreach (var unityTypeDef in m_UnityModule.GetAllTypes())
            {
                if (debugTypeDef == null && unityTypeDef.FullName == typeof(Debug).FullName)
                {
                    debugTypeDef = unityTypeDef;
                    continue;
                }
            }

            TypeDefinition networkManagerTypeDef = null;
            TypeDefinition networkBehaviourTypeDef = null;
            TypeDefinition networkHandlerDelegateTypeDef = null;
            TypeDefinition rpcParamsTypeDef = null;
            TypeDefinition serverRpcParamsTypeDef = null;
            TypeDefinition clientRpcParamsTypeDef = null;
            TypeDefinition fastBufferWriterTypeDef = null;
            TypeDefinition fastBufferReaderTypeDef = null;
            TypeDefinition networkVariableSerializationTypesTypeDef = null;
            foreach (var netcodeTypeDef in m_NetcodeModule.GetAllTypes())
            {
                if (networkManagerTypeDef == null && netcodeTypeDef.Name == nameof(NetworkManager))
                {
                    networkManagerTypeDef = netcodeTypeDef;
                    continue;
                }

                if (networkBehaviourTypeDef == null && netcodeTypeDef.Name == nameof(NetworkBehaviour))
                {
                    networkBehaviourTypeDef = netcodeTypeDef;
                    continue;
                }

                if (networkHandlerDelegateTypeDef == null && netcodeTypeDef.Name == nameof(NetworkManager.RpcReceiveHandler))
                {
                    networkHandlerDelegateTypeDef = netcodeTypeDef;
                    continue;
                }

                if (rpcParamsTypeDef == null && netcodeTypeDef.Name == nameof(__RpcParams))
                {
                    rpcParamsTypeDef = netcodeTypeDef;
                    continue;
                }

                if (serverRpcParamsTypeDef == null && netcodeTypeDef.Name == nameof(ServerRpcParams))
                {
                    serverRpcParamsTypeDef = netcodeTypeDef;
                    continue;
                }

                if (clientRpcParamsTypeDef == null && netcodeTypeDef.Name == nameof(ClientRpcParams))
                {
                    clientRpcParamsTypeDef = netcodeTypeDef;
                    continue;
                }

                if (fastBufferWriterTypeDef == null && netcodeTypeDef.Name == nameof(FastBufferWriter))
                {
                    fastBufferWriterTypeDef = netcodeTypeDef;
                    continue;
                }

                if (fastBufferReaderTypeDef == null && netcodeTypeDef.Name == nameof(FastBufferReader))
                {
                    fastBufferReaderTypeDef = netcodeTypeDef;
                    continue;
                }

                if (networkVariableSerializationTypesTypeDef == null && netcodeTypeDef.Name == nameof(NetworkVariableSerializationTypes))
                {
                    networkVariableSerializationTypesTypeDef = netcodeTypeDef;
                    continue;
                }
            }

            foreach (var methodDef in debugTypeDef.Methods)
            {
                switch (methodDef.Name)
                {
                    case k_Debug_LogError:
                        if (methodDef.Parameters.Count == 1)
                        {
                            m_Debug_LogError_MethodRef = moduleDefinition.ImportReference(methodDef);
                        }

                        break;
                }
            }

            m_NetworkManager_TypeRef = moduleDefinition.ImportReference(networkManagerTypeDef);
            foreach (var propertyDef in networkManagerTypeDef.Properties)
            {
                switch (propertyDef.Name)
                {
                    case k_NetworkManager_LocalClientId:
                        m_NetworkManager_getLocalClientId_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                    case k_NetworkManager_IsListening:
                        m_NetworkManager_getIsListening_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                    case k_NetworkManager_IsHost:
                        m_NetworkManager_getIsHost_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                    case k_NetworkManager_IsServer:
                        m_NetworkManager_getIsServer_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                    case k_NetworkManager_IsClient:
                        m_NetworkManager_getIsClient_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                }
            }

            foreach (var fieldDef in networkManagerTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case k_NetworkManager_LogLevel:
                        m_NetworkManager_LogLevel_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                    case k_NetworkManager_rpc_func_table:
                        m_NetworkManager_rpc_func_table_FieldRef = moduleDefinition.ImportReference(fieldDef);

                        m_NetworkManager_rpc_func_table_Add_MethodRef = fieldDef.FieldType.Resolve().Methods.First(m => m.Name == "Add");
                        m_NetworkManager_rpc_func_table_Add_MethodRef.DeclaringType = fieldDef.FieldType;
                        m_NetworkManager_rpc_func_table_Add_MethodRef = moduleDefinition.ImportReference(m_NetworkManager_rpc_func_table_Add_MethodRef);
                        break;
                    case k_NetworkManager_rpc_name_table:
                        m_NetworkManager_rpc_name_table_FieldRef = moduleDefinition.ImportReference(fieldDef);

                        m_NetworkManager_rpc_name_table_Add_MethodRef = fieldDef.FieldType.Resolve().Methods.First(m => m.Name == "Add");
                        m_NetworkManager_rpc_name_table_Add_MethodRef.DeclaringType = fieldDef.FieldType;
                        m_NetworkManager_rpc_name_table_Add_MethodRef = moduleDefinition.ImportReference(m_NetworkManager_rpc_name_table_Add_MethodRef);
                        break;
                }
            }

            m_NetworkBehaviour_TypeRef = moduleDefinition.ImportReference(networkBehaviourTypeDef);
            foreach (var propertyDef in networkBehaviourTypeDef.Properties)
            {
                switch (propertyDef.Name)
                {
                    case k_NetworkBehaviour_NetworkManager:
                        m_NetworkBehaviour_getNetworkManager_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                    case k_NetworkBehaviour_OwnerClientId:
                        m_NetworkBehaviour_getOwnerClientId_MethodRef = moduleDefinition.ImportReference(propertyDef.GetMethod);
                        break;
                }
            }

            foreach (var methodDef in networkBehaviourTypeDef.Methods)
            {
                switch (methodDef.Name)
                {
                    case k_NetworkBehaviour_beginSendServerRpc:
                        m_NetworkBehaviour_beginSendServerRpc_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                    case k_NetworkBehaviour_endSendServerRpc:
                        m_NetworkBehaviour_endSendServerRpc_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                    case k_NetworkBehaviour_beginSendClientRpc:
                        m_NetworkBehaviour_beginSendClientRpc_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                    case k_NetworkBehaviour_endSendClientRpc:
                        m_NetworkBehaviour_endSendClientRpc_MethodRef = moduleDefinition.ImportReference(methodDef);
                        break;
                }
            }

            foreach (var fieldDef in networkBehaviourTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case k_NetworkBehaviour_rpc_exec_stage:
                        m_NetworkBehaviour_rpc_exec_stage_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                }
            }

            foreach (var ctor in networkHandlerDelegateTypeDef.Resolve().GetConstructors())
            {
                if (ctor.HasParameters &&
                    ctor.Parameters.Count == 2 &&
                    ctor.Parameters[0].ParameterType.Name == nameof(System.Object) &&
                    ctor.Parameters[1].ParameterType.Name == nameof(IntPtr))
                {
                    m_NetworkHandlerDelegateCtor_MethodRef = moduleDefinition.ImportReference(ctor);
                    break;
                }
            }

            m_RpcParams_TypeRef = moduleDefinition.ImportReference(rpcParamsTypeDef);
            foreach (var fieldDef in rpcParamsTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case k_RpcParams_Server:
                        m_RpcParams_Server_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                    case k_RpcParams_Client:
                        m_RpcParams_Client_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                }
            }

            m_ServerRpcParams_TypeRef = moduleDefinition.ImportReference(serverRpcParamsTypeDef);
            foreach (var fieldDef in serverRpcParamsTypeDef.Fields)
            {
                switch (fieldDef.Name)
                {
                    case k_ServerRpcParams_Receive:
                        foreach (var recvFieldDef in fieldDef.FieldType.Resolve().Fields)
                        {
                            switch (recvFieldDef.Name)
                            {
                                case k_ServerRpcReceiveParams_SenderClientId:
                                    m_ServerRpcParams_Receive_SenderClientId_FieldRef = moduleDefinition.ImportReference(recvFieldDef);
                                    break;
                            }
                        }

                        m_ServerRpcParams_Receive_FieldRef = moduleDefinition.ImportReference(fieldDef);
                        break;
                }
            }

            m_ClientRpcParams_TypeRef = moduleDefinition.ImportReference(clientRpcParamsTypeDef);
            m_FastBufferWriter_TypeRef = moduleDefinition.ImportReference(fastBufferWriterTypeDef);
            m_FastBufferReader_TypeRef = moduleDefinition.ImportReference(fastBufferReaderTypeDef);

            // Find all extension methods for FastBufferReader and FastBufferWriter to enable user-implemented methods to be called
            var assemblies = new List<AssemblyDefinition> { m_MainModule.Assembly };
            foreach (var reference in m_MainModule.AssemblyReferences)
            {
                var assembly = m_AssemblyResolver.Resolve(reference);
                if (assembly != null)
                {
                    assemblies.Add(assembly);
                }
            }

            var extensionConstructor = moduleDefinition.ImportReference(typeof(ExtensionAttribute).GetConstructor(new Type[] { }));
            foreach (var assembly in assemblies)
            {
                foreach (var module in assembly.Modules)
                {
                    foreach (var type in module.Types)
                    {
                        var resolvedType = type.Resolve();
                        if (!resolvedType.IsSealed || !resolvedType.IsAbstract || resolvedType.IsNested)
                        {
                            continue;
                        }

                        foreach (var method in type.Methods)
                        {
                            if (!method.IsStatic)
                            {
                                continue;
                            }

                            var isExtension = false;

                            foreach (var attr in method.CustomAttributes)
                            {
                                if (attr.Constructor.Resolve() == extensionConstructor.Resolve())
                                {
                                    isExtension = true;
                                }
                            }

                            if (!isExtension)
                            {
                                continue;
                            }

                            var parameters = method.Parameters;

                            if (parameters.Count == 2 && parameters[0].ParameterType.Resolve() == m_FastBufferWriter_TypeRef.MakeByReferenceType().Resolve())
                            {
                                m_FastBufferWriter_ExtensionMethodRefs.Add(m_MainModule.ImportReference(method));
                            }
                            else if (parameters.Count == 2 && parameters[0].ParameterType.Resolve() == m_FastBufferReader_TypeRef.MakeByReferenceType().Resolve())
                            {
                                m_FastBufferReader_ExtensionMethodRefs.Add(m_MainModule.ImportReference(method));
                            }
                        }
                    }
                }
            }

            foreach (var method in networkVariableSerializationTypesTypeDef.Methods)
            {
                if (!method.IsStatic)
                {
                    continue;
                }

                switch (method.Name)
                {
                    case nameof(NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedByMemcpy):
                        m_NetworkVariableSerializationTypes_InitializeSerializer_UnmanagedByMemcpy_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeSerializer_UnmanagedINetworkSerializable):
                        m_NetworkVariableSerializationTypes_InitializeSerializer_UnmanagedINetworkSerializable_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeSerializer_ManagedINetworkSerializable):
                        m_NetworkVariableSerializationTypes_InitializeSerializer_ManagedINetworkSerializable_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeSerializer_FixedString):
                        m_NetworkVariableSerializationTypes_InitializeSerializer_FixedString_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeEqualityChecker_ManagedIEquatable):
                        m_NetworkVariableSerializationTypes_InitializeEqualityChecker_ManagedIEquatable_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedIEquatable):
                        m_NetworkVariableSerializationTypes_InitializeEqualityChecker_UnmanagedIEquatable_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeEqualityChecker_UnmanagedValueEquals):
                        m_NetworkVariableSerializationTypes_InitializeEqualityChecker_UnmanagedValueEquals_MethodRef = method;
                        break;
                    case nameof(NetworkVariableSerializationTypes.InitializeEqualityChecker_ManagedClassEquals):
                        m_NetworkVariableSerializationTypes_InitializeEqualityChecker_ManagedClassEquals_MethodRef = method;
                        break;
                }
            }

            return true;
        }

        // This gets all fields from this type as well as any parent types, up to (but not including) the base NetworkBehaviour class
        // Importantly... this also resolves any generics, so if the base class is Foo<T> and contains a field of NetworkVariable<T>,
        // and this class is Bar : Foo<int>, it will properly resolve NetworkVariable<T> to NetworkVariable<int>.
        private void GetAllFieldsAndResolveGenerics(TypeDefinition type, ref List<TypeReference> fieldTypes, Dictionary<string, TypeReference> genericParameters = null)
        {
            foreach (var field in type.Fields)
            {
                if (field.FieldType.IsGenericInstance)
                {
                    var genericType = (GenericInstanceType)field.FieldType;
                    var newGenericType = new GenericInstanceType(field.FieldType.Resolve());
                    for (var i = 0; i < genericType.GenericArguments.Count; ++i)
                    {
                        var argument = genericType.GenericArguments[i];

                        if (genericParameters != null && genericParameters.ContainsKey(argument.Name))
                        {
                            newGenericType.GenericArguments.Add(genericParameters[argument.Name]);
                        }
                        else
                        {
                            newGenericType.GenericArguments.Add(argument);
                        }
                    }
                    fieldTypes.Add(newGenericType);
                }
                else
                {
                    fieldTypes.Add(field.FieldType);
                }
            }

            if (type.BaseType == null || type.BaseType.Name == nameof(NetworkBehaviour))
            {
                return;
            }
            var genericParams = new Dictionary<string, TypeReference>();
            var resolved = type.BaseType.Resolve();
            if (type.BaseType.IsGenericInstance)
            {
                var genericType = (GenericInstanceType)type.BaseType;
                for (var i = 0; i < genericType.GenericArguments.Count; ++i)
                {
                    genericParams[resolved.GenericParameters[i].Name] = genericType.GenericArguments[i];
                }
            }
            GetAllFieldsAndResolveGenerics(resolved, ref fieldTypes, genericParams);
        }

        private void ProcessNetworkBehaviour(TypeDefinition typeDefinition, string[] assemblyDefines)
        {
            var rpcHandlers = new List<(uint RpcMethodId, MethodDefinition RpcHandler)>();
            var rpcNames = new List<(uint RpcMethodId, string RpcMethodName)>();

            bool isEditorOrDevelopment = assemblyDefines.Contains("UNITY_EDITOR") || assemblyDefines.Contains("DEVELOPMENT_BUILD");

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                var rpcAttribute = CheckAndGetRpcAttribute(methodDefinition);
                if (rpcAttribute == null)
                {
                    continue;
                }

                var rpcMethodId = methodDefinition.Hash();
                if (rpcMethodId == 0)
                {
                    continue;
                }

                if (methodDefinition.HasCustomAttributes)
                {
                    foreach (var attribute in methodDefinition.CustomAttributes)
                    {
                        if (attribute.AttributeType.Name == nameof(AsyncStateMachineAttribute))
                        {
                            m_Diagnostics.AddError(methodDefinition, $"{methodDefinition.FullName}: RPCs cannot be 'async'");
                        }
                    }
                }

                InjectWriteAndCallBlocks(methodDefinition, rpcAttribute, rpcMethodId);

                rpcHandlers.Add((rpcMethodId, GenerateStaticHandler(methodDefinition, rpcAttribute, rpcMethodId)));

                if (isEditorOrDevelopment)
                {
                    rpcNames.Add((rpcMethodId, methodDefinition.Name));
                }
            }

            if (!typeDefinition.HasGenericParameters && !typeDefinition.IsGenericInstance)
            {
                var fieldTypes = new List<TypeReference>();
                GetAllFieldsAndResolveGenerics(typeDefinition, ref fieldTypes);
                foreach (var type in fieldTypes)
                {
                    //var type = field.FieldType;
                    if (type.IsGenericInstance)
                    {
                        if (type.Resolve().Name == typeof(NetworkVariable<>).Name || type.Resolve().Name == typeof(NetworkList<>).Name)
                        {
                            var genericInstanceType = (GenericInstanceType)type;
                            var wrappedType = genericInstanceType.GenericArguments[0];
                            if (!m_WrappedNetworkVariableTypes.Contains(wrappedType))
                            {
                                m_WrappedNetworkVariableTypes.Add(wrappedType);
                            }
                        }
                    }
                }
            }

            if (rpcHandlers.Count > 0 || rpcNames.Count > 0)
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

                var instructions = new List<Instruction>();
                var processor = staticCtorMethodDef.Body.GetILProcessor();

                foreach (var (rpcMethodId, rpcHandler) in rpcHandlers)
                {
                    typeDefinition.Methods.Add(rpcHandler);

                    // NetworkManager.__rpc_func_table.Add(RpcMethodId, HandleFunc);
                    instructions.Add(processor.Create(OpCodes.Ldsfld, m_NetworkManager_rpc_func_table_FieldRef));
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)rpcMethodId)));
                    instructions.Add(processor.Create(OpCodes.Ldnull));
                    instructions.Add(processor.Create(OpCodes.Ldftn, rpcHandler));
                    instructions.Add(processor.Create(OpCodes.Newobj, m_NetworkHandlerDelegateCtor_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Call, m_NetworkManager_rpc_func_table_Add_MethodRef));
                }

                foreach (var (rpcMethodId, rpcMethodName) in rpcNames)
                {
                    // NetworkManager.__rpc_name_table.Add(RpcMethodId, RpcMethodName);
                    instructions.Add(processor.Create(OpCodes.Ldsfld, m_NetworkManager_rpc_name_table_FieldRef));
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)rpcMethodId)));
                    instructions.Add(processor.Create(OpCodes.Ldstr, rpcMethodName));
                    instructions.Add(processor.Create(OpCodes.Call, m_NetworkManager_rpc_name_table_Add_MethodRef));
                }

                instructions.Reverse();
                instructions.ForEach(instruction => processor.Body.Instructions.Insert(0, instruction));
            }

            // override NetworkBehaviour.__getTypeName() method to return concrete type
            {
                var networkBehaviour_TypeDef = m_NetworkBehaviour_TypeRef.Resolve();
                var baseGetTypeNameMethod = networkBehaviour_TypeDef.Methods.First(p => p.Name.Equals(nameof(NetworkBehaviour.__getTypeName)));

                var newGetTypeNameMethod = new MethodDefinition(
                    nameof(NetworkBehaviour.__getTypeName),
                    (baseGetTypeNameMethod.Attributes & ~MethodAttributes.NewSlot) | MethodAttributes.ReuseSlot,
                    baseGetTypeNameMethod.ReturnType)
                {
                    ImplAttributes = baseGetTypeNameMethod.ImplAttributes,
                    SemanticsAttributes = baseGetTypeNameMethod.SemanticsAttributes
                };

                var processor = newGetTypeNameMethod.Body.GetILProcessor();
                processor.Body.Instructions.Add(processor.Create(OpCodes.Ldstr, typeDefinition.Name));
                processor.Body.Instructions.Add(processor.Create(OpCodes.Ret));

                typeDefinition.Methods.Add(newGetTypeNameMethod);
            }

            m_MainModule.RemoveRecursiveReferences();
        }

        private CustomAttribute CheckAndGetRpcAttribute(MethodDefinition methodDefinition)
        {
            CustomAttribute rpcAttribute = null;
            foreach (var customAttribute in methodDefinition.CustomAttributes)
            {
                var customAttributeType_FullName = customAttribute.AttributeType.FullName;

                if (customAttributeType_FullName == CodeGenHelpers.ServerRpcAttribute_FullName ||
                    customAttributeType_FullName == CodeGenHelpers.ClientRpcAttribute_FullName)
                {
                    bool isValid = true;

                    if (methodDefinition.IsStatic)
                    {
                        m_Diagnostics.AddError(methodDefinition, "RPC method must not be static!");
                        isValid = false;
                    }

                    if (methodDefinition.IsAbstract)
                    {
                        m_Diagnostics.AddError(methodDefinition, "RPC method must not be abstract!");
                        isValid = false;
                    }

                    if (methodDefinition.HasGenericParameters)
                    {
                        m_Diagnostics.AddError(methodDefinition, "RPC method must not be generic!");
                        isValid = false;
                    }

                    if (methodDefinition.ReturnType != methodDefinition.Module.TypeSystem.Void)
                    {
                        m_Diagnostics.AddError(methodDefinition, "RPC method must return `void`!");
                        isValid = false;
                    }

                    if (customAttributeType_FullName == CodeGenHelpers.ServerRpcAttribute_FullName &&
                        !methodDefinition.Name.EndsWith("ServerRpc", StringComparison.OrdinalIgnoreCase))
                    {
                        m_Diagnostics.AddError(methodDefinition, "ServerRpc method must end with 'ServerRpc' suffix!");
                        isValid = false;
                    }

                    if (customAttributeType_FullName == CodeGenHelpers.ClientRpcAttribute_FullName &&
                        !methodDefinition.Name.EndsWith("ClientRpc", StringComparison.OrdinalIgnoreCase))
                    {
                        m_Diagnostics.AddError(methodDefinition, "ClientRpc method must end with 'ClientRpc' suffix!");
                        isValid = false;
                    }

                    if (isValid)
                    {
                        rpcAttribute = customAttribute;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            if (rpcAttribute == null)
            {
                if (methodDefinition.Name.EndsWith("ServerRpc", StringComparison.OrdinalIgnoreCase))
                {
                    m_Diagnostics.AddError(methodDefinition, "ServerRpc method must be marked with 'ServerRpc' attribute!");
                }
                else if (methodDefinition.Name.EndsWith("ClientRpc", StringComparison.OrdinalIgnoreCase))
                {
                    m_Diagnostics.AddError(methodDefinition, "ClientRpc method must be marked with 'ClientRpc' attribute!");
                }

                return null;
            }
            // Checks for IsSerializable are moved to later as the check is now done by dynamically seeing if any valid
            // serializer OR extension method exists for it.
            return rpcAttribute;
        }

        private MethodReference GetFastBufferWriterWriteMethod(string name, TypeReference paramType)
        {
            foreach (var method in m_FastBufferWriter_TypeRef.Resolve().Methods)
            {
                if (method.Name == name)
                {
                    var parameters = method.Parameters;

                    if (parameters.Count == 0 || (parameters.Count > 1 && !parameters[1].IsOptional))
                    {
                        continue;
                    }

                    if (parameters[0].ParameterType.IsArray != paramType.IsArray)
                    {
                        continue;
                    }

                    var checkType = paramType.Resolve();
                    if (paramType.IsArray)
                    {
                        checkType = ((ArrayType)paramType).ElementType.Resolve();
                    }

                    if ((parameters[0].ParameterType.Resolve() == checkType ||
                         (parameters[0].ParameterType.Resolve() == checkType.MakeByReferenceType().Resolve() && parameters[0].IsIn)))
                    {
                        return method;
                    }

                    if (parameters[0].ParameterType == paramType ||
                        (parameters[0].ParameterType == paramType.MakeByReferenceType() && parameters[0].IsIn))
                    {
                        return method;
                    }

                    if (method.HasGenericParameters && method.GenericParameters.Count == 1)
                    {
                        if (method.GenericParameters[0].HasConstraints)
                        {
                            var meetsConstraints = true;
                            foreach (var constraint in method.GenericParameters[0].Constraints)
                            {
#if CECIL_CONSTRAINTS_ARE_TYPE_REFERENCES
                                var resolvedConstraint = constraint.Resolve();
                                var constraintTypeRef = constraint;
#else
                                var resolvedConstraint = constraint.ConstraintType.Resolve();
                                var constraintTypeRef = constraint.ConstraintType;
#endif

                                var resolvedConstraintName = resolvedConstraint.FullNameWithGenericParameters(new[] { method.GenericParameters[0] }, new[] { checkType });
                                if (constraintTypeRef.IsGenericInstance)
                                {
                                    var genericConstraint = (GenericInstanceType)constraintTypeRef;
                                    if (genericConstraint.HasGenericArguments && genericConstraint.GenericArguments[0].Resolve() != null)
                                    {
                                        resolvedConstraintName = constraintTypeRef.FullName;
                                    }
                                }
                                if ((resolvedConstraint.IsInterface && !checkType.HasInterface(resolvedConstraintName)) ||
                                    (resolvedConstraint.IsClass && !checkType.Resolve().IsSubclassOf(resolvedConstraintName)) ||
                                    (resolvedConstraint.Name == "ValueType" && !checkType.IsValueType))
                                {
                                    meetsConstraints = false;
                                    break;
                                }
                            }

                            if (meetsConstraints)
                            {
                                var instanceMethod = new GenericInstanceMethod(method);
                                if (paramType.IsArray)
                                {
                                    instanceMethod.GenericArguments.Add(((ArrayType)paramType).ElementType);
                                }
                                else
                                {
                                    instanceMethod.GenericArguments.Add(paramType);
                                }
                                return instanceMethod;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private bool GetWriteMethodForParameter(TypeReference paramType, out MethodReference methodRef)
        {
            var assemblyQualifiedName = paramType.FullName + ", " + paramType.Resolve().Module.Assembly.FullName;
            var foundMethodRef = m_FastBufferWriter_WriteValue_MethodRefs.TryGetValue(assemblyQualifiedName, out methodRef);

            if (!foundMethodRef)
            {
                foreach (var method in m_FastBufferWriter_ExtensionMethodRefs)
                {
                    var parameters = method.Resolve().Parameters;

                    if (method.Name == k_WriteValueMethodName)
                    {
                        if (parameters[1].IsIn)
                        {
                            if (((ByReferenceType)parameters[1].ParameterType).ElementType.FullName == paramType.FullName &&
                                ((ByReferenceType)parameters[1].ParameterType).ElementType.IsArray == paramType.IsArray)
                            {
                                methodRef = method;
                                m_FastBufferWriter_WriteValue_MethodRefs[assemblyQualifiedName] = methodRef;
                                return true;
                            }
                        }
                        else
                        {
                            if (parameters[1].ParameterType.FullName == paramType.FullName &&
                                parameters[1].ParameterType.IsArray == paramType.IsArray)
                            {
                                methodRef = method;
                                m_FastBufferWriter_WriteValue_MethodRefs[assemblyQualifiedName] = methodRef;
                                return true;
                            }
                        }
                    }
                }

                var typeMethod = GetFastBufferWriterWriteMethod(k_WriteValueMethodName, paramType);
                if (typeMethod != null)
                {
                    methodRef = m_MainModule.ImportReference(typeMethod);
                    m_FastBufferWriter_WriteValue_MethodRefs[assemblyQualifiedName] = methodRef;
                    foundMethodRef = true;
                }
            }

            return foundMethodRef;
        }
        private MethodReference GetFastBufferReaderReadMethod(string name, TypeReference paramType)
        {
            foreach (var method in m_FastBufferReader_TypeRef.Resolve().Methods)
            {
                var paramTypeDef = paramType.Resolve();
                if (method.Name == name)
                {
                    var parameters = method.Parameters;

                    if (parameters.Count == 0 || (parameters.Count > 1 && !parameters[1].IsOptional))
                    {
                        continue;
                    }

                    if (!parameters[0].IsOut)
                    {
                        return null;
                    }

                    var methodParam = ((ByReferenceType)parameters[0].ParameterType).ElementType;

                    if (methodParam.IsArray != paramType.IsArray)
                    {
                        continue;
                    }

                    var checkType = paramType.Resolve();
                    if (paramType.IsArray)
                    {
                        checkType = ((ArrayType)paramType).ElementType.Resolve();
                    }

                    if (methodParam.Resolve() == checkType.Resolve() || methodParam.Resolve() == checkType.MakeByReferenceType().Resolve())
                    {
                        return method;
                    }

                    if (methodParam.Resolve() == paramType || methodParam.Resolve() == paramType.MakeByReferenceType().Resolve())
                    {
                        return method;
                    }

                    if (method.HasGenericParameters && method.GenericParameters.Count == 1)
                    {
                        if (method.GenericParameters[0].HasConstraints)
                        {
                            var meetsConstraints = true;
                            foreach (var constraint in method.GenericParameters[0].Constraints)
                            {
#if CECIL_CONSTRAINTS_ARE_TYPE_REFERENCES
                                var resolvedConstraint = constraint.Resolve();
                                var constraintTypeRef = constraint;
#else
                                var resolvedConstraint = constraint.ConstraintType.Resolve();
                                var constraintTypeRef = constraint.ConstraintType;
#endif


                                var resolvedConstraintName = resolvedConstraint.FullNameWithGenericParameters(new[] { method.GenericParameters[0] }, new[] { checkType });
                                if (constraintTypeRef.IsGenericInstance)
                                {
                                    var genericConstraint = (GenericInstanceType)constraintTypeRef;
                                    if (genericConstraint.HasGenericArguments && genericConstraint.GenericArguments[0].Resolve() != null)
                                    {
                                        resolvedConstraintName = constraintTypeRef.FullName;
                                    }
                                }

                                if ((resolvedConstraint.IsInterface && !checkType.HasInterface(resolvedConstraintName)) ||
                                    (resolvedConstraint.IsClass && !checkType.Resolve().IsSubclassOf(resolvedConstraintName)) ||
                                    (resolvedConstraint.Name == "ValueType" && !checkType.IsValueType))
                                {
                                    meetsConstraints = false;
                                    break;
                                }
                            }

                            if (meetsConstraints)
                            {
                                var instanceMethod = new GenericInstanceMethod(method);
                                if (paramType.IsArray)
                                {
                                    instanceMethod.GenericArguments.Add(((ArrayType)paramType).ElementType);
                                }
                                else
                                {
                                    instanceMethod.GenericArguments.Add(paramType);
                                }

                                return instanceMethod;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private bool GetReadMethodForParameter(TypeReference paramType, out MethodReference methodRef)
        {
            var assemblyQualifiedName = paramType.FullName + ", " + paramType.Resolve().Module.Assembly.FullName;

            var foundMethodRef = m_FastBufferReader_ReadValue_MethodRefs.TryGetValue(assemblyQualifiedName, out methodRef);
            if (!foundMethodRef)
            {
                foreach (var method in m_FastBufferReader_ExtensionMethodRefs)
                {
                    var parameters = method.Resolve().Parameters;
                    if (method.Name == k_ReadValueMethodName &&
                        parameters[1].IsOut &&
                        ((ByReferenceType)parameters[1].ParameterType).ElementType.FullName == paramType.FullName &&
                        ((ByReferenceType)parameters[1].ParameterType).ElementType.IsArray == paramType.IsArray)
                    {
                        methodRef = method;
                        m_FastBufferReader_ReadValue_MethodRefs[assemblyQualifiedName] = methodRef;
                        return true;
                    }
                }

                var typeMethod = GetFastBufferReaderReadMethod(k_ReadValueMethodName, paramType);
                if (typeMethod != null)
                {
                    methodRef = m_MainModule.ImportReference(typeMethod);
                    m_FastBufferReader_ReadValue_MethodRefs[assemblyQualifiedName] = methodRef;
                    foundMethodRef = true;
                }
            }

            return foundMethodRef;
        }

        private void InjectWriteAndCallBlocks(MethodDefinition methodDefinition, CustomAttribute rpcAttribute, uint rpcMethodId)
        {
            var typeSystem = methodDefinition.Module.TypeSystem;
            var instructions = new List<Instruction>();
            var processor = methodDefinition.Body.GetILProcessor();
            var isServerRpc = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRpcAttribute_FullName;
            var requireOwnership = true; // default value MUST be == `ServerRpcAttribute.RequireOwnership`
            var rpcDelivery = RpcDelivery.Reliable; // default value MUST be == `RpcAttribute.Delivery`
            foreach (var attrField in rpcAttribute.Fields)
            {
                switch (attrField.Name)
                {
                    case k_RpcAttribute_Delivery:
                        rpcDelivery = (RpcDelivery)attrField.Argument.Value;
                        break;
                    case k_ServerRpcAttribute_RequireOwnership:
                        requireOwnership = attrField.Argument.Type == typeSystem.Boolean && (bool)attrField.Argument.Value;
                        break;
                }
            }

            var paramCount = methodDefinition.Parameters.Count;
            var hasRpcParams =
                paramCount > 0 &&
                ((isServerRpc && methodDefinition.Parameters[paramCount - 1].ParameterType.FullName == CodeGenHelpers.ServerRpcParams_FullName) ||
                 (!isServerRpc && methodDefinition.Parameters[paramCount - 1].ParameterType.FullName == CodeGenHelpers.ClientRpcParams_FullName));

            methodDefinition.Body.InitLocals = true;
            // NetworkManager networkManager;
            methodDefinition.Body.Variables.Add(new VariableDefinition(m_NetworkManager_TypeRef));
            int netManLocIdx = methodDefinition.Body.Variables.Count - 1;
            // FastBufferWriter bufferWriter;
            methodDefinition.Body.Variables.Add(new VariableDefinition(m_FastBufferWriter_TypeRef));
            int bufWriterLocIdx = methodDefinition.Body.Variables.Count - 1;

            // XXXRpcParams
            if (!hasRpcParams)
            {
                methodDefinition.Body.Variables.Add(new VariableDefinition(isServerRpc ? m_ServerRpcParams_TypeRef : m_ClientRpcParams_TypeRef));
            }
            int rpcParamsIdx = !hasRpcParams ? methodDefinition.Body.Variables.Count - 1 : -1;

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var lastInstr = processor.Create(OpCodes.Nop);

                // networkManager = this.NetworkManager;
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Call, m_NetworkBehaviour_getNetworkManager_MethodRef));
                instructions.Add(processor.Create(OpCodes.Stloc, netManLocIdx));

                // if (networkManager == null || !networkManager.IsListening) return;
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Brfalse, returnInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, m_NetworkManager_getIsListening_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, lastInstr));

                instructions.Add(returnInstr);
                instructions.Add(lastInstr);
            }

            {
                var beginInstr = processor.Create(OpCodes.Nop);
                var endInstr = processor.Create(OpCodes.Nop);
                var lastInstr = processor.Create(OpCodes.Nop);

                // if (__rpc_exec_stage != __RpcExecStage.Server) -> ServerRpc
                // if (__rpc_exec_stage != __RpcExecStage.Client) -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Ldfld, m_NetworkBehaviour_rpc_exec_stage_FieldRef));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkBehaviour.__RpcExecStage.Server : NetworkBehaviour.__RpcExecStage.Client)));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, 0));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Brfalse, lastInstr));

                // if (networkManager.IsClient || networkManager.IsHost) { ... } -> ServerRpc
                // if (networkManager.IsServer || networkManager.IsHost) { ... } -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, isServerRpc ? m_NetworkManager_getIsClient_MethodRef : m_NetworkManager_getIsServer_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, beginInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, m_NetworkManager_getIsHost_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brfalse, lastInstr));

                instructions.Add(beginInstr);

                // var bufferWriter = __beginSendServerRpc(rpcMethodId, serverRpcParams, rpcDelivery) -> ServerRpc
                // var bufferWriter = __beginSendClientRpc(rpcMethodId, clientRpcParams, rpcDelivery) -> ClientRpc
                if (isServerRpc)
                {
                    // ServerRpc

                    if (requireOwnership)
                    {
                        var roReturnInstr = processor.Create(OpCodes.Ret);
                        var roLastInstr = processor.Create(OpCodes.Nop);

                        // if (this.OwnerClientId != networkManager.LocalClientId) { ... } return;
                        instructions.Add(processor.Create(OpCodes.Ldarg_0));
                        instructions.Add(processor.Create(OpCodes.Call, m_NetworkBehaviour_getOwnerClientId_MethodRef));
                        instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                        instructions.Add(processor.Create(OpCodes.Callvirt, m_NetworkManager_getLocalClientId_MethodRef));
                        instructions.Add(processor.Create(OpCodes.Ceq));
                        instructions.Add(processor.Create(OpCodes.Ldc_I4, 0));
                        instructions.Add(processor.Create(OpCodes.Ceq));
                        instructions.Add(processor.Create(OpCodes.Brfalse, roLastInstr));

                        var logNextInstr = processor.Create(OpCodes.Nop);

                        // if (LogLevel.Normal > networkManager.LogLevel)
                        instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldfld, m_NetworkManager_LogLevel_FieldRef));
                        instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)LogLevel.Normal));
                        instructions.Add(processor.Create(OpCodes.Cgt));
                        instructions.Add(processor.Create(OpCodes.Ldc_I4, 0));
                        instructions.Add(processor.Create(OpCodes.Ceq));
                        instructions.Add(processor.Create(OpCodes.Brfalse, logNextInstr));

                        // Debug.LogError(...);
                        instructions.Add(processor.Create(OpCodes.Ldstr, "Only the owner can invoke a ServerRpc that requires ownership!"));
                        instructions.Add(processor.Create(OpCodes.Call, m_Debug_LogError_MethodRef));

                        instructions.Add(logNextInstr);

                        instructions.Add(roReturnInstr);
                        instructions.Add(roLastInstr);
                    }

                    // var bufferWriter = __beginSendServerRpc(rpcMethodId, serverRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // rpcMethodId
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)rpcMethodId)));

                    // rpcParams
                    instructions.Add(hasRpcParams ? processor.Create(OpCodes.Ldarg, paramCount) : processor.Create(OpCodes.Ldloc, rpcParamsIdx));

                    // rpcDelivery
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)rpcDelivery));

                    // __beginSendServerRpc
                    instructions.Add(processor.Create(OpCodes.Call, m_NetworkBehaviour_beginSendServerRpc_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, bufWriterLocIdx));
                }
                else
                {
                    // ClientRpc

                    // var bufferWriter = __beginSendClientRpc(rpcMethodId, clientRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // rpcMethodId
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)rpcMethodId)));

                    // rpcParams
                    instructions.Add(hasRpcParams ? processor.Create(OpCodes.Ldarg, paramCount) : processor.Create(OpCodes.Ldloc, rpcParamsIdx));

                    // rpcDelivery
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)rpcDelivery));

                    // __beginSendClientRpc
                    instructions.Add(processor.Create(OpCodes.Call, m_NetworkBehaviour_beginSendClientRpc_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, bufWriterLocIdx));
                }

                // write method parameters into stream
                for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
                {
                    var paramDef = methodDefinition.Parameters[paramIndex];
                    var paramType = paramDef.ParameterType;
                    if (paramType.FullName == CodeGenHelpers.ClientRpcSendParams_FullName ||
                        paramType.FullName == CodeGenHelpers.ClientRpcReceiveParams_FullName)
                    {
                        m_Diagnostics.AddError($"Rpcs may not accept {paramType.FullName} as a parameter. Use {nameof(ClientRpcParams)} instead.");
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.ServerRpcSendParams_FullName ||
                        paramType.FullName == CodeGenHelpers.ServerRpcReceiveParams_FullName)
                    {
                        m_Diagnostics.AddError($"Rpcs may not accept {paramType.FullName} as a parameter. Use {nameof(ServerRpcParams)} instead.");
                        continue;
                    }
                    // ServerRpcParams
                    if (paramType.FullName == CodeGenHelpers.ServerRpcParams_FullName)
                    {
                        if (paramIndex != paramCount - 1)
                        {
                            m_Diagnostics.AddError(methodDefinition, $"{nameof(ServerRpcParams)} must be the last parameter in a ServerRpc.");
                        }
                        if (!isServerRpc)
                        {
                            m_Diagnostics.AddError($"ClientRpcs may not accept {nameof(ServerRpcParams)} as a parameter.");
                        }
                        continue;
                    }
                    // ClientRpcParams
                    if (paramType.FullName == CodeGenHelpers.ClientRpcParams_FullName)
                    {
                        if (paramIndex != paramCount - 1)
                        {
                            m_Diagnostics.AddError(methodDefinition, $"{nameof(ClientRpcParams)} must be the last parameter in a ClientRpc.");
                        }
                        if (isServerRpc)
                        {
                            m_Diagnostics.AddError($"ServerRpcs may not accept {nameof(ClientRpcParams)} as a parameter.");
                        }
                        continue;
                    }

                    Instruction jumpInstruction = null;

                    if (!paramType.IsValueType)
                    {
                        if (!GetWriteMethodForParameter(typeSystem.Boolean, out var boolMethodRef))
                        {
                            m_Diagnostics.AddError(methodDefinition, $"Couldn't find boolean serializer! Something's wrong!");
                            return;
                        }

                        methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                        int isSetLocalIndex = methodDefinition.Body.Variables.Count - 1;

                        // bool isSet = (param != null);
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Ldnull));
                        instructions.Add(processor.Create(OpCodes.Cgt_Un));
                        instructions.Add(processor.Create(OpCodes.Stloc, isSetLocalIndex));

                        // bufferWriter.WriteValueSafe(isSet);
                        instructions.Add(processor.Create(OpCodes.Ldloca, bufWriterLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldloca, isSetLocalIndex));

                        for (var i = 1; i < boolMethodRef.Parameters.Count; ++i)
                        {
                            var param = boolMethodRef.Parameters[i];
                            methodDefinition.Body.Variables.Add(new VariableDefinition(param.ParameterType));
                            int overloadParamLocalIdx = methodDefinition.Body.Variables.Count - 1;
                            instructions.Add(processor.Create(OpCodes.Ldloca, overloadParamLocalIdx));
                            instructions.Add(processor.Create(OpCodes.Initobj, param.ParameterType));
                            instructions.Add(processor.Create(OpCodes.Ldloc, overloadParamLocalIdx));
                        }

                        instructions.Add(processor.Create(OpCodes.Call, boolMethodRef));

                        // if(isSet) {
                        jumpInstruction = processor.Create(OpCodes.Nop);
                        instructions.Add(processor.Create(OpCodes.Ldloc, isSetLocalIndex));
                        instructions.Add(processor.Create(OpCodes.Brfalse, jumpInstruction));
                    }

                    var foundMethodRef = GetWriteMethodForParameter(paramType, out var methodRef);
                    if (foundMethodRef)
                    {
                        // bufferWriter.WriteNetworkSerializable(param) for INetworkSerializable, OR
                        // bufferWriter.WriteNetworkSerializable(param, -1, 0) for INetworkSerializable arrays, OR
                        // bufferWriter.WriteValueSafe(param) for value types, OR
                        // bufferWriter.WriteValueSafe(param, -1, 0) for arrays of value types, OR
                        // bufferWriter.WriteValueSafe(param, false) for strings
                        var method = methodRef.Resolve();
                        var checkParameter = method.Parameters[0];
                        var isExtensionMethod = false;
                        if (methodRef.Resolve().DeclaringType != m_FastBufferWriter_TypeRef.Resolve())
                        {
                            isExtensionMethod = true;
                            checkParameter = method.Parameters[1];
                        }
                        if (!isExtensionMethod || method.Parameters[0].ParameterType.IsByReference)
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloca, bufWriterLocIdx));
                        }
                        else
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloc, bufWriterLocIdx));
                        }
                        if (checkParameter.IsIn || checkParameter.IsOut || checkParameter.ParameterType.IsByReference)
                        {
                            instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        }
                        else
                        {
                            instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        }
                        // Special handling for WriteValue() on arrays and strings since they have additional arguments.
                        if (paramType.IsArray && ((!isExtensionMethod && methodRef.Parameters.Count == 3) ||
                            (isExtensionMethod && methodRef.Parameters.Count == 4)))
                        {
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_M1));
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_0));
                        }
                        else if (paramType == typeSystem.String && ((!isExtensionMethod && methodRef.Parameters.Count == 2) ||
                            (isExtensionMethod && methodRef.Parameters.Count == 3)))
                        {
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_0));
                        }
                        else
                        {
                            if (isExtensionMethod && methodRef.Parameters.Count > 2)
                            {
                                for (var i = 2; i < methodRef.Parameters.Count; ++i)
                                {
                                    var param = methodRef.Parameters[i];
                                    methodDefinition.Body.Variables.Add(new VariableDefinition(param.ParameterType));
                                    int overloadParamLocalIdx = methodDefinition.Body.Variables.Count - 1;
                                    instructions.Add(processor.Create(OpCodes.Ldloca, overloadParamLocalIdx));
                                    instructions.Add(processor.Create(OpCodes.Initobj, param.ParameterType));
                                    instructions.Add(processor.Create(OpCodes.Ldloc, overloadParamLocalIdx));
                                }
                            }
                            else if (!isExtensionMethod && methodRef.Parameters.Count > 1)
                            {
                                for (var i = 1; i < methodRef.Parameters.Count; ++i)
                                {
                                    var param = methodRef.Parameters[i];
                                    methodDefinition.Body.Variables.Add(new VariableDefinition(param.ParameterType));
                                    int overloadParamLocalIdx = methodDefinition.Body.Variables.Count - 1;
                                    instructions.Add(processor.Create(OpCodes.Ldloca, overloadParamLocalIdx));
                                    instructions.Add(processor.Create(OpCodes.Initobj, param.ParameterType));
                                    instructions.Add(processor.Create(OpCodes.Ldloc, overloadParamLocalIdx));
                                }
                            }
                        }
                        instructions.Add(processor.Create(OpCodes.Call, methodRef));
                    }
                    else
                    {
                        m_Diagnostics.AddError(methodDefinition, $"{methodDefinition.Name} - Don't know how to serialize {paramType.Name}. RPC parameter types must either implement {nameof(INetworkSerializeByMemcpy)} or {nameof(INetworkSerializable)}. If this type is external and you are sure its memory layout makes it serializable by memcpy, you can replace {paramType} with {typeof(ForceNetworkSerializeByMemcpy<>).Name}<{paramType}>, or you can create extension methods for {nameof(FastBufferReader)}.{nameof(FastBufferReader.ReadValueSafe)}(this {nameof(FastBufferReader)}, out {paramType}) and {nameof(FastBufferWriter)}.{nameof(FastBufferWriter.WriteValueSafe)}(this {nameof(FastBufferWriter)}, in {paramType}) to define serialization for this type.");
                        continue;
                    }

                    if (jumpInstruction != null)
                    {
                        instructions.Add(jumpInstruction);
                    }
                }

                instructions.Add(endInstr);

                // __endSendServerRpc(ref bufferWriter, rpcMethodId, serverRpcParams, rpcDelivery) -> ServerRpc
                // __endSendClientRpc(ref bufferWriter, rpcMethodId, clientRpcParams, rpcDelivery) -> ClientRpc
                if (isServerRpc)
                {
                    // ServerRpc

                    // __endSendServerRpc(ref bufferWriter, rpcMethodId, serverRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // bufferWriter
                    instructions.Add(processor.Create(OpCodes.Ldloca, bufWriterLocIdx));

                    // rpcMethodId
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)rpcMethodId)));
                    if (hasRpcParams)
                    {
                        // rpcParams
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                    }
                    else
                    {
                        // default
                        instructions.Add(processor.Create(OpCodes.Ldloc, rpcParamsIdx));
                    }
                    // rpcDelivery
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)rpcDelivery));

                    // __endSendServerRpc
                    instructions.Add(processor.Create(OpCodes.Call, m_NetworkBehaviour_endSendServerRpc_MethodRef));
                }
                else
                {
                    // ClientRpc

                    // __endSendClientRpc(ref bufferWriter, rpcMethodId, clientRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // bufferWriter
                    instructions.Add(processor.Create(OpCodes.Ldloca, bufWriterLocIdx));

                    // rpcMethodId
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)rpcMethodId)));
                    if (hasRpcParams)
                    {
                        // rpcParams
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                    }
                    else
                    {
                        // default
                        instructions.Add(processor.Create(OpCodes.Ldloc, rpcParamsIdx));
                    }
                    // rpcDelivery
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)rpcDelivery));

                    // __endSendClientRpc
                    instructions.Add(processor.Create(OpCodes.Call, m_NetworkBehaviour_endSendClientRpc_MethodRef));
                }

                instructions.Add(lastInstr);
            }

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var lastInstr = processor.Create(OpCodes.Nop);

                // if (__rpc_exec_stage == __RpcExecStage.Server) -> ServerRpc
                // if (__rpc_exec_stage == __RpcExecStage.Client) -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Ldfld, m_NetworkBehaviour_rpc_exec_stage_FieldRef));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkBehaviour.__RpcExecStage.Server : NetworkBehaviour.__RpcExecStage.Client)));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Brfalse, returnInstr));

                // if (networkManager.IsServer || networkManager.IsHost) -> ServerRpc
                // if (networkManager.IsClient || networkManager.IsHost) -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, isServerRpc ? m_NetworkManager_getIsServer_MethodRef : m_NetworkManager_getIsClient_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, lastInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, m_NetworkManager_getIsHost_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, lastInstr));

                instructions.Add(returnInstr);
                instructions.Add(lastInstr);
            }

            instructions.Reverse();
            instructions.ForEach(instruction => processor.Body.Instructions.Insert(0, instruction));
        }

        private MethodDefinition GenerateStaticHandler(MethodDefinition methodDefinition, CustomAttribute rpcAttribute, uint rpcMethodId)
        {
            var typeSystem = methodDefinition.Module.TypeSystem;
            var rpcHandler = new MethodDefinition(
                $"__rpc_handler_{rpcMethodId}",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                methodDefinition.Module.TypeSystem.Void);
            rpcHandler.Parameters.Add(new ParameterDefinition("target", ParameterAttributes.None, m_NetworkBehaviour_TypeRef));
            rpcHandler.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, m_FastBufferReader_TypeRef));
            rpcHandler.Parameters.Add(new ParameterDefinition("rpcParams", ParameterAttributes.None, m_RpcParams_TypeRef));

            var processor = rpcHandler.Body.GetILProcessor();

            var isServerRpc = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRpcAttribute_FullName;
            var requireOwnership = true; // default value MUST be == `ServerRpcAttribute.RequireOwnership`
            foreach (var attrField in rpcAttribute.Fields)
            {
                switch (attrField.Name)
                {
                    case k_ServerRpcAttribute_RequireOwnership:
                        requireOwnership = attrField.Argument.Type == typeSystem.Boolean && (bool)attrField.Argument.Value;
                        break;
                }
            }

            rpcHandler.Body.InitLocals = true;
            // NetworkManager networkManager;
            rpcHandler.Body.Variables.Add(new VariableDefinition(m_NetworkManager_TypeRef));
            int netManLocIdx = rpcHandler.Body.Variables.Count - 1;

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var lastInstr = processor.Create(OpCodes.Nop);

                // networkManager = this.NetworkManager;
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, m_NetworkBehaviour_getNetworkManager_MethodRef);
                processor.Emit(OpCodes.Stloc, netManLocIdx);

                // if (networkManager == null || !networkManager.IsListening) return;
                processor.Emit(OpCodes.Ldloc, netManLocIdx);
                processor.Emit(OpCodes.Brfalse, returnInstr);
                processor.Emit(OpCodes.Ldloc, netManLocIdx);
                processor.Emit(OpCodes.Callvirt, m_NetworkManager_getIsListening_MethodRef);
                processor.Emit(OpCodes.Brtrue, lastInstr);

                processor.Append(returnInstr);
                processor.Append(lastInstr);
            }

            if (isServerRpc && requireOwnership)
            {
                var roReturnInstr = processor.Create(OpCodes.Ret);
                var roLastInstr = processor.Create(OpCodes.Nop);

                // if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId) { ... } return;
                processor.Emit(OpCodes.Ldarg_2);
                processor.Emit(OpCodes.Ldfld, m_RpcParams_Server_FieldRef);
                processor.Emit(OpCodes.Ldfld, m_ServerRpcParams_Receive_FieldRef);
                processor.Emit(OpCodes.Ldfld, m_ServerRpcParams_Receive_SenderClientId_FieldRef);
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, m_NetworkBehaviour_getOwnerClientId_MethodRef);
                processor.Emit(OpCodes.Ceq);
                processor.Emit(OpCodes.Ldc_I4, 0);
                processor.Emit(OpCodes.Ceq);
                processor.Emit(OpCodes.Brfalse, roLastInstr);

                var logNextInstr = processor.Create(OpCodes.Nop);

                // if (LogLevel.Normal > networkManager.LogLevel)
                processor.Emit(OpCodes.Ldloc, netManLocIdx);
                processor.Emit(OpCodes.Ldfld, m_NetworkManager_LogLevel_FieldRef);
                processor.Emit(OpCodes.Ldc_I4, (int)LogLevel.Normal);
                processor.Emit(OpCodes.Cgt);
                processor.Emit(OpCodes.Ldc_I4, 0);
                processor.Emit(OpCodes.Ceq);
                processor.Emit(OpCodes.Brfalse, logNextInstr);

                // Debug.LogError(...);
                processor.Emit(OpCodes.Ldstr, "Only the owner can invoke a ServerRpc that requires ownership!");
                processor.Emit(OpCodes.Call, m_Debug_LogError_MethodRef);

                processor.Append(logNextInstr);

                processor.Append(roReturnInstr);
                processor.Append(roLastInstr);
            }

            // read method parameters from stream
            int paramCount = methodDefinition.Parameters.Count;
            int[] paramLocalMap = new int[paramCount];
            for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
            {
                var paramDef = methodDefinition.Parameters[paramIndex];
                var paramType = paramDef.ParameterType;

                // local variable
                rpcHandler.Body.Variables.Add(new VariableDefinition(paramType));
                int localIndex = rpcHandler.Body.Variables.Count - 1;
                paramLocalMap[paramIndex] = localIndex;

                // ServerRpcParams, ClientRpcParams
                {
                    // ServerRpcParams
                    if (paramType.FullName == CodeGenHelpers.ServerRpcParams_FullName)
                    {
                        processor.Emit(OpCodes.Ldarg_2);
                        processor.Emit(OpCodes.Ldfld, m_RpcParams_Server_FieldRef);
                        processor.Emit(OpCodes.Stloc, localIndex);
                        continue;
                    }

                    // ClientRpcParams
                    if (paramType.FullName == CodeGenHelpers.ClientRpcParams_FullName)
                    {
                        processor.Emit(OpCodes.Ldarg_2);
                        processor.Emit(OpCodes.Ldfld, m_RpcParams_Client_FieldRef);
                        processor.Emit(OpCodes.Stloc, localIndex);
                        continue;
                    }
                }

                Instruction jumpInstruction = null;

                if (!paramType.IsValueType)
                {
                    if (!GetReadMethodForParameter(typeSystem.Boolean, out var boolMethodRef))
                    {
                        m_Diagnostics.AddError(methodDefinition, $"Couldn't find boolean deserializer! Something's wrong!");
                    }

                    // reader.ReadValueSafe(out bool isSet)
                    rpcHandler.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                    int isSetLocalIndex = rpcHandler.Body.Variables.Count - 1;
                    processor.Emit(OpCodes.Ldarga, 1);
                    processor.Emit(OpCodes.Ldloca, isSetLocalIndex);

                    for (var i = 1; i < boolMethodRef.Parameters.Count; ++i)
                    {
                        var param = boolMethodRef.Parameters[i];
                        rpcHandler.Body.Variables.Add(new VariableDefinition(param.ParameterType));
                        int overloadParamLocalIdx = rpcHandler.Body.Variables.Count - 1;
                        processor.Emit(OpCodes.Ldloca, overloadParamLocalIdx);
                        processor.Emit(OpCodes.Initobj, param.ParameterType);
                        processor.Emit(OpCodes.Ldloc, overloadParamLocalIdx);
                    }

                    processor.Emit(OpCodes.Call, boolMethodRef);

                    // paramType param = null;
                    processor.Emit(OpCodes.Ldnull);
                    processor.Emit(OpCodes.Stloc, localIndex);

                    // if(isSet) {
                    jumpInstruction = processor.Create(OpCodes.Nop);
                    processor.Emit(OpCodes.Ldloc, isSetLocalIndex);
                    processor.Emit(OpCodes.Brfalse, jumpInstruction);
                }

                var foundMethodRef = GetReadMethodForParameter(paramType, out var methodRef);
                if (foundMethodRef)
                {
                    // reader.ReadValueSafe(out localVar);

                    var checkParameter = methodRef.Resolve().Parameters[0];

                    var isExtensionMethod = methodRef.Resolve().DeclaringType != m_FastBufferReader_TypeRef.Resolve();
                    if (!isExtensionMethod || checkParameter.ParameterType.IsByReference)
                    {
                        processor.Emit(OpCodes.Ldarga, 1);
                    }
                    else
                    {
                        processor.Emit(OpCodes.Ldarg, 1);
                    }
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    if (paramType == typeSystem.String)
                    {
                        processor.Emit(OpCodes.Ldc_I4_0);
                    }
                    else
                    {
                        if (isExtensionMethod && methodRef.Parameters.Count > 2)
                        {
                            for (var i = 2; i < methodRef.Parameters.Count; ++i)
                            {
                                var param = methodRef.Parameters[i];
                                rpcHandler.Body.Variables.Add(new VariableDefinition(param.ParameterType));
                                int overloadParamLocalIdx = rpcHandler.Body.Variables.Count - 1;
                                processor.Emit(OpCodes.Ldloca, overloadParamLocalIdx);
                                processor.Emit(OpCodes.Initobj, param.ParameterType);
                                processor.Emit(OpCodes.Ldloc, overloadParamLocalIdx);
                            }
                        }
                        else if (!isExtensionMethod && methodRef.Parameters.Count > 1)
                        {
                            for (var i = 1; i < methodRef.Parameters.Count; ++i)
                            {
                                var param = methodRef.Parameters[i];
                                rpcHandler.Body.Variables.Add(new VariableDefinition(param.ParameterType));
                                int overloadParamLocalIdx = rpcHandler.Body.Variables.Count - 1;
                                processor.Emit(OpCodes.Ldloca, overloadParamLocalIdx);
                                processor.Emit(OpCodes.Initobj, param.ParameterType);
                                processor.Emit(OpCodes.Ldloc, overloadParamLocalIdx);
                            }
                        }
                    }
                    processor.Emit(OpCodes.Call, methodRef);
                }
                else
                {
                    m_Diagnostics.AddError(methodDefinition, $"{methodDefinition.Name} - Don't know how to serialize {paramType.Name}. RPC parameter types must either implement {nameof(INetworkSerializeByMemcpy)} or {nameof(INetworkSerializable)}. If this type is external and you are sure its memory layout makes it serializable by memcpy, you can replace {paramType} with {typeof(ForceNetworkSerializeByMemcpy<>).Name}<{paramType}>, or you can create extension methods for {nameof(FastBufferReader)}.{nameof(FastBufferReader.ReadValueSafe)}(this {nameof(FastBufferReader)}, out {paramType}) and {nameof(FastBufferWriter)}.{nameof(FastBufferWriter.WriteValueSafe)}(this {nameof(FastBufferWriter)}, in {paramType}) to define serialization for this type.");
                    continue;
                }

                if (jumpInstruction != null)
                {
                    processor.Append(jumpInstruction);
                }
            }

            // NetworkBehaviour.__rpc_exec_stage = __RpcExecStage.Server; -> ServerRpc
            // NetworkBehaviour.__rpc_exec_stage = __RpcExecStage.Client; -> ClientRpc
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkBehaviour.__RpcExecStage.Server : NetworkBehaviour.__RpcExecStage.Client));
            processor.Emit(OpCodes.Stfld, m_NetworkBehaviour_rpc_exec_stage_FieldRef);

            // NetworkBehaviour.XXXRpc(...);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Castclass, methodDefinition.DeclaringType);
            Enumerable.Range(0, paramCount).ToList().ForEach(paramIndex => processor.Emit(OpCodes.Ldloc, paramLocalMap[paramIndex]));
            processor.Emit(OpCodes.Callvirt, methodDefinition);

            // NetworkBehaviour.__rpc_exec_stage = __RpcExecStage.None;
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldc_I4, (int)NetworkBehaviour.__RpcExecStage.None);
            processor.Emit(OpCodes.Stfld, m_NetworkBehaviour_rpc_exec_stage_FieldRef);

            processor.Emit(OpCodes.Ret);
            return rpcHandler;
        }
    }
}
