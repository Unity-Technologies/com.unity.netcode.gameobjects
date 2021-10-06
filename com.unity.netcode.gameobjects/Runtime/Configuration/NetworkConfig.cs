using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Collections;

namespace Unity.Netcode
{
    /// <summary>
    /// The configuration object used to start server, client and hosts
    /// </summary>
    [Serializable]
    public class NetworkConfig
    {
        /// <summary>
        /// The protocol version. Different versions doesn't talk to each other.
        /// </summary>
        [Tooltip("Use this to make two builds incompatible with each other")]
        public ushort ProtocolVersion = 0;

        /// <summary>
        /// The transport hosts the sever uses
        /// </summary>
        [Tooltip("The NetworkTransport to use")]
        public NetworkTransport NetworkTransport = null;

        /// <summary>
        /// The default player prefab
        /// </summary>
        [Tooltip("When set, NetworkManager will automatically create and spawn the assigned player prefab. This can be overridden by adding it to the NetworkPrefabs list and selecting override.")]
        public GameObject PlayerPrefab;

        /// <summary>
        /// A list of prefabs that can be dynamically spawned.
        /// </summary>
        [SerializeField]
        [Tooltip("The prefabs that can be spawned across the network")]
        internal List<NetworkPrefab> NetworkPrefabs = new List<NetworkPrefab>();

        /// <summary>
        /// This dictionary provides a quick way to check and see if a NetworkPrefab has a NetworkPrefab override.
        /// Generated at runtime and OnValidate
        /// </summary>
        internal Dictionary<uint, NetworkPrefab> NetworkPrefabOverrideLinks = new Dictionary<uint, NetworkPrefab>();

        internal Dictionary<uint, uint> OverrideToNetworkPrefab = new Dictionary<uint, uint>();


        /// <summary>
        /// The tickrate of network ticks. This value controls how often netcode runs user code and sends out data.
        /// </summary>
        [Tooltip("The tickrate. This value controls how often netcode runs user code and sends out data. The value is in 'ticks per seconds' which means a value of 50 will result in 50 ticks being executed per second or a fixed delta time of 0.02.")]
        public uint TickRate = 30;

        /// <summary>
        /// The amount of seconds to wait for handshake to complete before timing out a client
        /// </summary>
        [Tooltip("The amount of seconds to wait for the handshake to complete before the client times out")]
        public int ClientConnectionBufferTimeout = 10;

        /// <summary>
        /// Whether or not to use connection approval
        /// </summary>
        [Tooltip("Whether or not to force clients to be approved before they connect")]
        public bool ConnectionApproval = false;

        /// <summary>
        /// The data to send during connection which can be used to decide on if a client should get accepted
        /// </summary>
        [Tooltip("The connection data sent along with connection requests")]
        public byte[] ConnectionData = new byte[0];

        /// <summary>
        /// If your logic uses the NetworkTime, this should probably be turned off. If however it's needed to maximize accuracy, this is recommended to be turned on
        /// </summary>
        [Tooltip("Enable this to re-sync the NetworkTime after the initial sync")]
        public bool EnableTimeResync = false;

        /// <summary>
        /// If time re-sync is turned on, this specifies the interval between syncs in seconds.
        /// </summary>
        [Tooltip("The amount of seconds between re-syncs of NetworkTime, if enabled")]
        public int TimeResyncInterval = 30;

        /// <summary>
        /// Whether or not to ensure that NetworkVariables can be read even if a client accidentally writes where its not allowed to. This costs some CPU and bandwidth.
        /// </summary>
        [Tooltip("Ensures that NetworkVariables can be read even if a client accidental writes where its not allowed to. This will cost some CPU time and bandwidth")]
        public bool EnsureNetworkVariableLengthSafety = false;

        /// <summary>
        /// Enables scene management. This will allow network scene switches and automatic scene difference corrections upon connect.
        /// SoftSynced scene objects wont work with this disabled. That means that disabling SceneManagement also enables PrefabSync.
        /// </summary>
        [Tooltip("Enables scene management. This will allow network scene switches and automatic scene difference corrections upon connect.\n" +
                 "SoftSynced scene objects wont work with this disabled. That means that disabling SceneManagement also enables PrefabSync.")]
        public bool EnableSceneManagement = true;

        /// <summary>
        /// Whether or not the netcode should check for differences in the prefabs at connection.
        /// If you dynamically add prefabs at runtime, turn this OFF
        /// </summary>
        [Tooltip("Whether or not the netcode should check for differences in the prefab lists at connection")]
        public bool ForceSamePrefabs = true;

        /// <summary>
        /// If true, NetworkIds will be reused after the NetworkIdRecycleDelay.
        /// </summary>
        [Tooltip("If true, NetworkIds will be reused after the NetworkIdRecycleDelay")]
        public bool RecycleNetworkIds = true;

        /// <summary>
        /// The amount of seconds a NetworkId has to be unused in order for it to be reused.
        /// </summary>
        [Tooltip("The amount of seconds a NetworkId has to unused in order for it to be reused")]
        public float NetworkIdRecycleDelay = 120f;

        /// <summary>
        /// Decides how many bytes to use for Rpc messaging. Leave this to 2 bytes unless you are facing hash collisions
        /// </summary>
        [Tooltip("The maximum amount of bytes to use for RPC messages.")]
        public HashSize RpcHashSize = HashSize.VarIntFourBytes;

