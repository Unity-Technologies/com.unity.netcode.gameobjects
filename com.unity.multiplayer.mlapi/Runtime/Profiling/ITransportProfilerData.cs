using System.Collections.Generic;

namespace Unity.Netcode
{
    public interface ITransportProfilerData
    {
        void BeginNewTick();
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}
