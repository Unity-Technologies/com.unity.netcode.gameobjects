namespace MLAPI.Profiling
{
    public interface IHasProfilableTransport
    {
        ITransportProfilerData GetTransport();
    }
}
