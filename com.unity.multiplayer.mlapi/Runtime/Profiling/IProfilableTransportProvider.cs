namespace MLAPI.Profiling
{
    public interface IProfilableTransportProvider
    {
        ITransportProfilerData Transport { get; }
    }
}
