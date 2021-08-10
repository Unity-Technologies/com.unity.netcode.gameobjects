namespace Unity.Netcode
{
    public interface IProfilableTransportProvider
    {
        ITransportProfilerData Transport { get; }
    }
}
