using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Unity.Netcode.Logging
{
    internal interface ILogContext
    {
        public void AppendTo(LogBuilder builder)
        {
        }
    }

    internal struct Context : ILogContext
    {
        public readonly LogLevel Level;
        private readonly string m_CallingFunction;
        internal readonly string Message;
        internal Object RelevantObjectOverride;

        private readonly GenericContext m_Other;

        public Context(LogLevel level, string msg, [CallerMemberName] string memberName = "")
        {
            Level = level;
            Message = msg;
            m_CallingFunction = memberName;

            m_Other = GenericContext.Create();
            RelevantObjectOverride = null;
        }

        internal Context(LogLevel level, string msg, bool noCaller)
        {
            Level = level;
            Message = msg;
            m_CallingFunction = null;

            m_Other = GenericContext.Create();
            RelevantObjectOverride = null;
        }

        public void AppendTo(LogBuilder builder)
        {
            // [CallingFunction]
            if (!string.IsNullOrEmpty(m_CallingFunction))
            {
                builder.AppendTag(m_CallingFunction);
            }

            // [SomeContext][SomeName:SomeValue]
            m_Other.AppendTo(builder);

            // Human-readable log message
            builder.Append(" ");
            builder.Append(Message);
        }

        public Context AddInfo(object key, object value)
        {
            m_Other.StoreInfo(key, value);
            return this;
        }

        public Context AddTag(string msg)
        {
            m_Other.StoreTag(msg);
            return this;
        }

        public Context AddObject(Object obj)
        {
            RelevantObjectOverride = obj;
            return this;
        }
    }
}
