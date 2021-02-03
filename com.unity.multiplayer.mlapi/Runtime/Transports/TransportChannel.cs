using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class TransportChannel
    {
        public TransportChannel(Transport.TransportType id, ChannelType type)
        {
            Id = (byte)id;
            Type = type;
            Name = id.ToString();

            Debug.Log("Name: " + Name);

            ChannelByteToString[Id] = Name;
            ChannelStringToByte[Name] = Id;
        }

        /// <summary>
        /// Channel identifier
        /// </summary>
        public byte Id;

        /// <summary>
        /// Channel type
        /// </summary>
        public ChannelType Type;

        /// <summary>
        /// Pretty-print name of the channel
        /// </summary>
        public string Name;


        public static Dictionary<byte, string> ChannelByteToString = new Dictionary<byte, string>(); // demo?
        public static Dictionary<string, byte> ChannelStringToByte = new Dictionary<string, byte>(); // demo?
    }
}
