using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.Netcode.Shared;

namespace Unity.Netcode.Editor.CodeGen
{
    namespace Import
    {
#pragma warning disable IDE1006 // disable naming rule violation check
        internal static class ModuleNames
        {
            public const string NetcodeRuntime = "Unity.Netcode.Runtime.dll";
            public const string NetStandard = "netstandard.dll";
            public const string UnityEngineCoreModule = "UnityEngine.CoreModule.dll";
        }

        internal static class TypeNames
        {
            public const string NetworkManager = "Unity.Netcode.NetworkManager";
            public const string NetworkManager_RpcReceiveHandler = "Unity.Netcode.NetworkManager/RpcReceiveHandler";
            public const string NetworkBehaviour = "Unity.Netcode.NetworkBehaviour";
            public const string NetworkBehaviour___RpcExecStage = "Unity.Netcode.NetworkBehaviour/__RpcExecStage";

            public const string INetworkMessage = "Unity.Netcode.INetworkMessage";
            public const string INetworkSerializable = "Unity.Netcode.INetworkSerializable";
            public const string INetworkSerializeByMemcpy = "Unity.Netcode.INetworkSerializeByMemcpy";

            public const string __RpcParams = "Unity.Netcode.__RpcParams";

            public const string MessagingSystem = "Unity.Netcode.MessagingSystem";
            public const string MessagingSystem_MessageHandler = "Unity.Netcode.MessagingSystem/MessageHandler";
            public const string MessagingSystem_MessageWithHandler = "Unity.Netcode.MessagingSystem/MessageWithHandler";

            public const string ILPPMessageProvider = "Unity.Netcode.ILPPMessageProvider";

            public const string ClientRpcParams = "Unity.Netcode.ClientRpcParams";
            public const string ServerRpcParams = "Unity.Netcode.ServerRpcParams";
            public const string ClientRpcSendParams = "Unity.Netcode.ClientRpcSendParams";
            public const string ServerRpcSendParams = "Unity.Netcode.ServerRpcSendParams";
            public const string ClientRpcReceiveParams = "Unity.Netcode.ClientRpcReceiveParams";
            public const string ServerRpcReceiveParams = "Unity.Netcode.ServerRpcReceiveParams";

            public const string ClientRpcAttribute = "Unity.Netcode.ClientRpcAttribute";
            public const string ServerRpcAttribute = "Unity.Netcode.ServerRpcAttribute";

            public const string FastBufferReader = "Unity.Netcode.FastBufferReader";
            public const string FastBufferWriter = "Unity.Netcode.FastBufferWriter";

            public const string RpcDelivery = "Unity.Netcode.RpcDelivery";
            public const string LogLevel = "Unity.Netcode.LogLevel";


            public const string Debug = "UnityEngine.Debug";

            public const string Type = "System.Type";
            public const string List = "System.Collections.Generic.List`1";

            public const string Module = "<Module>";

            public const string ForceNetworkSerializeByMemcpy_ShortName = "ForceNetworkSerializeByMemcpy";
        }

        internal static class EnumValueNames
        {
            public const string RpcDelivery_Reliable = "Reliable";

            public const string LogLevel_Normal = "Normal";

            public const string RpcExecStage_None = "None";
            public const string RpcExecStage_Server = "Server";
            public const string RpcExecStage_Client = "Client";
        }

        internal static class FieldNames
        {
            public const string MessagingSystem_MessageType = "MessageType";
            public const string MessagingSystem_Handler = "Handler";

            public const string NetworkManager___rpc_func_table = "__rpc_func_table";
            public const string NetworkManager_RpcReceiveHandler = "RpcReceiveHandler";
            public const string NetworkManager___rpc_name_table = "__rpc_name_table";

            public const string NetworkBehaviour___rpc_exec_stage = "__rpc_exec_stage";

            public const string ILPPMessageProvider___network_message_types = "__network_message_types";

            public const string NetworkManager_LogLevel = "LogLevel";

            public const string RpcAttribute_Delivery = "Delivery";
            public const string ServerRpcAttribute_RequireOwnership = "RequireOwnership";
            public const string RpcParams_Server = "Server";
            public const string RpcParams_Client = "Client";
            public const string ServerRpcParams_Receive = "Receive";
            public const string ServerRpcReceiveParams_SenderClientId = "SenderClientId";
        }

        internal static class ImportProperties
        {
            public const string NetworkBehaviour_NetworkManager = "NetworkManager";
            public const string NetworkBehaviour_OwnerClientId = "OwnerClientId";
            public const string NetworkManager_LocalClientId = "LocalClientId";
            public const string NetworkManager_IsListening = "IsListening";
            public const string NetworkManager_IsHost = "IsHost";
            public const string NetworkManager_IsServer = "IsServer";
            public const string NetworkManager_IsClient = "IsClient";

        }

        internal static class MethodNames
        {
            public const string MessagingSystem_ReceiveMessage = "ReceiveMessage";

