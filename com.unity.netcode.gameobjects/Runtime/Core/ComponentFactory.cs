using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    internal static class ComponentFactory
    {
        internal delegate object CreateObjectDelegate(NetworkManager networkManager);

        private static Dictionary<Type, CreateObjectDelegate> m_Delegates = new Dictionary<Type, CreateObjectDelegate>();

        public static T Create<T>(NetworkManager networkManager)
        {
            return (T)m_Delegates[typeof(T)](networkManager);
        }

        public static void Register<T>(CreateObjectDelegate creator)
        {
            m_Delegates[typeof(T)] = creator;
        }

        public static void Deregister<T>()
        {
            m_Delegates.Remove(typeof(T));
        }

        public static void SetDefaults()
        {
            SetDefault<IDeferredMessageManager>(networkManager => new DeferredMessageManager(networkManager));
        }

        private static void SetDefault<T>(CreateObjectDelegate creator)
        {
            if (!m_Delegates.ContainsKey(typeof(T)))
            {
                m_Delegates[typeof(T)] = creator;
            }
        }
    }
}
