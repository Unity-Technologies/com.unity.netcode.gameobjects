namespace Unity.Netcode
{
    public interface INetworkSimulatorConfiguration
    {
        string Name { get; set; }
        string Description { get; set; }
        int PacketDelayMs { get; set; }
        int PacketJitterMs { get; set; }
        int PacketLossInterval { get; set; }
        int PacketLossPercent { get; set; }
        int PacketDuplicationPercent { get; set; }
    }
}
