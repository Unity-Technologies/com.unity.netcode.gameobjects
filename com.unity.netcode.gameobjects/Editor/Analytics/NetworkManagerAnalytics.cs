#if UNITY_EDITOR
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.Netcode.Editor
{
    [Serializable]
    internal struct NetworkManagerAnalytics : IAnalytic.IData, IEquatable<NetworkManagerAnalytics>
    {
        public bool IsDistributedAuthority;
        public bool WasServer;
        public bool WasClient;
        public bool UsedCMBService;
        public bool IsUsingMultiplayerSDK;
        public string NetworkTransport;
        public bool EnableSceneManagement;
        public int TickRate;
        public override string ToString()
        {
            var message = new StringBuilder();
            message.AppendLine($"{nameof(IsDistributedAuthority)}: {IsDistributedAuthority}");
            message.AppendLine($"{nameof(WasServer)}: {WasServer}");
            message.AppendLine($"{nameof(WasClient)}: {WasClient}");
            message.AppendLine($"{nameof(UsedCMBService)}: {UsedCMBService}");
            message.AppendLine($"{nameof(IsUsingMultiplayerSDK)}: {IsUsingMultiplayerSDK}");
            message.AppendLine($"{nameof(NetworkTransport)}: {NetworkTransport}");
            message.AppendLine($"{nameof(EnableSceneManagement)}: {EnableSceneManagement}");
            message.AppendLine($"{nameof(TickRate)}: {TickRate}");
            return message.ToString();
        }

        internal void LogAnalyticData(int sessionNumber)
        {
            Debug.Log($"{nameof(NetworkManagerAnalytics)} Session-{sessionNumber}:\n {ToString()}");
        }
        public bool Equals(NetworkManagerAnalytics other)
        {
            return IsDistributedAuthority == other.IsDistributedAuthority && WasServer == other.WasServer && WasClient == other.WasClient
                && UsedCMBService == other.UsedCMBService && IsUsingMultiplayerSDK == other.IsUsingMultiplayerSDK
                && EnableSceneManagement == other.EnableSceneManagement && TickRate == other.TickRate
                && NetworkTransport.Equals(other.NetworkTransport);
        }
    }
}
#endif
