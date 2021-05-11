using UnityEngine;

namespace MLAPI.Timing
{
    public interface INetworkStats
    {
        float GetRtt();

        NetworkTime GetLastReceivedSnapshotTick();
    }
}
