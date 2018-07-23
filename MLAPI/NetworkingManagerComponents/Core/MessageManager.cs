using MLAPI.MonoBehaviours.Core;
using MLAPI.NetworkingManagerComponents.Binary;
using System;
using System.Collections.Generic;

namespace MLAPI.NetworkingManagerComponents.Core
{
    internal static class MessageManager
    {
        internal static readonly Dictionary<string, int> channels = new Dictionary<string, int>();
        internal static readonly Dictionary<int, string> reverseChannels = new Dictionary<int, string>();
        internal static readonly Dictionary<string, ushort> messageTypes = new Dictionary<string, ushort>();
        internal static readonly Dictionary<ushort, string> reverseMessageTypes = new Dictionary<ushort, string>();

        internal static readonly Dictionary<ushort, Dictionary<int, Action<uint, BitReader>>> messageCallbacks = new Dictionary<ushort, Dictionary<int, Action<uint, BitReader>>>();
        internal static readonly Dictionary<ushort, int> messageHandlerCounter = new Dictionary<ushort, int>();
        internal static readonly Dictionary<ushort, Stack<int>> releasedMessageHandlerCounters = new Dictionary<ushort, Stack<int>>();

        private static NetworkingManager netManager => NetworkingManager.singleton;

        
        internal static int AddIncomingMessageHandler(string name, Action<uint, BitReader> action)
        {
            if (messageTypes.ContainsKey(name))
            {
                if (messageCallbacks.ContainsKey(messageTypes[name]))
                {
                    int handlerId = 0;
                    if (messageHandlerCounter.ContainsKey(messageTypes[name]))
                    {
                        if (!releasedMessageHandlerCounters.ContainsKey(messageTypes[name]))
                            releasedMessageHandlerCounters.Add(messageTypes[name], new Stack<int>());

                        if (releasedMessageHandlerCounters[messageTypes[name]].Count == 0)
                        {
                            handlerId = messageHandlerCounter[messageTypes[name]];
                            messageHandlerCounter[messageTypes[name]]++;
                        }
                        else
                        {
                            handlerId = releasedMessageHandlerCounters[messageTypes[name]].Pop();
                        }
                    }
                    else
                    {
                        messageHandlerCounter.Add(messageTypes[name], handlerId + 1);
                    }
                    messageCallbacks[messageTypes[name]].Add(handlerId, action);
                    return handlerId;
                }
                else
                {
                    messageCallbacks.Add(messageTypes[name], new Dictionary<int, Action<uint, BitReader>>());
                    messageHandlerCounter.Add(messageTypes[name], 1);
                    messageCallbacks[messageTypes[name]].Add(0, action);
                    return 0;
                }
            }
            else
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("The message type " + name + " has not been registered. Please define it in the netConfig");
                return -1;
            }
        }

        internal static void RemoveIncomingMessageHandler(string name, int counter)
        {
            if (counter == -1)
                return;

            if (messageTypes.ContainsKey(name) && messageCallbacks.ContainsKey(messageTypes[name]) && messageCallbacks[messageTypes[name]].ContainsKey(counter))
            {
                messageCallbacks[messageTypes[name]].Remove(counter);
                if (!releasedMessageHandlerCounters.ContainsKey(messageTypes[name]))
                    releasedMessageHandlerCounters.Add(messageTypes[name], new Stack<int>());
                releasedMessageHandlerCounters[messageTypes[name]].Push(counter);
            }
        }
        
    }
}
