using System.Collections.Generic;

namespace Unity.Netcode.Logging
{
    internal delegate string LogCollectionBuilder<in TItem>(TItem item);
    internal readonly struct CollectionContext<TItem> : ILogContext
    {
        private readonly LogCollectionBuilder<TItem> m_Delegate;
        private readonly IEnumerable<TItem> m_Collection;

        public CollectionContext(IEnumerable<TItem> collection, LogCollectionBuilder<TItem> builder)
        {
            m_Delegate = builder;
            m_Collection = collection;
        }

        public void AppendTo(LogBuilder builder)
        {
            foreach (var item in m_Collection)
            {
                builder.AppendLine(m_Delegate(item));
            }
        }
    }
}