            public const string NetworkBehaviour___beginSendServerRpc = "__beginSendServerRpc";
            public const string NetworkBehaviour___endSendServerRpc = "__endSendServerRpc";
            public const string NetworkBehaviour___beginSendClientRpc = "__beginSendClientRpc";
            public const string NetworkBehaviour___endSendClientRpc = "__endSendClientRpc";
            public const string NetworkBehaviour___getTypeName = "__getTypeName";

            public const string FastBufferReader_ReadValueSafe = "ReadValueSafe";
            public const string FastBufferWriter_WriteValueSafe = "WriteValueSafe";

            public const string Type_GetTypeFromHandle = "GetTypeFromHandle";
            public const string List_Add = "Add";

            public const string Module_StaticConstructor = ".cctor";

            public const string Debug_LogError = "LogError";
        }
#pragma warning restore IDE1006 // disable naming rule violation check
    }
    internal static class CodeGenHelpers
    {
        public const string RuntimeAssemblyName = "Unity.Netcode.Runtime";

        public static uint Hash(this MethodDefinition methodDefinition)
        {
            var sigArr = Encoding.UTF8.GetBytes($"{methodDefinition.Module.Name} / {methodDefinition.FullName}");
            var sigLen = sigArr.Length;
            unsafe
            {
                fixed (byte* sigPtr = sigArr)
                {
                    return XXHash.Hash32(sigPtr, sigLen);
                }
            }
        }

        public static bool IsSubclassOf(this TypeDefinition typeDefinition, string classTypeFullName)
        {
            if (!typeDefinition.IsClass)
            {
                return false;
            }

            var baseTypeRef = typeDefinition.BaseType;
            while (baseTypeRef != null)
            {
                if (baseTypeRef.FullName == classTypeFullName)
                {
                    return true;
                }

                try
                {
                    baseTypeRef = baseTypeRef.Resolve().BaseType;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static int GetEnumValue(TypeDefinition enumDefinition, string value)
        {
            foreach (var field in enumDefinition.Fields)
            {
                if (field.Name == value)
                {
                    return (int)field.Constant;
                }
            }

            throw new Exception(string.Format("Enum value not found for {0}", value));
        }

        public static string FullNameWithGenericParameters(this TypeReference typeReference, GenericParameter[] contextGenericParameters, TypeReference[] contextGenericParameterTypes)
        {
            var name = typeReference.FullName;
            if (typeReference.HasGenericParameters)
            {
                name += "<";
                for (var i = 0; i < typeReference.Resolve().GenericParameters.Count; ++i)
                {
                    if (i != 0)
                    {
                        name += ", ";
                    }

                    for (var j = 0; j < contextGenericParameters.Length; ++j)
                    {
                        if (typeReference.GenericParameters[i].FullName == contextGenericParameters[i].FullName)
                        {
                            name += contextGenericParameterTypes[i].FullName;
                            break;
                        }
                    }
                }

                name += ">";
            }

            return name;
        }

        public static bool HasInterface(this TypeReference typeReference, string interfaceTypeFullName)
        {
            if (typeReference.IsArray)
            {
                return false;
            }

            try
            {
                var typeDef = typeReference.Resolve();
                var typeFaces = typeDef.Interfaces;
                return typeFaces.Any(iface => iface.InterfaceType.FullName == interfaceTypeFullName);
            }
            catch
            {
                return false;
            }
        }

        public static void AddError(this List<DiagnosticMessage> diagnostics, string message)
        {
            diagnostics.AddError((SequencePoint)null, message);
        }

        public static void AddError(this List<DiagnosticMessage> diagnostics, MethodDefinition methodDefinition, string message)
        {
            diagnostics.AddError(methodDefinition.DebugInformation.SequencePoints.FirstOrDefault(), message);
        }

        public static void AddError(this List<DiagnosticMessage> diagnostics, SequencePoint sequencePoint, string message)
        {
            diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Error,
                File = sequencePoint?.Document.Url.Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", ""),
                Line = sequencePoint?.StartLine ?? 0,
                Column = sequencePoint?.StartColumn ?? 0,
                MessageData = $" - {message}"
            });
        }

        public static void AddWarning(this List<DiagnosticMessage> diagnostics, string message)
        {
            diagnostics.AddWarning((SequencePoint)null, message);
        }

        public static void AddWarning(this List<DiagnosticMessage> diagnostics, MethodDefinition methodDefinition, string message)
        {
            diagnostics.AddWarning(methodDefinition.DebugInformation.SequencePoints.FirstOrDefault(), message);
        }

        public static void AddWarning(this List<DiagnosticMessage> diagnostics, SequencePoint sequencePoint, string message)
        {
            diagnostics.Add(new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Warning,
                File = sequencePoint?.Document.Url.Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", ""),
                Line = sequencePoint?.StartLine ?? 0,
                Column = sequencePoint?.StartColumn ?? 0,
                MessageData = $" - {message}"
            });
        }

