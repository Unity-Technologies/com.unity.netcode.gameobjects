namespace MLAPI.Messaging
{
    public struct ServerRpcSendParams
    {
        public NetworkUpdateManager.NetworkUpdateStages UpdateStage;
    }

    public struct ServerRpcReceiveParams
    {
        public ulong SenderClientId;
    }

    public struct ServerRpcParams
    {
        public ServerRpcSendParams Send;
        public ServerRpcReceiveParams Receive;
    }

    public struct ClientRpcSendParams
    {
        public NetworkUpdateManager.NetworkUpdateStages UpdateStage;
        public ulong[] TargetClientIds;
    }

    public struct ClientRpcReceiveParams
    {
    }

    public struct ClientRpcParams
    {
        public ClientRpcSendParams Send;
        public ClientRpcReceiveParams Receive;
    }
}
