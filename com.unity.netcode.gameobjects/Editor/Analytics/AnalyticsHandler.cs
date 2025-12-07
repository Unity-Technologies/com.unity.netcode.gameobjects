#if UNITY_EDITOR
using System;
using UnityEngine.Analytics;

namespace Unity.Netcode.Editor
{
    internal class AnalyticsHandler<T> : IAnalytic where T : IAnalytic.IData
    {
        private T m_Data;

        internal T Data => m_Data;

        public AnalyticsHandler(T data)
        {
            m_Data = data;
        }
        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_Data;
            error = null;
            return data != null;
        }
    }
}
#endif
