#if MULTIPLAYER_TOOLS
using System;
using System.Collections.Generic;
using Unity.Multiplayer.Tools;
using Unity.Multiplayer.Tools.MetricTypes;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Profiling;

namespace Unity.Netcode
{
    internal class NetworkMetrics : INetworkMetrics
    {
        static Dictionary<uint, string> s_SceneEventTypeNames;

        static ProfilerMarker s_FrameDispatch = new ProfilerMarker($"{nameof(NetworkMetrics)}.DispatchFrame");

        static NetworkMetrics()
        {
            s_SceneEventTypeNames = new Dictionary<uint, string>();
            foreach (SceneEventType type in Enum.GetValues(typeof(SceneEventType)))
            {
                s_SceneEventTypeNames[(uint)type] = type.ToString();
            }
        }

        private static string GetSceneEventTypeName(uint typeCode)
        {
            if (!s_SceneEventTypeNames.TryGetValue(typeCode, out string name))
            {
                name = "Unknown";
            }

            return name;
        }

        private readonly INetcodeForGameObjectsDispatcher m_Dispatcher;

        public NetworkMetrics()
        {
            m_Dispatcher = new NetcodeForGameObjectsDispatcher();
            Dispatcher.RegisterObserver(NetcodeObserver.Observer);
        }

        internal IMetricDispatcher Dispatcher => m_Dispatcher.Dispatcher;

        public void SetConnectionId(ulong connectionId)
        {
            Dispatcher.SetConnectionId(connectionId);
        }

        public void TrackTransportBytesSent(long bytesCount)
        {
            m_Dispatcher.TrackTransportBytesSent(bytesCount);
        }

        public void TrackTransportBytesReceived(long bytesCount)
        {
            m_Dispatcher.TrackTransportBytesReceived(bytesCount);
        }

        public void TrackNetworkMessageSent(ulong receivedClientId, string messageType, long bytesCount)
        {
            m_Dispatcher.TrackNetworkMessageSent(new NetworkMessageEvent(new ConnectionInfo(receivedClientId), messageType, bytesCount));
        }

        public void TrackNetworkMessageReceived(ulong senderClientId, string messageType, long bytesCount)
        {
            m_Dispatcher.TrackNetworkMessageReceived(new NetworkMessageEvent(new ConnectionInfo(senderClientId), messageType, bytesCount));
        }

        public void TrackNamedMessageSent(ulong receiverClientId, string messageName, long bytesCount)
        {
            m_Dispatcher.TrackNamedMessageSent(new NamedMessageEvent(new ConnectionInfo(receiverClientId), messageName, bytesCount));
        }

        public void TrackNamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, string messageName, long bytesCount)
        {
            foreach (var receiver in receiverClientIds)
            {
                TrackNamedMessageSent(receiver, messageName, bytesCount);
            }
        }

        public void TrackNamedMessageReceived(ulong senderClientId, string messageName, long bytesCount)
        {
            m_Dispatcher.TrackNamedMessageReceived(new NamedMessageEvent(new ConnectionInfo(senderClientId), messageName, bytesCount));
        }

        public void TrackUnnamedMessageSent(ulong receiverClientId, long bytesCount)
        {
            m_Dispatcher.TrackUnnamedMessageSent(new UnnamedMessageEvent(new ConnectionInfo(receiverClientId), bytesCount));
        }

