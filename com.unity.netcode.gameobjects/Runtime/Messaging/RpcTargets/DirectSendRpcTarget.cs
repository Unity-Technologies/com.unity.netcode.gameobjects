namespace Unity.Netcode
{
    internal class DirectSendRpcTarget : BaseRpcTarget, IIndividualRpcTarget
    {
        public BaseRpcTarget Target => this;

        internal ulong ClientId;

        public override void Dispose()
        {
            CheckLockBeforeDispose();
        }

        internal override void Send(NetworkBehaviour behaviour, ref RpcMessage message, NetworkDelivery delivery, RpcParams rpcParams)
        {
            SendMessageToClient(behaviour, ClientId, ref message, delivery);
        }

        public void SetClientId(ulong clientId)
        {
            ClientId = clientId;
        }

        internal DirectSendRpcTarget(NetworkManager manager) : base(manager)
        {

        }

        internal DirectSendRpcTarget(ulong clientId, NetworkManager manager) : base(manager)
        {
            ClientId = clientId;
        }
    }
}
