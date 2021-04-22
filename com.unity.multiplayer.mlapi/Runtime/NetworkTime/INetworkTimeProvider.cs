using UnityEngine;

namespace MLAPI.NetworkTime
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

}
