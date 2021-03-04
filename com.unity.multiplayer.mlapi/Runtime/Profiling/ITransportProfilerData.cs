using System.Collections.Generic;

namespace MLAPI.Profiling
{
    public interface ITransportProfilerData
    {
        void BeginNewTick();
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}