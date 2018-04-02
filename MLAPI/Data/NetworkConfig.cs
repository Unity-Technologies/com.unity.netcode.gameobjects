using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
        /// Channels used by the NetworkedTransport
        /// </summary>
        public List<Channel> Channels = new List<Channel>();
        /// <summary>
        /// Registered MessageTypes
        /// </summary>
        public List<string> MessageTypes = new List<string>();
        /// <summary>
        /// List of MessageTypes that can be passed through by Server. MessageTypes in this list should thus not be trusted to as great of an extent as normal messages.
        /// </summary>
        public List<string> PassthroughMessageTypes = new List<string>();
        internal HashSet<ushort> PassthroughMessageHashSet = new HashSet<ushort>();
        /// <summary>
        /// Set of channels that will have all message contents encrypted when used
        /// </summary>
        public List<string> EncryptedChannels = new List<string>();
        internal HashSet<string> EncryptedChannelsHashSet = new HashSet<string>();
        /// <summary>
        /// A list of SceneNames that can be used during networked games.
        /// </summary>
        public List<string> RegisteredScenes = new List<string>();
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
        /// The port for the NetworkTransport to use
        /// </summary>
        public int Port = 7777;
        /// <summary>
        /// The address to connect to
        /// </summary>
        public string Address = "127.0.0.1";
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
        public byte[] ConnectionData = new byte[0];
        /// <summary>
        /// The amount of seconds to keep a lag compensation position history
        /// </summary>
        public float SecondsHistory = 5;
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
        public bool EnableSceneSwitching = false;

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

            using (MemoryStream writeStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(writeStream))
                {
                    writer.Write(ProtocolVersion);
                    for (int i = 0; i < Channels.Count; i++)
                    {
                        writer.Write(Channels[i].Name);
                        writer.Write((byte)Channels[i].Type);
                    }
                    for (int i = 0; i < MessageTypes.Count; i++)
                    {
                        writer.Write(MessageTypes[i]);
                    }
                    if (AllowPassthroughMessages)
                    {
                        for (int i = 0; i < PassthroughMessageTypes.Count; i++)
                        {
                            writer.Write(PassthroughMessageTypes[i]);
                        }
                    }
                    if (EnableSceneSwitching)
                    {
                        for (int i = 0; i < RegisteredScenes.Count; i++)
                        {
                            writer.Write(RegisteredScenes[i]);
                        }
                    }
                    if(EnableEncryption)
                    {
                        for (int i = 0; i < EncryptedChannels.Count; i++)
                        {
                            writer.Write(EncryptedChannels[i]);
                        }
                    }
                    writer.Write(HandleObjectSpawning);
                    writer.Write(EnableEncryption);
                    writer.Write(AllowPassthroughMessages);
                    writer.Write(EnableSceneSwitching);
                    writer.Write(SignKeyExchange);
                }
                using (SHA256Managed sha256 = new SHA256Managed())
                {
                    //Returns a 256 bit / 32 byte long checksum of the config
                    if (cache)
                    {
                        ConfigHash = sha256.ComputeHash(writeStream.ToArray());
                        return ConfigHash;
                    }
                    return sha256.ComputeHash(writeStream.ToArray());
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
