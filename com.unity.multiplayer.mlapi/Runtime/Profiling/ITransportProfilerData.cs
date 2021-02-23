using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public interface ITransportProfilerData
    {
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}
