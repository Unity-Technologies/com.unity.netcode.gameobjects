using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Unity.Netcode.Logging
{
    internal interface ILogContext
    {
        public void AppendTo(LogBuilder builder);
    }

    internal struct Context : ILogContext, IDisposable
    {
        public readonly LogLevel Level;
        private readonly string m_CallingFunction;
        internal readonly string Message;
        internal Object RelevantObjectOverride;

        private readonly GenericContext m_Other;
        private List<ILogContext> m_Prepend;
        private List<ILogContext> m_Postpend;

        public Context(LogLevel level, string msg, [CallerMemberName] string memberName = "")
        {
            Level = level;
            Message = msg;
            m_CallingFunction = memberName;

            m_Other = GenericContext.Create();
            RelevantObjectOverride = null;
            m_Prepend = null;
            m_Postpend = null;
        }

        internal Context(LogLevel level, string msg, bool noCaller)
        {
            Level = level;
            Message = msg;
            m_CallingFunction = null;

            m_Other = GenericContext.Create();
            RelevantObjectOverride = null;
            m_Prepend = null;
            m_Postpend = null;
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

            if (m_Prepend != null)
            {
                foreach (var context in m_Prepend)
                {
                    context.AppendTo(builder);
                }
            }

            // Human-readable log message
            builder.Append(" ");
            builder.Append(Message);

            if (m_Postpend != null)
            {
                foreach (var context in m_Postpend)
                {
                    context.AppendTo(builder);
                }
            }
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

        public Context AddNetworkObject(NetworkObject networkObject)
        {
            AddPrepend(new LogContextNetworkObject(networkObject));
            RelevantObjectOverride = networkObject;
            return this;
        }

        public Context AddNetworkBehaviour(NetworkBehaviour networkBehaviour)
        {
            AddPrepend(new LogContextNetworkBehaviour(networkBehaviour));
            RelevantObjectOverride = networkBehaviour;
            return this;
        }

        public Context AddCollection<TItem>(IEnumerable<TItem> collection, LogCollectionBuilder<TItem> builder)
        {
            AddPostpend(new CollectionContext<TItem>(collection, builder));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPrepend(ILogContext prepend)
        {
            if (m_Prepend == null)
            {
                m_Prepend = PreallocatedStore.GetPreallocated();
            }
            m_Prepend.Add(prepend);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddPostpend(ILogContext postpend)
        {
            if (m_Postpend == null)
            {
                m_Postpend = PreallocatedStore.GetPreallocated();
            }
            m_Postpend.Add(postpend);
        }

        public void Dispose()
        {
            m_Other.Dispose();
            PreallocatedStore.Free(m_Prepend);
            PreallocatedStore.Free(m_Postpend);
            m_Prepend = null;
            m_Postpend = null;
        }

        private static class PreallocatedStore
        {
            private static readonly Queue<List<ILogContext>> k_Preallocated = new();

            internal static List<ILogContext> GetPreallocated()
            {
                if (k_Preallocated.Count > 0)
                {
                    k_Preallocated.Dequeue();
                }

                return new List<ILogContext>();
            }

            internal static void Free(List<ILogContext> collection)
            {
                collection.Clear();
                k_Preallocated.Enqueue(collection);
            }
        }
    }
}
