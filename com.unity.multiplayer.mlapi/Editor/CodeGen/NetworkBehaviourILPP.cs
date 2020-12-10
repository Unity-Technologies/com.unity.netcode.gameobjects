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

        public override bool WillProcess(ICompiledAssembly compiledAssembly) => compiledAssembly.References
            .Any(filePath => Path.GetFileNameWithoutExtension(filePath) == CodeGenHelpers.RuntimeAssemblyName);

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
                if (ImportReferences(mainModule))
                {
                    // process `NetworkBehaviour` types
                    mainModule.Types
                        .Where(t => t.IsSubclassOf(CodeGenHelpers.NetworkBehaviour_FullName))
                        .ToList()
                        .ForEach(ProcessNetworkBehaviour);
                }
                else _diagnostics.AddError("todo: informative error message -> ImportReferences(mainModule)");
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

        private TypeReference NetworkManager_TypeRef;
        private FieldReference NetworkManager_ntable_FieldRef;
        private MethodReference NetworkManager_ntable_Add_MethodRef;
        private MethodReference NetworkManager_getSingleton_MethodRef;
        private MethodReference NetworkManager_getIsListening_MethodRef;
        private MethodReference NetworkManager_getIsHost_MethodRef;
        private MethodReference NetworkManager_getIsServer_MethodRef;
        private MethodReference NetworkManager_getIsClient_MethodRef;
        private TypeReference NetworkBehaviour_TypeRef;
        private MethodReference NetworkBehaviour_BeginServerRPC_MethodRef;
        private MethodReference NetworkBehaviour_EndServerRPC_MethodRef;
        private MethodReference NetworkBehaviour_BeginClientRPC_MethodRef;
        private MethodReference NetworkBehaviour_EndClientRPC_MethodRef;
        private FieldReference NetworkBehaviour_nexec_FieldRef;
        private MethodReference NetworkHandlerDelegateCtor_MethodRef;
        private FieldReference ClientRPCOptions_TargetClientIds_FieldRef;
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
            foreach (var methodInfo in networkManagerType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case "get_Singleton":
                        NetworkManager_getSingleton_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "get_IsListening":
                        NetworkManager_getIsListening_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "get_IsHost":
                        NetworkManager_getIsHost_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "get_IsServer":
                        NetworkManager_getIsServer_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "get_IsClient":
                        NetworkManager_getIsClient_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }

            foreach (var fieldInfo in networkManagerType.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (fieldInfo.Name)
                {
                    case "__ntable":
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
                    case "BeginServerRPC":
                        NetworkBehaviour_BeginServerRPC_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "EndServerRPC":
                        NetworkBehaviour_EndServerRPC_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "BeginClientRPC":
                        NetworkBehaviour_BeginClientRPC_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "EndClientRPC":
                        NetworkBehaviour_EndClientRPC_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                }
            }

            foreach (var fieldInfo in networkBehaviourType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                switch (fieldInfo.Name)
                {
                    case "__nexec":
                        NetworkBehaviour_nexec_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var networkHandlerDelegateType = typeof(Action<NetworkedBehaviour, BitReader>);
            NetworkHandlerDelegateCtor_MethodRef = moduleDefinition.ImportReference(
                networkHandlerDelegateType
                    .GetConstructor(new[] {typeof(object), typeof(IntPtr)}));

            var clientRPCOptionsType = typeof(ClientRPCOptions);
            foreach (var fieldInfo in clientRPCOptionsType.GetFields())
            {
                switch (fieldInfo.Name)
                {
                    case "TargetClientIds":
                        ClientRPCOptions_TargetClientIds_FieldRef = moduleDefinition.ImportReference(fieldInfo);
                        break;
                }
            }

            var bitWriterType = typeof(BitWriter);
            BitWriter_TypeRef = moduleDefinition.ImportReference(bitWriterType);
            foreach (var methodInfo in bitWriterType.GetMethods())
            {
                switch (methodInfo.Name)
                {
                    case "WriteBool":
                        BitWriter_WriteBool_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteChar":
                        BitWriter_WriteChar_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteSByte":
                        BitWriter_WriteSByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteByte":
                        BitWriter_WriteByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteInt16Packed":
                        BitWriter_WriteInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteUInt16Packed":
                        BitWriter_WriteUInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteInt32Packed":
                        BitWriter_WriteInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteUInt32Packed":
                        BitWriter_WriteUInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteInt64Packed":
                        BitWriter_WriteInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteUInt64Packed":
                        BitWriter_WriteUInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteSinglePacked":
                        BitWriter_WriteSinglePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteDoublePacked":
                        BitWriter_WriteDoublePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteStringPacked":
                        BitWriter_WriteStringPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteColorPacked":
                        BitWriter_WriteColorPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteVector2Packed":
                        BitWriter_WriteVector2Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteVector3Packed":
                        BitWriter_WriteVector3Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteVector4Packed":
                        BitWriter_WriteVector4Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteRotationPacked":
                        BitWriter_WriteRotationPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteRayPacked":
                        BitWriter_WriteRayPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "WriteRay2DPacked":
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
                    case "ReadBool":
                        BitReader_ReadBool_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadChar":
                        BitReader_ReadChar_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadSByte":
                        BitReader_ReadSByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadByte":
                        BitReader_ReadByte_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadInt16Packed":
                        BitReader_ReadInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadUInt16Packed":
                        BitReader_ReadUInt16Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadInt32Packed":
                        BitReader_ReadInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadUInt32Packed":
                        BitReader_ReadUInt32Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadInt64Packed":
                        BitReader_ReadInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadUInt64Packed":
                        BitReader_ReadUInt64Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadSinglePacked":
                        BitReader_ReadSinglePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadDoublePacked":
                        BitReader_ReadDoublePacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadStringPacked":
                        BitReader_ReadStringPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadColorPacked":
                        BitReader_ReadColorPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadVector2Packed":
                        BitReader_ReadVector2Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadVector3Packed":
                        BitReader_ReadVector3Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadVector4Packed":
                        BitReader_ReadVector4Packed_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadRotationPacked":
                        BitReader_ReadRotationPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadRayPacked":
                        BitReader_ReadRayPacked_MethodRef = moduleDefinition.ImportReference(methodInfo);
                        break;
                    case "ReadRay2DPacked":
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
                        ".cctor",
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
            bool isServerRPC = false;
            foreach (var customAttribute in methodDefinition.CustomAttributes)
            {
                var customAttributeType_FullName = customAttribute.AttributeType.FullName;

                if (customAttributeType_FullName == CodeGenHelpers.ServerRPCAttribute_FullName ||
                    customAttributeType_FullName == CodeGenHelpers.ClientRPCAttribute_FullName)
                {
                    if (methodDefinition.IsStatic)
                    {
                        _diagnostics.AddError(methodDefinition, "todo: informative error message -> methodDefinition.IsStatic");
                        return null;
                    }

                    if (methodDefinition.IsAbstract)
                    {
                        _diagnostics.AddError(methodDefinition, "todo: informative error message -> methodDefinition.IsAbstract");
                        return null;
                    }

                    if (methodDefinition.ReturnType != methodDefinition.Module.TypeSystem.Void)
                    {
                        _diagnostics.AddError(methodDefinition, "todo: informative error message -> methodDefinition.ReturnType != methodDefinition.Module.TypeSystem.Void");
                        return null;
                    }

                    if (customAttributeType_FullName == CodeGenHelpers.ServerRPCAttribute_FullName &&
                        !methodDefinition.Name.EndsWith("ServerRPC"))
                    {
                        _diagnostics.AddError(methodDefinition, "todo: informative error message -> !methodDefinition.Name.EndsWith('ServerRPC')");
                        return null;
                    }

                    if (customAttributeType_FullName == CodeGenHelpers.ClientRPCAttribute_FullName &&
                        !methodDefinition.Name.EndsWith("ClientRPC"))
                    {
                        _diagnostics.AddError(methodDefinition, "todo: informative error message -> !methodDefinition.Name.EndsWith('ClientRPC')");
                        return null;
                    }

                    isServerRPC = customAttributeType_FullName == CodeGenHelpers.ServerRPCAttribute_FullName;
                    rpcAttribute = customAttribute;
                    break;
                }
            }

            if (rpcAttribute == null) return null;

            int paramCount = methodDefinition.Parameters.Count;
            for (int paramIndex = 0; paramIndex < paramCount; ++paramIndex)
            {
                var paramDef = methodDefinition.Parameters[paramIndex];
                var paramType = paramDef.ParameterType;

                if (paramType.IsSupportedType()) continue; // Basic types

                // todo: StaticArray[]
                // if (paramType.IsArray && paramType.GetElementType().IsSupportedType()) continue;

                // todo: IEnumerable<T> for List/Set<T> etc. support
                /*
                // IEnumerable<T> or based on IEnumerable<T>
                if (paramType.FullName.StartsWith(IEnumerable_FullName)) continue;
                if (paramType.HasGenericInterface(IEnumerable_FullName)) continue;
                */

                // todo: double-check method overloading
                // todo: IEnumerable<KeyValuePair<K, V>> for Dictionary<K, V> support
                // todo: IEnumerable<Tuple<T1, T2, T3...T7>> for Tuple<T1-T7> support
                // todo: IEnumerable<Tuple<T1, T2...TRest>> for Tuple<T1-TRest> support (nested tuples)

                // ServerRPCOptions, ClientRPCOptions
                if (paramType.FullName == CodeGenHelpers.ServerRPCOptions_FullName && isServerRPC && paramIndex == paramCount - 1) continue;
                if (paramType.FullName == CodeGenHelpers.ClientRPCOptions_FullName && !isServerRPC && paramIndex == paramCount - 1) continue;

                _diagnostics.AddError(methodDefinition, $"todo: informative error message -> CheckAndGetRPCAttribute --- unsupported parameter type: {paramType.FullName}");
                rpcAttribute = null;
            }

            return rpcAttribute;
        }

        private void InjectWriteAndCallBlocks(MethodDefinition methodDefinition, CustomAttribute rpcAttribute, uint methodDefHash)
        {
            var instructions = new List<Instruction>();
            var processor = methodDefinition.Body.GetILProcessor();
            var isServerRPC = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRPCAttribute_FullName;
            var paramCount = methodDefinition.Parameters.Count;
            var hasRPCOptions = !isServerRPC && paramCount > 0 && methodDefinition.Parameters[paramCount - 1].ParameterType.FullName == CodeGenHelpers.ClientRPCOptions_FullName;

            methodDefinition.Body.InitLocals = true;
            // NetworkManager networkManager;
            methodDefinition.Body.Variables.Add(new VariableDefinition(NetworkManager_TypeRef));
            int netManLocIdx = methodDefinition.Body.Variables.Count - 1;
            // BitWriter writer;
            methodDefinition.Body.Variables.Add(new VariableDefinition(BitWriter_TypeRef));
            int writerLocIdx = methodDefinition.Body.Variables.Count - 1;

            {
                var nextInstr = processor.Create(OpCodes.Nop);
                var returnInstr = processor.Create(OpCodes.Ret);

                // networkManager = NetworkManager.Singleton;
                instructions.Add(processor.Create(OpCodes.Call, NetworkManager_getSingleton_MethodRef));
                instructions.Add(processor.Create(OpCodes.Stloc, netManLocIdx));

                // if (networkManager == null || !networkManager.IsListening) return;
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Brfalse, returnInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getIsListening_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, nextInstr));

                instructions.Add(returnInstr);
                instructions.Add(nextInstr);
            }

            {
                var nextInstr = processor.Create(OpCodes.Nop);
                var endInstr = processor.Create(OpCodes.Nop);
                var writeInstr = processor.Create(OpCodes.Nop);

                // if (__nexec != NExec.Server) -> ServerRPC
                // if (__nexec != NExec.Client) -> ClientRPC
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Ldfld, NetworkBehaviour_nexec_FieldRef));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRPC ? NetworkedBehaviour.NExec.Server : NetworkedBehaviour.NExec.Client)));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, 0));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Brfalse, nextInstr));

                // if (networkManager.IsClient || networkManager.IsHost) { ... } -> ServerRPC
                // if (networkManager.IsServer || networkManager.IsHost) { ... } -> ClientRPC
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, isServerRPC ? NetworkManager_getIsClient_MethodRef : NetworkManager_getIsServer_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, writeInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getIsHost_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brfalse, nextInstr));

                instructions.Add(writeInstr);

                // var writer = BeginServerRPC() -> ServerRPC
                // var writer = BeginClientRPC(targetClientIds) -> ClientRPC
                if (isServerRPC)
                {
                    // ServerRPC
                    // var writer = BeginServerRPC();
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_BeginServerRPC_MethodRef));
                    instructions.Add(processor.Create(OpCodes.Stloc, writerLocIdx));
                }
                else
                {
                    // ClientRPC
                    // var writer = BeginClientRPC(targetClientIds);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    if (hasRPCOptions)
                    {
                        // targetClientIds
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                        instructions.Add(processor.Create(OpCodes.Ldfld, ClientRPCOptions_TargetClientIds_FieldRef));
                    }
                    else
                    {
                        // null
                        instructions.Add(processor.Create(OpCodes.Ldnull));
                    }

                    // BeginClientRPC
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_BeginClientRPC_MethodRef));
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
                var typeSystem = methodDefinition.Module.TypeSystem;
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

                    // todo: StaticArray[]
                    /*
                    if (paramType.IsArray)
                    {
                        // instructions.Add(processor.Create(OpCodes.Ldarg, paramIndex + 1));
                        // instructions.Add(processor.Create(OpCodes.Ldlen));
                    }
                    */

                    // todo: double-check method overloading
                    // todo: IEnumerable<T> for List/Set<T> etc. support
                    // todo: IEnumerable<KeyValuePair<K, V>> for Dictionary<K, V> support
                    // todo: IEnumerable<Tuple<T1, T2, T3...T7>> for Tuple<T1-T7> support
                    // todo: IEnumerable<Tuple<T1, T2...TRest>> for Tuple<T1-TRest> support (nested tuples)

                    // todo: ServerRPCOptions, ClientRPCOptions
                }

                instructions.Add(endInstr);

                // EndServerRPC(writer) -> ServerRPC
                // EndClientRPC(writer, targetClientIds) -> ClientRPC
                if (isServerRPC)
                {
                    // ServerRPC
                    // EndServerRPC(writer);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));
                    instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_EndServerRPC_MethodRef));
                }
                else
                {
                    // ClientRPC
                    // EndClientRPC(writer, targetClientIds);
                    instructions.Add(processor.Create(OpCodes.Ldarg_0));

                    // writer
                    instructions.Add(processor.Create(OpCodes.Ldloc, writerLocIdx));

                    if (hasRPCOptions)
                    {
                        // targetClientIds
                        instructions.Add(processor.Create(OpCodes.Ldarg, paramCount));
                        instructions.Add(processor.Create(OpCodes.Ldfld, ClientRPCOptions_TargetClientIds_FieldRef));
                    }
                    else
                    {
                        // null
                        instructions.Add(processor.Create(OpCodes.Ldnull));
                    }

                    // EndClientRPC
                    instructions.Add(processor.Create(OpCodes.Call, NetworkBehaviour_EndClientRPC_MethodRef));
                }

                instructions.Add(nextInstr);
            }

            {
                var returnInstr = processor.Create(OpCodes.Ret);
                var nextInstr = processor.Create(OpCodes.Nop);

                // if (__nexec == NExec.Server) -> ServerRPC
                // if (__nexec == NExec.Client) -> ClientRPC
                instructions.Add(processor.Create(OpCodes.Ldarg_0));
                instructions.Add(processor.Create(OpCodes.Ldfld, NetworkBehaviour_nexec_FieldRef));
                instructions.Add(processor.Create(OpCodes.Ldc_I4, (int)(isServerRPC ? NetworkedBehaviour.NExec.Server : NetworkedBehaviour.NExec.Client)));
                instructions.Add(processor.Create(OpCodes.Ceq));
                instructions.Add(processor.Create(OpCodes.Brfalse, returnInstr));

                // if (networkManager.IsServer || networkManager.IsHost) -> ServerRPC
                // if (networkManager.IsClient || networkManager.IsHost) -> ClientRPC
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, isServerRPC ? NetworkManager_getIsServer_MethodRef : NetworkManager_getIsClient_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, nextInstr));
                instructions.Add(processor.Create(OpCodes.Ldloc, netManLocIdx));
                instructions.Add(processor.Create(OpCodes.Callvirt, NetworkManager_getIsHost_MethodRef));
                instructions.Add(processor.Create(OpCodes.Brtrue, nextInstr));

                instructions.Add(returnInstr);
                instructions.Add(nextInstr);
            }

            instructions.Reverse();
            instructions.ForEach(instruction => processor.Body.Instructions.Insert(0, instruction));
        }

        private MethodDefinition GenerateStaticHandler(MethodDefinition methodDefinition, CustomAttribute rpcAttribute)
        {
            var nhandler = new MethodDefinition(
                $"{methodDefinition.Name}__nhandler",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                methodDefinition.Module.TypeSystem.Void);
            nhandler.Parameters.Add(new ParameterDefinition("target", ParameterAttributes.None, NetworkBehaviour_TypeRef));
            nhandler.Parameters.Add(new ParameterDefinition("reader", ParameterAttributes.None, BitReader_TypeRef));

            var processor = nhandler.Body.GetILProcessor();
            var isServerRPC = rpcAttribute.AttributeType.FullName == CodeGenHelpers.ServerRPCAttribute_FullName;

            nhandler.Body.InitLocals = true;
            // read method parameters from stream
            var typeSystem = methodDefinition.Module.TypeSystem;
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

                // todo: StaticArray[] -> stackalloc?

                // todo: double-check method overloading
                // todo: IEnumerable<T> for List/Set<T> etc. support
                // todo: IEnumerable<KeyValuePair<K, V>> for Dictionary<K, V> support
                // todo: IEnumerable<Tuple<T1, T2, T3...T7>> for Tuple<T1-T7> support
                // todo: IEnumerable<Tuple<T1, T2...TRest>> for Tuple<T1-TRest> support (nested tuples)

                // todo: ServerRPCOptions, ClientRPCOptions
            }

            // NetworkBehaviour.__nexec = NExec.Server; -> ServerRPC
            // NetworkBehaviour.__nexec = NExec.Client; -> ClientRPC
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldc_I4, (int)(isServerRPC ? NetworkedBehaviour.NExec.Server : NetworkedBehaviour.NExec.Client));
            processor.Emit(OpCodes.Stfld, NetworkBehaviour_nexec_FieldRef);

            // NetworkBehaviour.XXXRPC(...);
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
