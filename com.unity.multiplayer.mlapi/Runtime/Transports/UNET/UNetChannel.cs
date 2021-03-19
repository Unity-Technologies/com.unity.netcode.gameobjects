using System;
using UnityEngine.Networking;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class UNetChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public NetworkChannel Id;

        /// <summary>
        /// The type of channel
        /// </summary>
        public QosType Type;
    }
}