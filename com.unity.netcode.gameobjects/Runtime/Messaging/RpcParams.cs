using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    [MovedFrom("MLAPI.Messaging")]
    public interface IHasUpdateStage
    {
        NetworkUpdateStage UpdateStage
        {
            get;
            set;
        }
    }

    [MovedFrom("MLAPI.Messaging")]
    public struct ServerRpcSendParams : IHasUpdateStage
    {
        private NetworkUpdateStage m_UpdateStage;

        public NetworkUpdateStage UpdateStage
        {
            get => m_UpdateStage;
            set => m_UpdateStage = value;
        }
    }

    [MovedFrom("MLAPI.Messaging")]
    public struct ServerRpcReceiveParams : IHasUpdateStage
    {
        private NetworkUpdateStage m_UpdateStage;

        public NetworkUpdateStage UpdateStage
        {
            get => m_UpdateStage;
            set => m_UpdateStage = value;
        }
        public ulong SenderClientId;
    }

    [MovedFrom("MLAPI.Messaging")]
    public struct ServerRpcParams
    {
        public ServerRpcSendParams Send;
        public ServerRpcReceiveParams Receive;
    }

    [MovedFrom("MLAPI.Messaging")]
    public struct ClientRpcSendParams : IHasUpdateStage
    {
        private NetworkUpdateStage m_UpdateStage;

        public NetworkUpdateStage UpdateStage
        {
            get => m_UpdateStage;
            set => m_UpdateStage = value;
        }
        public ulong[] TargetClientIds;
    }

    [MovedFrom("MLAPI.Messaging")]
    public struct ClientRpcReceiveParams : IHasUpdateStage
    {
        private NetworkUpdateStage m_UpdateStage;

        public NetworkUpdateStage UpdateStage
        {
            get => m_UpdateStage;
            set => m_UpdateStage = value;
        }
    }

    [MovedFrom("MLAPI.Messaging")]
    public struct ClientRpcParams
    {
        public ClientRpcSendParams Send;
        public ClientRpcReceiveParams Receive;
    }

#pragma warning disable IDE1006 // disable naming rule violation check
    // RuntimeAccessModifiersILPP will make this `public`
    internal struct __RpcParams
#pragma warning restore IDE1006 // restore naming rule violation check
    {
        public ServerRpcParams Server;
        public ClientRpcParams Client;
    }
}
