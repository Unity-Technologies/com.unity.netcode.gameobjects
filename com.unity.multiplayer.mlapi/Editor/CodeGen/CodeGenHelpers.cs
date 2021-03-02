using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MLAPI.Messaging;
using MLAPI.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;

#if !UNITY_2019_4_OR_NEWER
#error MLAPI requires Unity 2019.4 or newer
#endif

namespace MLAPI.Editor.CodeGen
{
    internal static class CodeGenHelpers
    {
        public const string RuntimeAssemblyName = "Unity.Multiplayer.MLAPI.Runtime";

        public static readonly string NetworkBehaviour_FullName = typeof(NetworkBehaviour).FullName;
        public static readonly string ServerRpcAttribute_FullName = typeof(ServerRpcAttribute).FullName;
        public static readonly string ClientRpcAttribute_FullName = typeof(ClientRpcAttribute).FullName;
        public static readonly string ServerRpcParams_FullName = typeof(ServerRpcParams).FullName;
        public static readonly string ClientRpcParams_FullName = typeof(ClientRpcParams).FullName;
        public static readonly string INetworkSerializable_FullName = typeof(INetworkSerializable).FullName;
        public static readonly string INetworkSerializable_NetworkSerialize_Name = nameof(INetworkSerializable.NetworkSerialize);
        public static readonly string UnityColor_FullName = typeof(Color).FullName;
        public static readonly string UnityColor32_FullName = typeof(Color32).FullName;
        public static readonly string UnityVector2_FullName = typeof(Vector2).FullName;
        public static readonly string UnityVector3_FullName = typeof(Vector3).FullName;
        public static readonly string UnityVector4_FullName = typeof(Vector4).FullName;
        public static readonly string UnityQuaternion_FullName = typeof(Quaternion).FullName;
        public static readonly string UnityRay_FullName = typeof(Ray).FullName;
        public static readonly string UnityRay2D_FullName = typeof(Ray2D).FullName;

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

        public static bool IsSubclassOf(this TypeDefinition typeDefinition, string ClassTypeFullName)
        {
            if (!typeDefinition.IsClass) return false;

            var baseTypeRef = typeDefinition.BaseType;
            while (baseTypeRef != null)
            {
                if (baseTypeRef.FullName == ClassTypeFullName)
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

        public static bool HasInterface(this TypeReference typeReference, string InterfaceTypeFullName)
        {
            if (typeReference.IsArray) return false;

            try
            {
                var typeDef = typeReference.Resolve();
                var typeFaces = typeDef.Interfaces;
                return typeFaces.Any(iface => iface.InterfaceType.FullName == InterfaceTypeFullName);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSerializable(this TypeReference typeReference)
        {
            var typeSystem = typeReference.Module.TypeSystem;

            // C# primitives
            if (typeReference == typeSystem.Boolean) return true;
            if (typeReference == typeSystem.Char) return true;
            if (typeReference == typeSystem.SByte) return true;
            if (typeReference == typeSystem.Byte) return true;
            if (typeReference == typeSystem.Int16) return true;
            if (typeReference == typeSystem.UInt16) return true;
            if (typeReference == typeSystem.Int32) return true;
            if (typeReference == typeSystem.UInt32) return true;
            if (typeReference == typeSystem.Int64) return true;
            if (typeReference == typeSystem.UInt64) return true;
            if (typeReference == typeSystem.Single) return true;
            if (typeReference == typeSystem.Double) return true;
            if (typeReference == typeSystem.String) return true;

            // Unity primitives
            if (typeReference.FullName == UnityColor_FullName) return true;
            if (typeReference.FullName == UnityColor32_FullName) return true;
            if (typeReference.FullName == UnityVector2_FullName) return true;
            if (typeReference.FullName == UnityVector3_FullName) return true;
            if (typeReference.FullName == UnityVector4_FullName) return true;
            if (typeReference.FullName == UnityQuaternion_FullName) return true;
            if (typeReference.FullName == UnityRay_FullName) return true;
            if (typeReference.FullName == UnityRay2D_FullName) return true;

            // Enum
            if (typeReference.GetEnumAsInt() != null) return true;

            // INetworkSerializable
            if (typeReference.HasInterface(INetworkSerializable_FullName)) return true;

            // Static array
            if (typeReference.IsArray) return typeReference.GetElementType().IsSerializable();

            return false;
        }

        public static TypeReference GetEnumAsInt(this TypeReference typeReference)
        {
            if (typeReference.IsArray) return null;

            try
            {
                var typeDef = typeReference.Resolve();
                return typeDef.IsEnum ? typeDef.GetEnumUnderlyingType() : null;
            }
            catch
            {
                return null;
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

        public static AssemblyDefinition AssemblyDefinitionFor(ICompiledAssembly compiledAssembly)
        {
            var assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);

            //apparently, it will happen that when we ask to resolve a type that lives inside MLAPI.Runtime, and we
            //are also postprocessing MLAPI.Runtime, type resolving will fail, because we do not actually try to resolve
            //inside the assembly we are processing. Let's make sure we do that, so that we can use postprocessor features inside
            //MLAPI.Runtime itself as well.
            assemblyResolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }
    }
}