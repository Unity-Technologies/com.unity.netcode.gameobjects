using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public interface ITransportProfilerData
    {
        Dictionary<string, int> GetTransportGetData();
    }
}
