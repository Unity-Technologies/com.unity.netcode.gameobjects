namespace Unity.Netcode
{
    internal interface IGroupRpcTarget
    {
        public void Add(ulong clientId);
        public void Clear();
        public BaseRpcTarget Target { get; }
    }
}
