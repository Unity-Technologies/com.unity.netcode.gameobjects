using System;
namespace MLAPI.Messaging.Buffering
{
    internal struct PreBufferPreset
    {
        public byte MessageType;
        public bool AllowBuffer;
        public ulong ClientId;
        public byte Channel;
        public float ReceiveTime;
        public ArraySegment<byte> Data;
    }
}
