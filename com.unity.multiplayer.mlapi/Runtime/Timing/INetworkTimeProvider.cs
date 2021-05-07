using UnityEngine;

namespace MLAPI.Timing
{
    public interface INetworkTimeProvider
    {
        /// <summary>
        /// Called once per frame to advance time.
        /// </summary>
        /// <param name="predictedTime">The predicted time to advance.</param>
        /// <param name="serverTime">The server time to advance.</param>
        /// <param name="deltaTime">The real delta time which passed.</param>
        /// <returns>true if advancing the the time succeeded; otherwise false if there was a hard correction.</returns>
        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime);

        /// <summary>
        /// Called once on clients only to initialize time.
        /// </summary>
        /// <param name="predictedTime">The predicted time to initialize. In value is serverTime.</param>
        /// <param name="serverTime">the server time to initialize. In value is a time matching the tick of the initial received approval packet.</param>
        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime);
    }
}
