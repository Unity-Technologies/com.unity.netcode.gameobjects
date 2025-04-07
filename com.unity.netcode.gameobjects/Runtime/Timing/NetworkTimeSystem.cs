using System;
using Unity.Profiling;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// <see cref="NetworkTimeSystem"/> is a standalone system which can be used to run a network time simulation.
    /// The network time system maintains both a local and a server time. The local time is based on the server time
    /// as last received from the server plus an offset based on the current RTT - in other words, it is a best-guess
    /// effort at predicting what the server tick will be when a given network action is processed on the server.
    /// </summary>
    public class NetworkTimeSystem
    {
        /// <remarks>
        /// This was the original comment when it lived in NetworkManager:
        /// todo talk with UX/Product, find good default value for this
        /// </remarks>
        private const float k_DefaultBufferSizeSec = 0.05f;

        /// <summary>
        /// Time synchronization frequency defaults to 1 synchronization message per second
        /// </summary>
        private const double k_TimeSyncFrequency = 1.0d;

        /// <summary>
        /// The threshold, in seconds, used to force a hard catchup of network time
        /// </summary>
        private const double k_HardResetThresholdSeconds = 0.2d;

        /// <summary>
        /// Default adjustment ratio
        /// </summary>
        private const double k_DefaultAdjustmentRatio = 0.01d;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private static ProfilerMarker s_SyncTime = new ProfilerMarker($"{nameof(NetworkManager)}.SyncTime");
#endif
        private double m_PreviousTimeSec;
        private double m_TimeSec;
        private double m_CurrentLocalTimeOffset;
        private double m_DesiredLocalTimeOffset;
        private double m_CurrentServerTimeOffset;
        private double m_DesiredServerTimeOffset;

        /// <summary>
        /// Gets or sets the amount of time in seconds the server should buffer incoming client messages.
        /// This increases the difference between local and server time so that messages arrive earlier on the server.
        /// </summary>
        public double LocalBufferSec { get; set; }

        /// <summary>
        /// Gets or sets the amount of the time in seconds the client should buffer incoming messages from the server. This increases server time.
        /// A higher value increases latency but makes the game look more smooth in bad networking conditions.
        /// This value must be higher than the tick length client side.
        /// </summary>
        public double ServerBufferSec { get; set; }

        /// <summary>
        /// Gets or sets a threshold in seconds used to force a hard catchup of network time.
        /// </summary>
        public double HardResetThresholdSec { get; set; }

        /// <summary>
        /// Gets or sets the ratio at which the NetworkTimeSystem speeds up or slows down time.
        /// </summary>
        public double AdjustmentRatio { get; set; }

        /// <summary>
        /// The current local time with the local time offset applied
        /// </summary>
        public double LocalTime => m_TimeSec + m_CurrentLocalTimeOffset;

        /// <summary>
        /// The current server time with the server time offset applied
        /// </summary>
        public double ServerTime => m_TimeSec + m_CurrentServerTimeOffset;

        private float m_TickLatencyAverage = 2.0f;

        /// <summary>
        /// The averaged latency in network ticks between a client and server.
        /// </summary>
        /// <remarks>
        /// For a distributed authority network topology, this latency is between the client and the
        /// distributed authority service instance.<br />
        /// Note: <see cref="Components.NetworkTransform"/> uses this value plus an additional global
        /// offset <see cref="Components.NetworkTransform.InterpolationBufferTickOffset"/> when interpolation
        /// is enabled. <br />
        /// To see the current <see cref="Components.NetworkTransform"/> tick latency: <br />
        /// - <see cref="Components.NetworkTransform.GetTickLatency"/> <br />
        /// - <see cref="Components.NetworkTransform.GetTickLatencyInSeconds"/> <br />
        /// </remarks>
        public int TickLatency = 1;

        internal double LastSyncedServerTimeSec { get; private set; }
        internal double LastSyncedRttSec { get; private set; }
        internal double LastSyncedHalfRttSec { get; private set; }

        private NetworkConnectionManager m_ConnectionManager;
        private NetworkTransport m_NetworkTransport;
        private NetworkTickSystem m_NetworkTickSystem;
        private NetworkManager m_NetworkManager;
        private double m_TickFrequency;

        /// <summary>
        /// <see cref="k_TimeSyncFrequency"/>
        /// </summary>
        private int m_TimeSyncFrequencyTicks;

        /// <summary>
        /// The constructor class for <see cref="NetworkTickSystem"/>
        /// </summary>
        /// <param name="localBufferSec">The amount of time, in seconds, the server should buffer incoming client messages.</param>
        /// <param name="serverBufferSec">The amount of the time in seconds the client should buffer incoming messages from the server.</param>
        /// <param name="hardResetThresholdSec">The threshold, in seconds, used to force a hard catchup of network time.</param>
        /// <param name="adjustmentRatio">The ratio at which the NetworkTimeSystem speeds up or slows down time.</param>
        public NetworkTimeSystem(double localBufferSec, double serverBufferSec = k_DefaultBufferSizeSec, double hardResetThresholdSec = k_HardResetThresholdSeconds, double adjustmentRatio = k_DefaultAdjustmentRatio)
        {
            LocalBufferSec = localBufferSec;
            ServerBufferSec = serverBufferSec;
            HardResetThresholdSec = hardResetThresholdSec;
            AdjustmentRatio = adjustmentRatio;
            m_TickLatencyAverage = 2;
        }

        /// <summary>
        /// The primary time system is initialized when a server-host or client is started
        /// </summary>
        internal NetworkTickSystem Initialize(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
            m_ConnectionManager = networkManager.ConnectionManager;
            m_NetworkTransport = networkManager.NetworkConfig.NetworkTransport;
            m_TimeSyncFrequencyTicks = (int)(k_TimeSyncFrequency * networkManager.NetworkConfig.TickRate);
            m_NetworkTickSystem = new NetworkTickSystem(networkManager.NetworkConfig.TickRate, 0, 0);
            m_TickFrequency = 1.0 / networkManager.NetworkConfig.TickRate;
            // Only the server side needs to register for tick based time synchronization
            if (m_ConnectionManager.LocalClient.IsServer)
            {
                m_NetworkTickSystem.Tick += OnTickSyncTime;
            }

            return m_NetworkTickSystem;
        }

        internal void UpdateTime()
        {
            // As a client wait to run the time system until we are connected.
            // As a client or server don't worry about the time system if we are no longer processing messages
            if (!m_ConnectionManager.LocalClient.IsServer && !m_ConnectionManager.LocalClient.IsConnected)
            {
                return;
            }

            // Only update RTT here, server time is updated by time sync messages
            var reset = Advance(m_NetworkManager.RealTimeProvider.UnscaledDeltaTime);
            if (reset)
            {
                m_NetworkTickSystem.Reset(LocalTime, ServerTime);
            }

            m_NetworkTickSystem.UpdateTick(LocalTime, ServerTime);

            if (!m_ConnectionManager.LocalClient.IsServer)
            {
                Sync(LastSyncedServerTimeSec + m_NetworkManager.RealTimeProvider.UnscaledDeltaTime, m_NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId) / 1000d);
            }
        }

        /// <summary>
        /// Server-Side:
        /// Synchronizes time with clients based on the given <see cref="m_TimeSyncFrequencyTicks"/>.
        /// Also: <see cref="k_TimeSyncFrequency"/>
        /// </summary>
        /// <remarks>
        /// The default is to send 1 time synchronization message per second
        /// </remarks>
        private void OnTickSyncTime()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_SyncTime.Begin();
