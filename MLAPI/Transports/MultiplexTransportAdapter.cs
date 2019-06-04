using System;

namespace MLAPI.Transports.Multiplex
{
    public class MultiplexTransportAdapter : Transport
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

        public ConnectionIdSpreadMethod SpreadMethod = ConnectionIdSpreadMethod.MakeRoomLastBits;
        public Transport[] Transports = new Transport[0];
        public override ulong ServerClientId => 0;

        private byte _lastProcessedTransportIndex;

        public override void DisconnectLocalClient()
        {
            Transports[GetFirstSupportedTransportIndex()].DisconnectLocalClient();
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            GetMultiplexTransportDetails(clientId, out byte transportId, out ulong connectionId);

            Transports[transportId].DisconnectRemoteClient(connectionId);
        }

        public override void FlushSendQueue(ulong clientId)
        {
            GetMultiplexTransportDetails(clientId, out byte transportId, out ulong connectionId);

            Transports[transportId].FlushSendQueue(connectionId);
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

        public override NetEventType PollEvent(out ulong clientId, out string channelName, out ArraySegment<byte> payload)
        {
            if (_lastProcessedTransportIndex > Transports.Length)
                _lastProcessedTransportIndex = 0;

            for (byte i = _lastProcessedTransportIndex; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    _lastProcessedTransportIndex = i;

                    return Transports[i].PollEvent(out clientId, out channelName, out payload);
                }
            }

            clientId = 0;
            channelName = null;
            payload = new ArraySegment<byte>();

            return NetEventType.Nothing;
        }

        public override void Send(ulong clientId, ArraySegment<byte> data, string channelName, bool skipQueue)
        {
            GetMultiplexTransportDetails(clientId, out byte transportId, out ulong connectionId);

            Transports[transportId].Send(clientId, data, channelName, skipQueue);
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

        public override void StartClient()
        {
            for (int i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    Transports[i].StartClient();
                }
            }
        }

        public override void StartServer()
        {
            for (int i = 0; i < Transports.Length; i++)
            {
                if (Transports[i].IsSupported)
                {
                    Transports[i].StartServer();
                }
            }
        }


        public ulong GetMLAPIClientId(byte transportId, ulong connectionId, bool isServer)
        {
            if (isServer)
            {
                return ServerClientId;
            }
            else
            {
                // 0 Is reserved.
                connectionId += 1;

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

                            return clientId | shiftedTransportId;
                        }
                    case ConnectionIdSpreadMethod.MakeRoomFirstBits:
                        {
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            // Drop first bits of connectionId
                            ulong clientId = (connectionId >> bits);

                            // Place transportId there
                            ulong shiftedTransportId = (ulong)transportId << ((sizeof(ulong) * 8) - bits);

                            return clientId | shiftedTransportId;
                        }
                    case ConnectionIdSpreadMethod.ReplaceLastBits:
                        {
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            // Drop the last bits of connectionId
                            ulong clientId = ((connectionId >> bits) << bits);

                            // Return the transport inserted at the end
                            return clientId | transportId;
                        }
                    case ConnectionIdSpreadMethod.MakeRoomLastBits:
                        {
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            // Drop the last bits of connectionId
                            ulong clientId = (connectionId << bits);

                            // Return the transport inserted at the end
                            return clientId | transportId;
                        }
                    case ConnectionIdSpreadMethod.Spread:
                        {
                            return connectionId * (ulong)Transports.Length + (ulong)transportId;
                        }
                    default:
                        {
                            return ServerClientId;
                        }
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
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            transportId = (byte)(clientId >> ((sizeof(ulong) * 8) - bits));
                            connectionId = ((clientId << bits) >> bits) + 1;
                            break;
                        }
                    case ConnectionIdSpreadMethod.MakeRoomFirstBits:
                        {
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            transportId = (byte)(clientId >> ((sizeof(ulong) * 8) - bits));
                            connectionId = (clientId << bits) + 1;
                            break;
                        }
                    case ConnectionIdSpreadMethod.ReplaceLastBits:
                        {
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            transportId = (byte)((clientId << ((sizeof(ulong) * 8) - bits)) >> ((sizeof(ulong) * 8) - bits));
                            connectionId = ((clientId >> bits) << bits) + 1;
                            break;
                        }
                    case ConnectionIdSpreadMethod.MakeRoomLastBits:
                        {
                            // Calculate bits to store transportId
                            byte bits = (byte)UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Log(Transports.Length, 2));

                            transportId = (byte)((clientId << ((sizeof(ulong) * 8) - bits)) >> ((sizeof(ulong) * 8) - bits));
                            connectionId = (clientId >> bits) + 1;
                            break;
                        }
                    case ConnectionIdSpreadMethod.Spread:
                        {
                            transportId = (byte)(clientId % (ulong)Transports.Length);
                            connectionId = (clientId / (ulong)Transports.Length) + 1;
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
    }
}
