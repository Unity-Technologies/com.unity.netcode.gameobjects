#if UNITY_2020_2_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using MLAPI.Profiling;
#endif
using UnityEditor;

namespace MLAPI
{
    [InitializeOnLoad]
    internal static class MLAPIProfilerModule
    {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
        private const string k_RpcModuleName = "MLAPI RPCs";
        private const string k_OperationModuleName = "MLAPI Operations";
        private const string k_MessageModuleName = "MLAPI Messages";

        /// <summary>
        /// This needs to be in synced with the internal dynamic module structure to provide our own counters
        /// </summary>
        [Serializable]
        private class MLAPIProfilerCounter
        {
            // Note: These fields are named this way for internal serialization
            public string Name;
            public string Category;
        }

        /// <summary>
        /// This needs to be in synced with the internal dynamic module structure to provide our own counters
        /// </summary>
        [Serializable]
        private class MLAPIProfilerModuleData
        {
            // Note: These fields are named this way for internal serialization
            public List<MLAPIProfilerCounter> ChartCounters = new List<MLAPIProfilerCounter>();
            public List<MLAPIProfilerCounter> DetailCounters = new List<MLAPIProfilerCounter>();
            public string Name;
        }

        [Serializable]
        private class MLAPIModules
        {
            // Note: These fields are named this way for internal serialization
            public List<MLAPIProfilerModuleData> Modules;
        }

        private static List<MLAPIProfilerCounter> CreateRPCCounters() => new List<MLAPIProfilerCounter>()
        {
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcSent, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcReceived, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcBatchesSent, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcBatchesReceived, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcQueueProcessed, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcInQueueSize, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.RpcOutQueueSize, Category = ProfilerCategory.Network.Name },
        };

        private static List<MLAPIProfilerCounter> CreateOperationsCounters() => new List<MLAPIProfilerCounter>()
        {
            new MLAPIProfilerCounter { Name = ProfilerConstants.Connections, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.ReceiveTickRate, Category = ProfilerCategory.Network.Name },
        };

        private static List<MLAPIProfilerCounter> CreateMessagesCounters() => new List<MLAPIProfilerCounter>()
        {
            new MLAPIProfilerCounter { Name = ProfilerConstants.NamedMessageReceived, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.UnnamedMessageReceived, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.NamedMessageSent, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.UnnamedMessageSent, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.ByteSent, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.ByteReceived, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.NetworkVarUpdates, Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { Name = ProfilerConstants.NetworkVarDeltas, Category = ProfilerCategory.Network.Name },
        };

        private delegate List<MLAPIProfilerCounter> CounterListFactoryDelegate();

        private static bool CreateMLAPIDynamicModule(ref MLAPIModules mlapiModules, string moduleName, CounterListFactoryDelegate counterListFactoryDelegate)
        {
            var module = mlapiModules.Modules.Find(x => x.Name == moduleName);
            if (module == null)
            {
                var newModule = new MLAPIProfilerModuleData
                {
                    Name = moduleName,
                    ChartCounters = counterListFactoryDelegate(),
                    DetailCounters = counterListFactoryDelegate(),
                };
                mlapiModules.Modules.Add(newModule);
                return true;
            }

            return false;
        }
#endif

        static MLAPIProfilerModule()
        {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
            var dynamicModulesJson = EditorPrefs.GetString("ProfilerWindow.DynamicModules");
            var dynamicModules = JsonUtility.FromJson<MLAPIModules>(dynamicModulesJson);

            if (dynamicModules != null)
            {
                bool wasCreated = CreateMLAPIDynamicModule(ref dynamicModules, k_RpcModuleName, CreateRPCCounters);
                wasCreated |= CreateMLAPIDynamicModule(ref dynamicModules, k_OperationModuleName, CreateOperationsCounters);
                wasCreated |= CreateMLAPIDynamicModule(ref dynamicModules, k_MessageModuleName, CreateMessagesCounters);

                if (wasCreated)
                {
                    EditorPrefs.SetString("ProfilerWindow.DynamicModules", JsonUtility.ToJson(dynamicModules));
                }
            }
#endif
        }
    }
}
