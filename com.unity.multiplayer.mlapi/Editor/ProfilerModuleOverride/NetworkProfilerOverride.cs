using System;
using System.Collections.Generic;
using MLAPI.Profiling;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace ProfilerModuleOverride
{
    [InitializeOnLoad]
    public class NetworkProfilerOverride
    {
#if UNITY_2020_2_OR_NEWER
        const string RPCModuleName = "MLAPI RPCs";
        const string OperationModuleName = "MLAPI Operations";
        const string MessageModuleName = "MLAPI Messages";

        [System.Serializable]
        public class MLAPIProfilerCounter
        {
            public string m_Name;
            public string m_Category;
        }

        [System.Serializable]
        public class MLAPIProfilerModuleData
        {
            public MLAPIProfilerModuleData()
            {
                m_ChartCounters = new List<MLAPIProfilerCounter>();
                m_DetailCounters = new List<MLAPIProfilerCounter>();
            }

            public List<MLAPIProfilerCounter> m_ChartCounters;
            public List<MLAPIProfilerCounter> m_DetailCounters;
            public string m_Name;
        }

        [System.Serializable]
        public class MLAPIModules
        {
            public List<MLAPIProfilerModuleData> m_Modules;
        }

        private static List<MLAPIProfilerCounter> CreateRPCCounters()
        {
            var list = new List<MLAPIProfilerCounter>();
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCsSent.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCsReceived.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCBatchesSent.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCBatchesReceived.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCQueueProcessed.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCsInQueueSize.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfRPCsOutQueueSize.ToString(), m_Category = ProfilerCategory.Network.Name });
            return list;
        }

        private static List<MLAPIProfilerCounter> CreateOperationsCounters()
        {
            var list = new List<MLAPIProfilerCounter>();
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfConnections.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.ReceiveTickRate.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfTransportSends.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfTransportSendQueues.ToString(), m_Category = ProfilerCategory.Network.Name });
            return list;
        }

        private static List<MLAPIProfilerCounter> CreateMessagesCounters()
        {
            var list = new List<MLAPIProfilerCounter>();
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfNamedMessages.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberOfUnnamedMessages.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberBytesSent.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberBytesReceived.ToString(), m_Category = ProfilerCategory.Network.Name });
            list.Add(new MLAPIProfilerCounter() { m_Name = ProfilerConstants.NumberNetworkVarsReceived.ToString(), m_Category = ProfilerCategory.Network.Name });
            return list;
        }

        private delegate List<MLAPIProfilerCounter> CounterFunction();

        private static bool UpdateOrCreateModule(ref MLAPIModules mlapiModules, string moduleName, CounterFunction counterFunction)
        {
            bool hasChanged = false;
            var module = mlapiModules.m_Modules.Find(x => x.m_Name == moduleName);
            if (module == null)
            {
                var newModule = new MLAPIProfilerModuleData();
                newModule.m_Name = moduleName;
                newModule.m_ChartCounters = counterFunction();
                newModule.m_DetailCounters = counterFunction();
                mlapiModules.m_Modules.Add(newModule);
                hasChanged = true;
            }
            else
            {
                var counters = counterFunction();
                if (module.m_ChartCounters.Count != counters.Count || module.m_DetailCounters.Count != counters.Count)
                {
                    module.m_ChartCounters = counters;
                    module.m_DetailCounters = counters;
                    hasChanged = true;
                }
            }

            return hasChanged;
        }
#endif

        static NetworkProfilerOverride()
        {
            // NetworkingOperationsProfilerOverrides.drawDetailsViewOverride = OperationsDrawDetailsViewOverride;
            // NetworkingMessagesProfilerOverrides.getCustomChartCounters = GetCustomMessageCounters;
            // NetworkingOperationsProfilerOverrides.getCustomChartCounters = GetCustomOperationsCounters;
#if UNITY_2020_2_OR_NEWER
            var mlapiModulesJson = EditorPrefs.GetString("ProfilerWindow.DynamicModules");

            var mlapiModules = JsonUtility.FromJson<MLAPIModules>(mlapiModulesJson);

            if (mlapiModules != null)
            {
                bool hasChanged = UpdateOrCreateModule(ref mlapiModules, RPCModuleName, CreateRPCCounters);
                hasChanged = hasChanged && UpdateOrCreateModule(ref mlapiModules, OperationModuleName, CreateOperationsCounters);
                hasChanged = hasChanged && UpdateOrCreateModule(ref mlapiModules, MessageModuleName, CreateMessagesCounters);
                if (hasChanged)
                {
                    EditorPrefs.SetString("ProfilerWindow.DynamicModules", JsonUtility.ToJson(mlapiModules));
                }
            }

            MLAPI.NetworkingManager.OnPerformanceDataEvent += OnPerformanceTickData;
#endif
        }

        static void OnPerformanceTickData(PerformanceTickData tickData)
        {

#if UNITY_2020_2_OR_NEWER
            //Operations
            ProfilerInfo.ConnectionsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfConnections.ToString());
            ProfilerInfo.TickRateCounterValue.Value += tickData.GetData(ProfilerConstants.ReceiveTickRate.ToString());
            ProfilerInfo.TransportSendsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfTransportSends.ToString());
            ProfilerInfo.TransportSendQueuesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfTransportSendQueues.ToString());

            //Messages
            ProfilerInfo.NamedMessagesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfNamedMessages.ToString());
            ProfilerInfo.UnnamedMessagesCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfUnnamedMessages.ToString());
            ProfilerInfo.BytesSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberBytesSent.ToString());
            ProfilerInfo.BytesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberBytesReceived.ToString());
            ProfilerInfo.NetworkVarsCounterValue.Value += tickData.GetData(ProfilerConstants.NumberNetworkVarsReceived.ToString());

            //RPCs
            ProfilerInfo.RPCsSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsSent.ToString());
            ProfilerInfo.RPCsReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsReceived.ToString());
            ProfilerInfo.RPCBatchesSentCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesSent.ToString());
            ProfilerInfo.RPCBatchesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesReceived.ToString());
            ProfilerInfo.RPCBatchesReceivedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCBatchesReceived.ToString());
            ProfilerInfo.RPCQueueProcessedCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCQueueProcessed.ToString());
            ProfilerInfo.RPCsInQueueSizeCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsInQueueSize.ToString());
            ProfilerInfo.RPCsOutQueueSizeCounterValue.Value += tickData.GetData(ProfilerConstants.NumberOfRPCsOutQueueSize.ToString());
