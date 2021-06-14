using System;
using UnityEngine;

namespace MLAPI.Timing
{
    /// <summary>
    /// <see cref="INetworkTimeProvider"/> used by the server or host. Advances local time and server time exactly by deltaTime.
    /// localTime and serverTime are always the same value with this provider.
    /// </summary>
    public class ServerNetworkTimeProvider : INetworkTimeProvider
    {
        /// <inheritdoc/>
        public bool AdvanceTime(ref NetworkTime localTime, ref NetworkTime serverTime, double deltaTime)
        {
            localTime += deltaTime;
            serverTime += deltaTime;
            return false;
        }

        /// <inheritdoc/>
        public void InitializeClient(ref NetworkTime localTime, ref NetworkTime serverTime)
        {
            throw new InvalidOperationException($"{nameof(InitializeClient)} should never be called for server only {nameof(INetworkTimeProvider)}: {nameof(ServerNetworkTimeProvider)}");
        }
    }
}