        public static TypeDefinition FindTypeRecursive(this AssemblyDefinition assemblyDefinition, PostProcessorAssemblyResolver resolver, string fullTypeName, HashSet<string> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<string>();
            }
            var type = FindTypeWithin(assemblyDefinition, fullTypeName);
            if (type != null)
            {
                return type;
            }

            // Iterating in reverse because the system libraries are first and are unlikely to be what we're looking for
            for (var i = assemblyDefinition.Modules.Count - 1; i >= 0; --i)
            {
                var moduleDefinition = assemblyDefinition.Modules[i];
                for (var j = moduleDefinition.AssemblyReferences.Count - 1; j >= 0; --j)
                {
                    var assemblyReference = moduleDefinition.AssemblyReferences[j];
                    if (assemblyReference == null)
                    {
                        continue;
                    }
                    if (visited.Contains(assemblyReference.Name))
                    {
                        continue;
                    }

                    visited.Add(assemblyReference.Name);
                    var assembly = resolver.Resolve(assemblyReference);
                    if (assembly == null)
                    {
                        continue;
                    }
                    type = FindTypeRecursive(assembly, resolver, fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        public static TypeDefinition FindTypeWithin(this AssemblyDefinition assemblyDefinition, string fullTypeName)
        {
            foreach (var module in assemblyDefinition.Modules)
            {
                if (module == null)
                {
                    continue;
                }
                foreach (var type in module.GetAllTypes())
                {
                    if (type.FullName == fullTypeName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        public static TypeDefinition FindType(this ModuleDefinition module, string fullTypeName)
        {
            foreach (var type in module.GetAllTypes())
            {
                if (type.FullName == fullTypeName)
                {
                    return type;
                }
            }

            return null;
        }

        public static ModuleDefinition FindReferencedModule(this AssemblyDefinition assemblyDefinition, PostProcessorAssemblyResolver resolver, string moduleName, HashSet<string> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<string>();
            }
            var module = FindModuleWithin(assemblyDefinition, moduleName);
            if (module != null)
            {
                return module;
            }

            // Iterating in reverse because the system libraries are first and are unlikely to be what we're looking for
            for (var i = assemblyDefinition.Modules.Count - 1; i >= 0; --i)
            {
                var moduleDefinition = assemblyDefinition.Modules[i];
                for (var j = moduleDefinition.AssemblyReferences.Count - 1; j >= 0; --j)
                {
                    var assemblyReference = moduleDefinition.AssemblyReferences[j];
                    if (assemblyReference == null)
                    {
                        continue;
                    }

                    if (visited.Contains(assemblyReference.Name))
                    {
                        continue;
                    }

                    visited.Add(assemblyReference.Name);

                    var assembly = resolver.Resolve(assemblyReference);
                    if (assembly == null)
                    {
                        continue;
                    }
                    module = FindReferencedModule(assembly, resolver, moduleName, visited);
                    if (module != null)
                    {
                        return module;
                    }
                }
            }

            return null;
        }

        public static ModuleDefinition FindModuleWithin(this AssemblyDefinition assemblyDefinition, string moduleName)
        {
            foreach (var module in assemblyDefinition.Modules)
            {
                if (module == null)
                {
                    continue;
                }
                if (module.Name == moduleName)
                {
                    return module;
                }
            }

            return null;
        }

        public static void RemoveRecursiveReferences(this ModuleDefinition moduleDefinition)
        {
            // Weird behavior from Cecil: When importing a reference to a specific implementation of a generic
            // method, it's importing the main module as a reference into itself. This causes Unity to have issues
            // when attempting to iterate the assemblies to discover unit tests, as it goes into infinite recursion
            // and eventually hits a stack overflow. I wasn't able to find any way to stop Cecil from importing the module
            // into itself, so at the end of it all, we're just going to go back and remove it again.
            var moduleName = moduleDefinition.Name;
            if (moduleName.EndsWith(".dll") || moduleName.EndsWith(".exe"))
            {
                moduleName = moduleName.Substring(0, moduleName.Length - 4);
            }

            foreach (var reference in moduleDefinition.AssemblyReferences)
            {
                var referenceName = reference.Name.Split(',')[0];
                if (referenceName.EndsWith(".dll") || referenceName.EndsWith(".exe"))
                {
                    referenceName = referenceName.Substring(0, referenceName.Length - 4);
                }

                if (moduleName == referenceName)
                {
                    try
                    {
                        moduleDefinition.AssemblyReferences.Remove(reference);
                        break;
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
            }
        }

        public static AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly, out PostProcessorAssemblyResolver assemblyResolver)
        {
            assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);

            //apparently, it will happen that when we ask to resolve a type that lives inside Unity.Netcode.Runtime, and we
            //are also postprocessing Unity.Netcode.Runtime, type resolving will fail, because we do not actually try to resolve
            //inside the assembly we are processing. Let's make sure we do that, so that we can use postprocessor features inside
            //Unity.Netcode.Runtime itself as well.
            assemblyResolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }
    }
}