#endif

            // Check if we need to send a time synchronization message, and if so send it
            if (m_ConnectionManager.LocalClient.IsServer && m_NetworkTickSystem.ServerTime.Tick % m_TimeSyncFrequencyTicks == 0)
            {
                var message = new TimeSyncMessage
                {
                    Tick = m_NetworkTickSystem.ServerTime.Tick
                };
                m_ConnectionManager.SendMessage(ref message, NetworkDelivery.Unreliable, m_ConnectionManager.ConnectedClientIds);
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            s_SyncTime.End();
#endif
        }

        /// <summary>
        /// Invoke when shutting down the NetworkManager
        /// </summary>
        internal void Shutdown()
        {
            if (m_ConnectionManager.LocalClient.IsServer)
            {
                m_NetworkTickSystem.Tick -= OnTickSyncTime;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkTimeSystem"/> class for a server instance.
        /// The server will not apply any buffer values which ensures that local time equals server time.
        /// </summary>
        /// <returns>The instance.</returns>
        public static NetworkTimeSystem ServerTimeSystem()
        {
            return new NetworkTimeSystem(0, 0, double.MaxValue);
        }

        /// <summary>
        /// Advances the time system by a certain amount of time. Should be called once per frame with Time.unscaledDeltaTime or similar.
        /// </summary>
        /// <param name="deltaTimeSec">The amount of time to advance. The delta time which passed since Advance was last called.</param>
        /// <returns>True if a hard reset of the time system occurred due to large time offset differences. False if normal time advancement occurred</returns>
        public bool Advance(double deltaTimeSec)
        {
            m_PreviousTimeSec = m_TimeSec;
            m_TimeSec += deltaTimeSec;
            // TODO: For client-server, we need a latency message sent by clients to tell us their tick latency
            if (LastSyncedRttSec > 0.0f)
            {
                m_TickLatencyAverage = Mathf.Lerp(m_TickLatencyAverage, (float)((LastSyncedRttSec + deltaTimeSec) / m_TickFrequency), (float)deltaTimeSec);
                TickLatency = (int)Mathf.Max(2.0f, Mathf.Round(m_TickLatencyAverage));
            }

            if (Math.Abs(m_DesiredLocalTimeOffset - m_CurrentLocalTimeOffset) > HardResetThresholdSec || Math.Abs(m_DesiredServerTimeOffset - m_CurrentServerTimeOffset) > HardResetThresholdSec)
            {
                m_TimeSec += m_DesiredServerTimeOffset;

                m_DesiredLocalTimeOffset -= m_DesiredServerTimeOffset;
                m_CurrentLocalTimeOffset = m_DesiredLocalTimeOffset;

                m_DesiredServerTimeOffset = 0;
                m_CurrentServerTimeOffset = 0;

                return true;
            }

            m_CurrentLocalTimeOffset += deltaTimeSec * (m_DesiredLocalTimeOffset > m_CurrentLocalTimeOffset ? AdjustmentRatio : -AdjustmentRatio);
            m_CurrentServerTimeOffset += deltaTimeSec * (m_DesiredServerTimeOffset > m_CurrentServerTimeOffset ? AdjustmentRatio : -AdjustmentRatio);

            return false;
        }

        /// <summary>
        /// Resets the time system to a time based on the given network parameters.
        /// </summary>
        /// <param name="serverTimeSec">The most recent server time value received in seconds.</param>
        /// <param name="rttSec">The current RTT in seconds. Can be an averaged or a raw value.</param>
        public void Reset(double serverTimeSec, double rttSec)
        {
            Sync(serverTimeSec, rttSec);
            Advance(0);
        }
        internal int SyncCount;
        /// <summary>
        /// Synchronizes the time system with up-to-date network statistics but does not change any time values or advance the time.
        /// </summary>
        /// <param name="serverTimeSec">The most recent server time value received in seconds.</param>
        /// <param name="rttSec">The current RTT in seconds. Can be an averaged or a raw value.</param>
        public void Sync(double serverTimeSec, double rttSec)
        {
            LastSyncedRttSec = rttSec;
            LastSyncedHalfRttSec = (rttSec * 0.5d);
            LastSyncedServerTimeSec = serverTimeSec;

            var timeDif = serverTimeSec - m_TimeSec;

            m_DesiredServerTimeOffset = timeDif - ServerBufferSec;
            // We adjust our desired local time offset to be half RTT since the delivery of
            // the TimeSyncMessage should only take half of the RTT time (legacy was using 1 full RTT)
            m_DesiredLocalTimeOffset = timeDif + LastSyncedHalfRttSec + LocalBufferSec;
        }

        internal double ServerTimeOffset => m_DesiredServerTimeOffset;
        internal double LocalTimeOffset => m_DesiredLocalTimeOffset;
    }
}
