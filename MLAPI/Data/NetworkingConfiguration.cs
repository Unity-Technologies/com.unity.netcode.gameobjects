using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine.Networking;

namespace MLAPI
{
    public class NetworkingConfiguration
    {
        public ushort ProtocolVersion = 0;
        public SortedDictionary<string, QosType> Channels = new SortedDictionary<string, QosType>();
        public List<string> MessageTypes = new List<string>();
        public List<string> PassthroughMessageTypes = new List<string>();
        internal HashSet<ushort> RegisteredPassthroughMessageTypes = new HashSet<ushort>();
        public HashSet<int> EncryptedChannels = new HashSet<int>();
        public List<string> RegisteredScenes = new List<string>();
        public int MessageBufferSize = 65535;
        public int ReceiveTickrate = 64;
        public int MaxReceiveEventsPerTickRate = 500;
        public int SendTickrate = 64;
        public int EventTickrate = 64;
        public int MaxConnections = 100;
        public int Port = 7777;
        public string Address = "127.0.0.1";
        public int ClientConnectionBufferTimeout = 10;
        public bool ConnectionApproval = false;
        public Action<byte[], int, Action<int, bool>> ConnectionApprovalCallback = null;
        public byte[] ConnectionData = new byte[0];
        public float SecondsHistory = 5;
        public bool HandleObjectSpawning = true;

        public bool EnableEncryption = true;
        public bool SignKeyExchange = true;
        public string RSAPrivateKey = "<RSAKeyValue><Modulus>vBEvOQki/EftWOgwh4G8/nFRvcDJLylc8P7Dhz5m/hpkkNtAMzizNKYUrGbs7sYWlEuMYBOWrzkIDGOMoOsYc9uCi+8EcmNoHDlIhK5yNfZUexYBF551VbvZ625LSBR7kmBxkyo4IPuA09fYCHeUFm3prt4h6aTD0Hjc7ZsJHUU=</Modulus><Exponent>EQ==</Exponent><P>ydgcrq5qLJOdDQibD3m9+o3/dkKoFeCC110dnMgdpEteCruyBdL0zjGKKvjjgy3XTSSp43EN591NiXaBp0JtDw==</P><Q>7obHrUnUCsSHUsIJ7+JOrupcGrQ0XaYcQ+Uwb2v7d2YUzwZ46U4gI9snfD2J0tc3DGEh3v3G0Q8q7bxEe3H4aw==</Q><DP>L34k3c6vkgSdbHp+1nb/hj+HZx6+I0PijQbZyolwYuSOmR0a1DGjA1bzVWe9D86NAxevgM9OkOjG8yrxVIgZqQ==</DP><DQ>OB+2gyBuIKa2bdNNodrlVlVC2RtXnZB/HwjAGjeGdnJfP8VJoE6eJo3rLEq3BG7fxq1xYaUfuLhGVg4uOyngGQ==</DQ><InverseQ>o97PimYu58qH5eFmySRCIsyhBr/tK2GM17Zd9QQPJZRSorrhIJn1m6gwQ/G5aJLIM/3Yl04CoyqmQGsPXMzW2w==</InverseQ><D>CxAR1i22w4vCquB7U0Pd8Nl9R2Wxez6rHTwpnoszPB+rkAzlqKj7e5FMgpykhoQfciKPyWqQZKkAeTMIRbN56JinvpAt5POId/28HDd5xjGymHE81k3RzoHqzQXFIOF1TSYKUWzjPPF/TU4nn7auD4i6lOODATsMqtLr5DRBN/0=</D></RSAKeyValue>"; //CHANGE THESE FOR PRODUCTION!
        public string RSAPublicKey = "<RSAKeyValue><Modulus>vBEvOQki/EftWOgwh4G8/nFRvcDJLylc8P7Dhz5m/hpkkNtAMzizNKYUrGbs7sYWlEuMYBOWrzkIDGOMoOsYc9uCi+8EcmNoHDlIhK5yNfZUexYBF551VbvZ625LSBR7kmBxkyo4IPuA09fYCHeUFm3prt4h6aTD0Hjc7ZsJHUU=</Modulus><Exponent>EQ==</Exponent></RSAKeyValue>"; //CHANGE THESE FOR PRODUCTION!

        public bool AllowPassthroughMessages = true;
        public bool EnableSceneSwitching = false;

        //Cached config hash
        private byte[] ConfigHash = null;
        public byte[] GetConfig(bool cache = true)
        {
            if (ConfigHash != null && cache)
                return ConfigHash;

            using(MemoryStream writeStream = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(writeStream))
                {
                    writer.Write(ProtocolVersion);
                    foreach (KeyValuePair<string, QosType> pair in Channels)
                    {
                        writer.Write(pair.Key);
                        writer.Write((int)pair.Value);
                    }
                    for (int i = 0; i < MessageTypes.Count; i++)
                    {
                        writer.Write(MessageTypes[i]);
                    }
                    if(AllowPassthroughMessages)
                    {
                        for (int i = 0; i < PassthroughMessageTypes.Count; i++)
                        {
                            writer.Write(PassthroughMessageTypes[i]);
                        }
                    }
                    if(EnableSceneSwitching)
                    {
                        for (int i = 0; i < RegisteredScenes.Count; i++)
                        {
                            writer.Write(RegisteredScenes[i]);
                        }
                    }
                    writer.Write(HandleObjectSpawning);
                    writer.Write(EnableEncryption);
                    writer.Write(AllowPassthroughMessages);
                    writer.Write(EnableSceneSwitching);
                }
                using(SHA256Managed sha256 = new SHA256Managed())
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
