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
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace MLAPI.Editor.CodeGen
{
    internal sealed class NetworkBehaviourILPP : ILPostProcessor
    {
        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.Name == CodeGenHelpers.RuntimeAssemblyName ||
            compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

        private readonly List<DiagnosticMessage> _diagnostics = new List<DiagnosticMessage>();

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly)) return null;
            _diagnostics.Clear();

            // read
            var assemblyDefinition = CodeGenHelpers.AssemblyDefinitionFor(compiledAssembly);
            if (assemblyDefinition == null)
            {
                _diagnostics.AddError($"Cannot read assembly definition: {compiledAssembly.Name}");
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
                else _diagnostics.AddError($"Cannot import references into main module: {mainModule.Name}");
            }
            else _diagnostics.AddError($"Cannot get main module from assembly definition: {compiledAssembly.Name}");

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

        private TypeReference NetworkManager_TypeRef;
        private FieldReference NetworkManager_ntable_FieldRef;
        private MethodReference NetworkManager_ntable_Add_MethodRef;
        private MethodReference NetworkManager_getSingleton_MethodRef;
        private MethodReference NetworkManager_getIsListening_MethodRef;
        private MethodReference NetworkManager_getIsHost_MethodRef;
        private MethodReference NetworkManager_getIsServer_MethodRef;
        private MethodReference NetworkManager_getIsClient_MethodRef;
        private TypeReference NetworkBehaviour_TypeRef;
        private MethodReference NetworkBehaviour_BeginSendServerRpc_MethodRef;
        private MethodReference NetworkBehaviour_EndSendServerRpc_MethodRef;
        private MethodReference NetworkBehaviour_BeginSendClientRpc_MethodRef;
        private MethodReference NetworkBehaviour_EndSendClientRpc_MethodRef;
        private FieldReference NetworkBehaviour_nexec_FieldRef;
        private MethodReference NetworkHandlerDelegateCtor_MethodRef;
        private TypeReference ServerRpcParams_TypeRef;
        private FieldReference ServerRpcParams_Send_FieldRef;
        private FieldReference ServerRpcParams_Receive_FieldRef;
        private TypeReference ServerRpcSendParams_TypeRef;
        private TypeReference ServerRpcReceiveParams_TypeRef;
        private FieldReference ServerRpcReceiveParams_SenderClientId_FieldRef;
        private TypeReference ClientRpcParams_TypeRef;
        private FieldReference ClientRpcParams_Send_FieldRef;
        private FieldReference ClientRpcParams_Receive_FieldRef;
        private TypeReference ClientRpcSendParams_TypeRef;
        private TypeReference ClientRpcReceiveParams_TypeRef;
        private TypeReference BitWriter_TypeRef;
        private MethodReference BitWriter_WriteBool_MethodRef;
        private MethodReference BitWriter_WriteChar_MethodRef;
        private MethodReference BitWriter_WriteSByte_MethodRef;
        private MethodReference BitWriter_WriteByte_MethodRef;
        private MethodReference BitWriter_WriteInt16Packed_MethodRef;
        private MethodReference BitWriter_WriteUInt16Packed_MethodRef;
        private MethodReference BitWriter_WriteInt32Packed_MethodRef;
        private MethodReference BitWriter_WriteUInt32Packed_MethodRef;
        private MethodReference BitWriter_WriteInt64Packed_MethodRef;
        private MethodReference BitWriter_WriteUInt64Packed_MethodRef;
        private MethodReference BitWriter_WriteSinglePacked_MethodRef;
        private MethodReference BitWriter_WriteDoublePacked_MethodRef;
        private MethodReference BitWriter_WriteStringPacked_MethodRef;
        private MethodReference BitWriter_WriteColorPacked_MethodRef;
        private MethodReference BitWriter_WriteVector2Packed_MethodRef;
        private MethodReference BitWriter_WriteVector3Packed_MethodRef;
        private MethodReference BitWriter_WriteVector4Packed_MethodRef;
        private MethodReference BitWriter_WriteRotationPacked_MethodRef;
        private MethodReference BitWriter_WriteRayPacked_MethodRef;
        private MethodReference BitWriter_WriteRay2DPacked_MethodRef;
        private TypeReference BitReader_TypeRef;
        private MethodReference BitReader_ReadBool_MethodRef;
        private MethodReference BitReader_ReadChar_MethodRef;
        private MethodReference BitReader_ReadSByte_MethodRef;
        private MethodReference BitReader_ReadByte_MethodRef;
        private MethodReference BitReader_ReadInt16Packed_MethodRef;
        private MethodReference BitReader_ReadUInt16Packed_MethodRef;
        private MethodReference BitReader_ReadInt32Packed_MethodRef;
        private MethodReference BitReader_ReadUInt32Packed_MethodRef;
        private MethodReference BitReader_ReadInt64Packed_MethodRef;
        private MethodReference BitReader_ReadUInt64Packed_MethodRef;
        private MethodReference BitReader_ReadSinglePacked_MethodRef;
        private MethodReference BitReader_ReadDoublePacked_MethodRef;
        private MethodReference BitReader_ReadStringPacked_MethodRef;
        private MethodReference BitReader_ReadColorPacked_MethodRef;
        private MethodReference BitReader_ReadVector2Packed_MethodRef;
        private MethodReference BitReader_ReadVector3Packed_MethodRef;
        private MethodReference BitReader_ReadVector4Packed_MethodRef;
        private MethodReference BitReader_ReadRotationPacked_MethodRef;
        private MethodReference BitReader_ReadRayPacked_MethodRef;
        private MethodReference BitReader_ReadRay2DPacked_MethodRef;

        private bool ImportReferences(ModuleDefinition moduleDefinition)
        {
            var networkManagerType = typeof(NetworkingManager);
            NetworkManager_TypeRef = moduleDefinition.ImportReference(networkManagerType);
            foreach (var propertyInfo in networkManagerType.GetProperties())
            {
                switch (propertyInfo.Name)
                {
                    case nameof(NetworkingManager.Singleton):
                        NetworkManager_getSingleton_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case nameof(NetworkingManager.IsListening):
                        NetworkManager_getIsListening_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case nameof(NetworkingManager.IsHost):
                        NetworkManager_getIsHost_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case nameof(NetworkingManager.IsServer):
                        NetworkManager_getIsServer_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                    case nameof(NetworkingManager.IsClient):
                        NetworkManager_getIsClient_MethodRef = moduleDefinition.ImportReference(propertyInfo.GetMethod);
                        break;
                }
            }

            foreach (var fieldInfo in networkManagerType.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (fieldInfo.Name)
                {
                    case nameof(NetworkingManager.__ntable):
                        NetworkManager_ntable_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        NetworkManager_ntable_Add_MethodRef = moduleDefinition.ImportReference(fieldInfo.FieldType.GetMethod("Add"));
                        break;
                }
            }

            var networkBehaviourType = typeof(NetworkedBehaviour);
            NetworkBehaviour_TypeRef = moduleDefinition.ImportReference(networkBehaviourType);
            foreach (var methodInfo in networkBehaviourType.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (methodInfo.Name)
                {
                    case nameof(NetworkedBehaviour.BeginSendServerRpc):
                        NetworkBehaviour_BeginSendServerRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(NetworkedBehaviour.EndSendServerRpc):
                        NetworkBehaviour_EndSendServerRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(NetworkedBehaviour.BeginSendClientRpc):
                        NetworkBehaviour_BeginSendClientRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(NetworkedBehaviour.EndSendClientRpc):
                        NetworkBehaviour_EndSendClientRpc_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }

            foreach (var fieldInfo in networkBehaviourType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (fieldInfo.Name)
                {
                    case nameof(NetworkedBehaviour.__nexec):
                        NetworkBehaviour_nexec_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var networkHandlerDelegateType = typeof(Action<NetworkedBehaviour, BitReader, ulong>);
            NetworkHandlerDelegateCtor_MethodRef = moduleDefinition.ImportReference(
                networkHandlerDelegateType
                    .GetConstructor(new[] {typeof(object), typeof(IntPtr)}));

            var serverRpcParamsType = typeof(ServerRpcParams);
            ServerRpcParams_TypeRef = moduleDefinition.ImportReference(serverRpcParamsType);
            foreach (var fieldInfo in serverRpcParamsType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case nameof(ServerRpcParams.Send):
                        ServerRpcParams_Send_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                    case nameof(ServerRpcParams.Receive):
                        ServerRpcParams_Receive_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var serverRpcSendParamsType = typeof(ServerRpcSendParams);
            ServerRpcSendParams_TypeRef = moduleDefinition.ImportReference(serverRpcSendParamsType);

            var serverRpcReceiveParamsType = typeof(ServerRpcReceiveParams);
            ServerRpcReceiveParams_TypeRef = moduleDefinition.ImportReference(serverRpcReceiveParamsType);
            foreach (var fieldInfo in serverRpcReceiveParamsType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case nameof(ServerRpcReceiveParams.SenderClientId):
                        ServerRpcReceiveParams_SenderClientId_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var clientRpcParamsType = typeof(ClientRpcParams);
            ClientRpcParams_TypeRef = moduleDefinition.ImportReference(clientRpcParamsType);
            foreach (var fieldInfo in clientRpcParamsType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case nameof(ClientRpcParams.Send):
                        ClientRpcParams_Send_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                    case nameof(ClientRpcParams.Receive):
                        ClientRpcParams_Receive_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var clientRpcSendParamsType = typeof(ClientRpcSendParams);
            ClientRpcSendParams_TypeRef = moduleDefinition.ImportReference(clientRpcSendParamsType);

            var clientRpcReceiveParamsType = typeof(ClientRpcReceiveParams);
            ClientRpcReceiveParams_TypeRef = moduleDefinition.ImportReference(clientRpcReceiveParamsType);

            var bitWriterType = typeof(BitWriter);
            BitWriter_TypeRef = moduleDefinition.ImportReference(bitWriterType);
            foreach (var methodInfo in bitWriterType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case nameof(BitWriter.WriteBool):
                        BitWriter_WriteBool_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteChar):
                        BitWriter_WriteChar_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteSByte):
                        BitWriter_WriteSByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteByte):
                        BitWriter_WriteByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteInt16Packed):
                        BitWriter_WriteInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteUInt16Packed):
                        BitWriter_WriteUInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteInt32Packed):
                        BitWriter_WriteInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteUInt32Packed):
                        BitWriter_WriteUInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteInt64Packed):
                        BitWriter_WriteInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteUInt64Packed):
                        BitWriter_WriteUInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteSinglePacked):
                        BitWriter_WriteSinglePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteDoublePacked):
                        BitWriter_WriteDoublePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteStringPacked):
                        BitWriter_WriteStringPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteColorPacked):
                        BitWriter_WriteColorPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteVector2Packed):
                        BitWriter_WriteVector2Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteVector3Packed):
                        BitWriter_WriteVector3Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteVector4Packed):
                        BitWriter_WriteVector4Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteRotationPacked):
                        BitWriter_WriteRotationPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteRayPacked):
                        BitWriter_WriteRayPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitWriter.WriteRay2DPacked):
                        BitWriter_WriteRay2DPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }

            var bitReaderType = typeof(BitReader);
            BitReader_TypeRef = moduleDefinition.ImportReference(bitReaderType);
            foreach (var methodInfo in bitReaderType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case nameof(BitReader.ReadBool):
                        BitReader_ReadBool_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadChar):
                        BitReader_ReadChar_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadSByte):
                        BitReader_ReadSByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadByte):
                        BitReader_ReadByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadInt16Packed):
                        BitReader_ReadInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadUInt16Packed):
                        BitReader_ReadUInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadInt32Packed):
                        BitReader_ReadInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadUInt32Packed):
                        BitReader_ReadUInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadInt64Packed):
                        BitReader_ReadInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadUInt64Packed):
                        BitReader_ReadUInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadSinglePacked):
                        BitReader_ReadSinglePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadDoublePacked):
                        BitReader_ReadDoublePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadStringPacked):
                        BitReader_ReadStringPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadColorPacked):
                        BitReader_ReadColorPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadVector2Packed):
                        BitReader_ReadVector2Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadVector3Packed):
                        BitReader_ReadVector3Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadVector4Packed):
                        BitReader_ReadVector4Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadRotationPacked):
                        BitReader_ReadRotationPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadRayPacked):
                        BitReader_ReadRayPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case nameof(BitReader.ReadRay2DPacked):
                        BitReader_ReadRay2DPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
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
                        _diagnostics.AddError(methodDefinition, "RPC method must not be static!");
                        isValid = false;
                    }

                    if (methodDefinition.IsAbstract)
                    {
                        _diagnostics.AddError(methodDefinition, "RPC method must not be abstract!");
                        isValid = false;
                    }

                    if (methodDefinition.ReturnType != methodDefinition.Module.TypeSystem.Void)
                    {
                        _diagnostics.AddError(methodDefinition, "RPC method must return `void`!");
                        isValid = false;
                    }

                    if (customAttributeType_FullName == CodeGenHelpers.ServerRpcAttribute_FullName &&
                        !methodDefinition.Name.EndsWith("ServerRpc", StringComparison.OrdinalIgnoreCase))
                    {
                        _diagnostics.AddError(methodDefinition, "ServerRpc method must end with 'ServerRpc' suffix!");
                        isValid = false;
                    }

                    if (customAttributeType_FullName == CodeGenHelpers.ClientRpcAttribute_FullName &&
                        !methodDefinition.Name.EndsWith("ClientRpc", StringComparison.OrdinalIgnoreCase))
                    {
                        _diagnostics.AddError(methodDefinition, "ClientRpc method must end with 'ClientRpc' suffix!");
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
                    _diagnostics.AddError(methodDefinition, "ServerRpc method must be marked with 'ServerRpc' attribute!");
                }
                else if (methodDefinition.Name.EndsWith("ClientRpc", StringComparison.OrdinalIgnoreCase))
                {
                    _diagnostics.AddError(methodDefinition, "ClientRpc method must be marked with 'ClientRpc' attribute!");
                }

                return null;
            }

            int paramCount = methodDefinition.Parameters.Count;
            for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
            {
                var paramDef = methodDefinition.Parameters[paramIndex];
                var paramType = paramDef.ParameterType;

                if (paramType.IsSupportedType()) continue;

                // ServerRpcParams
                if (paramType.FullName == CodeGenHelpers.ServerRpcParams_FullName && isServerRpc && paramIndex == paramCount - 1) continue;
                // ClientRpcParams
                if (paramType.FullName == CodeGenHelpers.ClientRpcParams_FullName && !isServerRpc && paramIndex == paramCount - 1) continue;

                _diagnostics.AddError(methodDefinition, $"RPC method parameter does not support serialization: {paramType.FullName}");
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
            var isReliableRpc = true;
            foreach (var attrField in rpcAttribute.Fields)
            {
                switch (attrField.Name)
                {
                    case nameof(RpcAttribute.IsReliable):
                        isReliableRpc = attrField.Argument.Type == typeSystem.Boolean && (bool)attrField.Argument.Value;
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
            // BitWriter writer;
            methodDefinition.Body.Variables.Add(new VariableDefinition(BitWriter_TypeRef));
            int writerLocIdx = methodDefinition.Body.Variables.Count - 1;
            // XXXRpcSendParams
            if (!hasRpcParams) methodDefinition.Body.Variables.Add(new VariableDefinition(isServerRpc ? ServerRpcSendParams_TypeRef : ClientRpcSendParams_TypeRef));
            int sendParamsIdx = !hasRpcParams ? methodDefinition.Body.Variables.Count - 1 : -1;

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var lastInstr = processor.Create(OpCodes.Nop);

                // networkManager = NetworkManager.Singleton;
                instructions.Add(processor.Create(OpCodes.Call, NetworkManager_getSingleton_MethodRef));
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
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkedBehaviour.NExec.Server : NetworkedBehaviour.NExec.Client)));
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

                // var writer = BeginSendServerRpc(sendParams, isReliable) -> ServerRpc
                // var writer = BeginSendClientRpc(sendParams, isReliable) -> ClientRpc
                if (isServerRpc)
                {
                    // ServerRpc
                    // var writer = BeginSendServerRpc(sendParams, isReliable);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    if (hasRpcParams)
                    {
                        // rpcParams.Send
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                        instructions.Add(processor.Create(OpCodes.Ldfld, ServerRpcParams_Send_FieldRef));
                    }
                    else
                    {
                        // default
                        instructions.Add(processor.Create(OpCodes.Ldloca, sendParamsIdx));
                        instructions.Add(processor.Create(OpCodes.Initobj, ServerRpcSendParams_TypeRef));
                        instructions.Add(processor.Create(OpCodes.Ldloc, sendParamsIdx));
                    }

                    // isReliable
                    instructions.Add(processor.Create(isReliableRpc ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

                    // BeginSendServerRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_BeginSendServerRpc_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, writerLocIdx));
                }
                else
                {
                    // ClientRpc
                    // var writer = BeginSendClientRpc(sendParams, isReliable);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    if (hasRpcParams)
                    {
                        // rpcParams.Send
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                        instructions.Add(processor.Create(OpCodes.Ldfld, ClientRpcParams_Send_FieldRef));
                    }
                    else
                    {
                        // default
                        instructions.Add(processor.Create(OpCodes.Ldloca, sendParamsIdx));
                        instructions.Add(processor.Create(OpCodes.Initobj, ClientRpcSendParams_TypeRef));
                        instructions.Add(processor.Create(OpCodes.Ldloc, sendParamsIdx));
                    }

                    // isReliable
                    instructions.Add(processor.Create(isReliableRpc ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

                    // BeginSendClientRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_BeginSendClientRpc_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, writerLocIdx));
                }

                // if (writer != null)
                instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                instructions.Add(processor.Create(OpCodes.Brfalse, endInstr));

                // writer.WriteUInt32Packed(123123); // NetworkMethodId
                instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, unchecked((int)methodDefHash)));
                instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt32Packed_MethodRef));
                // write method parameters into stream
                for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
                {
                    var paramDef = methodDefinition.Parameters[paramIndex];
                    var paramType = paramDef.ParameterType;

                    if (paramType == typeSystem.Boolean)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteBool_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Char)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteChar_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.SByte)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteSByte_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Byte)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteByte_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Int16)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteInt16Packed_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.UInt16)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt16Packed_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Int32)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteInt32Packed_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.UInt32)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt32Packed_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Int64)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteInt64Packed_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.UInt64)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt64Packed_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Single)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteSinglePacked_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.Double)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteDoublePacked_MethodRef));
                        continue;
                    }

                    if (paramType == typeSystem.String)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteStringPacked_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityColor_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteColorPacked_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityVector2_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteVector2Packed_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityVector3_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteVector3Packed_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityVector4_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteVector4Packed_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityQuaternion_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteRotationPacked_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityRay_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteRayPacked_MethodRef));
                        continue;
                    }

                    if (paramType.FullName == CodeGenHelpers.UnityRay2D_FullName)
                    {
                        instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteRay2DPacked_MethodRef));
                        continue;
                    }

                    // INetworkSerializable
                    if (paramType.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                    {
                        var paramTypeDef = paramType.Resolve();
                        var paramTypeNetworkWrite_MethodDef = paramTypeDef.Methods.FirstOrDefault(m => m.Name == CodeGenHelpers.INetworkSerializable_NetworkWrite_Name);
                        if (paramTypeNetworkWrite_MethodDef != null)
                        {
                            if (paramType.IsValueType)
                            {
                                // struct (pass by value)
                                instructions.Add(processor.Create(OpCodes.Ldarga, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Call, paramTypeNetworkWrite_MethodDef));
                            }
                            else
                            {
                                // class (pass by reference)
                                instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                                instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                                instructions.Add(processor.Create(OpCodes.Callvirt, paramTypeNetworkWrite_MethodDef));
                            }

                            continue;
                        }
                    }

                    // Enum
                    {
                        var paramEnumType = paramType.GetEnumAsInt();
                        if (paramEnumType != null)
                        {
                            instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                            instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                            if (paramEnumType == typeSystem.SByte) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteSByte_MethodRef));
                            if (paramEnumType == typeSystem.Byte) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteByte_MethodRef));
                            if (paramEnumType == typeSystem.Int16) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteInt16Packed_MethodRef));
                            if (paramEnumType == typeSystem.UInt16) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt16Packed_MethodRef));
                            if (paramEnumType == typeSystem.Int32) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteInt32Packed_MethodRef));
                            if (paramEnumType == typeSystem.UInt32) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt32Packed_MethodRef));
                            if (paramEnumType == typeSystem.Int64) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteInt64Packed_MethodRef));
                            if (paramEnumType == typeSystem.UInt64) instructions.Add(processor.Create(OpCodes.Callvirt, BitWriter_WriteUInt64Packed_MethodRef));

                            continue;
                        }
                    }
                }

                instructions.Add(endInstr);

                // EndSendServerRpc(writer, sendParams, isReliable) -> ServerRpc
                // EndSendClientRpc(writer, sendParams, isReliable) -> ClientRpc
                if (isServerRpc)
                {
                    // ServerRpc
                    // EndSendServerRpc(writer, sendParams, isReliable);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // writer
                    instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));

                    if (hasRpcParams)
                    {
                        // rpcParams.Send
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                        instructions.Add(processor.Create(OpCodes.Ldfld, ServerRpcParams_Send_FieldRef));
                    }
                    else
                    {
                        // default
                        instructions.Add(processor.Create(OpCodes.Ldloc, sendParamsIdx));
                    }

                    // isReliable
                    instructions.Add(processor.Create(isReliableRpc ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

                    // EndSendServerRpc
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_EndSendServerRpc_MethodRef));
                }
                else
                {
                    // ClientRpc
                    // EndSendClientRpc(writer, sendParams, isReliable);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // writer
                    instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));

                    if (hasRpcParams)
                    {
                        // rpcParams.Send
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                        instructions.Add(processor.Create(OpCodes.Ldfld, ClientRpcParams_Send_FieldRef));
                    }
                    else
                    {
                        // default
                        instructions.Add(processor.Create(OpCodes.Ldloc, sendParamsIdx));
                    }

                    // isReliable
                    instructions.Add(processor.Create(isReliableRpc ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

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
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkedBehaviour.NExec.Server : NetworkedBehaviour.NExec.Client)));
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
            nhandler.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, BitReader_TypeRef));
            nhandler.Parameters.Add(new ParameterDefinition("sender", ParameterAttributes.None, typeSystem.UInt64));

            var processor = nhandler.Body.GetILProcessor();
            var isServerRpc = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRpcAttribute_FullName;

            nhandler.Body.InitLocals = true;
            // read method parameters from stream
            int paramCount = methodDefinition.Parameters.Count;
            for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
            {
                var paramDef = methodDefinition.Parameters[paramIndex];
                var paramType = paramDef.ParameterType;

                // local variable to storage argument
                nhandler.Body.Variables.Add(new VariableDefinition(paramType));

                if (paramType == typeSystem.Boolean)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadBool_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Char)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadChar_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.SByte)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadSByte_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Byte)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadByte_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Int16)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadInt16Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.UInt16)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadUInt16Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Int32)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadInt32Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.UInt32)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadUInt32Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Int64)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadInt64Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.UInt64)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadUInt64Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Single)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadSinglePacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.Double)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadDoublePacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType == typeSystem.String)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Ldnull);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadStringPacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityColor_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadColorPacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityVector2_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadVector2Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityVector3_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadVector3Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityVector4_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadVector4Packed_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityQuaternion_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadRotationPacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityRay_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadRayPacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                if (paramType.FullName == CodeGenHelpers.UnityRay2D_FullName)
                {
                    processor.Emit(OpCodes.Ldarg_1);
                    processor.Emit(OpCodes.Callvirt, BitReader_ReadRay2DPacked_MethodRef);
                    processor.Emit(OpCodes.Stloc, paramIndex);
                    continue;
                }

                // INetworkSerializable
                if (paramType.HasInterface(CodeGenHelpers.INetworkSerializable_FullName))
                {
                    var paramTypeDef = paramType.Resolve();
                    var paramTypeNetworkRead_MethodDef = paramTypeDef.Methods.FirstOrDefault(m => m.Name == CodeGenHelpers.INetworkSerializable_NetworkRead_Name);
                    if (paramTypeNetworkRead_MethodDef != null)
                    {
                        if (paramType.IsValueType)
                        {
                            // struct (pass by value)
                            processor.Emit(OpCodes.Ldloca, paramIndex);
                            processor.Emit(OpCodes.Ldarg_1);
                            processor.Emit(OpCodes.Call, paramTypeNetworkRead_MethodDef);
                        }
                        else
                        {
                            // class (pass by reference)
                            var paramTypeDefCtor = paramTypeDef.GetConstructors().FirstOrDefault(m => m.Parameters.Count == 0);
                            if (paramTypeDefCtor != null)
                            {
                                // new INetworkSerializable()
                                processor.Emit(OpCodes.Newobj, paramTypeDefCtor);
                                processor.Emit(OpCodes.Stloc, paramIndex);

                                // INetworkSerializable.NetworkRead(reader)
                                processor.Emit(OpCodes.Ldloc, paramIndex);
                                processor.Emit(OpCodes.Ldarg_1);
                                processor.Emit(OpCodes.Callvirt, paramTypeNetworkRead_MethodDef);
                            }
                        }

                        continue;
                    }
                }

                // Enum
                {
                    var paramEnumType = paramType.GetEnumAsInt();
                    if (paramEnumType != null)
                    {
                        processor.Emit(OpCodes.Ldarg_1);
                        if (paramEnumType == typeSystem.SByte) processor.Emit(OpCodes.Callvirt, BitReader_ReadSByte_MethodRef);
                        if (paramEnumType == typeSystem.Byte) processor.Emit(OpCodes.Callvirt, BitReader_ReadByte_MethodRef);
                        if (paramEnumType == typeSystem.Int16) processor.Emit(OpCodes.Callvirt, BitReader_ReadInt16Packed_MethodRef);
                        if (paramEnumType == typeSystem.UInt16) processor.Emit(OpCodes.Callvirt, BitReader_ReadUInt16Packed_MethodRef);
                        if (paramEnumType == typeSystem.Int32) processor.Emit(OpCodes.Callvirt, BitReader_ReadInt32Packed_MethodRef);
                        if (paramEnumType == typeSystem.UInt32) processor.Emit(OpCodes.Callvirt, BitReader_ReadUInt32Packed_MethodRef);
                        if (paramEnumType == typeSystem.Int64) processor.Emit(OpCodes.Callvirt, BitReader_ReadInt64Packed_MethodRef);
                        if (paramEnumType == typeSystem.UInt64) processor.Emit(OpCodes.Callvirt, BitReader_ReadUInt64Packed_MethodRef);
                        processor.Emit(OpCodes.Stloc, paramIndex);

                        continue;
                    }
                }

                // ServerRpcParams, ClientRpcParams
                {
                    // ServerRpcParams
                    if (paramType.FullName == CodeGenHelpers.ServerRpcParams_FullName)
                    {
                        processor.Emit(OpCodes.Ldloca, paramIndex);
                        processor.Emit(OpCodes.Ldflda, ServerRpcParams_Receive_FieldRef);
                        processor.Emit(OpCodes.Ldarg_2);
                        processor.Emit(OpCodes.Stfld, ServerRpcReceiveParams_SenderClientId_FieldRef);
                        continue;
                    }

                    // ClientRpcParams
                    if (paramType.FullName == CodeGenHelpers.ClientRpcParams_FullName)
                    {
                        continue;
                    }
                }
            }

            // NetworkBehaviour.__nexec = NExec.Server; -> ServerRpc
            // NetworkBehaviour.__nexec = NExec.Client; -> ClientRpc
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldc_I4, (int)(isServerRpc ? NetworkedBehaviour.NExec.Server : NetworkedBehaviour.NExec.Client));
            processor.Emit(OpCodes.Stfld, NetworkBehaviour_nexec_FieldRef);

            // NetworkBehaviour.XXXRpc(...);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Castclass, methodDefinition.DeclaringType);
            Enumerable.Range(0, paramCount).ToList().ForEach(paramIndex => processor.Emit(OpCodes.Ldloc, paramIndex));
            processor.Emit(OpCodes.Callvirt, methodDefinition);

            // NetworkBehaviour.__nexec = NExec.None;
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldc_I4, (int)NetworkedBehaviour.NExec.None);
            processor.Emit(OpCodes.Stfld, NetworkBehaviour_nexec_FieldRef);

            processor.Emit(OpCodes.Ret);
            return nhandler;
        }
    }
}
