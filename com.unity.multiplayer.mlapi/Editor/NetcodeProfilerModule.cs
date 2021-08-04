using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.Editor
{
    [InitializeOnLoad]
    internal static class NetcodeProfilerModule
    {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
        private const string k_RpcModuleName = "Netcode RPCs";
        private const string k_OperationModuleName = "Netcode Operations";
        private const string k_MessageModuleName = "Netcode Messages";

#pragma warning disable IDE1006 // disable naming rule violation check
        /// <summary>
        /// This needs to be in synced with the internal dynamic module structure to provide our own counters
        /// </summary>
        [Serializable]
        private class NetcodeProfilerCounter
        {
            // Note: These fields are named this way for internal serialization
            public string m_Name;
            public string m_Category;
        }

        /// <summary>
        /// This needs to be in synced with the internal dynamic module structure to provide our own counters
        /// </summary>
        [Serializable]
        private class NetcodeProfilerModuleData
        {
            // Note: These fields are named this way for internal serialization
            public List<NetcodeProfilerCounter> m_ChartCounters = new List<NetcodeProfilerCounter>();
            public List<NetcodeProfilerCounter> m_DetailCounters = new List<NetcodeProfilerCounter>();
            public string m_Name;
        }

        /// <summary>
        /// This needs to be in synced with the internal dynamic module structure to provide our own counters
        /// </summary>
        [Serializable]
        private class NetcodeModules
        {
            // Note: These fields are named this way for internal serialization
            public List<NetcodeProfilerModuleData> m_Modules;
        }
#pragma warning restore IDE1006 // restore naming rule violation check

        private static List<NetcodeProfilerCounter> CreateRPCCounters() => new List<NetcodeProfilerCounter>()
        {
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.RpcSent, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.RpcReceived, m_Category = ProfilerCategory.Network.Name },
        };

        private static List<NetcodeProfilerCounter> CreateOperationsCounters() => new List<NetcodeProfilerCounter>()
        {
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.Connections, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.ReceiveTickRate, m_Category = ProfilerCategory.Network.Name },
        };

        private static List<NetcodeProfilerCounter> CreateMessagesCounters() => new List<NetcodeProfilerCounter>()
        {
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.NamedMessageReceived, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.UnnamedMessageReceived, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.NamedMessageSent, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.UnnamedMessageSent, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.ByteSent, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.ByteReceived, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.NetworkVarUpdates, m_Category = ProfilerCategory.Network.Name },
            new NetcodeProfilerCounter { m_Name = ProfilerConstants.NetworkVarDeltas, m_Category = ProfilerCategory.Network.Name },
        };

        private delegate List<NetcodeProfilerCounter> CounterListFactoryDelegate();

        private static bool CreateNetcodeDynamicModule(ref NetcodeModules netcodeModules, string moduleName, CounterListFactoryDelegate counterListFactoryDelegate)
        {
            var module = netcodeModules.m_Modules.Find(x => x.m_Name == moduleName);
            if (module == null)
            {
                var newModule = new NetcodeProfilerModuleData
                {
                    m_Name = moduleName,
                    m_ChartCounters = counterListFactoryDelegate(),
                    m_DetailCounters = counterListFactoryDelegate(),
                };
                netcodeModules.m_Modules.Add(newModule);

                return true;
            }

            return false;
        }
#endif

        static NetcodeProfilerModule()
        {
#if UNITY_2020_2_OR_NEWER && ENABLE_PROFILER
            var dynamicModulesJson = EditorPrefs.GetString("ProfilerWindow.DynamicModules");
            var dynamicModules = JsonUtility.FromJson<NetcodeModules>(dynamicModulesJson);

            if (dynamicModules != null)
            {
                bool wasCreated = CreateNetcodeDynamicModule(ref dynamicModules, k_RpcModuleName, CreateRPCCounters);
                wasCreated |= CreateNetcodeDynamicModule(ref dynamicModules, k_OperationModuleName, CreateOperationsCounters);
                wasCreated |= CreateNetcodeDynamicModule(ref dynamicModules, k_MessageModuleName, CreateMessagesCounters);

                if (wasCreated)
                {
                    EditorPrefs.SetString("ProfilerWindow.DynamicModules", JsonUtility.ToJson(dynamicModules));
                }
            }
#endif
        }
    }
}
