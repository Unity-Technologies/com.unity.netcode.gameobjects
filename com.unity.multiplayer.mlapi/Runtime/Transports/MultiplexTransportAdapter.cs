using System;
using System.Collections.Generic;
using MLAPI.Transports.Tasks;

namespace MLAPI.Transports.Multiplex
{
    /// <summary>
    /// Multiplex transport adapter.
    /// </summary>
    public class MultiplexTransportAdapter : NetworkTransport
    {
        /// <summary>
        /// The method to use to distribute the transport connectionIds in a fixed size 64 bit integer.
        /// </summary>
        public enum ConnectionIdSpreadMethod
        {
            /// <summary>
            /// Drops the first few bits (left side) by shifting the transport clientId to the left and inserting the transportId in the first bits.
            /// Ensure that ALL transports dont use the last bits in their produced clientId.
            /// For incremental clientIds, this is the most space efficient assuming that every transport get used an equal amount.
            /// </summary>
            MakeRoomLastBits,

            /// <summary>
            /// Drops the first few bits (left side) and replaces them with the transport index.
            /// Ensure that ALL transports dont use the first few bits in the produced clientId.
            /// </summary>
            ReplaceFirstBits,

            /// <summary>
            /// Drops the last few bits (right side) and replaces them with the transport index.
            /// Ensure that ALL transports dont use the last bits in their produced clientId.
            /// This option is for advanced users and will not work with the official MLAPI transports as they use the last bits.
            /// </summary>
            ReplaceLastBits,

            /// <summary>
            /// Drops the last few bits (right side) by shifting the transport clientId to the right and inserting the transportId in the first bits.
            /// Ensure that ALL transports dont use the first bits in their produced clientId.
            /// </summary>
            MakeRoomFirstBits,

            /// <summary>
            /// Spreads the clientIds evenly among the transports.
            /// </summary>
            Spread
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public ConnectionIdSpreadMethod SpreadMethod = ConnectionIdSpreadMethod.MakeRoomLastBits;
        public NetworkTransport[] Transports = new NetworkTransport[0];
        public override ulong ServerClientId => 0;

        private byte m_LastProcessedTransportIndex;

        public override bool IsSupported => true;

        public override void DisconnectLocalClient()
        {
            Transports[GetFirstSupportedTransportIndex()].DisconnectLocalClient();
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            GetMultiplexTransportDetails(clientId, out byte transportId, out ulong connectionId);

            Transports[transportId].DisconnectRemoteClient(connectionId);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            GetMultiplexTransportDetails(clientId, out byte transportId, out ulong connectionId);

            return Transports[transportId].GetCurrentRtt(connectionId);
        }

        public override void Init()
        {
            for (int i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    Transports[i].Init();
                }
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out NetworkChannel networkChannel, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (m_LastProcessedTransportIndex >= Transports.Length - 1)
            {
                m_LastProcessedTransportIndex = 0;
            }

            for (byte i = m_LastProcessedTransportIndex; i < Transports.Length; i++)
            {
                m_LastProcessedTransportIndex = i;

                if (Transports[i].IsSupported)
                {
                    var networkEvent = Transports[i].PollEvent(out ulong connectionId, out networkChannel, out payload, out receiveTime);

                    if (networkEvent != NetworkEvent.Nothing)
                    {
                        clientId = GetMLAPIClientId(i, connectionId, false);

                        return networkEvent;
                    }
                }
            }

            clientId = 0;
            networkChannel = 0;
            payload = new ArraySegment<byte>();
            receiveTime = 0;

            return NetworkEvent.Nothing;
        }

        public override void Send(ulong clientId, ArraySegment<byte> data, NetworkChannel networkChannel)
        {
            GetMultiplexTransportDetails(clientId, out byte transportId, out ulong connectionId);

            Transports[transportId].Send(connectionId, data, networkChannel);
        }

        public override void Shutdown()
        {
            for (int i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    Transports[i].Shutdown();
                }
            }
        }

        public override SocketTasks StartClient()
        {
            var socketTasks = new List<SocketTask>();

            for (int i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    socketTasks.AddRange(Transports[i].StartClient().Tasks);
                }
            }

            return new SocketTasks { Tasks = socketTasks.ToArray() };
        }

        public override SocketTasks StartServer()
        {
            var socketTasks = new List<SocketTask>();

            for (int i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    socketTasks.AddRange(Transports[i].StartServer().Tasks);
                }
            }

