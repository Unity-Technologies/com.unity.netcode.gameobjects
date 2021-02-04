using System;
using MLAPI.Transports;

namespace MLAPI.Messaging.Buffering
{
    internal struct PreBufferPreset
    {
        public byte MessageType;
        public bool AllowBuffer;
        public ulong ClientId;
        public Channel Channel;
        public float ReceiveTime;
        public ArraySegment<byte> Data;
    }
}
