using System.Collections.Generic;

namespace Unity.Multiplayer.Netcode
{
    public interface ITransportProfilerData
    {
        void BeginNewTick();
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}
