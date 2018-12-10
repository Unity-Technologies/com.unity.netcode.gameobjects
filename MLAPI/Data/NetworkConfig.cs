using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI.Data;
using MLAPI.Serialization;
using MLAPI.Transports;
using BitStream = MLAPI.Serialization.BitStream;
using System.Security.Cryptography.X509Certificates;

namespace MLAPI.Configuration
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
        public ushort ProtocolVersion = 0;
        /// <summary>
        /// The transport to be used
        /// </summary>
        public DefaultTransport Transport = DefaultTransport.UNET;
        /// <summary>
        /// The transport hosts the sever uses
        /// </summary>
        public IUDPTransport NetworkTransport = null;
        /// <summary>
        /// Only used if the transport is MLPAI-Relay
        /// </summary>
        public string RelayAddress = "127.0.0.1";
        /// <summary>
        /// Only used if the transport is MLPAI-Relay
        /// </summary>
        public ushort RelayPort = 8888;
        /// <summary>
        /// Wheter or not to use the relay
        /// </summary>
        public bool RelayEnabled = true;
        /// <summary>
        /// Channels used by the NetworkedTransport
        /// </summary>
        [HideInInspector]
        public List<Channel> Channels = new List<Channel>();
        /// <summary>
        /// A list of SceneNames that can be used during networked games.
        /// </summary>
        [HideInInspector]
        public List<string> RegisteredScenes = new List<string>();
        /// <summary>
        /// A list of spawnable prefabs
        /// </summary>
        [HideInInspector]
        public List<NetworkedPrefab> NetworkedPrefabs = new List<NetworkedPrefab>();
        /// <summary>
        /// The default player prefab
        /// </summary>
        [SerializeField]
        [HideInInspector]
        internal string PlayerPrefabName;
        /// <summary>
        /// The size of the receive message buffer. This is the max message size including any MLAPI overheads.
        /// </summary>
        public int MessageBufferSize = 1024;
        /// <summary>
        /// Amount of times per second the receive queue is emptied and all messages inside are processed.
        /// </summary>
        public int ReceiveTickrate = 64;
        /// <summary>
        /// The max amount of messages to process per ReceiveTickrate. This is to prevent flooding.
        /// </summary>
        public int MaxReceiveEventsPerTickRate = 500;
        /// <summary>
        /// The amount of times per second every pending message will be sent away.
        /// </summary>
        public int SendTickrate = 64;
        /// <summary>
        /// The amount of times per second internal frame events will occur, examples include SyncedVar send checking.
        /// </summary>
        public int EventTickrate = 64;
        /// <summary>
        /// The max amount of Clients that can connect.
        /// </summary>
        public int MaxConnections = 100;
        /// <summary>
        /// The port for the NetworkTransport to use when connecting
        /// </summary>
        public int ConnectPort = 7777;
        /// <summary>
        /// The address to connect to
        /// </summary>
        public string ConnectAddress = "127.0.0.1";
        /// <summary>
        /// The amount of seconds to wait for handshake to complete before timing out a client
        /// </summary>
        public int ClientConnectionBufferTimeout = 10;
        /// <summary>
        /// Wheter or not to use connection approval
        /// </summary>
        public bool ConnectionApproval = false;
        /// <summary>
        /// The data to send during connection which can be used to decide on if a client should get accepted
        /// </summary>
        [HideInInspector]
        public byte[] ConnectionData = new byte[0];
        /// <summary>
        /// The amount of seconds to keep a lag compensation position history
        /// </summary>
        public int SecondsHistory = 5;
        /// <summary>
        /// Wheter or not to make the library handle object spawning
        /// </summary>
        public bool HandleObjectSpawning = true;
        /// <summary>
        /// Wheter or not to enable scene switching
        /// </summary>
        public bool EnableSceneSwitching = true;
        /// <summary>
        /// If your logic uses the NetworkedTime, this should probably be turned off. If however it's needed to maximize accuracy, this is recommended to be turned on
        /// </summary>
        public bool EnableTimeResync = false;
        /// <summary>
        /// Wheter or not the MLAPI should check for differences in the prefabs at connection. 
        /// If you dynamically add prefabs at runtime, turn this OFF
        /// </summary>
        public bool ForceSamePrefabs = true;
        /// <summary>
        /// Decides how many bytes to use for Rpc messaging. Leave this to 2 bytes unless you are facing hash collisions
        /// </summary>
        public HashSize RpcHashSize = HashSize.VarIntTwoBytes;
        /// <summary>
        /// Decides how many bytes to use for Prefab names. Leave this to 2 bytes unless you are facing hash collisions
        /// </summary>
        public HashSize PrefabHashSize = HashSize.VarIntTwoBytes;
        /// <summary>
        /// Wheter or not to enable encryption
        /// The amount of seconds to wait on all clients to load requested scene before the SwitchSceneProgress onComplete callback, that waits for all clients to complete loading, is called anyway.
        /// </summary>
        public int LoadSceneTimeOut = 120;
        /// <summary>
        /// Wheter or not to enable the ECDHE key exchange to allow for encryption and authentication of messages
        /// </summary>
        [Header("Cryptography")]
        public bool EnableEncryption = false;
        /// <summary>
        /// Wheter or not to enable signed diffie hellman key exchange.
        /// </summary>
        public bool SignKeyExchange = false;
        /// <summary>
        /// Pfx file in base64 encoding containing private and public key
        /// </summary>
        [TextArea]
        public string ServerBase64PfxCertificate;
        /// <summary>
        /// Gets the currently in use certificate
        /// </summary>
        public X509Certificate2 ServerX509Certificate
        {
            get
            {
                return serverX509Certificate;
            }
            internal set
            {
                serverX509CertificateBytes = null;
                serverX509Certificate = value;
            }
        }
        private X509Certificate2 serverX509Certificate;
        /// <summary>
        /// Gets the cached binary representation of the server certificate that's used for handshaking
        /// </summary>
        public byte[] ServerX509CertificateBytes
        {
            get
            {
                if (serverX509CertificateBytes == null)
                    serverX509CertificateBytes = ServerX509Certificate.Export(X509ContentType.Cert);
                return serverX509CertificateBytes;
            }
        }
        private byte[] serverX509CertificateBytes = null;

        private void Sort()
        {
            Channels = Channels.OrderBy(x => x.Name).ToList();
            RegisteredScenes.Sort();
        }

        /// <summary>
        /// Returns a base64 encoded version of the config
        /// </summary>
        /// <returns></returns>
        public string ToBase64()
        {
            NetworkConfig config = this;
            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt16Packed(config.ProtocolVersion);
                    writer.WriteBits((byte)config.Transport, 5);

                    writer.WriteUInt16Packed((ushort)config.Channels.Count);
                    for (int i = 0; i < config.Channels.Count; i++)
                    {
                        writer.WriteString(config.Channels[i].Name);
                        writer.WriteBits((byte)config.Channels[i].Type, 5);
                    }

                    writer.WriteUInt16Packed((ushort)config.RegisteredScenes.Count);
                    for (int i = 0; i < config.RegisteredScenes.Count; i++)
                    {
                        writer.WriteString(config.RegisteredScenes[i]);
                    }

                    writer.WriteUInt16Packed((ushort)config.NetworkedPrefabs.Count);
                    for (int i = 0; i < config.NetworkedPrefabs.Count; i++)
                    {
                        writer.WriteBool(config.NetworkedPrefabs[i].playerPrefab);
                        writer.WriteString(config.NetworkedPrefabs[i].name);
                    }

                    writer.WriteInt32Packed(config.MessageBufferSize);
                    writer.WriteInt32Packed(config.ReceiveTickrate);
                    writer.WriteInt32Packed(config.MaxReceiveEventsPerTickRate);
                    writer.WriteInt32Packed(config.SendTickrate);
                    writer.WriteInt32Packed(config.EventTickrate);
                    writer.WriteInt32Packed(config.MaxConnections);
                    writer.WriteInt32Packed(config.ConnectPort);
                    writer.WriteString(config.ConnectAddress);
                    writer.WriteInt32Packed(config.ClientConnectionBufferTimeout);
                    writer.WriteBool(config.ConnectionApproval);
                    writer.WriteInt32Packed(config.SecondsHistory);
                    writer.WriteBool(config.HandleObjectSpawning);
                    writer.WriteBool(config.EnableEncryption);
                    writer.WriteBool(config.SignKeyExchange);
                    writer.WriteBool(config.EnableSceneSwitching);
                    writer.WriteInt32Packed(config.LoadSceneTimeOut);
                    writer.WriteBool(config.EnableTimeResync);
                    writer.WriteBits((byte)config.RpcHashSize, 3);
                    stream.PadStream();

                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }
        
        /// <summary>
        /// Sets the NetworkConfig data with that from a base64 encoded version
        /// </summary>
        /// <param name="base64">The base64 encoded version</param>
        /// <param name="createDummyObject">Wheter or not to create dummy objects for NetworkedPrefabs</param>
        public void FromBase64(string base64, bool createDummyObject = false)
        {
            NetworkConfig config = this;
            byte[] binary = Convert.FromBase64String(base64);
            using (BitStream stream = new BitStream(binary))
            {
                using (PooledBitReader reader = PooledBitReader.Get(stream))
                {

                    config.ProtocolVersion = reader.ReadUInt16Packed();
                    config.Transport = (DefaultTransport)reader.ReadBits(5);

                    ushort channelCount = reader.ReadUInt16Packed();
                    config.Channels.Clear();
                    for (int i = 0; i < channelCount; i++)
                    {
                        Channel channel = new Channel()
                        {
                            Name = reader.ReadString().ToString(),
                            Type = (ChannelType)reader.ReadBits(5)
                        };
                        config.Channels.Add(channel);
                    }

                    ushort sceneCount = reader.ReadUInt16Packed();
                    config.RegisteredScenes.Clear();
                    for (int i = 0; i < sceneCount; i++)
                    {
                        config.RegisteredScenes.Add(reader.ReadString().ToString());
                    }

                    ushort networkedPrefabsCount = reader.ReadUInt16Packed();
                    config.NetworkedPrefabs.Clear();
                    GameObject root = createDummyObject ? new GameObject("MLAPI: Dummy prefabs") : null;
                    for (int i = 0; i < networkedPrefabsCount; i++)
                    {
                        bool playerPrefab = reader.ReadBool();
                        string prefabName = reader.ReadString().ToString();
                        GameObject dummyPrefab = createDummyObject ? new GameObject("REPLACEME: " + prefabName + "(Dummy prefab)", typeof(NetworkedObject)) : null;
                        if (dummyPrefab != null)
                        {
                            dummyPrefab.GetComponent<NetworkedObject>().NetworkedPrefabName = prefabName;
                            dummyPrefab.transform.SetParent(root.transform); //This is just here to not ruin your hierarchy
                        }
                        NetworkedPrefab networkedPrefab = new NetworkedPrefab()
                        {
                            playerPrefab = playerPrefab,
                            prefab = dummyPrefab
                        };
                        config.NetworkedPrefabs.Add(networkedPrefab);
                    }

                    config.MessageBufferSize = reader.ReadInt32Packed();
                    config.ReceiveTickrate = reader.ReadInt32Packed();
                    config.MaxReceiveEventsPerTickRate = reader.ReadInt32Packed();
                    config.SendTickrate = reader.ReadInt32Packed();
                    config.EventTickrate = reader.ReadInt32Packed();
                    config.MaxConnections = reader.ReadInt32Packed();
                    config.ConnectPort = reader.ReadInt32Packed();
                    config.ConnectAddress = reader.ReadString().ToString();
                    config.ClientConnectionBufferTimeout = reader.ReadInt32Packed();
                    config.ConnectionApproval = reader.ReadBool();
                    config.SecondsHistory = reader.ReadInt32Packed();
                    config.HandleObjectSpawning = reader.ReadBool();
                    config.EnableEncryption = reader.ReadBool();
                    config.SignKeyExchange = reader.ReadBool();
                    config.EnableSceneSwitching = reader.ReadBool();
                    config.LoadSceneTimeOut = reader.ReadInt32Packed();
                    config.EnableTimeResync = reader.ReadBool();
                    config.RpcHashSize = (HashSize)reader.ReadBits(3);
                }
            }
        }
        

        private ulong? ConfigHash = null;
        /// <summary>
        /// Gets a SHA256 hash of parts of the NetworkingConfiguration instance
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public ulong GetConfig(bool cache = true)
        {
            if (ConfigHash != null && cache)
                return ConfigHash.Value;

            Sort();

            using (PooledBitStream stream = PooledBitStream.Get())
            {
                using (PooledBitWriter writer = PooledBitWriter.Get(stream))
                {
                    writer.WriteUInt16Packed(ProtocolVersion);
                    writer.WriteString(MLAPIConstants.MLAPI_PROTOCOL_VERSION);

                    for (int i = 0; i < Channels.Count; i++)
                    {
                        writer.WriteString(Channels[i].Name);
                        writer.WriteByte((byte)Channels[i].Type);
                    }

                    if (EnableSceneSwitching)
                    {
                        for (int i = 0; i < RegisteredScenes.Count; i++)
                        {
                            writer.WriteString(RegisteredScenes[i]);
                        }
                    }

                    if (HandleObjectSpawning && ForceSamePrefabs)
                    {
                        List<NetworkedPrefab> sortedPrefabList = NetworkedPrefabs.OrderBy(x => x.hash).ToList();
                        for (int i = 0; i < sortedPrefabList.Count; i++)
                        {
                            writer.WriteUInt64Packed(sortedPrefabList[i].hash);
                        }
                    }

                    writer.WriteBool(ForceSamePrefabs);
                    writer.WriteBool(HandleObjectSpawning);
                    writer.WriteBool(EnableEncryption);
                    writer.WriteBool(EnableSceneSwitching);
                    writer.WriteBool(SignKeyExchange);
                    writer.WriteBits((byte)RpcHashSize, 3);
                    writer.WriteBits((byte)PrefabHashSize, 3);
                    stream.PadStream();

                    if (cache)
                    {
                        ConfigHash = stream.ToArray().GetStableHash64();
                        return ConfigHash.Value;
                    }

                    return stream.ToArray().GetStableHash64();
                }
            }
        }

        /// <summary>
        /// Compares a SHA256 hash with the current NetworkingConfiguration instances hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public bool CompareConfig(ulong hash)
        {
            return hash == GetConfig();
        }
    }
}
