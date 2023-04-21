using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    /// <summary>
    /// This class is used to support testable code by allowing any supported component used by NetworkManager to be replaced
    /// with a mock component or a test version that overloads certain methods to change or record their behavior.
    /// Components currently supported by ComponentFactory:
    /// - IDeferredMessageManager
    /// </summary>
    internal static class ComponentFactory
    {
        internal delegate object CreateObjectDelegate(NetworkManager networkManager);

        private static Dictionary<Type, CreateObjectDelegate> s_Delegates = new Dictionary<Type, CreateObjectDelegate>();

        /// <summary>
        /// Instantiates an instance of a given interface
        /// </summary>
        /// <param name="networkManager">The network manager</param>
        /// <typeparam name="T">The interface to instantiate it with</typeparam>
        /// <returns></returns>
        public static T Create<T>(NetworkManager networkManager)
        {
            return (T)s_Delegates[typeof(T)](networkManager);
        }

        /// <summary>
        /// Overrides the default creation logic for a given interface type
        /// </summary>
        /// <param name="creator">The factory delegate to create the instance</param>
        /// <typeparam name="T">The interface type to override</typeparam>
        public static void Register<T>(CreateObjectDelegate creator)
        {
            s_Delegates[typeof(T)] = creator;
        }

        /// <summary>
        /// Reverts the creation logic for a given interface type to the default logic
        /// </summary>
        /// <typeparam name="T">The interface type to revert</typeparam>
        public static void Deregister<T>()
        {
            s_Delegates.Remove(typeof(T));
            SetDefaults();
        }

        /// <summary>
        /// Initializes the default creation logic for all supported component types
        /// </summary>
        public static void SetDefaults()
        {
            SetDefault<IDeferredNetworkMessageManager>(networkManager => new DeferredMessageManager(networkManager));

            SetDefault<IRealTimeProvider>(networkManager => new RealTimeProvider());
        }

        private static void SetDefault<T>(CreateObjectDelegate creator)
        {
            if (!s_Delegates.ContainsKey(typeof(T)))
            {
                s_Delegates[typeof(T)] = creator;
            }
        }
    }
}
