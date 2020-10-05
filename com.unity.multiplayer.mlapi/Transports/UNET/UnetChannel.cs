using System;
using UnityEngine.Networking;

namespace MLAPI.Transports
{
    /// <summary>
    /// A transport channel used by the MLAPI
    /// </summary>
    [Serializable]
    public class UnetChannel
    {
        /// <summary>
        /// The name of the channel
        /// </summary>
        public string Name;

        /// <summary>
        /// The type of channel
        /// </summary>
        public QosType Type;
    }
}