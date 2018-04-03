using MLAPI.MonoBehaviours.Core;
using System;

namespace MLAPI.Data
{
    public struct NetId
    {
        public byte HostId;
        public ushort ConnectionId;
        public byte Meta;

        public bool IsHost()
        {
            return Meta == 1;
        }

        public bool IsInvalid()
        {
            return Meta == 2;
        }

        public static NetId ServerNetId
        {
            get
            {
                return new NetId((byte)NetworkingManager.singleton.serverHostId, (ushort)NetworkingManager.singleton.serverConnectionId, false, false);
            }
        }

        public NetId(byte hostId, ushort connectionId, bool isHost, bool isInvalid)
        {
            HostId = hostId;
            ConnectionId = connectionId;
            if (isHost)
                Meta = 1;
            else if (isInvalid)
                Meta = 2;
            else
                Meta = 0;
        }


        private static byte[] tempUIntBytes = new byte[4];
        private static byte[] tempUShortBytes = new byte[2];
        public NetId(uint clientId)
        {
            tempUIntBytes = BitConverter.GetBytes(clientId);
            HostId = tempUIntBytes[0];
            ConnectionId = BitConverter.ToUInt16(tempUIntBytes, 1);
            Meta = tempUIntBytes[3];
        }

        public uint GetClientId()
        {
            tempUShortBytes = BitConverter.GetBytes(ConnectionId);
            tempUIntBytes[0] = HostId;
            tempUIntBytes[1] = tempUShortBytes[0];
            tempUIntBytes[2] = tempUShortBytes[1];
            tempUIntBytes[3] = Meta;
            return BitConverter.ToUInt32(tempUIntBytes, 0);
        }

        public override bool Equals (object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            NetId key = (NetId)obj;
            return (HostId == key.HostId) && (ConnectionId == key.ConnectionId);
        }

        public override int GetHashCode()
        {
            return (int)GetClientId();
        }

        public static bool operator ==(NetId client1, NetId client2)
        {
            return (client1.HostId == client2.HostId && client1.ConnectionId == client2.ConnectionId) || (client1.IsHost() == client2.IsHost());
        }

        public static bool operator !=(NetId client1, NetId client2)
        {
            return !(client1 == client2);
        }
    }
}
