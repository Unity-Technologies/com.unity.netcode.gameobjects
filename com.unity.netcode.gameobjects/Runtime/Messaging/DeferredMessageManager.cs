using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Netcode
{
    internal class DeferredMessageManager : IDeferredNetworkMessageManager
    {
        protected struct TriggerData
        {
            public FastBufferReader Reader;
            public NetworkMessageHeader Header;
            public ulong SenderId;
            public float Timestamp;
            public int SerializedHeaderSize;
        }
        protected struct TriggerInfo
        {
            public float Expiry;
            public NativeList<TriggerData> TriggerData;
        }

        protected readonly Dictionary<IDeferredNetworkMessageManager.TriggerType, Dictionary<ulong, TriggerInfo>> m_Triggers = new Dictionary<IDeferredNetworkMessageManager.TriggerType, Dictionary<ulong, TriggerInfo>>();

        private readonly NetworkManager m_NetworkManager;

        internal DeferredMessageManager(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }

        /// <summary>
        /// Defers processing of a message until the moment a specific networkObjectId is spawned.
        /// This is to handle situations where an RPC or other object-specific message arrives before the spawn does,
        /// either due to it being requested in OnNetworkSpawn before the spawn call has been executed
        ///
        /// There is a one second maximum lifetime of triggers to avoid memory leaks. After one second has passed
        /// without the requested object ID being spawned, the triggers for it are automatically deleted.
        /// </summary>
        public virtual unsafe void DeferMessage(IDeferredNetworkMessageManager.TriggerType trigger, ulong key, FastBufferReader reader, ref NetworkContext context)
        {
            if (!m_Triggers.TryGetValue(trigger, out var triggers))
            {
                triggers = new Dictionary<ulong, TriggerInfo>();
                m_Triggers[trigger] = triggers;
            }

            if (!triggers.TryGetValue(key, out var triggerInfo))
            {
                triggerInfo = new TriggerInfo
                {
                    Expiry = m_NetworkManager.RealTimeProvider.RealTimeSinceStartup + m_NetworkManager.NetworkConfig.SpawnTimeout,
                    TriggerData = new NativeList<TriggerData>(Allocator.Persistent)
                };
                triggers[key] = triggerInfo;
            }

            triggerInfo.TriggerData.Add(new TriggerData
            {
                Reader = new FastBufferReader(reader.GetUnsafePtr(), Allocator.Persistent, reader.Length),
                Header = context.Header,
                Timestamp = context.Timestamp,
                SenderId = context.SenderId,
                SerializedHeaderSize = context.SerializedHeaderSize
            });
        }

        /// <summary>
        /// Cleans up any trigger that's existed for more than a second.
        /// These triggers were probably for situations where a request was received after a despawn rather than before a spawn.
        /// </summary>
        public virtual unsafe void CleanupStaleTriggers()
        {
            foreach (var kvp in m_Triggers)
            {
                ulong* staleKeys = stackalloc ulong[kvp.Value.Count];
                int index = 0;
                foreach (var kvp2 in kvp.Value)
                {
                    if (kvp2.Value.Expiry < m_NetworkManager.RealTimeProvider.RealTimeSinceStartup)
                    {
                        staleKeys[index++] = kvp2.Key;
                        PurgeTrigger(kvp.Key, kvp2.Key, kvp2.Value);
                    }
                }

                for (var i = 0; i < index; ++i)
                {
                    kvp.Value.Remove(staleKeys[i]);
                }
            }
        }

        protected virtual void PurgeTrigger(IDeferredNetworkMessageManager.TriggerType triggerType, ulong key, TriggerInfo triggerInfo)
        {
            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
            {
                NetworkLog.LogWarning($"Deferred messages were received for a trigger of type {triggerType} with key {key}, but that trigger was not received within within {m_NetworkManager.NetworkConfig.SpawnTimeout} second(s).");
            }

            foreach (var data in triggerInfo.TriggerData)
            {
                data.Reader.Dispose();
            }

            triggerInfo.TriggerData.Dispose();
        }

        public virtual void ProcessTriggers(IDeferredNetworkMessageManager.TriggerType trigger, ulong key)
        {
            if (m_Triggers.TryGetValue(trigger, out var triggers))
            {
                // This must happen after InvokeBehaviourNetworkSpawn, otherwise ClientRPCs and other messages can be
                // processed before the object is fully spawned. This must be the last thing done in the spawn process.
                if (triggers.TryGetValue(key, out var triggerInfo))
                {
                    triggers.Remove(key);
                    foreach (var deferredMessage in triggerInfo.TriggerData)
                    {
                        // Reader will be disposed within HandleMessage
                        m_NetworkManager.ConnectionManager.MessageManager.HandleMessage(deferredMessage.Header, deferredMessage.Reader, deferredMessage.SenderId, deferredMessage.Timestamp, deferredMessage.SerializedHeaderSize);
                    }

                    triggerInfo.TriggerData.Dispose();
                }
            }
        }

        /// <summary>
        /// Cleans up any trigger that's existed for more than a second.
        /// These triggers were probably for situations where a request was received after a despawn rather than before a spawn.
        /// </summary>
        public virtual void CleanupAllTriggers()
        {
            foreach (var kvp in m_Triggers)
            {
                foreach (var kvp2 in kvp.Value)
                {
                    foreach (var data in kvp2.Value.TriggerData)
                    {
                        data.Reader.Dispose();
                    }
                    kvp2.Value.TriggerData.Dispose();
                }
            }
            m_Triggers.Clear();
        }
    }
}
