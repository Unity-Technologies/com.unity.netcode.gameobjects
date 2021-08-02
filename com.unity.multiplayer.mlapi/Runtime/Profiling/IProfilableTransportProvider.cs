namespace Unity.Netcode.Profiling
{
    public interface IProfilableTransportProvider
    {
        ITransportProfilerData Transport { get; }
    }
}
