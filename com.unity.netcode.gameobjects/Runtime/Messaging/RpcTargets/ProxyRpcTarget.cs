namespace Unity.Netcode
{
    internal class ProxyRpcTarget : ProxyRpcTargetGroup, IIndividualRpcTarget
    {
        internal ProxyRpcTarget(ulong clientId, NetworkManager manager) : base(manager)
        {
            Add(clientId);
        }

        public void SetClientId(ulong clientId)
        {
            Clear();
            Add(clientId);
        }
    }
}
