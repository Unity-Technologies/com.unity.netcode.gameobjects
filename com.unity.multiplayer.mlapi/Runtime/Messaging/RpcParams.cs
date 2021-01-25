namespace MLAPI.Messaging
{
    public struct ServerRpcSendParams
    {
        public NetworkUpdateManager.NetworkUpdateStage UpdateStage;
    }

    public struct ServerRpcReceiveParams
    {
        public NetworkUpdateManager.NetworkUpdateStage UpdateStage;
        public ulong SenderClientId;
    }

    public struct ServerRpcParams
    {
        public ServerRpcSendParams Send;
        public ServerRpcReceiveParams Receive;
    }

    public struct ClientRpcSendParams
    {
        public NetworkUpdateManager.NetworkUpdateStage UpdateStage;
        public ulong[] TargetClientIds;
    }

    public struct ClientRpcReceiveParams
    {
        public NetworkUpdateManager.NetworkUpdateStage UpdateStage;
    }

    public struct ClientRpcParams
    {
        public ClientRpcSendParams Send;
        public ClientRpcReceiveParams Receive;
    }

    internal struct RpcParams
    {
        public ServerRpcParams Server;
        public ClientRpcParams Client;
    }
}