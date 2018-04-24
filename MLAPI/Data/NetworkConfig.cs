using MLAPI.Data.Transports.UNET;
using MLAPI.NetworkingManagerComponents.Binary;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace MLAPI.Data
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
        /// The transport hosts the sever uses
        /// </summary>
        public IUDPTransport NetworkTransport = new UnetTransport();
        /// <summary>
        /// Channels used by the NetworkedTransport
        /// </summary>
        public List<Channel> Channels = new List<Channel>();
        /// <summary>
        /// Registered MessageTypes
        /// </summary>
        public List<MessageType> MessageTypes = new List<MessageType>();
        internal HashSet<ushort> PassthroughMessageHashSet = new HashSet<ushort>();
        internal HashSet<string> EncryptedChannelsHashSet = new HashSet<string>();
        internal List<string> EncryptedChannels = new List<string>();
        /// <summary>
        /// A list of SceneNames that can be used during networked games.
        /// </summary>
        public List<string> RegisteredScenes = new List<string>();
        /// <summary>
        /// A list of spawnable prefabs
        /// </summary>
        [HideInInspector]
        public List<NetworkedPrefab> NetworkedPrefabs = new List<NetworkedPrefab>();
        internal Dictionary<string, int> NetworkPrefabIds;
        internal Dictionary<int, string> NetworkPrefabNames;
        /// <summary>
        /// The default player prefab
        /// </summary>
        [SerializeField]
        [HideInInspector]
        internal string PlayerPrefabName;
        /// <summary>
        /// The size of the receive message buffer. This is the max message size.
        /// </summary>
        public int MessageBufferSize = 65535;
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
        /// Wheter or not to enable encryption
        /// </summary>
        public bool EnableEncryption = true;
        /// <summary>
        /// Wheter or not to enable signed diffie hellman key exchange.
        /// </summary>
        public bool SignKeyExchange = true;
        /// <summary>
        /// Private RSA XML key to use for signing key exchange
        /// </summary>
        [TextArea]
        public string RSAPrivateKey = "<RSAKeyValue><Modulus>vBEvOQki/EftWOgwh4G8/nFRvcDJLylc8P7Dhz5m/hpkkNtAMzizNKYUrGbs7sYWlEuMYBOWrzkIDGOMoOsYc9uCi+8EcmNoHDlIhK5yNfZUexYBF551VbvZ625LSBR7kmBxkyo4IPuA09fYCHeUFm3prt4h6aTD0Hjc7ZsJHUU=</Modulus><Exponent>EQ==</Exponent><P>ydgcrq5qLJOdDQibD3m9+o3/dkKoFeCC110dnMgdpEteCruyBdL0zjGKKvjjgy3XTSSp43EN591NiXaBp0JtDw==</P><Q>7obHrUnUCsSHUsIJ7+JOrupcGrQ0XaYcQ+Uwb2v7d2YUzwZ46U4gI9snfD2J0tc3DGEh3v3G0Q8q7bxEe3H4aw==</Q><DP>L34k3c6vkgSdbHp+1nb/hj+HZx6+I0PijQbZyolwYuSOmR0a1DGjA1bzVWe9D86NAxevgM9OkOjG8yrxVIgZqQ==</DP><DQ>OB+2gyBuIKa2bdNNodrlVlVC2RtXnZB/HwjAGjeGdnJfP8VJoE6eJo3rLEq3BG7fxq1xYaUfuLhGVg4uOyngGQ==</DQ><InverseQ>o97PimYu58qH5eFmySRCIsyhBr/tK2GM17Zd9QQPJZRSorrhIJn1m6gwQ/G5aJLIM/3Yl04CoyqmQGsPXMzW2w==</InverseQ><D>CxAR1i22w4vCquB7U0Pd8Nl9R2Wxez6rHTwpnoszPB+rkAzlqKj7e5FMgpykhoQfciKPyWqQZKkAeTMIRbN56JinvpAt5POId/28HDd5xjGymHE81k3RzoHqzQXFIOF1TSYKUWzjPPF/TU4nn7auD4i6lOODATsMqtLr5DRBN/0=</D></RSAKeyValue>"; //CHANGE THESE FOR PRODUCTION!
        /// <summary>
        /// Public RSA XML key to use for signing key exchange
        /// </summary>
        [TextArea]
        public string RSAPublicKey = "<RSAKeyValue><Modulus>vBEvOQki/EftWOgwh4G8/nFRvcDJLylc8P7Dhz5m/hpkkNtAMzizNKYUrGbs7sYWlEuMYBOWrzkIDGOMoOsYc9uCi+8EcmNoHDlIhK5yNfZUexYBF551VbvZ625LSBR7kmBxkyo4IPuA09fYCHeUFm3prt4h6aTD0Hjc7ZsJHUU=</Modulus><Exponent>EQ==</Exponent></RSAKeyValue>"; //CHANGE THESE FOR PRODUCTION!
        /// <summary>
        /// Wheter or not to allow any type of passthrough messages
        /// </summary>
        public bool AllowPassthroughMessages = true;
        /// <summary>
        /// Wheter or not to enable scene switching
        /// </summary>
        public bool EnableSceneSwitching = true;
        /// <summary>
        /// If your logic uses the NetwokrTime, this should probably be turned off. If however it's needed to maximize accuracy, this is recommended to be turned on
        /// </summary>
        public bool EnableTimeResync = false;

        private byte[] ConfigHash = null;
        /// <summary>
        /// Gets a SHA256 hash of parts of the NetworkingConfiguration instance
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public byte[] GetConfig(bool cache = true)
        {
            if (ConfigHash != null && cache)
                return ConfigHash;

            using (BitWriter writer = new BitWriter())
            {
                writer.WriteUShort(ProtocolVersion);
                for (int i = 0; i < Channels.Count; i++)
                {
                    writer.WriteString(Channels[i].Name);
                    writer.WriteByte((byte)Channels[i].Type);
                    if (EnableEncryption)
                        writer.WriteBool(Channels[i].Encrypted);
                }
                for (int i = 0; i < MessageTypes.Count; i++)
                {
                    writer.WriteString(MessageTypes[i].Name);
                    if (AllowPassthroughMessages)
                        writer.WriteBool(MessageTypes[i].Passthrough);
                }
                if (EnableSceneSwitching)
                {
                    for (int i = 0; i < RegisteredScenes.Count; i++)
                    {
                        writer.WriteString(RegisteredScenes[i]);
                    }
                }
                if (HandleObjectSpawning)
                {
                    for (int i = 0; i < NetworkedPrefabs.Count; i++)
                    {
                        writer.WriteString(NetworkedPrefabs[i].name);
                    }
                }
                writer.WriteBool(HandleObjectSpawning);
                writer.WriteBool(EnableEncryption);
                writer.WriteBool(AllowPassthroughMessages);
                writer.WriteBool(EnableSceneSwitching);
                writer.WriteBool(SignKeyExchange);

                using (SHA256Managed sha256 = new SHA256Managed())
                {
                    //Returns a 256 bit / 32 byte long checksum of the config
                    if (cache)
                    {
                        ConfigHash = sha256.ComputeHash(writer.Finalize());
                        return ConfigHash;
                    }
                    return sha256.ComputeHash(writer.Finalize());
                }
            }
        }

        /// <summary>
        /// Compares a SHA256 hash with the current NetworkingConfiguration instances hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public bool CompareConfig(byte[] hash)
        {
            byte[] localConfigHash = GetConfig();

            if (hash.Length != localConfigHash.Length)
                return false;

            for (int i = 0; i < hash.Length; i++)
            {
                if (hash[i] != localConfigHash[i])
                    return false;
            }
            return true;
        }
    }
}
