using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Netcode
{
    [MovedFrom("MLAPI.Profiling")]
    public interface ITransportProfilerData
    {
        void BeginNewTick();
        IReadOnlyDictionary<string, int> GetTransportProfilerData();
    }
}