        /// <summary>
        /// The amount of seconds to wait for all clients to load or unload a requested scene
        /// </summary>
        [Tooltip("The amount of seconds to wait for all clients to load or unload a requested scene (only when EnableSceneManagement is enabled)")]
        public int LoadSceneTimeOut = 120;

        /// <summary>
        /// The amount of time a message should be buffered for without being consumed. If it is not consumed within this time, it will be dropped.
        /// </summary>
        [Tooltip("The amount of time a message should be buffered for without being consumed. If it is not consumed within this time, it will be dropped")]
        public float MessageBufferTimeout = 20f;

        /// <summary>
        /// Whether or not to enable network logs.
        /// </summary>
        public bool EnableNetworkLogs = true;

        /// <summary>
        /// Whether or not to enable Snapshot System for variable updates. Not supported in this version.
        /// </summary>
        public bool UseSnapshotDelta { get; } = false;
        /// <summary>
        /// Whether or not to enable Snapshot System for spawn and despawn commands. Not supported in this version.
        /// </summary>
        public bool UseSnapshotSpawn { get; } = false;
        /// <summary>
        /// When Snapshot System spawn is enabled: max size of Snapshot Messages. Meant to fit MTU.
        /// </summary>
        public int SnapshotMaxSpawnUsage { get; } = 1200;

        public const int RttAverageSamples = 5; // number of RTT to keep an average of (plus one)
        public const int RttWindowSize = 64; // number of slots to use for RTT computations (max number of in-flight packets)
        /// <summary>
        /// Returns a base64 encoded version of the configuration
        /// </summary>
        /// <returns></returns>
        public string ToBase64()
        {
            NetworkConfig config = this;
            var writer = new FastBufferWriter(MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(config.ProtocolVersion);
                writer.WriteValueSafe(config.TickRate);
                writer.WriteValueSafe(config.ClientConnectionBufferTimeout);
                writer.WriteValueSafe(config.ConnectionApproval);
                writer.WriteValueSafe(config.LoadSceneTimeOut);
                writer.WriteValueSafe(config.EnableTimeResync);
                writer.WriteValueSafe(config.EnsureNetworkVariableLengthSafety);
                writer.WriteValueSafe(config.RpcHashSize);
                writer.WriteValueSafe(ForceSamePrefabs);
                writer.WriteValueSafe(EnableSceneManagement);
                writer.WriteValueSafe(RecycleNetworkIds);
                writer.WriteValueSafe(NetworkIdRecycleDelay);
                writer.WriteValueSafe(EnableNetworkLogs);

                // Allocates
                return Convert.ToBase64String(writer.ToArray());
            }
        }

        /// <summary>
        /// Sets the NetworkConfig data with that from a base64 encoded version
        /// </summary>
        /// <param name="base64">The base64 encoded version</param>
        public void FromBase64(string base64)
        {
            NetworkConfig config = this;
            byte[] binary = Convert.FromBase64String(base64);
            using var reader = new FastBufferReader(binary, Allocator.Temp);
            using (reader)
            {
                reader.ReadValueSafe(out config.ProtocolVersion);
                reader.ReadValueSafe(out config.TickRate);
                reader.ReadValueSafe(out config.ClientConnectionBufferTimeout);
                reader.ReadValueSafe(out config.ConnectionApproval);
                reader.ReadValueSafe(out config.LoadSceneTimeOut);
                reader.ReadValueSafe(out config.EnableTimeResync);
                reader.ReadValueSafe(out config.EnsureNetworkVariableLengthSafety);
                reader.ReadValueSafe(out config.RpcHashSize);
                reader.ReadValueSafe(out config.ForceSamePrefabs);
                reader.ReadValueSafe(out config.EnableSceneManagement);
                reader.ReadValueSafe(out config.RecycleNetworkIds);
                reader.ReadValueSafe(out config.NetworkIdRecycleDelay);
                reader.ReadValueSafe(out config.EnableNetworkLogs);
            }
        }


        private ulong? m_ConfigHash = null;

        /// <summary>
        /// Gets a SHA256 hash of parts of the NetworkConfig instance
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public ulong GetConfig(bool cache = true)
        {
            if (m_ConfigHash != null && cache)
            {
                return m_ConfigHash.Value;
            }

            var writer = new FastBufferWriter(MessagingSystem.NON_FRAGMENTED_MESSAGE_MAX_SIZE, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(ProtocolVersion);
                writer.WriteValueSafe(NetworkConstants.PROTOCOL_VERSION);

                if (ForceSamePrefabs)
                {
                    var sortedDictionary = NetworkPrefabOverrideLinks.OrderBy(x => x.Key);
                    foreach (var sortedEntry in sortedDictionary)

                    {
                        writer.WriteValueSafe(sortedEntry.Key);
                    }
                }
                writer.WriteValueSafe(ConnectionApproval);
                writer.WriteValueSafe(ForceSamePrefabs);
                writer.WriteValueSafe(EnableSceneManagement);
                writer.WriteValueSafe(EnsureNetworkVariableLengthSafety);
                writer.WriteValueSafe(RpcHashSize);

                if (cache)
                {
                    m_ConfigHash = XXHash.Hash64(writer.ToArray());
                    return m_ConfigHash.Value;
                }

                return XXHash.Hash64(writer.ToArray());
            }
        }

        /// <summary>
        /// Compares a SHA256 hash with the current NetworkConfig instances hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public bool CompareConfig(ulong hash)
        {
            return hash == GetConfig();
        }
    }
}

