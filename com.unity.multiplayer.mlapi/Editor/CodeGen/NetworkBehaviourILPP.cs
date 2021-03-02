using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using MLAPI.Messaging;
using MLAPI.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
#if UNITY_2020_2_OR_NEWER
using ILPPInterface = Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor;
#else
using ILPPInterface = MLAPI.Editor.CodeGen.ILPostProcessor;
#endif

namespace MLAPI.Editor.CodeGen
{
    internal sealed class NetworkBehaviourILPP : ILPPInterface
    {
        public override ILPPInterface GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

        private readonly List<DiagnosticMessage> m_Diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly)) return null;
            m_Diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly);
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
                    // process `NetworkBehaviour` types
                    mainModule.Types
                        .Where(t => t.IsSubclassOf(CodeGenHelpers.NetworkBehaviour_FullName))
                        .ToList()
                        .ForEach(ProcessNetworkBehaviour);
                }
                else m_Diagnostics.AddError($"Cannot import references into main module: {mainModule.Name}");
            }
            else m_Diagnostics.AddError($"Cannot get main module from assembly definition: {compiledAssembly.Name}");

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

        private TypeReference NetworkManager_TypeRef;
        private MethodReference NetworkManager_getLocalClientId_MethodRef;
        private MethodReference NetworkManager_getIsListening_MethodRef;
        private MethodReference NetworkManager_getIsHost_MethodRef;
        private MethodReference NetworkManager_getIsServer_MethodRef;
        private MethodReference NetworkManager_getIsClient_MethodRef;
        private FieldReference NetworkManager_ntable_FieldRef;
        private MethodReference NetworkManager_ntable_Add_MethodRef;
        private TypeReference NetworkBehaviour_TypeRef;
        private MethodReference NetworkBehaviour_BeginSendServerRpc_MethodRef;
        private MethodReference NetworkBehaviour_EndSendServerRpc_MethodRef;
        private MethodReference NetworkBehaviour_BeginSendClientRpc_MethodRef;
        private MethodReference NetworkBehaviour_EndSendClientRpc_MethodRef;
        private FieldReference NetworkBehaviour_nexec_FieldRef;
        private MethodReference NetworkBehaviour_getNetworkManager_MethodRef;
        private MethodReference NetworkBehaviour_getOwnerClientId_MethodRef;
        private MethodReference NetworkHandlerDelegateCtor_MethodRef;
        private TypeReference RpcParams_TypeRef;
        private FieldReference RpcParams_Server_FieldRef;
        private FieldReference RpcParams_Client_FieldRef;
        private TypeReference ServerRpcParams_TypeRef;
        private FieldReference ServerRpcParams_Receive_FieldRef;
        private FieldReference ServerRpcParams_Receive_SenderClientId_FieldRef;
        private TypeReference ClientRpcParams_TypeRef;
        private TypeReference NetworkSerializer_TypeRef;
        private MethodReference NetworkSerializer_SerializeBool_MethodRef;
        private MethodReference NetworkSerializer_SerializeChar_MethodRef;
        private MethodReference NetworkSerializer_SerializeSbyte_MethodRef;
        private MethodReference NetworkSerializer_SerializeByte_MethodRef;
        private MethodReference NetworkSerializer_SerializeShort_MethodRef;
        private MethodReference NetworkSerializer_SerializeUshort_MethodRef;
        private MethodReference NetworkSerializer_SerializeInt_MethodRef;
        private MethodReference NetworkSerializer_SerializeUint_MethodRef;
        private MethodReference NetworkSerializer_SerializeLong_MethodRef;
        private MethodReference NetworkSerializer_SerializeUlong_MethodRef;
        private MethodReference NetworkSerializer_SerializeFloat_MethodRef;
        private MethodReference NetworkSerializer_SerializeDouble_MethodRef;
        private MethodReference NetworkSerializer_SerializeString_MethodRef;
        private MethodReference NetworkSerializer_SerializeColor_MethodRef;
        private MethodReference NetworkSerializer_SerializeColor32_MethodRef;
        private MethodReference NetworkSerializer_SerializeVector2_MethodRef;
        private MethodReference NetworkSerializer_SerializeVector3_MethodRef;
        private MethodReference NetworkSerializer_SerializeVector4_MethodRef;
        private MethodReference NetworkSerializer_SerializeQuaternion_MethodRef;
        private MethodReference NetworkSerializer_SerializeRay_MethodRef;
        private MethodReference NetworkSerializer_SerializeRay2D_MethodRef;
        private MethodReference NetworkSerializer_SerializeBoolArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeCharArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeSbyteArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeByteArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeShortArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeUshortArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeIntArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeUintArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeLongArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeUlongArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeFloatArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeDoubleArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeStringArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeColorArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeColor32Array_MethodRef;
        private MethodReference NetworkSerializer_SerializeVector2Array_MethodRef;
        private MethodReference NetworkSerializer_SerializeVector3Array_MethodRef;
        private MethodReference NetworkSerializer_SerializeVector4Array_MethodRef;
        private MethodReference NetworkSerializer_SerializeQuaternionArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeRayArray_MethodRef;
        private MethodReference NetworkSerializer_SerializeRay2DArray_MethodRef;

        private const string k_NetworkManager_LocalClientId = nameof(NetworkManager.LocalClientId);
        private const string k_NetworkManager_IsListening = nameof(NetworkManager.IsListening);
        private const string k_NetworkManager_IsHost = nameof(NetworkManager.IsHost);
        private const string k_NetworkManager_IsServer = nameof(NetworkManager.IsServer);
        private const string k_NetworkManager_IsClient = nameof(NetworkManager.IsClient);
#pragma warning disable 618
        private const string k_NetworkManager_ntable = nameof(NetworkManager.__ntable);

        private const string k_NetworkBehaviour_BeginSendServerRpc = nameof(NetworkBehaviour.__beginSendServerRpc);
        private const string k_NetworkBehaviour_EndSendServerRpc = nameof(NetworkBehaviour.__endSendServerRpc);
        private const string k_NetworkBehaviour_BeginSendClientRpc = nameof(NetworkBehaviour.__beginSendClientRpc);
        private const string k_NetworkBehaviour_EndSendClientRpc = nameof(NetworkBehaviour.__endSendClientRpc);
        private const string k_NetworkBehaviour_nexec = nameof(NetworkBehaviour.__nexec);
