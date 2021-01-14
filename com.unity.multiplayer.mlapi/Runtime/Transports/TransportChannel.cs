using System;
using System.Collections.Generic;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class TransportChannel
    {
        public TransportChannel(string name, ChannelType type, byte id)
        {
            Name = name;
            Type = type;
            Id = id;

            ChannelByteToString[Id] = Name;
            ChannelStringToByte[Name] = Id;
        }

        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name;

        public byte Id;

        /// <summary>
        /// The type of channel
        /// </summary>
        public ChannelType Type;

        public static Dictionary<byte, string> ChannelByteToString = new Dictionary<byte, string>();
        public static Dictionary<string, byte> ChannelStringToByte = new Dictionary<string, byte>();
    }
}
