namespace Unity.Netcode
{
    internal interface IGroupRpcTarget
    {
        void Add(ulong clientId);
        void Clear();
        BaseRpcTarget Target { get; }
    }
}
