using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents
{
    internal static class MessageManager
    {
        internal static Dictionary<string, int> channels;
        internal static Dictionary<int, string> reverseChannels;
        internal static Dictionary<string, ushort> messageTypes;
        internal static Dictionary<ushort, string> reverseMessageTypes;
        
        internal static Dictionary<ushort, Dictionary<int, Action<int, byte[]>>> messageCallbacks;
        internal static Dictionary<ushort, int> messageHandlerCounter;
        internal static Dictionary<ushort, Stack<int>> releasedMessageHandlerCounters;
        //Key: messageType, Value key: networkId, value value: handlerIds
        //internal static Dictionary<ushort, Dictionary<uint, List<int>>> targetedMessages;
        

        private static NetworkingManager netManager
        {
            get
            {
                return NetworkingManager.singleton;
            }
        }

        
        internal static int AddIncomingMessageHandler(string name, Action<int, byte[]> action, uint networkId)
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
                    messageCallbacks.Add(messageTypes[name], new Dictionary<int, Action<int, byte[]>>());
                    messageHandlerCounter.Add(messageTypes[name], 1);
                    messageCallbacks[messageTypes[name]].Add(0, action);
                    return 0;
                }
            }
            else
            {
                Debug.LogWarning("MLAPI: The message type " + name + " has not been registered. Please define it in the netConfig");
                return -1;
            }
        }

        internal static void RemoveIncomingMessageHandler(string name, int counter, uint networkId)
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
