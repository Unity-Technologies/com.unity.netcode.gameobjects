using System;

namespace MLAPI.Transports.UNET
{
    /// <summary>
    /// Represents a Transport host
    /// </summary>
    [Serializable]
    public class TransportHost
    {
        /// <summary>
        /// The name of the host
        /// </summary>
        public string Name = Guid.NewGuid().ToString().Replace("-", "");
        /// <summary>
        /// The port the host should listen to
        /// </summary>
        public int Port = 7777;
        /// <summary>
        /// If true, the socket will listen on TCP-Websockets, otherwise UDP
        /// </summary>
        public bool Websockets = false;
    }
}
