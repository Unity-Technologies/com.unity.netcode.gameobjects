using System;
using MLAPI.Configuration;
using MLAPI.Transports;
using UnityEngine;

namespace MLAPI.Timing
{
    public class NetworkTimeSystem: INetworkStats
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
        /// Delegate for invoking an event whenever a network tick passes
        /// </summary>
        /// <param name="time">The predicted time for the tick.</param>
        public delegate void NetworkTickDelegate(NetworkTime time);

        /// <summary>
        /// Gets invoked before every network tick.
        /// </summary>
        public event NetworkTickDelegate OnNetworkTick = null;

        /// <summary>
        /// Gets invoked during every network tick. Used by internal components like <see cref="NetworkManager"/>
        /// </summary>
        internal event NetworkTickDelegate OnNetworkTickInternal = null;

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTimeSystem"/>.
        /// </summary>
        /// <param name="tickRate">The tickrate.</param>
        /// <param name="isServer">true if the system will be used for a server or host.</param>
        /// <param name="networkManager">The networkManager to extract the RTT from. Will be removed in the future to reduce dependencies.</param>
        internal NetworkTimeSystem(int tickRate, bool isServer, NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            Init(tickRate, isServer, this);
        }

        public NetworkTimeSystem(int tickRate, bool isServer, INetworkStats networkStats)
        {
            Init(tickRate, isServer, networkStats);
        }

        private void Init(int tickRate, bool isServer, INetworkStats networkStats)
        {
            m_TickRate = tickRate;

            if (isServer)
            {
                m_NetworkTimeProvider = new ServerNetworkTimeProvider();
            }
            else
            {
                m_NetworkTimeProvider = new ClientNetworkTimeProvider(this, tickRate);
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
                LastReceivedServerSnapshotTick = new NetworkTime(TickRate, LastReceivedServerSnapshotTick.Tick + 1);

                // set exposed time values to correct fixed values
                m_PredictedTime = new NetworkTime(TickRate, i);
                m_ServerTime = new NetworkTime(TickRate, i - predictedToServerDifference);

                OnNetworkTick?.Invoke(m_PredictedTime);
                OnNetworkTickInternal.Invoke(m_ServerTime);
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
            LastReceivedServerSnapshotTick = new NetworkTime(TickRate, serverTick);

            m_ServerTime = new NetworkTime(TickRate, serverTick);

            // This should be overriden by the time provider but setting it in case it's not
            m_PredictedTime = new NetworkTime(TickRate, serverTick);

            m_NetworkTimeProvider.InitializeClient(ref m_PredictedTime, ref m_ServerTime);
        }


        #region NetworkStats
        // TODO this is temporary until we have a better way to measure RTT. Most likely a separate stats class will be used to track this.

        private NetworkManager m_NetworkManager;

        /// <summary>
        /// Gets the tick of the last server snapshot which has been received.
        /// </summary>
        internal NetworkTime LastReceivedServerSnapshotTick { get; private set; }

        public float GetRtt()
        {
            if (m_NetworkManager.IsServer)
            {
                return 0f;
            }
            return m_NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(m_NetworkManager.ServerClientId) / 1000f;
        }

        public NetworkTime GetLastReceivedSnapshotTick()
        {
            return LastReceivedServerSnapshotTick;
        }

        #endregion
    }
}
