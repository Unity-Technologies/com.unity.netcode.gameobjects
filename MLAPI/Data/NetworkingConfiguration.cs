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
        public Dictionary<string, QosType> Channels = new Dictionary<string, QosType>();
        public List<string> MessageTypes = new List<string>();
        public int MessageBufferSize = 65536;
        public int MaxMessagesPerFrame = 150;
        public int MaxConnections = 100;
        public int Port = 7777;
        public string Address;
        public int ClientConnectionBufferTimeout = 10;
        public bool ConnectionApproval = false;
        public Action<byte[], int, Action<int, bool>> ConnectionApprovalCallback;
        public byte[] ConnectionData;
        public bool HandleObjectSpawning = true;
        //TODO
        public bool CompressMessages = false;
        //Should only be used for dedicated servers and will require the servers RSA keypair being hard coded into clients in order to exchange a AES key
        //TODO
        public bool EncryptMessages = false;


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
                    writer.Write(HandleObjectSpawning);
                    writer.Write(CompressMessages);
                    writer.Write(EncryptMessages);
                }
                using(SHA256Managed sha256 = new SHA256Managed())
                {
                    //Returns a 256 bit / 32 byte long checksum of the config
                    if (cache)
                    {
                        ConfigHash = sha256.ComputeHash(writeStream);
                        return ConfigHash;
                    }
                    return sha256.ComputeHash(writeStream);
                }
            }
        }

        public bool CompareConfig(byte[] hash)
        {
            return hash == GetConfig();
        }
    }
}
