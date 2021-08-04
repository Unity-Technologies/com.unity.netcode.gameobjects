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

    internal class CommandContextSizeCapture
    {
        private readonly InternalCommandContext m_Context;
        private long m_Overhead;
        private long m_CurrentSize;

        public CommandContextSizeCapture(InternalCommandContext context)
        {
            m_Context = context;
        }

        public long Size => m_CurrentSize + m_Overhead;

        public CaptureScope MeasureOverhead()
        {
            return new CaptureScope(m_Context, overhead => m_Overhead = overhead);
        }

        public CaptureScope Measure()
        {
            return new CaptureScope(m_Context, size => m_CurrentSize = size);
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
