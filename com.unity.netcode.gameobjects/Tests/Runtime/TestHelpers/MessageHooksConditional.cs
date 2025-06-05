using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace Unity.Netcode.TestHelpers.Runtime
{
    public class MessageHooksConditional : ConditionalPredicateBase
    {
        private List<MessageHookEntry> m_MessageHookEntries;

        public bool AllMessagesReceived { get; internal set; }
        public int NumberOfMessagesReceived
        {
            get
            {
                return m_MessageHookEntries.Where((c) => !c.MessageHooks.IsWaiting).Count();
            }
        }

        public string GetHooksStillWaiting()
        {
            var retMessageTypes = string.Empty;
            var waitingMessages = m_MessageHookEntries.Where((c) => c.MessageHooks.IsWaiting);

            foreach (var waitingMessage in waitingMessages)
            {
                retMessageTypes += $":{waitingMessage.MessageType}:";
            }

            return retMessageTypes;
        }


        protected override bool OnHasConditionBeenReached()
        {
            AllMessagesReceived = NumberOfMessagesReceived == m_MessageHookEntries.Count;

            if (AllMessagesReceived)
            {
                foreach (var entry in m_MessageHookEntries)
                {
                    entry.RemoveHook();
                }
            }

            return AllMessagesReceived;
        }

        protected override void OnFinished()
        {
            base.OnFinished();
        }

        public void Reset()
        {
            foreach (var entry in m_MessageHookEntries)
            {
                entry.Initialize();
            }
        }

        public MessageHooksConditional(List<MessageHookEntry> messageHookEntries)
        {
            m_MessageHookEntries = messageHookEntries;
        }
    }

    public enum ReceiptType
    {
        Received,
        Handled
    }

    public class MessageHookEntry
    {
        internal MessageHooks MessageHooks;
        protected NetworkManager m_NetworkManager;
        private MessageHooks.MessageReceiptCheck m_MessageReceiptCheck;
        private MessageHooks.MessageHandleCheck m_MessageHandleCheck;
        internal string MessageType;
        private ReceiptType m_ReceiptType;

        public void Initialize()
        {
            MessageHooks = new MessageHooks();
            if (m_ReceiptType == ReceiptType.Handled)
            {
                Assert.IsNotNull(m_MessageHandleCheck, $"{nameof(m_MessageHandleCheck)} is null, did you forget to initialize?");
                MessageHooks.HandleCheck = m_MessageHandleCheck;
            }
            else
            {
                Assert.IsNotNull(m_MessageReceiptCheck, $"{nameof(m_MessageReceiptCheck)} is null, did you forget to initialize?");
                MessageHooks.ReceiptCheck = m_MessageReceiptCheck;
            }
            Assert.IsNotNull(m_NetworkManager.ConnectionManager.MessageManager, $"{nameof(NetworkMessageManager)} is null! Did you forget to start first?");
            m_NetworkManager.ConnectionManager.MessageManager.Hook(MessageHooks);
        }

        internal void AssignMessageType<T>() where T : INetworkMessage
        {
            MessageType = typeof(T).Name;
            if (m_ReceiptType == ReceiptType.Handled)
            {
                m_MessageHandleCheck = MessageHooks.CheckForMessageOfTypeHandled<T>;
            }
            else
            {
                m_MessageReceiptCheck = MessageHooks.CheckForMessageOfTypeReceived<T>;
            }
            Initialize();
        }

        internal void RemoveHook()
        {
            m_NetworkManager.ConnectionManager.MessageManager.Unhook(MessageHooks);
        }

        internal void AssignMessageType(Type type)
        {
            MessageType = type.Name;
            if (m_ReceiptType == ReceiptType.Handled)
            {
                m_MessageHandleCheck = (message) =>
                {
                    return message.GetType() == type;
                };
            }
            else
            {
                m_MessageReceiptCheck = (messageType) =>
                {
                    return messageType == type;
                };
            }
            Initialize();
        }

        public MessageHookEntry(NetworkManager networkManager, ReceiptType type = ReceiptType.Handled)
        {
            m_NetworkManager = networkManager;
            m_ReceiptType = type;
        }
    }
}