            return new SocketTasks { Tasks = socketTasks.ToArray() };
        }


        public ulong GetMLAPIClientId(byte transportId, ulong connectionId, bool isServer)
        {
            if (isServer)
            {
                return ServerClientId;
            }

            switch (SpreadMethod)
            {
                case ConnectionIdSpreadMethod.ReplaceFirstBits:
                {
                    // Calculate bits to store transportId
                    byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                    // Drop first bits of connectionId
                    ulong clientId = ((connectionId << bits) >> bits);

                    // Place transportId there
                    ulong shiftedTransportId = (ulong)transportId << ((sizeof(ulong) * 8) - bits);

                    return (clientId | shiftedTransportId) + 1;
                }
                case ConnectionIdSpreadMethod.MakeRoomFirstBits:
                {
                    // Calculate bits to store transportId
                    byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                    // Drop first bits of connectionId
                    ulong clientId = (connectionId >> bits);

                    // Place transportId there
                    ulong shiftedTransportId = (ulong)transportId << ((sizeof(ulong) * 8) - bits);

                    return (clientId | shiftedTransportId) + 1;
                }
                case ConnectionIdSpreadMethod.ReplaceLastBits:
                {
                    // Calculate bits to store transportId
                    byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                    // Drop the last bits of connectionId
                    ulong clientId = ((connectionId >> bits) << bits);

                    // Return the transport inserted at the end
                    return (clientId | transportId) + 1;
                }
                case ConnectionIdSpreadMethod.MakeRoomLastBits:
                {
                    // Calculate bits to store transportId
                    byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                    // Drop the last bits of connectionId
                    ulong clientId = (connectionId << bits);

                    // Return the transport inserted at the end
                    return (clientId | transportId) + 1;
                }
                case ConnectionIdSpreadMethod.Spread:
                {
                    return (connectionId * (ulong)Transports.Length + (ulong)transportId) + 1;
                }
                default:
                {
                    return ServerClientId;
                }
            }
        }

        public void GetMultiplexTransportDetails(ulong clientId, out byte transportId, out ulong connectionId)
        {
            if (clientId == ServerClientId)
            {
                transportId = GetFirstSupportedTransportIndex();
                connectionId = Transports[transportId].ServerClientId;
            }
            else
            {
                switch (SpreadMethod)
                {
                    case ConnectionIdSpreadMethod.ReplaceFirstBits:
                    {
                        // The first clientId is reserved. Thus every clientId is always offset by 1
                        clientId--;

                        // Calculate bits to store transportId
                        byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                        transportId = (byte)(clientId >> ((sizeof(ulong) * 8) - bits));
                        connectionId = ((clientId << bits) >> bits);
                        break;
                    }
                    case ConnectionIdSpreadMethod.MakeRoomFirstBits:
                    {
                        // The first clientId is reserved. Thus every clientId is always offset by 1
                        clientId--;

                        // Calculate bits to store transportId
                        byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                        transportId = (byte)(clientId >> ((sizeof(ulong) * 8) - bits));
                        connectionId = (clientId << bits);
                        break;
                    }
                    case ConnectionIdSpreadMethod.ReplaceLastBits:
                    {
                        // The first clientId is reserved. Thus every clientId is always offset by 1
                        clientId--;

                        // Calculate bits to store transportId
                        byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                        transportId = (byte)((clientId << ((sizeof(ulong) * 8) - bits)) >> ((sizeof(ulong) * 8) - bits));
                        connectionId = ((clientId >> bits) << bits);
                        break;
                    }
                    case ConnectionIdSpreadMethod.MakeRoomLastBits:
                    {
                        // The first clientId is reserved. Thus every clientId is always offset by 1
                        clientId--;

                        // Calculate bits to store transportId
                        byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                        transportId = (byte)((clientId << ((sizeof(ulong) * 8) - bits)) >> ((sizeof(ulong) * 8) - bits));
                        connectionId = (clientId >> bits);
                        break;
                    }
                    case ConnectionIdSpreadMethod.Spread:
                    {
                        // The first clientId is reserved. Thus every clientId is always offset by 1
                        clientId--;

                        transportId = (byte)(clientId % (ulong)Transports.Length);
                        connectionId = (clientId / (ulong)Transports.Length);
                        break;
                    }
                    default:
                    {
                        transportId = GetFirstSupportedTransportIndex();
                        connectionId = Transports[transportId].ServerClientId;
                        break;
                    }
                }
            }
        }

        public byte GetFirstSupportedTransportIndex()
        {
            for (byte i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    return i;
                }
            }

            return 0;
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}