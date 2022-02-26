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
                return AllMessagesReceived;
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

    public class MessageHookEntry
    {
        internal MessageHooks MessageHooks;
        protected NetworkManager m_NetworkManager;
        private MessageHooks.MessageReceiptCheck m_MessageReceiptCheck;
        internal string MessageType;

        public void Initialize()
        {
            Assert.IsNotNull(m_MessageReceiptCheck, $"{nameof(m_MessageReceiptCheck)} is null, did you forget to initialize?");
            MessageHooks = new MessageHooks();
            MessageHooks.ReceiptCheck = m_MessageReceiptCheck;
            Assert.IsNotNull(m_NetworkManager.MessagingSystem, $"{nameof(NetworkManager.MessagingSystem)} is null! Did you forget to start first?");
            m_NetworkManager.MessagingSystem.Hook(MessageHooks);
        }

        internal void AssignMessageType<T>() where T : INetworkMessage
        {
            MessageType = typeof(T).Name;
            m_MessageReceiptCheck = MessageHooks.CheckForMessageOfType<T>;
            Initialize();
        }

        public MessageHookEntry(NetworkManager networkManager)
        {
            m_NetworkManager = networkManager;
        }
    }
}

