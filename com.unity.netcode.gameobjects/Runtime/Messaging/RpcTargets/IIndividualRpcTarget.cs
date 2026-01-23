namespace Unity.Netcode
{
    internal interface IIndividualRpcTarget
    {
        public void SetClientId(ulong clientId);
        public BaseRpcTarget Target { get; }
    }
}
