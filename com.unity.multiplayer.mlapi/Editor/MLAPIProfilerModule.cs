using System;
using System.Collections.Generic;
using MLAPI.Profiling;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

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
            public string m_Name;
            public string m_Category;
        }

        /// <summary>
        /// This needs to be in synced with the internal dynamic module structure to provide our own counters
        /// </summary>
        [Serializable]
        private class MLAPIProfilerModuleData
        {
            public List<MLAPIProfilerCounter> m_ChartCounters = new List<MLAPIProfilerCounter>();
            public List<MLAPIProfilerCounter> m_DetailCounters = new List<MLAPIProfilerCounter>();
            public string m_Name;
        }

        [Serializable]
        private class MLAPIModules
        {
            public List<MLAPIProfilerModuleData> m_Modules;
        }

        private static List<MLAPIProfilerCounter> CreateRPCCounters() => new List<MLAPIProfilerCounter>()
        {
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCsSent, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCsReceived, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCBatchesSent, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCBatchesReceived, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCQueueProcessed, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCsInQueueSize, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.RPCsOutQueueSize, m_Category = ProfilerCategory.Network.Name },
        };

        private static List<MLAPIProfilerCounter> CreateOperationsCounters() => new List<MLAPIProfilerCounter>()
        {
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.Connections, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.ReceiveTickRate, m_Category = ProfilerCategory.Network.Name },
        };

        private static List<MLAPIProfilerCounter> CreateMessagesCounters() => new List<MLAPIProfilerCounter>()
        {
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.NamedMessagesReceived, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.UnnamedMessagesReceived, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.BytesSent, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.BytesReceived, m_Category = ProfilerCategory.Network.Name },
            new MLAPIProfilerCounter { m_Name = ProfilerConstants.NetworkVarsReceived, m_Category = ProfilerCategory.Network.Name },
        };

        private delegate List<MLAPIProfilerCounter> CounterListFactoryDelegate();

        private static bool CreateMLAPIDynamicModule(ref MLAPIModules mlapiModules, string moduleName, CounterListFactoryDelegate counterListFactoryDelegate)
        {
            var module = mlapiModules.m_Modules.Find(x => x.m_Name == moduleName);
            if (module == null)
            {
                var newModule = new MLAPIProfilerModuleData();
                newModule.m_Name = moduleName;
                newModule.m_ChartCounters = counterListFactoryDelegate();
                newModule.m_DetailCounters = counterListFactoryDelegate();
                mlapiModules.m_Modules.Add(newModule);
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