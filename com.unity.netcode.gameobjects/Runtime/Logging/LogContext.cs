using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.Netcode
{
    internal interface ILogContext
    {
        public void AppendTo(ContextBuilder builder)
        {
        }
    }

    internal struct Context : ILogContext
    {
        public readonly LogLevel Level;
        private readonly string m_CallingFunction;
        internal readonly string Message;
        public GameObject GameObjectOverride;


        private readonly GenericContext m_Other;

        public Context(LogLevel level, string msg, [CallerMemberName] string memberName = "")
        {
            Level = level;
            Message = msg;
            m_CallingFunction = memberName;

            m_Other = GenericContext.Create();
            GameObjectOverride = null;
        }

        internal Context(LogLevel level, string msg, bool noCaller)
        {
            Level = level;
            Message = msg;
            m_CallingFunction = null;

            m_Other = GenericContext.Create();
            GameObjectOverride = null;
        }

        public void AppendTo(ContextBuilder builder)
        {
            // [CallingFunction]
            if (!string.IsNullOrEmpty(m_CallingFunction))
            {
                builder.AppendContext(m_CallingFunction);
            }

            // [SomeContext][SomeName:SomeValue]
            m_Other.AppendTo(builder);

            // Human-readable log message
            builder.Append(" ");
            builder.Append(Message);
        }

        public Context With(object key, object value)
        {
            m_Other.StoreInfo(key, value);
            return this;
        }

        public Context With(string msg)
        {
            m_Other.StoreContext(msg);
            return this;
        }

        public Context ForNetworkPrefab(NetworkPrefab networkPrefab)
        {
            GameObjectOverride = networkPrefab.Prefab.gameObject;
            m_Other.StoreInfo(nameof(NetworkPrefab), networkPrefab.Prefab.name);
            return this;
        }
    }
}
