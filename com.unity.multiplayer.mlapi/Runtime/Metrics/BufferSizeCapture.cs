using System;

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
        private long m_Overhead;
        private long m_CurrentSize;

        public CommandContextSizeCapture(InternalCommandContext context)
        {
            m_Context = context;
            m_Overhead = 0L;
            m_CurrentSize = 0L;
        }

        public long Size => m_CurrentSize + m_Overhead;

        public CaptureScope MeasureOverhead()
        {
            return new CaptureScope(m_Context, SetOverhead);
        }

        public CaptureScope Measure()
        {
            return new CaptureScope(m_Context, SetCurrentSize);
        }

        private void SetOverhead(long overhead)
        {
            m_Overhead = overhead;
        }

        private void SetCurrentSize(long currentSize)
        {
            m_CurrentSize = currentSize;
        }

        public struct CaptureScope : IDisposable
        {
            private readonly InternalCommandContext m_Context;
            private readonly Action<long> m_Callback;
            private readonly long m_InitialSize;

            internal CaptureScope(InternalCommandContext context, Action<long> callback)
            {
                m_Context = context;
                m_InitialSize = context.NetworkWriter.GetStream().Position;
                m_Callback = callback;
            }

            public void Dispose()
            {
                m_Callback?.Invoke(m_Context.NetworkWriter.GetStream().Position - m_InitialSize);
            }
        }
    }
}
