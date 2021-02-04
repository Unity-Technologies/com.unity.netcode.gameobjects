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
        public TransportChannel(Channel id, ChannelType type)
        {
            Id = id;
            Type = type;
        }

        /// <summary>
        /// Channel identifier
        /// </summary>
        public Channel Id;

        /// <summary>
        /// Channel type
        /// </summary>
        public ChannelType Type;
    }
}
