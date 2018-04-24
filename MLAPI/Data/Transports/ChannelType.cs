namespace MLAPI.Data
{
    public enum ChannelType
    {
        Unreliable,
        UnreliableFragmented,
        UnreliableSequenced,
        Reliable,
        ReliableFragmented,
        ReliableSequenced,
        StateUpdate,
        ReliableStateUpdate,
        AllCostDelivery,
        UnreliableFragmentedSequenced,
        ReliableFragmentedSequenced
    }
}
