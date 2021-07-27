using MLAPI.Messaging;
using MLAPI.Serialization;

namespace MLAPI.Metrics
{
    internal struct CommandContextSizeCapture
    {
        private readonly long m_InitialLength;
        private readonly InternalCommandContext m_Context;

        private long m_PreviousLength;

        public CommandContextSizeCapture(InternalCommandContext context)
        {
            m_Context = context;
            m_InitialLength = context.NetworkWriter.GetStream().SafeGetLengthOrDefault();
            m_PreviousLength = m_InitialLength;
        }
        
        public long Flush()
        {
            var currentLength = m_Context.NetworkWriter.GetStream().SafeGetLengthOrDefault();
            var segmentLength = currentLength - m_PreviousLength + m_InitialLength;
            
            m_PreviousLength = currentLength;
            
            return segmentLength;
        }
    }
    
    internal struct BufferSizeCapture
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
            var currentLength = m_Buffer.Length;
            var segmentLength = currentLength - m_PreviousLength + m_InitialLength;
            
            m_PreviousLength = currentLength;
            
            return segmentLength;
        }
    }
}