#pragma warning restore 618
        private const string k_NetworkBehaviour_NetworkManager = nameof(NetworkBehaviour.NetworkManager);
        private const string k_NetworkBehaviour_OwnerClientId = nameof(NetworkBehaviour.OwnerClientId);

        private const string k_RpcAttribute_Delivery = nameof(RpcAttribute.Delivery);
        private const string k_ServerRpcAttribute_RequireOwnership = nameof(ServerRpcAttribute.RequireOwnership);
#pragma warning disable 618
        private const string k_RpcParams_Server = nameof(__RpcParams.Server);
        private const string k_RpcParams_Client = nameof(__RpcParams.Client);
#pragma warning restore 618
        private const string k_ServerRpcParams_Receive = nameof(ServerRpcParams.Receive);
        private const string k_ServerRpcReceiveParams_SenderClientId = nameof(ServerRpcReceiveParams.SenderClientId);

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            var networkManagerType = typeof(NetworkManager);
            NetworkManager_TypeRef = moduleDefinition.ImportReference(networkManagerType);
            foreach (var propertyInfo in networkManagerType.GetProperties())
            {
                switch (propertyInfo.Name)
                {
                    case k_NetworkManager_LocalClientId:
                        NetworkManager_getLocalClientId_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case k_NetworkManager_IsListening:
                        NetworkManager_getIsListening_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case k_NetworkManager_IsHost:
                        NetworkManager_getIsHost_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case k_NetworkManager_IsServer:
                        NetworkManager_getIsServer_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case k_NetworkManager_IsClient:
                        NetworkManager_getIsClient_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                }
            }

            foreach (var fieldInfo in networkManagerType.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (fieldInfo.Name)
                {
                    case k_NetworkManager_ntable:
                        NetworkManager_ntable_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        NetworkManager_ntable_Add_MethodRef = moduleDefinition.ImportReference(fieldInfo.FieldType.GetMethod("Add"));
                        break;
                }
            }

            var networkBehaviourType = typeof(NetworkBehaviour);
            NetworkBehaviour_TypeRef = moduleDefinition.ImportReference(networkBehaviourType);
            foreach (var propertyInfo in networkBehaviourType.GetProperties())
            {
                switch (propertyInfo.Name)
                {
                    case k_NetworkBehaviour_NetworkManager:
                        NetworkBehaviour_getNetworkManager_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case k_NetworkBehaviour_OwnerClientId:
                        NetworkBehaviour_getOwnerClientId_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                }
            }

            foreach (var methodInfo in networkBehaviourType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (methodInfo.Name)
                {
                    case k_NetworkBehaviour_BeginSendServerRpc:
                        NetworkBehaviour_BeginSendServerRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case k_NetworkBehaviour_EndSendServerRpc:
                        NetworkBehaviour_EndSendServerRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case k_NetworkBehaviour_BeginSendClientRpc:
                        NetworkBehaviour_BeginSendClientRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case k_NetworkBehaviour_EndSendClientRpc:
                        NetworkBehaviour_EndSendClientRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }

            foreach (var fieldInfo in networkBehaviourType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (fieldInfo.Name)
                {
                    case k_NetworkBehaviour_nexec:
                        NetworkBehaviour_nexec_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

#pragma warning disable 618
            var networkHandlerDelegateType = typeof(Action<NetworkBehaviour, NetworkSerializer, __RpcParams>);
            NetworkHandlerDelegateCtor_MethodRef = moduleDefinition.ImportReference(networkHandlerDelegateType.GetConstructor(new[] { typeof(object), typeof(IntPtr) }));

            var rpcParamsType = typeof(__RpcParams);
            RpcParams_TypeRef = moduleDefinition.ImportReference(rpcParamsType);
            foreach (var fieldInfo in rpcParamsType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case k_RpcParams_Server:
                        RpcParams_Server_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                    case k_RpcParams_Client:
                        RpcParams_Client_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }
#pragma warning restore 618

            var serverRpcParamsType = typeof(ServerRpcParams);
            ServerRpcParams_TypeRef = moduleDefinition.ImportReference(serverRpcParamsType);
            foreach (var fieldInfo in serverRpcParamsType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case k_ServerRpcParams_Receive:
                        foreach (var recvFieldInfo in fieldInfo.FieldType.GetFields())
                        {
                            switch (recvFieldInfo.Name)
                            {
                                case k_ServerRpcReceiveParams_SenderClientId:
                                    ServerRpcParams_Receive_SenderClientId_FieldRef = moduleDefinition.ImportReference(recvFieldInfo);
                                    break;
                            }
                        }

                        ServerRpcParams_Receive_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var clientRpcParamsType = typeof(ClientRpcParams);
            ClientRpcParams_TypeRef = moduleDefinition.ImportReference(clientRpcParamsType);

            var networkSerializerType = typeof(NetworkSerializer);
            NetworkSerializer_TypeRef = moduleDefinition.ImportReference(networkSerializerType);
            foreach (var methodInfo in networkSerializerType.GetMethods())
            {
                if (methodInfo.Name != nameof(NetworkSerializer.Serialize)) continue;
                var methodParams = methodInfo.GetParameters();
                if (methodParams.Length != 1) continue;
                var paramType = methodParams[0].ParameterType;
                if (paramType.IsByRef == false) continue;
                var paramTypeName = paramType.Name;

                if (paramTypeName == typeof(bool).MakeByRefType().Name) NetworkSerializer_SerializeBool_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(char).MakeByRefType().Name) NetworkSerializer_SerializeChar_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(sbyte).MakeByRefType().Name) NetworkSerializer_SerializeSbyte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(byte).MakeByRefType().Name) NetworkSerializer_SerializeByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(short).MakeByRefType().Name) NetworkSerializer_SerializeShort_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(ushort).MakeByRefType().Name) NetworkSerializer_SerializeUshort_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(int).MakeByRefType().Name) NetworkSerializer_SerializeInt_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(uint).MakeByRefType().Name) NetworkSerializer_SerializeUint_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(long).MakeByRefType().Name) NetworkSerializer_SerializeLong_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(ulong).MakeByRefType().Name) NetworkSerializer_SerializeUlong_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(float).MakeByRefType().Name) NetworkSerializer_SerializeFloat_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(double).MakeByRefType().Name) NetworkSerializer_SerializeDouble_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(string).MakeByRefType().Name) NetworkSerializer_SerializeString_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Color).MakeByRefType().Name) NetworkSerializer_SerializeColor_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Color32).MakeByRefType().Name) NetworkSerializer_SerializeColor32_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Vector2).MakeByRefType().Name) NetworkSerializer_SerializeVector2_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Vector3).MakeByRefType().Name) NetworkSerializer_SerializeVector3_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Vector4).MakeByRefType().Name) NetworkSerializer_SerializeVector4_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Quaternion).MakeByRefType().Name) NetworkSerializer_SerializeQuaternion_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Ray).MakeByRefType().Name) NetworkSerializer_SerializeRay_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Ray2D).MakeByRefType().Name) NetworkSerializer_SerializeRay2D_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(bool[]).MakeByRefType().Name) NetworkSerializer_SerializeBoolArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(char[]).MakeByRefType().Name) NetworkSerializer_SerializeCharArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(sbyte[]).MakeByRefType().Name) NetworkSerializer_SerializeSbyteArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(byte[]).MakeByRefType().Name) NetworkSerializer_SerializeByteArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(short[]).MakeByRefType().Name) NetworkSerializer_SerializeShortArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(ushort[]).MakeByRefType().Name) NetworkSerializer_SerializeUshortArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(int[]).MakeByRefType().Name) NetworkSerializer_SerializeIntArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(uint[]).MakeByRefType().Name) NetworkSerializer_SerializeUintArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(long[]).MakeByRefType().Name) NetworkSerializer_SerializeLongArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(ulong[]).MakeByRefType().Name) NetworkSerializer_SerializeUlongArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(float[]).MakeByRefType().Name) NetworkSerializer_SerializeFloatArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(double[]).MakeByRefType().Name) NetworkSerializer_SerializeDoubleArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(string[]).MakeByRefType().Name) NetworkSerializer_SerializeStringArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Color[]).MakeByRefType().Name) NetworkSerializer_SerializeColorArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Color32[]).MakeByRefType().Name) NetworkSerializer_SerializeColor32Array_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Vector2[]).MakeByRefType().Name) NetworkSerializer_SerializeVector2Array_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Vector3[]).MakeByRefType().Name) NetworkSerializer_SerializeVector3Array_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Vector4[]).MakeByRefType().Name) NetworkSerializer_SerializeVector4Array_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Quaternion[]).MakeByRefType().Name) NetworkSerializer_SerializeQuaternionArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Ray[]).MakeByRefType().Name) NetworkSerializer_SerializeRayArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
                else if (paramTypeName == typeof(Ray2D[]).MakeByRefType().Name) NetworkSerializer_SerializeRay2DArray_MethodRef = moduleDefinition.ImportReference(methodInfo);
            }

            return true;
        }

        private void ProcessNetworkBehaviour(TypeDefinition typeDefinition)
        {
            var staticHandlers = new List<(uint Hash, MethodDefinition Method)>();
            foreach (var methodDefinition in typeDefinition.Methods)
            {
                var rpcAttribute = CheckAndGetRPCAttribute(methodDefinition);
                if (rpcAttribute == null) continue;

                var methodDefHash = methodDefinition.Hash();
                if (methodDefHash == 0) continue;

                InjectWriteAndCallBlocks(methodDefinition, rpcAttribute, methodDefHash);
                staticHandlers.Add((methodDefHash, GenerateStaticHandler(methodDefinition, rpcAttribute)));
            }

            if (staticHandlers.Count > 0)
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
                foreach (var (hash, method) in staticHandlers)
                {
                    if (hash == 0 || method == null) continue;

                    typeDefinition.Methods.Add(method);

                    // NetworkManager.__ntable.Add(HandlerHash, HandlerMethod);
                    instructions.Add(processor.Create(OpCodes.Ldsfld, NetworkManager_ntable_FieldRef));
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)hash)));
                    instructions.Add(processor.Create(OpCodes.Ldnull));
                    instructions.Add(processor.Create(OpCodes.Ldftn, method));
                    instructions.Add(processor.Create(OpCodes.Newobj, NetworkHandlerDelegateCtor_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Call, NetworkManager_ntable_Add_MethodRef));
                }

                instructions.Reverse();
                instructions.ForEach(instruction => processor.Body.Instructions.Insert(0, instruction));
            }

            // process nested `NetworkBehaviour` types
            typeDefinition.NestedTypes
                .Where(t => t.IsSubclassOf(CodeGenHelpers.NetworkBehaviour_FullName))
                .ToList()
                .ForEach(ProcessNetworkBehaviour);
        }

        private CustomAttribute CheckAndGetRPCAttribute(MethodDefinition methodDefinition)
        {
            CustomAttribute rpcAttribute = null;
            bool isServerRpc = false;
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
                        isServerRpc = customAttributeType_FullName == CodeGenHelpers.ServerRpcAttribute_FullName;
                        rpcAttribute = customAttribute;
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

            int paramCount = methodDefinition.Parameters.Count;
            for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
            {
                var paramDef = methodDefinition.Parameters[paramIndex];
                var paramType = paramDef.ParameterType;

                // Serializable
                if (paramType.IsSerializable()) continue;
                // ServerRpcParams
                if (paramType.FullName == CodeGenHelpers.ServerRpcParams_FullName && isServerRpc && paramIndex == paramCount - 1) continue;
                // ClientRpcParams
                if (paramType.FullName == CodeGenHelpers.ClientRpcParams_FullName && !isServerRpc && paramIndex == paramCount - 1) continue;

                m_Diagnostics.AddError(methodDefinition, $"RPC method parameter does not support serialization: {paramType.FullName}");
                rpcAttribute = null;
            }

            return rpcAttribute;
        }

        private void InjectWriteAndCallBlocks(MethodDefinition methodDefinition, CustomAttribute rpcAttribute, uint methodDefHash)
        {
            var typeSystem = methodDefinition.Module.TypeSystem;
            var instructions = new List<Instruction>();
            var processor = methodDefinition.Body.GetILProcessor();
            var isServerRpc = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRpcAttribute_FullName;
            var requireOwnership = true; // default value MUST be = `ServerRpcAttribute.RequireOwnership`
            var rpcDelivery = RpcDelivery.Reliable; // default value MUST be = `RpcAttribute.Delivery`
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
            methodDefinition.Body.Variables.Add(new VariableDefinition(NetworkManager_TypeRef));
            int netManLocIdx = methodDefinition.Body.Variables.Count - 1;
            // NetworkSerializer serializer;
            methodDefinition.Body.Variables.Add(new VariableDefinition(NetworkSerializer_TypeRef));
            int serializerLocIdx = methodDefinition.Body.Variables.Count - 1;
            // uint methodHash;
            methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt32));
            int methodHashLocIdx = methodDefinition.Body.Variables.Count - 1;
            // XXXRpcParams
            if (!hasRpcParams) methodDefinition.Body.Variables.Add(new VariableDefinition(isServerRpc ? ServerRpcParams_TypeRef : ClientRpcParams_TypeRef));
            int rpcParamsIdx = !hasRpcParams ? methodDefinition.Body.Variables.Count - 1 : -1;

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var lastInstr = processor.Create(OpCodes.Nop);

                // networkManager = this.NetworkManager;
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_getNetworkManager_MethodRef));
                instructions.Add(processor.Create(OpCodes.Stloc, netManLocIdx));

                // if (networkManager == null || !networkManager.IsListening) return;
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Brfalse, returnInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getIsListening_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, lastInstr));

                instructions.Add(returnInstr);
                instructions.Add(lastInstr);
            }

            {
                var beginInstr = processor.Create(OpCodes.Nop);
                var endInstr = processor.Create(OpCodes.Nop);
                var lastInstr = processor.Create(OpCodes.Nop);

                // if (__nexec != NExec.Server) -> ServerRpc
                // if (__nexec != NExec.Client) -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Ldfld, NetworkBehaviour_nexec_FieldRef));
#pragma warning disable 618
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkBehaviour.__NExec.Server : NetworkBehaviour.__NExec.Client)));
#pragma warning restore 618
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, 0));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Brfalse, lastInstr));

                // if (networkManager.IsClient || networkManager.IsHost) { ... } -> ServerRpc
                // if (networkManager.IsServer || networkManager.IsHost) { ... } -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, isServerRpc ? NetworkManager_getIsClient_MethodRef : NetworkManager_getIsServer_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, beginInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getIsHost_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brfalse, lastInstr));

                instructions.Add(beginInstr);

                // var serializer = BeginSendServerRpc(serverRpcParams, rpcDelivery) -> ServerRpc
                // var serializer = BeginSendClientRpc(clientRpcParams, rpcDelivery) -> ClientRpc
                if (isServerRpc)
                {
                    // ServerRpc

                    if (requireOwnership)
                    {
                        var roReturnInstr = processor.Create(OpCodes.Ret);
                        var roLastInstr = processor.Create(OpCodes.Nop);

                        // if (this.OwnerClientId != networkManager.LocalClientId) return;
                        instructions.Add(processor.Create(OpCodes.Ldarg_0));
                        instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_getOwnerClientId_MethodRef));
                        instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getLocalClientId_MethodRef));
                        instructions.Add(processor.Create(OpCodes.Ceq));
                        instructions.Add(processor.Create(OpCodes.Ldc_I4, 0));
                        instructions.Add(processor.Create(OpCodes.Ceq));
                        instructions.Add(processor.Create(OpCodes.Brfalse, roLastInstr));

                        instructions.Add(roReturnInstr);
                        instructions.Add(roLastInstr);
                    }

                    // var serializer = BeginSendServerRpc(serverRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // rpcParams
                    instructions.Add(hasRpcParams ? processor.Create(OpCodes.Ldarg, paramCount) : processor.Create(OpCodes.Ldloc, rpcParamsIdx));

                    // rpcDelivery
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)rpcDelivery));

                    // BeginSendServerRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_BeginSendServerRpc_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, serializerLocIdx));
                }
                else
                {
                    // ClientRpc
                    // var serializer = BeginSendClientRpc(clientRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // rpcParams
                    instructions.Add(hasRpcParams ? processor.Create(OpCodes.Ldarg, paramCount) : processor.Create(OpCodes.Ldloc, rpcParamsIdx));

                    // rpcDelivery
                    instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)rpcDelivery));

                    // BeginSendClientRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_BeginSendClientRpc_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, serializerLocIdx));
                }

                // if (serializer != null)
                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                instructions.Add(processor.Create(OpCodes.Brfalse, endInstr));

                // methodHash = methodDefHash
                instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)methodDefHash)));
                instructions.Add(processor.Create(OpCodes.Stloc, methodHashLocIdx));
                // serializer.Serialize(ref methodHash); // NetworkMethodId
                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                instructions.Add(processor.Create(OpCodes.Ldloca, methodHashLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef));

                // write method parameters into stream
                for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
                {
                    var paramDef = methodDefinition.Parameters[paramIndex];
                    var paramType = paramDef.ParameterType;

                    // C# primitives (+arrays)

                    if (paramType == typeSystem.Boolean)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeBool_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Boolean)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeBoolArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Char)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeChar_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Char)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeCharArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.SByte)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeSbyte_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.SByte)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeSbyteArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Byte)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeByte_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Byte)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeByteArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Int16)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeShort_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Int16)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeShortArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.UInt16)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUshort_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.UInt16)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUshortArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Int32)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Int32)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeIntArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.UInt32)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.UInt32)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUintArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Int64)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeLong_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Int64)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeLongArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.UInt64)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUlong_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.UInt64)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUlongArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Single)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeFloat_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Single)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeFloatArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Double)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeDouble_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.Double)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeDoubleArray_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.String)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeString_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType() == typeSystem.String)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeStringArray_MethodRef));
                        continue;
                    }

                    // Unity primitives (+arrays)

                    if (paramType.FullName == CodeGenHelpers.UnityColor_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeColor_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityColor_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeColorArray_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityColor32_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeColor32_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityColor32_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeColor32Array_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityVector2_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeVector2_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityVector2_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeVector2Array_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityVector3_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeVector3_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityVector3_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeVector3Array_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityVector4_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeVector4_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityVector4_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeVector4Array_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityQuaternion_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeQuaternion_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityQuaternion_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeQuaternionArray_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityRay_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeRay_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityRay_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeRayArray_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityRay2D_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeRay2D_MethodRef));
                        continue;
                    }

                    if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityRay2D_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeRay2DArray_MethodRef));
                        continue;
                    }

                    // Enum

                    {
                        var paramEnumIntType = paramType.GetEnumAsInt();
                        if (paramEnumIntType != null)
                        {
                            if (paramEnumIntType == typeSystem.Int32)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.UInt32)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt32));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.Byte)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Byte));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeByte_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.SByte)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.SByte));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeSbyte_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.Int16)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int16));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeShort_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.UInt16)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt16));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUshort_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.Int64)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int64));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeLong_MethodRef));
                                continue;
                            }

                            if (paramEnumIntType == typeSystem.UInt64)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt64));
                                int localIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Stloc, localIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, localIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUlong_MethodRef));
                                continue;
                            }
                        }
                    }

                    // Enum array

                    if (paramType.IsArray)
                    {
                        var paramElemEnumIntType = paramType.GetElementType().GetEnumAsInt();
                        if (paramElemEnumIntType != null)
                        {
                            methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                            int arrLenLocalIndex = methodDefinition.Body.Variables.Count - 1;

                            var endifInstr = processor.Create(OpCodes.Nop);
                            var arrLenInstr = processor.Create(OpCodes.Nop);

                            instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                            instructions.Add(processor.Create(OpCodes.Brtrue, arrLenInstr));
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_M1));
                            instructions.Add(processor.Create(OpCodes.Br, endifInstr));
                            instructions.Add(arrLenInstr);
                            instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                            instructions.Add(processor.Create(OpCodes.Ldlen));
                            instructions.Add(processor.Create(OpCodes.Conv_I4));
                            instructions.Add(endifInstr);
                            instructions.Add(processor.Create(OpCodes.Stloc, arrLenLocalIndex));

                            instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                            instructions.Add(processor.Create(OpCodes.Ldloca, arrLenLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef));

                            methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                            int counterLocalIndex = methodDefinition.Body.Variables.Count - 1;

                            var forBodyInstr = processor.Create(OpCodes.Nop);
                            var forCheckInstr = processor.Create(OpCodes.Nop);

                            instructions.Add(processor.Create(OpCodes.Ldc_I4_0));
                            instructions.Add(processor.Create(OpCodes.Stloc, counterLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Br, forCheckInstr));
                            instructions.Add(forBodyInstr);

                            if (paramElemEnumIntType == typeSystem.Int32)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_I4));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.UInt32)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt32));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_U4));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.Byte)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Byte));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_U1));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeByte_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.SByte)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.SByte));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_I1));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeSbyte_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.Int16)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int16));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_I2));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeShort_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.UInt16)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt16));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_U2));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUshort_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.Int64)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int64));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_I8));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeLong_MethodRef));
                            }
                            else if (paramElemEnumIntType == typeSystem.UInt64)
                            {
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.UInt64));
                                int enumValLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_I8));
                                instructions.Add(processor.Create(OpCodes.Stloc, enumValLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, enumValLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeUlong_MethodRef));
                            }

                            instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_1));
                            instructions.Add(processor.Create(OpCodes.Add));
                            instructions.Add(processor.Create(OpCodes.Stloc, counterLocalIndex));
                            instructions.Add(forCheckInstr);
                            instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Ldloc, arrLenLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Clt));
                            instructions.Add(processor.Create(OpCodes.Brtrue, forBodyInstr));

                            continue;
                        }
                    }

                    // INetworkSerializable

                    if (paramType.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                    {
                        var paramTypeDef = paramType.Resolve();
                        var paramTypeNetworkSerialize_MethodDef = paramTypeDef.Methods.FirstOrDefault(m => m.Name == CodeGenHelpers.INetworkSerializable_NetworkSerialize_Name);
                        if (paramTypeNetworkSerialize_MethodDef != null)
                        {
                            if (paramType.IsValueType)
                            {
                                // struct (pass by value)
                                instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Call, paramTypeNetworkSerialize_MethodDef));
                            }
                            else
                            {
                                // class (pass by reference)
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                                int isSetLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldnull));
                                instructions.Add(processor.Create(OpCodes.Cgt_Un));
                                instructions.Add(processor.Create(OpCodes.Stloc, isSetLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, isSetLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeBool_MethodRef));

                                var notSetInstr = processor.Create(OpCodes.Nop);

                                instructions.Add(processor.Create(OpCodes.Ldloc, isSetLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Brfalse, notSetInstr));

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Callvirt, paramTypeNetworkSerialize_MethodDef));

                                instructions.Add(notSetInstr);
                            }

                            continue;
                        }
                    }

                    // INetworkSerializable[]
                    if (paramType.IsArray && paramType.GetElementType().HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                    {
                        var paramElemType = paramType.GetElementType();
                        var paramElemTypeDef = paramElemType.Resolve();
                        var paramElemNetworkSerialize_MethodDef = paramElemTypeDef.Methods.FirstOrDefault(m => m.Name == CodeGenHelpers.INetworkSerializable_NetworkSerialize_Name);
                        if (paramElemNetworkSerialize_MethodDef != null)
                        {
                            methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                            int arrLenLocalIndex = methodDefinition.Body.Variables.Count - 1;

                            var endifInstr = processor.Create(OpCodes.Nop);
                            var arrLenInstr = processor.Create(OpCodes.Nop);

                            instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                            instructions.Add(processor.Create(OpCodes.Brtrue, arrLenInstr));
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_M1));
                            instructions.Add(processor.Create(OpCodes.Br, endifInstr));
                            instructions.Add(arrLenInstr);
                            instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                            instructions.Add(processor.Create(OpCodes.Ldlen));
                            instructions.Add(processor.Create(OpCodes.Conv_I4));
                            instructions.Add(endifInstr);
                            instructions.Add(processor.Create(OpCodes.Stloc, arrLenLocalIndex));

                            instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                            instructions.Add(processor.Create(OpCodes.Ldloca, arrLenLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef));

                            methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                            int counterLocalIndex = methodDefinition.Body.Variables.Count - 1;

                            var forBodyInstr = processor.Create(OpCodes.Nop);
                            var forCheckInstr = processor.Create(OpCodes.Nop);

                            instructions.Add(processor.Create(OpCodes.Ldc_I4_0));
                            instructions.Add(processor.Create(OpCodes.Stloc, counterLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Br, forCheckInstr));
                            instructions.Add(forBodyInstr);

                            if (paramElemType.IsValueType)
                            {
                                // struct (pass by value)
                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelema, paramElemType));
                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Call, paramElemNetworkSerialize_MethodDef));
                            }
                            else
                            {
                                // class (pass by reference)
                                methodDefinition.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                                int isSetLocalIndex = methodDefinition.Body.Variables.Count - 1;

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_Ref));
                                instructions.Add(processor.Create(OpCodes.Ldnull));
                                instructions.Add(processor.Create(OpCodes.Cgt_Un));
                                instructions.Add(processor.Create(OpCodes.Stloc, isSetLocalIndex));

                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Ldloca, isSetLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkSerializer_SerializeBool_MethodRef));

                                var notSetInstr = processor.Create(OpCodes.Nop);

                                instructions.Add(processor.Create(OpCodes.Ldloc, isSetLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Brfalse, notSetInstr));

                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                                instructions.Add(processor.Create(OpCodes.Ldelem_Ref));
                                instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Callvirt, paramElemNetworkSerialize_MethodDef));

                                instructions.Add(notSetInstr);
                            }

                            instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Ldc_I4_1));
                            instructions.Add(processor.Create(OpCodes.Add));
                            instructions.Add(processor.Create(OpCodes.Stloc, counterLocalIndex));
                            instructions.Add(forCheckInstr);
                            instructions.Add(processor.Create(OpCodes.Ldloc, counterLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Ldloc, arrLenLocalIndex));
                            instructions.Add(processor.Create(OpCodes.Clt));
                            instructions.Add(processor.Create(OpCodes.Brtrue, forBodyInstr));

                            continue;
                        }
                    }
                }

                instructions.Add(endInstr);

                // EndSendServerRpc(serializer, serverRpcParams, rpcDelivery) -> ServerRpc
                // EndSendClientRpc(serializer, clientRpcParams, rpcDelivery) -> ClientRpc
                if (isServerRpc)
                {
                    // ServerRpc
                    // EndSendServerRpc(serializer, serverRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // serializer
                    instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));

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

                    // EndSendServerRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_EndSendServerRpc_MethodRef));
                }
                else
                {
                    // ClientRpc
                    // EndSendClientRpc(serializer, clientRpcParams, rpcDelivery);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // serializer
                    instructions.Add(processor.Create(OpCodes.Ldloc, serializerLocIdx));

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

                    // EndSendClientRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_EndSendClientRpc_MethodRef));
                }

                instructions.Add(lastInstr);
            }

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var lastInstr = processor.Create(OpCodes.Nop);

                // if (__nexec == NExec.Server) -> ServerRpc
                // if (__nexec == NExec.Client) -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Ldfld, NetworkBehaviour_nexec_FieldRef));
