namespace Unity.Multiplayer.Netcode.Profiling
{
    public interface IProfilableTransportProvider
    {
        ITransportProfilerData Transport { get; }
    }
}
