using System.Collections.Generic;

namespace Unity.Netcode.Profiling
{
    public interface ITransportProfilerData
    {
        void BeginNewTick();
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}
