using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    [MovedFrom("MLAPI.Profiling")]
    public interface IProfilableTransportProvider
    {
        ITransportProfilerData Transport { get; }
    }
}
