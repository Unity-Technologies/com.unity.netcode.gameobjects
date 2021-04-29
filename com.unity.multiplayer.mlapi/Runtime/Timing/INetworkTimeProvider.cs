using UnityEngine;

namespace MLAPI.Timing
{
    public interface INetworkTimeProvider
    {
        public bool HandleTime(ref NetworkTime predictedTime, ref NetworkTime serverTime , float deltaTime);
    }

    public class FixedNetworkTimeProvider: INetworkTimeProvider
    {
        public bool HandleTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime)
        {
            predictedTime.AddTime(deltaTime);
            serverTime.AddTime(deltaTime);
            return true;
        }
    }

    // public class DynamicNetworkTimeProvider: INetworkTimeProvider
    // {
    //     private readonly int targetInputBufferSize = 2;
    //
    //     private NetworkTickSystem m_NetworkTickSystem;
    //
    //     private float m_SmoothedRTT; // TODO needs to be updated
    //
    //     private float m_TimeScale;
    //
    //     public DynamicNetworkTimeProvider(NetworkTickSystem networkTickSystem)
    //     {
    //         m_NetworkTickSystem = networkTickSystem;
    //     }
    //
    //     private int Quantize(float value, float min, float max, int precision)
    //     {
    //         float adjusted = value - min;
    //         return (int)(adjusted * precision);
    //     }
    //
    //     public bool HandleTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime)
    //     {
    //         var adjustedDeltaTime = deltaTime * m_TimeScale;
    //
    //         // increment predicted time
    //         predictedTime.AddTime(adjustedDeltaTime);
    //
    //         // increment server time
    //         serverTime.AddTime(adjustedDeltaTime);
    //
    //         // time scale adjustment based on size of client received snapshot buffer TODO
    //
    //         // time scale adjustment based on whether we are behind / ahead of the server in terms of inputs
    //         // This implementation uses RTT to calculate that without server input which is not ideal. In the future we might want to add a field to the protocol which allows the server to send the exact input buffers size to the client.
    //
    //         float timeSinceLastSnapshot = m_NetworkTickSystem.LastReceivedServerSnapshot.RenderTime - serverTime.RenderTime;
    //
    //         //int preferredTick = m_NetworkClient.serverTime + (int)(((m_NetworkClient.timeSinceSnapshot + m_SmoothedRTT) / 1000.0f) * predictedTime.TickRate) + preferredBufferedCommandCount;
    //
    //         return true;
    //     }
    // }
}
