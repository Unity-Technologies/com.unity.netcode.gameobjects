#if !UNITY_2020_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

using Assembly = System.Reflection.Assembly;

namespace MLAPI.Editor.CodeGen
{
    internal static class ILPostProcessProgram
    {
        private static ILPostProcessor[] s_ILPostProcessors { get; set; }

        [InitializeOnLoadMethod]
        private static void OnInitializeOnLoad()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
            s_ILPostProcessors = FindAllPostProcessors();
        }

        private static ILPostProcessor[] FindAllPostProcessors()
        {
            var typesDerivedFrom = TypeCache.GetTypesDerivedFrom<ILPostProcessor>();
            var localILPostProcessors = new List<ILPostProcessor>(typesDerivedFrom.Count);

            foreach (var typeCollection in typesDerivedFrom)
            {
                try
                {
                    localILPostProcessors.Add((ILPostProcessor)Activator.CreateInstance(typeCollection));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Could not create {nameof(ILPostProcessor)} ({typeCollection.FullName}):{Environment.NewLine}{exception.StackTrace}");
                }
            }

            // Default sort by type full name
            localILPostProcessors.Sort((left, right) => string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal));

            return localILPostProcessors.ToArray();
        }

        private static void OnCompilationFinished(string targetAssembly, CompilerMessage[] messages)
        {
            if (messages.Length > 0)
            {
                if (messages.Any(msg => msg.type == CompilerMessageType.Error))
                {
                    return;
                }
            }

            // Should not run on the editor only assemblies
            if (targetAssembly.Contains("-Editor") || targetAssembly.Contains(".Editor"))
            {
                return;
            }

            // Should not run on Unity Engine modules but we can run on the MLAPI Runtime DLL 
            if ((targetAssembly.Contains("com.unity") || Path.GetFileName(targetAssembly).StartsWith("Unity")) && !targetAssembly.Contains("Unity.Multiplayer."))
            {
                return;
            }

            // Debug.Log($"Running MLAPI ILPP on {targetAssembly}");

            var outputDirectory = $"{Application.dataPath}/../{Path.GetDirectoryName(targetAssembly)}";
            var unityEngine = string.Empty;
            var mlapiRuntimeAssemblyPath = string.Empty;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var usesMLAPI = false;
            var foundThisAssembly = false;

            var depenencyPaths = new List<string>();
            foreach (var assembly in assemblies)
            {
                // Find the assembly currently being compiled from domain assembly list and check if it's using unet
                if (assembly.GetName().Name == Path.GetFileNameWithoutExtension(targetAssembly))
                {
                    foundThisAssembly = true;
                    foreach (var dependency in assembly.GetReferencedAssemblies())
                    {
                        // Since this assembly is already loaded in the domain this is a no-op and returns the
                        // already loaded assembly
                        depenencyPaths.Add(Assembly.Load(dependency).Location);
                        if (dependency.Name.Contains(CodeGenHelpers.RuntimeAssemblyName))
                        {
                            usesMLAPI = true;
                        }
                    }
                }

                try
                {
                    if (assembly.Location.Contains("UnityEngine.CoreModule"))
                    {
                        unityEngine = assembly.Location;
                    }

                    if (assembly.Location.Contains(CodeGenHelpers.RuntimeAssemblyName))
                    {
                        mlapiRuntimeAssemblyPath = assembly.Location;
                    }
                }
                catch (NotSupportedException)
                {
                    // in memory assembly, can't get location
                }
            }

            if (!foundThisAssembly)
            {
                // Target assembly not found in current domain, trying to load it to check references 
                // will lead to trouble in the build pipeline, so lets assume it should go to weaver.
                // Add all assemblies in current domain to dependency list since there could be a 
                // dependency lurking there (there might be generated assemblies so ignore file not found exceptions).
                // (can happen in runtime test framework on editor platform and when doing full library reimport)
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        if (!(assembly.ManifestModule is System.Reflection.Emit.ModuleBuilder))
                        {
                            depenencyPaths.Add(Assembly.Load(assembly.GetName().Name).Location);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }

                usesMLAPI = true;
            }

            // We check if we are the MLAPI!
            if (!usesMLAPI)
            {
                // we shall also check and see if it we are ourself
                usesMLAPI = targetAssembly.Contains(CodeGenHelpers.RuntimeAssemblyName);
            }

            if (!usesMLAPI)
            {
                return;
            }

            if (string.IsNullOrEmpty(unityEngine))
            {
                Debug.LogError("Failed to find UnityEngine assembly");
                return;
            }

            if (string.IsNullOrEmpty(mlapiRuntimeAssemblyPath))
            {
                Debug.LogError("Failed to find mlapi runtime assembly");
                return;
            }

            var assemblyPathName = Path.GetFileName(targetAssembly);

            var targetCompiledAssembly = new ILPostProcessCompiledAssembly(assemblyPathName, depenencyPaths.ToArray(), null, outputDirectory);

            void WriteAssembly(InMemoryAssembly inMemoryAssembly, string outputPath, string assName)
            {
                if (inMemoryAssembly == null)
                {
                    throw new ArgumentException("InMemoryAssembly has never been accessed or modified");
                }

                var asmPath = Path.Combine(outputPath, assName);
                var pdbFileName = $"{Path.GetFileNameWithoutExtension(assName)}.pdb";
                var pdbPath = Path.Combine(outputPath, pdbFileName);

                File.WriteAllBytes(asmPath, inMemoryAssembly.PeData);
                File.WriteAllBytes(pdbPath, inMemoryAssembly.PdbData);
            }

            foreach (var i in s_ILPostProcessors)
            {
                var result = i.Process(targetCompiledAssembly);
                if (result == null) continue;

                if (result.Diagnostics.Count > 0)
                {
                    Debug.LogError($"{nameof(ILPostProcessor)} - {i.GetType().Name} failed to run on {targetCompiledAssembly.Name}");

                    foreach (var message in result.Diagnostics)
                    {
                        switch (message.DiagnosticType)
                        {
                            case DiagnosticType.Error:
                                Debug.LogError($"{nameof(ILPostProcessor)} Error - {message.MessageData} {message.File}:{message.Line}");
                                break;
                            case DiagnosticType.Warning:
                                Debug.LogWarning($"{nameof(ILPostProcessor)} Warning - {message.MessageData} {message.File}:{message.Line}");
                                break;
                        }
                    }

                    continue;
                }

                // we now need to write out the result?
                WriteAssembly(result.InMemoryAssembly, outputDirectory, assemblyPathName);
            }
        }
    }
}
#endif