        public void TrackUnnamedMessageSent(IReadOnlyCollection<ulong> receiverClientIds, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackUnnamedMessageSent(receiverClientId, bytesCount);
            }
        }

        public void TrackUnnamedMessageReceived(ulong senderClientId, long bytesCount)
        {
            m_Dispatcher.TrackUnnamedMessageReceived(new UnnamedMessageEvent(new ConnectionInfo(senderClientId), bytesCount));
        }

        public void TrackNetworkVariableDeltaSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_Dispatcher.TrackNetworkVariableDeltaSent(
                new NetworkVariableEvent(
                    new ConnectionInfo(receiverClientId),
                    GetObjectIdentifier(networkObject),
                    variableName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackNetworkVariableDeltaReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string variableName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_Dispatcher.TrackNetworkVariableDeltaReceived(
                new NetworkVariableEvent(
                    new ConnectionInfo(senderClientId),
                    GetObjectIdentifier(networkObject),
                    variableName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackOwnershipChangeSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            m_Dispatcher.TrackOwnershipChangeSent(new OwnershipChangeEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
        }

        public void TrackOwnershipChangeReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            m_Dispatcher.TrackOwnershipChangeReceived(new OwnershipChangeEvent(new ConnectionInfo(senderClientId),
                GetObjectIdentifier(networkObject), bytesCount));
        }

        public void TrackObjectSpawnSent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            m_Dispatcher.TrackObjectSpawnSent(new ObjectSpawnedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
        }

        public void TrackObjectSpawnReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            m_Dispatcher.TrackObjectSpawnReceived(new ObjectSpawnedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));
        }

        public void TrackObjectDestroySent(ulong receiverClientId, NetworkObject networkObject, long bytesCount)
        {
            m_Dispatcher.TrackObjectDestroySent(new ObjectDestroyedEvent(new ConnectionInfo(receiverClientId), GetObjectIdentifier(networkObject), bytesCount));
        }

        public void TrackObjectDestroyReceived(ulong senderClientId, NetworkObject networkObject, long bytesCount)
        {
            m_Dispatcher.TrackObjectDestroyReceived(new ObjectDestroyedEvent(new ConnectionInfo(senderClientId), GetObjectIdentifier(networkObject), bytesCount));
        }

        public void TrackRpcSent(
            ulong receiverClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_Dispatcher.TrackRpcSent(
                new RpcEvent(
                    new ConnectionInfo(receiverClientId),
                    GetObjectIdentifier(networkObject),
                    rpcName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackRpcSent(
            ulong[] receiverClientIds,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackRpcSent(receiverClientId, networkObject, rpcName, networkBehaviourName, bytesCount);
            }
        }

        public void TrackRpcReceived(
            ulong senderClientId,
            NetworkObject networkObject,
            string rpcName,
            string networkBehaviourName,
            long bytesCount)
        {
            m_Dispatcher.TrackRpcReceived(
                new RpcEvent(new ConnectionInfo(senderClientId),
                    GetObjectIdentifier(networkObject),
                    rpcName,
                    networkBehaviourName,
                    bytesCount));
        }

        public void TrackServerLogSent(ulong receiverClientId, uint logType, long bytesCount)
        {
            m_Dispatcher.TrackServerLogSent(new ServerLogEvent(new ConnectionInfo(receiverClientId), (Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
        }

        public void TrackServerLogReceived(ulong senderClientId, uint logType, long bytesCount)
        {
            m_Dispatcher.TrackServerLogReceived(new ServerLogEvent(new ConnectionInfo(senderClientId), (Multiplayer.Tools.MetricTypes.LogLevel)logType, bytesCount));
        }

        public void TrackSceneEventSent(IReadOnlyList<ulong> receiverClientIds, uint sceneEventType, string sceneName, long bytesCount)
        {
            foreach (var receiverClientId in receiverClientIds)
            {
                TrackSceneEventSent(receiverClientId, sceneEventType, sceneName, bytesCount);
            }
        }

        public void TrackSceneEventSent(ulong receiverClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            m_Dispatcher.TrackSceneEventSent(new SceneEventMetric(new ConnectionInfo(receiverClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
        }

        public void TrackSceneEventReceived(ulong senderClientId, uint sceneEventType, string sceneName, long bytesCount)
        {
            m_Dispatcher.TrackSceneEventReceived(new SceneEventMetric(new ConnectionInfo(senderClientId), GetSceneEventTypeName(sceneEventType), sceneName, bytesCount));
        }

        public void DispatchFrame()
        {
            s_FrameDispatch.Begin();
            Dispatcher.Dispatch();
            s_FrameDispatch.End();
        }

        private static NetworkObjectIdentifier GetObjectIdentifier(NetworkObject networkObject)
        {
            return new NetworkObjectIdentifier(networkObject.GetNameForMetrics(), networkObject.NetworkObjectId);
        }
    }

    internal class NetcodeObserver
    {
        public static IMetricObserver Observer { get; } = MetricObserverFactory.Construct();
    }
}
#endif
