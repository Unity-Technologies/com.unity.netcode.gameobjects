using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using Unity.Netcode.Editor;
#endif
using UnityEngine;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// SIPTransport (SIngleProcessTransport)
    /// is a NetworkTransport designed to be used with multiple network instances in a single process
    /// it's designed for the netcode in a way where no networking stack has to be available
    /// it's designed for testing purposes and it's not designed with speed in mind
    /// </summary>
#if UNITY_EDITOR
    [DontShowInTransportDropdown]
#endif
    public class SIPTransport : NetworkTransport
    {
        private struct Event
        {
            public NetworkEvent Type;
            public ulong ConnectionId;
            public ArraySegment<byte> Data;
        }

        private class Peer
        {
            public ulong ConnectionId;
            public SIPTransport Transport;
            public Queue<Event> IncomingBuffer = new Queue<Event>();
        }

        private readonly Dictionary<ulong, Peer> m_Peers = new Dictionary<ulong, Peer>();
        private ulong m_ClientsCounter = 1;

        private static Peer s_Server;
        private Peer m_LocalConnection;

        public override ulong ServerClientId => 0;
        public ulong LocalClientId;

        public override void DisconnectLocalClient()
        {
            if (m_LocalConnection != null)
            {
                // Inject local disconnect
                m_LocalConnection.IncomingBuffer.Enqueue(new Event
                {
                    Type = NetworkEvent.Disconnect,
                    ConnectionId = m_LocalConnection.ConnectionId,
                    Data = new ArraySegment<byte>()
                });

                if (s_Server != null && m_LocalConnection != null)
                {
                    // Remove the connection
                    s_Server.Transport.m_Peers.Remove(m_LocalConnection.ConnectionId);
                }

                if (m_LocalConnection.ConnectionId == ServerClientId)
                {
                    StopServer();
                }

                // Remove the local connection
                m_LocalConnection = null;
            }
        }

        // Called by server
        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (m_Peers.ContainsKey(clientId))
            {
                // Inject disconnect into remote
                m_Peers[clientId].IncomingBuffer.Enqueue(new Event
                {
                    Type = NetworkEvent.Disconnect,
                    ConnectionId = clientId,
                    Data = new ArraySegment<byte>()
                });

                // Inject local disconnect
                m_LocalConnection.IncomingBuffer.Enqueue(new Event
                {
                    Type = NetworkEvent.Disconnect,
                    ConnectionId = clientId,
                    Data = new ArraySegment<byte>()
                });

                // Remove the local connection on remote
                m_Peers[clientId].Transport.m_LocalConnection = null;

                // Remove connection on server
                m_Peers.Remove(clientId);
            }
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            // Always returns 50ms
            return 50;
        }

        public override void Initialize()
        {
        }

        private void StopServer()
        {
            s_Server = null;
            m_Peers.Remove(ServerClientId);
        }

        public override void Shutdown()
        {
            // Inject disconnects to all the remotes
            foreach (KeyValuePair<ulong, Peer> onePeer in m_Peers)
            {
                onePeer.Value.IncomingBuffer.Enqueue(new Event
                {
                    Type = NetworkEvent.Disconnect,
                    ConnectionId = LocalClientId,
                    Data = new ArraySegment<byte>()
                });
            }

            if (m_LocalConnection != null && m_LocalConnection.ConnectionId == ServerClientId)
            {
                StopServer();
            }


            // TODO: Cleanup
        }

        public override bool StartClient()
        {
            if (s_Server == null)
            {
                // No server
                Debug.LogError("No server");
                return false;
            }

            if (m_LocalConnection != null)
            {
                // Already connected
                Debug.LogError("Already connected");
                return false;
            }

            // Generate an Id for the server that represents this client
            ulong serverConnectionId = ++s_Server.Transport.m_ClientsCounter;
            LocalClientId = serverConnectionId;

            // Create local connection
            m_LocalConnection = new Peer()
            {
                ConnectionId = serverConnectionId,
                Transport = this,
                IncomingBuffer = new Queue<Event>()
            };

            // Add the server as a local connection
            m_Peers.Add(ServerClientId, s_Server);

            // Add local connection as a connection on the server
            s_Server.Transport.m_Peers.Add(serverConnectionId, m_LocalConnection);

            // Sends a connect message to the server
            s_Server.Transport.m_LocalConnection.IncomingBuffer.Enqueue(new Event()
            {
                Type = NetworkEvent.Connect,
                ConnectionId = serverConnectionId,
                Data = new ArraySegment<byte>()
            });

            // Send a local connect message
            m_LocalConnection.IncomingBuffer.Enqueue(new Event
            {
                Type = NetworkEvent.Connect,
                ConnectionId = ServerClientId,
                Data = new ArraySegment<byte>()
            });

            return true;
        }

        public override bool StartServer()
        {
            if (s_Server != null)
            {
                // Can only have one server
                Debug.LogError("Server already started");
                return false;
            }

            if (m_LocalConnection != null)
            {
                // Already connected
                Debug.LogError("Already connected");
                return false;
            }

            // Create local connection
            m_LocalConnection = new Peer()
            {
                ConnectionId = ServerClientId,
                Transport = this,
                IncomingBuffer = new Queue<Event>()
            };

            // Set the local connection as the server
            s_Server = m_LocalConnection;

            m_Peers.Add(ServerClientId, s_Server);

            return true;
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (m_LocalConnection != null)
            {
                // Create copy since netcode wants the byte array back straight after the method call.
                // Hard on GC.
                byte[] copy = new byte[payload.Count];
                Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);

                if (m_Peers.ContainsKey(clientId))
                {
                    m_Peers[clientId].IncomingBuffer.Enqueue(new Event
                    {
                        Type = NetworkEvent.Data,
                        ConnectionId = m_LocalConnection.ConnectionId,
                        Data = new ArraySegment<byte>(copy)
                    });
                }
            }
        }

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (m_LocalConnection != null)
            {
                if (m_LocalConnection.IncomingBuffer.Count == 0)
                {
                    clientId = 0;
                    payload = new ArraySegment<byte>();
                    receiveTime = 0;
                    return NetworkEvent.Nothing;
                }

                var peerEvent = m_LocalConnection.IncomingBuffer.Dequeue();

                clientId = peerEvent.ConnectionId;
                payload = peerEvent.Data;
                receiveTime = 0;

                return peerEvent.Type;
            }

            clientId = 0;
            payload = new ArraySegment<byte>();
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }
    }
}
