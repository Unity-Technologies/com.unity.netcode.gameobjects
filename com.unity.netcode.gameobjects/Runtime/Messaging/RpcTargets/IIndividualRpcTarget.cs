namespace Unity.Netcode
{
    internal interface IIndividualRpcTarget
    {
        void SetClientId(ulong clientId);
        BaseRpcTarget Target { get; }
    }
}
