using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Collections;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEngine;

namespace Unity.Netcode.Editor.CodeGen
{
    internal static class CodeGenHelpers
    {
        public const string DotnetModuleName = "netstandard.dll";
        public const string UnityModuleName = "UnityEngine.CoreModule.dll";
        public const string NetcodeModuleName = "Unity.Netcode.Runtime.dll";

        public const string RuntimeAssemblyName = "Unity.Netcode.Runtime";

        public static readonly string NetworkBehaviour_FullName = typeof(NetworkBehaviour).FullName;
        public static readonly string INetworkMessage_FullName = typeof(INetworkMessage).FullName;
        public static readonly string ServerRpcAttribute_FullName = typeof(ServerRpcAttribute).FullName;
        public static readonly string ClientRpcAttribute_FullName = typeof(ClientRpcAttribute).FullName;
        public static readonly string ServerRpcParams_FullName = typeof(ServerRpcParams).FullName;
        public static readonly string ClientRpcParams_FullName = typeof(ClientRpcParams).FullName;
        public static readonly string ClientRpcSendParams_FullName = typeof(ClientRpcSendParams).FullName;
        public static readonly string ClientRpcReceiveParams_FullName = typeof(ClientRpcReceiveParams).FullName;
        public static readonly string ServerRpcSendParams_FullName = typeof(ServerRpcSendParams).FullName;
        public static readonly string ServerRpcReceiveParams_FullName = typeof(ServerRpcReceiveParams).FullName;
        public static readonly string INetworkSerializable_FullName = typeof(INetworkSerializable).FullName;
        public static readonly string INetworkSerializeByMemcpy_FullName = typeof(INetworkSerializeByMemcpy).FullName;
        public static readonly string IUTF8Bytes_FullName = typeof(IUTF8Bytes).FullName;
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
                // Note: this won't catch generics correctly.
                //
                //     class Foo<T>: IInterface<T> {}
                //     class Bar: Foo<int> {}
                //
                // Bar.HasInterface(IInterface<int>) -> returns false even though it should be true.
                //
                // This can be fixed (see GetAllFieldsAndResolveGenerics() in NetworkBehaviourILPP to understand how)
                // but right now we don't need that to work so it's left alone to reduce complexity
                if (typeDef.BaseType.HasInterface(interfaceTypeFullName))
                {
                    return true;
                }
                var typeFaces = typeDef.Interfaces;
                return typeFaces.Any(iface => iface.InterfaceType.FullName == interfaceTypeFullName);
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
            if (typeReference == typeSystem.Boolean)
            {
                return true;
            }

            if (typeReference == typeSystem.Char)
            {
                return true;
            }

            if (typeReference == typeSystem.SByte)
            {
                return true;
            }

            if (typeReference == typeSystem.Byte)
            {
                return true;
            }

            if (typeReference == typeSystem.Int16)
            {
                return true;
            }

            if (typeReference == typeSystem.UInt16)
            {
                return true;
            }

            if (typeReference == typeSystem.Int32)
            {
                return true;
            }

            if (typeReference == typeSystem.UInt32)
            {
                return true;
            }

            if (typeReference == typeSystem.Int64)
            {
                return true;
            }

            if (typeReference == typeSystem.UInt64)
            {
                return true;
            }

            if (typeReference == typeSystem.Single)
            {
                return true;
            }

            if (typeReference == typeSystem.Double)
            {
                return true;
            }

            if (typeReference == typeSystem.String)
            {
                return true;
            }

            // Unity primitives
            if (typeReference.FullName == UnityColor_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityColor32_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityVector2_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityVector3_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityVector4_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityQuaternion_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityRay_FullName)
            {
                return true;
            }

            if (typeReference.FullName == UnityRay2D_FullName)
            {
                return true;
            }

            // Enum
            if (typeReference.GetEnumAsInt() != null)
            {
                return true;
            }

            // INetworkSerializable
            if (typeReference.HasInterface(INetworkSerializable_FullName))
            {
                return true;
            }

            // Static array
            if (typeReference.IsArray)
            {
                return typeReference.GetElementType().IsSerializable();
            }

            return false;
        }

        public static TypeReference GetEnumAsInt(this TypeReference typeReference)
        {
            if (typeReference.IsArray)
            {
                return null;
            }

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

        private static void SearchForBaseModulesRecursive(AssemblyDefinition assemblyDefinition, PostProcessorAssemblyResolver assemblyResolver, ref ModuleDefinition unityModule, ref ModuleDefinition netcodeModule, HashSet<string> visited)
        {

            foreach (var module in assemblyDefinition.Modules)
            {
                if (module == null)
                {
                    continue;
                }

                if (unityModule != null && netcodeModule != null)
                {
                    return;
                }

                if (unityModule == null && module.Name == UnityModuleName)
                {
                    unityModule = module;
                    continue;
                }

                if (netcodeModule == null && module.Name == NetcodeModuleName)
                {
                    netcodeModule = module;
                    continue;
                }
            }
            if (unityModule != null && netcodeModule != null)
            {
                return;
            }

            foreach (var assemblyNameReference in assemblyDefinition.MainModule.AssemblyReferences)
            {
                if (assemblyNameReference == null)
                {
                    continue;
                }
                if (visited.Contains(assemblyNameReference.Name))
                {
                    continue;
                }

                visited.Add(assemblyNameReference.Name);

                var assembly = assemblyResolver.Resolve(assemblyNameReference);
                if (assembly == null)
                {
                    continue;
                }
                SearchForBaseModulesRecursive(assembly, assemblyResolver, ref unityModule, ref netcodeModule, visited);

                if (unityModule != null && netcodeModule != null)
                {
                    return;
                }
            }
        }

        public static (ModuleDefinition UnityModule, ModuleDefinition NetcodeModule) FindBaseModules(AssemblyDefinition assemblyDefinition, PostProcessorAssemblyResolver assemblyResolver)
        {
            ModuleDefinition unityModule = null;
            ModuleDefinition netcodeModule = null;
            var visited = new HashSet<string>();
            SearchForBaseModulesRecursive(assemblyDefinition, assemblyResolver, ref unityModule, ref netcodeModule, visited);

            return (unityModule, netcodeModule);
        }
    }
}
