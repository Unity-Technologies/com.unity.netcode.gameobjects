using MLAPI.Serialization;

namespace MLAPI.Metrics
{
    public struct BufferSizeCapture
    {
        private readonly long m_InitialLength;
        private readonly NetworkBuffer m_Buffer;

        private long m_PreviousLength;

        public BufferSizeCapture(NetworkBuffer buffer)
        {
            m_Buffer = buffer;
            m_InitialLength = buffer.Length;
            m_PreviousLength = m_InitialLength;
        }
        
        public long Flush()
        {
            var segmentLength = m_Buffer.Length - m_PreviousLength + m_InitialLength;
            
            m_PreviousLength = m_Buffer.Length;
            
            return segmentLength;
        }
    }
}