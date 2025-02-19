#if UNITY_EDITOR
using System.Text;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct NetworkManagerAnalytics
    {
        public bool IsUsingMultiplayerSDK;
        public bool UsedCMBService;
        public string NetworkTopology;
        public string NetworkTransport;
        public bool PlayerPrefabSet;
        public bool ConnectionApproval;
        public float ClientConnectionBufferTimeout;
        public bool EnsureNetworkVariableLengthSafety;
        public bool EnableSceneManagement;
        public float LoadSceneTimeOut;
        public float SpawnTimeout;
        public bool ForceSamePrefabs;
        public bool RecycleNetworkIds;
        public float NetworkIdRecycleDelay;
        public int RpcHashSize;
        public bool EnableTimeResync;
        public float TimeResyncInterval;
        public int TickRate;
        public bool IsUsingMultiplayerTools;
        public bool NetworkMessageMetrics;
        public bool NetworkProfilingMetrics;
        public bool WasServer;
        public bool WasClient;
        public float SessionDuration;
        internal void LogAnalytics(int sessionNumber)
        {
            var message = new StringBuilder();
            message.AppendLine($"{nameof(NetworkManagerAnalytics)}-{sessionNumber} Session Duration: {SessionDuration} Sever: {WasServer} Client: {WasClient}");
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
            Debug.Log($"{message}");
        }
    }
}
#endif
