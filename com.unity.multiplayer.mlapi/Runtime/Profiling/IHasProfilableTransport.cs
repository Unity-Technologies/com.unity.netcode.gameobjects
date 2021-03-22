namespace MLAPI.Profiling
{
    public interface IHasProfilableTransport
    {
        ITransportProfilerData Transport { get; }
    }
}