#endif
        }

        // private static List<NetworkCounterData> GetCustomMessageCounters()
        // {
        //     return new List<NetworkCounterData>
        //     {
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfNamedMessages.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfUnnamedMessages.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberBytesSent.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberBytesReceived.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberNetworkVarsReceived.ToString() },
        //     };
        // }
        //
        // private static List<NetworkCounterData> GetCustomOperationsCounters()
        // {
        //     return new List<NetworkCounterData>
        //     {
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfConnections.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.ReceiveTickRate.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfTransportSends.ToString() },
        //         new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfTransportSendQueues.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCsSent.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCsReceived.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCBatchesSent.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCBatchesReceived.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCQueueProcessed.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCsInQueueSize.ToString() },
        //         // new NetworkCounterData { category = ProfilerCategory.Network.Name, name = ProfilerConstants.NumberOfRPCsOutQueueSize.ToString() },
        //     };
        // }
        //
        // public static ClientInfo[] GetClientFromProfilerStream(int frame)
        // {
        //     using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frame, 0,
        //                                                                     HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnDontSort, false))
        //     {
        //         var returnVal =  new List<ClientInfo> { };
        //         if (frameData != null && frameData.valid)
        //         {
        //             var count = frameData.GetFrameMetaDataCount(ProfilerInfo.kNetworkingOperationsProfilerModuleGUID,
        //                 ProfilerInfo.kClientDataTag);
        //             for (int i = 0; i < count; i++)
        //             {
        //                 var clientInfos =
        //                     frameData.GetFrameMetaData<ClientInfo>(ProfilerInfo.kNetworkingOperationsProfilerModuleGUID, ProfilerInfo.kClientDataTag, i);
        //                 returnVal.AddRange(clientInfos);
        //             }
        //         }
        //         return returnVal.ToArray();
        //     }
        // }
        // static void OperationsDrawDetailsViewOverride(Rect position, int frame)
        // {
        //     var clientInfos = GetClientFromProfilerStream(frame);
        //
        //     if (clientInfos.Any())
        //     {
        //         EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        //         EditorGUILayout.LabelField("ClientID|GameObject");
        //         EditorGUILayout.LabelField("ChannelName");
        //         EditorGUILayout.LabelField("MessageName");
        //         EditorGUILayout.LabelField("TickType");
        //         EditorGUILayout.LabelField("BytesSent");
        //         EditorGUILayout.EndHorizontal();
        //         EditorGUILayout.Separator();
        //     }
        //
        //     unsafe
        //     {
        //         foreach (var clientInfo in clientInfos)
        //         {
        //             const int arraySize = 320;
        //             var arr = new byte[arraySize];
        //             Marshal.Copy((IntPtr) clientInfo.targetName, arr, 0, arraySize);
        //             var targetName = Encoding.ASCII.GetString(arr);
        //             Marshal.Copy((IntPtr) clientInfo.channelName, arr, 0, arraySize);
        //             var channelName = Encoding.ASCII.GetString(arr);
        //             Marshal.Copy((IntPtr) clientInfo.messageName, arr, 0, arraySize);
        //             var messageName = Encoding.ASCII.GetString(arr);
        //
        //             EditorGUILayout.BeginHorizontal();
        //             EditorGUILayout.LabelField($"{targetName}");
        //             EditorGUILayout.LabelField($"{channelName}");
        //             EditorGUILayout.LabelField($"{messageName}");
        //             EditorGUILayout.LabelField($"{clientInfo.type}");
        //             EditorGUILayout.LabelField($"{clientInfo.bytesSent}");
        //             EditorGUILayout.EndHorizontal();
        //         }
        //     }
        // }
    }
}
