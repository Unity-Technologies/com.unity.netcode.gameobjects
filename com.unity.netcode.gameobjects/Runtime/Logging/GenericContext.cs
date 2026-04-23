using System;
using System.Collections.Generic;

namespace Unity.Netcode.Logging
{
    internal readonly struct GenericContext : ILogContext, IDisposable
    {
        private readonly List<string> m_Contexts;
        private readonly Dictionary<object, object> m_Info;

        private GenericContext(List<string> contexts, Dictionary<object, object> info)
        {
            m_Contexts = contexts;
            m_Info = info;
        }

        public void AppendTo(LogBuilder builder)
        {
            if (m_Contexts != null)
            {
                foreach (var ctx in m_Contexts)
                {
                    builder.AppendTag(ctx);
                }
            }

            if (m_Info != null)
            {
                foreach (var (key, value) in m_Info)
                {
                    builder.AppendInfo(key, value);
                }
            }
        }

        public void StoreTag(string msg)
        {
            m_Contexts.Add(msg);
        }

        public void StoreInfo(object key, object value)
        {
            m_Info.Add(key, value);
        }

        public void ClearInfo(object key)
        {
            m_Info?.Remove(key);
        }

        public void Dispose()
        {
            PreallocatedStore.Free(this);
        }

        public static GenericContext Create()
        {
            return PreallocatedStore.GetPreallocated();
        }

        private static class PreallocatedStore
        {
            private static readonly Queue<GenericContext> k_Preallocated = new();

            internal static GenericContext GetPreallocated()
            {
                if (k_Preallocated.Count > 0)
                {
                    k_Preallocated.Dequeue();
                }

                var contexts = new List<string>();
                var info = new Dictionary<object, object>();
                return new GenericContext(contexts, info);
            }

            internal static void Free(GenericContext ctx)
            {
                ctx.m_Contexts.Clear();
                ctx.m_Info.Clear();
                k_Preallocated.Enqueue(ctx);
            }
        }
    }

}
