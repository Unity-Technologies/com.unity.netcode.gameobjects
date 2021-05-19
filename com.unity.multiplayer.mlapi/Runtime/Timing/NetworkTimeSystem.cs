using System;
using UnityEngine;

namespace MLAPI.Timing
{
    /// <summary>
    /// <see cref="NetworkTimeSystem"/> is a standalone system which can be used to run a network time simmulation.
    /// The network time system maintains both a predicted and a server time and also invokes events for each fixed tick which passes.
    /// </summary>
    public class NetworkTimeSystem : INetworkStats
    {
        private INetworkTimeProvider m_NetworkTimeProvider;
        private int m_TickRate;

        private NetworkTime m_PredictedTime;
        private NetworkTime m_ServerTime;

        /// <summary>
        /// Special value to indicate "No tick information"
        /// </summary>
        public const int NoTick = int.MinValue;

        /// <summary>
        /// The current predicted time. This is the time at which predicted or client authoritative objects move. This value is accurate when called in Update or NetworkFixedUpdate but does not work correctly for FixedUpdate.
        /// </summary>
        public NetworkTime PredictedTime => m_PredictedTime;

        /// <summary>
        /// The current server time. This value is mostly used for internal purposes and to interpolate state received from the server. This value is accurate when called in Update or NetworkFixedUpdate but does not work correctly for FixedUpdate.
        /// </summary>
        public NetworkTime ServerTime => m_ServerTime;

        /// <summary>
        /// The TickRate of the time system. This is used to decide how often a fixed network tick is run.
        /// </summary>
        public int TickRate => m_TickRate;

        /// <summary>
        /// The time provider used
        /// </summary>
        public INetworkTimeProvider NetworkTimeProvider => m_NetworkTimeProvider;

        /// <summary>
        /// Delegate for invoking an event whenever a network tick passes
        /// </summary>
        /// <param name="time">The predicted time for the tick.</param>
        public delegate void NetworkTickDelegate(NetworkTime time);

        /// <summary>
        /// Gets invoked before every network tick.
        /// </summary>
        public event NetworkTickDelegate NetworkTick = null;

        /// <summary>
        /// Gets invoked during every network tick. Used by internal components like <see cref="NetworkManager"/>
        /// </summary>
        internal event NetworkTickDelegate NetworkTickInternal = null;

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTimeSystem"/> class.
        /// </summary>
        /// <param name="tickRate">The tickrate.</param>
        /// <param name="isServer">true if the system will be used for a server or host.</param>
        /// <param name="networkManager">The networkManager to extract the RTT from. Will be removed in the future to reduce dependencies.</param>
        internal NetworkTimeSystem(int tickRate, bool isServer, NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            Init(tickRate, isServer, this);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTimeSystem"/> class.
        /// </summary>
        /// <param name="tickRate">The tickrate.</param>
        /// <param name="isServer">true if the system will be used for a server or host.</param>
        /// <param name="networkStats">The network stats source for RTT and other network information used to drive the time system.</param>
        public NetworkTimeSystem(int tickRate, bool isServer, INetworkStats networkStats)
        {
            Init(tickRate, isServer, networkStats);
        }

        // This should just be a constructor but isn't because of c# limited constructor overriding support.
        private void Init(int tickRate, bool isServer, INetworkStats networkStats)
        {
            m_TickRate = tickRate;

            if (isServer)
            {
                m_NetworkTimeProvider = new ServerNetworkTimeProvider();
            }
            else
            {
                m_NetworkTimeProvider = new ClientNetworkTimeProvider(networkStats, tickRate);
            }

            m_PredictedTime = new NetworkTime(tickRate);
            m_ServerTime = new NetworkTime(tickRate);
        }

        /// <summary>
        /// Called each network loop update during the <see cref="NetworkUpdateStage.PreUpdate"/> to advance the network time.
        /// </summary>
        /// <param name="deltaTime">The delta time used to advance time. During normal use this is <see cref="Time.deltaTime"/>.</param>
        public void AdvanceNetworkTime(float deltaTime)
        {
            // store old predicted tick to know how many fixed ticks passed
            var previousPredictedTick = PredictedTime.Tick;

            m_NetworkTimeProvider.AdvanceTime(ref m_PredictedTime, ref m_ServerTime, deltaTime);

            // cache times here so that we can adjust them to temporary values while simulating ticks.
            var cachePredictedTime = m_PredictedTime;
            var cacheServerTime = m_ServerTime;

            var currentPredictedTick = PredictedTime.Tick;
            var predictedToServerDifference = currentPredictedTick - ServerTime.Tick;

            for (int i = previousPredictedTick + 1; i <= currentPredictedTick; i++)
            {
                // TODO this is temporary code to just make this run somehow will be removed once we have snapshot ack
                m_LastReceivedServerSnapshotTick = new NetworkTime(TickRate, m_LastReceivedServerSnapshotTick.Tick + 1);

                // set exposed time values to correct fixed values
                m_PredictedTime = new NetworkTime(TickRate, i);
                m_ServerTime = new NetworkTime(TickRate, i - predictedToServerDifference);

                NetworkTick?.Invoke(m_PredictedTime);
                NetworkTickInternal?.Invoke(m_ServerTime);
            }

            // Set exposed time to values from tick system
            m_PredictedTime = cachePredictedTime;
            m_ServerTime = cacheServerTime;
        }

        /// <summary>
        /// Called on the client in the initial spawn packet to initialize the time with the correct server value.
        /// </summary>
        /// <param name="serverTick">The server tick at initialization time</param>
        public void InitializeClient(int serverTick)
        {
            m_LastReceivedServerSnapshotTick = new NetworkTime(TickRate, serverTick);

            m_ServerTime = new NetworkTime(TickRate, serverTick);

            // This should be overriden by the time provider but setting it in case it's not
            m_PredictedTime = new NetworkTime(TickRate, serverTick);

            m_NetworkTimeProvider.InitializeClient(ref m_PredictedTime, ref m_ServerTime);
        }

        // TODO this is temporary until we have a better way to measure RTT. Most likely a separate stats class will be used to track this.

        #region NetworkStats

        private NetworkManager m_NetworkManager;

        private NetworkTime m_LastReceivedServerSnapshotTick;

        /// <inheritdoc/>
        public float Rtt
        {
            get
            {
                if (m_NetworkManager.IsServer)
                {
                    return 0f;
                }

                return m_NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(m_NetworkManager.ServerClientId) / 1000f;
            }
        }

        /// <inheritdoc/>
        public NetworkTime LastReceivedSnapshotTick => m_LastReceivedServerSnapshotTick;

        #endregion
    }
}