#pragma warning disable 618
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkBehaviour.__NExec.Server : NetworkBehaviour.__NExec.Client)));
#pragma warning restore 618
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Brfalse, returnInstr));

                // if (networkManager.IsServer || networkManager.IsHost) -> ServerRpc
                // if (networkManager.IsClient || networkManager.IsHost) -> ClientRpc
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, isServerRpc ? NetworkManager_getIsServer_MethodRef : NetworkManager_getIsClient_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, lastInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getIsHost_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, lastInstr));

                instructions.Add(returnInstr);
                instructions.Add(lastInstr);
            }

            instructions.Reverse();
            instructions.ForEach(instruction => processor.Body.Instructions.Insert(0, instruction));
        }

        private MethodDefinition GenerateStaticHandler(MethodDefinition methodDefinition, CustomAttribute rpcAttribute)
        {
            var typeSystem = methodDefinition.Module.TypeSystem;
            var nhandler = new MethodDefinition(
                $"{methodDefinition.Name}__nhandler",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                methodDefinition.Module.TypeSystem.Void);
            nhandler.Parameters.Add(new ParameterDefinition("target", ParameterAttributes.None, NetworkBehaviour_TypeRef));
            nhandler.Parameters.Add(new ParameterDefinition("serializer", ParameterAttributes.None, NetworkSerializer_TypeRef));
            nhandler.Parameters.Add(new ParameterDefinition("rpcParams", ParameterAttributes.None, RpcParams_TypeRef));

            var processor = nhandler.Body.GetILProcessor();
            var isServerRpc = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRpcAttribute_FullName;
            var requireOwnership = true; // default value MUST be = `ServerRpcAttribute.RequireOwnership`
            foreach (var attrField in rpcAttribute.Fields)
            {
                switch (attrField.Name)
                {
                    case k_ServerRpcAttribute_RequireOwnership:
                        requireOwnership = attrField.Argument.Type == typeSystem.Boolean && (bool)attrField.Argument.Value;
                        break;
                }
            }
            
            nhandler.Body.InitLocals = true;

            if (isServerRpc && requireOwnership)
            {
                var roReturnInstr = processor.Create(OpCodes.Ret);
                var roLastInstr = processor.Create(OpCodes.Nop);

                // if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId) return;
                processor.Emit(OpCodes.Ldarg_2);
                processor.Emit(OpCodes.Ldfld, RpcParams_Server_FieldRef);
                processor.Emit(OpCodes.Ldfld, ServerRpcParams_Receive_FieldRef);
                processor.Emit(OpCodes.Ldfld, ServerRpcParams_Receive_SenderClientId_FieldRef);
                processor.Emit(OpCodes.Ldarg_0);
                processor.Emit(OpCodes.Call, NetworkBehaviour_getOwnerClientId_MethodRef);
                processor.Emit(OpCodes.Ceq);
                processor.Emit(OpCodes.Ldc_I4, 0);
                processor.Emit(OpCodes.Ceq);
                processor.Emit(OpCodes.Brfalse, roLastInstr);

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
                nhandler.Body.Variables.Add(new VariableDefinition(paramType));
                int localIndex = nhandler.Body.Variables.Count - 1;
                paramLocalMap[paramIndex] = localIndex;

                // C# primitives (+arrays)

                if (paramType == typeSystem.Boolean)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeBool_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Boolean)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeBoolArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Char)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeChar_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Char)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeCharArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.SByte)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeSbyte_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.SByte)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeSbyteArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Byte)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeByte_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Byte)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeByteArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Int16)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeShort_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Int16)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeShortArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.UInt16)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUshort_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.UInt16)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUshortArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Int32)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Int32)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeIntArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.UInt32)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.UInt32)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUintArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Int64)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeLong_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Int64)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeLongArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.UInt64)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUlong_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.UInt64)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUlongArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Single)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeFloat_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Single)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeFloatArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.Double)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeDouble_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.Double)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeDoubleArray_MethodRef);
                    continue;
                }

                if (paramType == typeSystem.String)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeString_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType() == typeSystem.String)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeStringArray_MethodRef);
                    continue;
                }

                // Unity primitives (+arrays)

                if (paramType.FullName == CodeGenHelpers.UnityColor_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeColor_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityColor_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeColorArray_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityColor32_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeColor32_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityColor32_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeColor32Array_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityVector2_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeVector2_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityVector2_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeVector2Array_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityVector3_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeVector3_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityVector3_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeVector3Array_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityVector4_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeVector4_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityVector4_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeVector4Array_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityQuaternion_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeQuaternion_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityQuaternion_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeQuaternionArray_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityRay_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeRay_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityRay_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeRayArray_MethodRef);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityRay2D_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeRay2D_MethodRef);
                    continue;
                }

                if (paramType.IsArray && paramType.GetElementType().FullName == CodeGenHelpers.UnityRay2D_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldloca, localIndex);
                    processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeRay2DArray_MethodRef);
                    continue;
                }

                // Enum

                {
                    var paramEnumIntType = paramType.GetEnumAsInt();
                    if (paramEnumIntType != null)
                    {
                        if (paramEnumIntType == typeSystem.Int32)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.UInt32)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.UInt32));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.Byte)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Byte));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeByte_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.SByte)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.SByte));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeSbyte_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.Int16)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int16));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeShort_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.UInt16)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.UInt16));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUshort_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.Int64)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int64));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeLong_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }

                        if (paramEnumIntType == typeSystem.UInt64)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.UInt64));
                            int enumLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUlong_MethodRef);

                            processor.Emit(OpCodes.Ldloc, enumLocalIndex);
                            processor.Emit(OpCodes.Stloc, localIndex);
                            continue;
                        }
                    }
                }

                // Enum array

                if (paramType.IsArray)
                {
                    var paramElemEnumIntType = paramType.GetElementType().GetEnumAsInt();
                    if (paramElemEnumIntType != null)
                    {
                        nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                        int arrLenLocalIndex = nhandler.Body.Variables.Count - 1;

                        processor.Emit(OpCodes.Ldarg_1);
                        processor.Emit(OpCodes.Ldloca, arrLenLocalIndex);
                        processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef);

                        var postForInstr = processor.Create(OpCodes.Nop);

                        processor.Emit(OpCodes.Ldloc, arrLenLocalIndex);
                        processor.Emit(OpCodes.Ldc_I4_M1);
                        processor.Emit(OpCodes.Cgt);
                        processor.Emit(OpCodes.Brfalse, postForInstr);

                        processor.Emit(OpCodes.Ldloc, arrLenLocalIndex);
                        processor.Emit(OpCodes.Newarr, paramType.GetElementType());
                        processor.Emit(OpCodes.Stloc, localIndex);

                        nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                        int counterLocalIndex = nhandler.Body.Variables.Count - 1;

                        var forBodyInstr = processor.Create(OpCodes.Nop);
                        var forCheckInstr = processor.Create(OpCodes.Nop);

                        processor.Emit(OpCodes.Ldc_I4_0);
                        processor.Emit(OpCodes.Stloc, counterLocalIndex);
                        processor.Emit(OpCodes.Br, forCheckInstr);
                        processor.Append(forBodyInstr);

                        if (paramElemEnumIntType == typeSystem.Int32)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I4);
                        }
                        else if (paramElemEnumIntType == typeSystem.UInt32)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.UInt32));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUint_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I4);
                        }
                        else if (paramElemEnumIntType == typeSystem.Byte)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Byte));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeByte_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I1);
                        }
                        else if (paramElemEnumIntType == typeSystem.SByte)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.SByte));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeSbyte_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I1);
                        }
                        else if (paramElemEnumIntType == typeSystem.Int16)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int16));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeShort_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I2);
                        }
                        else if (paramElemEnumIntType == typeSystem.UInt16)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.UInt16));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUshort_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I2);
                        }
                        else if (paramElemEnumIntType == typeSystem.Int64)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int64));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeLong_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I8);
                        }
                        else if (paramElemEnumIntType == typeSystem.UInt64)
                        {
                            nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.UInt64));
                            int enumValLocalIndex = nhandler.Body.Variables.Count - 1;

                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Ldloca, enumValLocalIndex);
                            processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeUlong_MethodRef);

                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldloc, enumValLocalIndex);
                            processor.Emit(OpCodes.Stelem_I8);
                        }

                        processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                        processor.Emit(OpCodes.Ldc_I4_1);
                        processor.Emit(OpCodes.Add);
                        processor.Emit(OpCodes.Stloc, counterLocalIndex);
                        processor.Append(forCheckInstr);
                        processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                        processor.Emit(OpCodes.Ldloc, arrLenLocalIndex);
                        processor.Emit(OpCodes.Clt);
                        processor.Emit(OpCodes.Brtrue, forBodyInstr);

                        processor.Append(postForInstr);
                        continue;
                    }
                }

                // INetworkSerializable

                if (paramType.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                {
                    var paramTypeDef = paramType.Resolve();
                    var paramTypeNetworkSerialize_MethodDef = paramTypeDef.Methods.FirstOrDefault(m => m.Name == CodeGenHelpers.INetworkSerializable_NetworkSerialize_Name);
                    if (paramTypeNetworkSerialize_MethodDef != null)
                    {
                        if (paramType.IsValueType)
                        {
                            // struct (pass by value)
                            processor.Emit(OpCodes.Ldloca, localIndex);
                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Call, paramTypeNetworkSerialize_MethodDef);
                        }
                        else
                        {
                            // class (pass by reference)
                            var paramTypeDefCtor = paramTypeDef.GetConstructors().FirstOrDefault(m => m.Parameters.Count == 0);
                            if (paramTypeDefCtor != null)
                            {
                                nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                                int isSetLocalIndex = nhandler.Body.Variables.Count - 1;

                                processor.Emit(OpCodes.Ldarg_1);
                                processor.Emit(OpCodes.Ldloca, isSetLocalIndex);
                                processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeBool_MethodRef);

                                var notSetInstr = processor.Create(OpCodes.Nop);

                                processor.Emit(OpCodes.Ldloc, isSetLocalIndex);
                                processor.Emit(OpCodes.Brfalse, notSetInstr);

                                // new INetworkSerializable()
                                processor.Emit(OpCodes.Newobj, paramTypeDefCtor);
                                processor.Emit(OpCodes.Stloc, localIndex);

                                // INetworkSerializable.NetworkSerialize(serializer)
                                processor.Emit(OpCodes.Ldloc, localIndex);
                                processor.Emit(OpCodes.Ldarg_1);
                                processor.Emit(OpCodes.Callvirt, paramTypeNetworkSerialize_MethodDef);

                                processor.Append(notSetInstr);
                            }
                        }

                        continue;
                    }
                }

                // INetworkSerializable[]
                if (paramType.IsArray && paramType.GetElementType().HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                {
                    var paramElemType = paramType.GetElementType();
                    var paramElemTypeDef = paramElemType.Resolve();
                    var paramElemNetworkSerialize_MethodDef = paramElemTypeDef.Methods.FirstOrDefault(m => m.Name == CodeGenHelpers.INetworkSerializable_NetworkSerialize_Name);
                    if (paramElemNetworkSerialize_MethodDef != null)
                    {
                        nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                        int arrLenLocalIndex = nhandler.Body.Variables.Count - 1;

                        processor.Emit(OpCodes.Ldarg_1);
                        processor.Emit(OpCodes.Ldloca, arrLenLocalIndex);
                        processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeInt_MethodRef);

                        var postForInstr = processor.Create(OpCodes.Nop);

                        processor.Emit(OpCodes.Ldloc, arrLenLocalIndex);
                        processor.Emit(OpCodes.Ldc_I4_M1);
                        processor.Emit(OpCodes.Cgt);
                        processor.Emit(OpCodes.Brfalse, postForInstr);

                        processor.Emit(OpCodes.Ldloc, arrLenLocalIndex);
                        processor.Emit(OpCodes.Newarr, paramElemType);
                        processor.Emit(OpCodes.Stloc, localIndex);

                        nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Int32));
                        int counterLocalIndex = nhandler.Body.Variables.Count - 1;

                        var forBodyInstr = processor.Create(OpCodes.Nop);
                        var forCheckInstr = processor.Create(OpCodes.Nop);

                        processor.Emit(OpCodes.Ldc_I4_0);
                        processor.Emit(OpCodes.Stloc, counterLocalIndex);
                        processor.Emit(OpCodes.Br, forCheckInstr);
                        processor.Append(forBodyInstr);

                        if (paramElemType.IsValueType)
                        {
                            // struct (pass by value)
                            processor.Emit(OpCodes.Ldloc, localIndex);
                            processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                            processor.Emit(OpCodes.Ldelema, paramElemType);
                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Call, paramElemNetworkSerialize_MethodDef);
                        }
                        else
                        {
                            // class (pass by reference)
                            var paramElemTypeDefCtor = paramElemTypeDef.GetConstructors().FirstOrDefault(m => m.Parameters.Count == 0);
                            if (paramElemTypeDefCtor != null)
                            {
                                nhandler.Body.Variables.Add(new VariableDefinition(typeSystem.Boolean));
                                int isSetLocalIndex = nhandler.Body.Variables.Count - 1;

                                processor.Emit(OpCodes.Ldarg_1);
                                processor.Emit(OpCodes.Ldloca, isSetLocalIndex);
                                processor.Emit(OpCodes.Callvirt, NetworkSerializer_SerializeBool_MethodRef);

                                var notSetInstr = processor.Create(OpCodes.Nop);

                                processor.Emit(OpCodes.Ldloc, isSetLocalIndex);
                                processor.Emit(OpCodes.Brfalse, notSetInstr);

                                processor.Emit(OpCodes.Ldloc, localIndex);
                                processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                                processor.Emit(OpCodes.Newobj, paramElemTypeDefCtor);
                                processor.Emit(OpCodes.Stelem_Ref);

                                processor.Emit(OpCodes.Ldloc, localIndex);
                                processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                                processor.Emit(OpCodes.Ldelem_Ref);
                                processor.Emit(OpCodes.Ldarg_1);
                                processor.Emit(OpCodes.Call, paramElemNetworkSerialize_MethodDef);

                                processor.Append(notSetInstr);
                            }
                        }

                        processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                        processor.Emit(OpCodes.Ldc_I4_1);
                        processor.Emit(OpCodes.Add);
                        processor.Emit(OpCodes.Stloc, counterLocalIndex);
                        processor.Append(forCheckInstr);
                        processor.Emit(OpCodes.Ldloc, counterLocalIndex);
                        processor.Emit(OpCodes.Ldloc, arrLenLocalIndex);
                        processor.Emit(OpCodes.Clt);
                        processor.Emit(OpCodes.Brtrue, forBodyInstr);

                        processor.Append(postForInstr);
                        continue;
                    }
                }

                // ServerRpcParams, ClientRpcParams
                {
                    // ServerRpcParams
                    if (paramType.FullName == CodeGenHelpers.ServerRpcParams_FullName)
                    {
                        processor.Emit(OpCodes.Ldarg_2);
                        processor.Emit(OpCodes.Ldfld, RpcParams_Server_FieldRef);
                        processor.Emit(OpCodes.Stloc, localIndex);
                        continue;
                    }

                    // ClientRpcParams
                    if (paramType.FullName == CodeGenHelpers.ClientRpcParams_FullName)
                    {
                        processor.Emit(OpCodes.Ldarg_2);
                        processor.Emit(OpCodes.Ldfld, RpcParams_Client_FieldRef);
                        processor.Emit(OpCodes.Stloc, localIndex);
                        continue;
                    }
                }
            }

            // NetworkBehaviour.__nexec = NExec.Server; -> ServerRpc
            // NetworkBehaviour.__nexec = NExec.Client; -> ClientRpc
            processor.Emit(OpCodes.Ldarg_0);
#pragma warning disable 618
            processor.Emit(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkBehaviour.__NExec.Server : NetworkBehaviour.__NExec.Client));
#pragma warning restore 618
            processor.Emit(OpCodes.Stfld, NetworkBehaviour_nexec_FieldRef);

            // NetworkBehaviour.XXXRpc(...);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Castclass, methodDefinition.DeclaringType);
            Enumerable.Range(0, paramCount).ToList().ForEach(paramIndex => processor.Emit(OpCodes.Ldloc, paramLocalMap[paramIndex]));
            processor.Emit(OpCodes.Callvirt, methodDefinition);

            // NetworkBehaviour.__nexec = NExec.None;
            processor.Emit(OpCodes.Ldarg_0);
#pragma warning disable 618
            processor.Emit(OpCodes.Ldc_I4, (int)NetworkBehaviour.__NExec.None);
#pragma warning restore 618
            processor.Emit(OpCodes.Stfld, NetworkBehaviour_nexec_FieldRef);

            processor.Emit(OpCodes.Ret);
            return nhandler;
        }
    }
}