using System;
using UnityEngine;

namespace MLAPI.Timing
{
    /// <summary>
    /// <see cref="INetworkTimeProvider"/> used by the server or host. Advances predicted time and server time exactly by deltaTime.
    /// predictedTime and serverTime are always the same value with this provider.
    /// </summary>
    public class ServerNetworkTimeProvider : INetworkTimeProvider
    {

        /// <inheritdoc/>
        public bool AdvanceTime(ref NetworkTime predictedTime, ref NetworkTime serverTime, float deltaTime)
        {
            predictedTime += deltaTime;
            serverTime += deltaTime;
            return true;
        }

        /// <inheritdoc/>
        public void InitializeClient(ref NetworkTime predictedTime, ref NetworkTime serverTime)
        {
            throw new InvalidOperationException($"{nameof(InitializeClient)} should never be called for server only {nameof(INetworkTimeProvider)}: {nameof(ServerNetworkTimeProvider)}");
        }
    }

}
