using System;

namespace MLAPI.Messaging
{
    public struct ServerRpcSendParams
    {
        public NetworkUpdateStage UpdateStage;
    }

    public struct ServerRpcReceiveParams
    {
        public NetworkUpdateStage UpdateStage;
        public ulong SenderClientId;
    }

    public struct ServerRpcParams
    {
        public ServerRpcSendParams Send;
        public ServerRpcReceiveParams Receive;
    }

    public struct ClientRpcSendParams
    {
        public NetworkUpdateStage UpdateStage;
        public ulong[] TargetClientIds;
    }

    public struct ClientRpcReceiveParams
    {
        public NetworkUpdateStage UpdateStage;
    }

    public struct ClientRpcParams
    {
        public ClientRpcSendParams Send;
        public ClientRpcReceiveParams Receive;
    }

#if UNITY_2020_2_OR_NEWER
    // RuntimeAccessModifiersILPP will make this `public`
    internal struct __RpcParams
#else
    [Obsolete("Please do not use, will no longer be exposed in the future versions (framework internal)")]
    public struct __RpcParams
#endif
    {
        public ServerRpcParams Server;
        public ClientRpcParams Client;
    }
}