namespace Unity.Multiplayer.Netcode
{
    public interface IProfilableTransportProvider
    {
        ITransportProfilerData Transport { get; }
    }
}
