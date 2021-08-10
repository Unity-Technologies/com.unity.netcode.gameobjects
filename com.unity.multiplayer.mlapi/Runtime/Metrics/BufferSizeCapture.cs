namespace Unity.Netcode
{
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

    internal struct CommandContextSizeCapture
    {
        private readonly InternalCommandContext m_Context;
        private long m_OverheadBegin;
        private long m_OverheadEnd;
        private long m_SegmentBegin;

        public CommandContextSizeCapture(InternalCommandContext context)
        {
            m_Context = context;
            m_OverheadBegin = 0L;
            m_OverheadEnd = 0L;
            m_SegmentBegin = 0L;
        }

        public void StartMeasureOverhead()
        {
            m_OverheadBegin = m_Context.NetworkWriter.GetStream().Position;
        }

        public void StopMeasureOverhead()
        {
            m_OverheadEnd = m_Context.NetworkWriter.GetStream().Position;
        }

        public void StartMeasureSegment()
        {
            m_SegmentBegin = m_Context.NetworkWriter.GetStream().Position;
        }

        public long StopMeasureSegment()
        {
            var segmentEnd = m_Context.NetworkWriter.GetStream().Position;

            return m_OverheadEnd - m_OverheadBegin + segmentEnd - m_SegmentBegin;
        }
    }
}
