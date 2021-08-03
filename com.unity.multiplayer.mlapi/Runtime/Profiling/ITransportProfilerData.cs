using System.Collections.Generic;

namespace Unity.Multiplayer.Netcode.Profiling
{
    public interface ITransportProfilerData
    {
        void BeginNewTick();
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}
