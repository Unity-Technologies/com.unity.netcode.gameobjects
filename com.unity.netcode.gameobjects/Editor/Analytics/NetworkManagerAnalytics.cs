#if UNITY_EDITOR
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.Netcode.Editor
{
    [Serializable]
    internal struct NetworkManagerAnalytics : IAnalytic.IData
    {
        public bool IsUsingMultiplayerSDK;
        public bool UsedCMBService;
        public string NetworkTopology;
        public string NetworkTransport;
        public bool PlayerPrefabSet;
        public bool ConnectionApproval;
        public int ClientConnectionBufferTimeout;
        public bool EnsureNetworkVariableLengthSafety;
        public bool EnableSceneManagement;
        public int LoadSceneTimeOut;
        public float SpawnTimeout;
        public bool ForceSamePrefabs;
        public bool RecycleNetworkIds;
        public float NetworkIdRecycleDelay;
        public int RpcHashSize;
        public bool EnableTimeResync;
        public int TimeResyncInterval;
        public int TickRate;
        public bool IsUsingMultiplayerTools;
        public bool NetworkMessageMetrics;
        public bool NetworkProfilingMetrics;
        public bool WasServer;
        public bool WasClient;
        public float SessionDuration;

        public override string ToString()
        {
            var message = new StringBuilder();
            message.AppendLine($"{nameof(WasServer)}: {WasServer}");
            message.AppendLine($"{nameof(WasClient)}: {WasClient}");
            message.AppendLine($"{nameof(SessionDuration)}: {SessionDuration}");
            message.AppendLine($"{nameof(IsUsingMultiplayerSDK)}: {IsUsingMultiplayerSDK}");
            message.AppendLine($"{nameof(UsedCMBService)}: {UsedCMBService}");
            message.AppendLine($"{nameof(NetworkTopology)}: {NetworkTopology}");
            message.AppendLine($"{nameof(NetworkTransport)}: {NetworkTransport}");
            message.AppendLine($"{nameof(PlayerPrefabSet)}: {PlayerPrefabSet}");
            message.AppendLine($"{nameof(ConnectionApproval)}: {ConnectionApproval}");
            message.AppendLine($"{nameof(ClientConnectionBufferTimeout)}: {ClientConnectionBufferTimeout}");
            message.AppendLine($"{nameof(EnsureNetworkVariableLengthSafety)}: {EnsureNetworkVariableLengthSafety}");
            message.AppendLine($"{nameof(EnableSceneManagement)}: {EnableSceneManagement}");
            message.AppendLine($"{nameof(LoadSceneTimeOut)}: {LoadSceneTimeOut}");
            message.AppendLine($"{nameof(SpawnTimeout)}: {SpawnTimeout}");
            message.AppendLine($"{nameof(ForceSamePrefabs)}: {ForceSamePrefabs}");
            message.AppendLine($"{nameof(RecycleNetworkIds)}: {RecycleNetworkIds}");
            message.AppendLine($"{nameof(NetworkIdRecycleDelay)}: {NetworkIdRecycleDelay}");
            message.AppendLine($"{nameof(RpcHashSize)}: {RpcHashSize}");
            message.AppendLine($"{nameof(EnableTimeResync)}: {EnableTimeResync}");
            message.AppendLine($"{nameof(TimeResyncInterval)}: {TimeResyncInterval}");
            message.AppendLine($"{nameof(TickRate)}: {TickRate}");
            message.AppendLine($"{nameof(IsUsingMultiplayerTools)}: {IsUsingMultiplayerTools}");
            message.AppendLine($"{nameof(NetworkMessageMetrics)}: {NetworkMessageMetrics}");
            message.AppendLine($"{nameof(NetworkProfilingMetrics)}: {NetworkProfilingMetrics}");
            return message.ToString();
        }
        internal void LogAnalytics(int sessionNumber)
        {
            Debug.Log($"{ToString()}");
        }
    }
}
#endif
