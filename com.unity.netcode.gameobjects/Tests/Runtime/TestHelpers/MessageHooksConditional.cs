using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;


namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// A Netcode for GameObjects message conditional class to determine if a specific message has been sent or received.
    /// </summary>
    public class MessageHooksConditional : ConditionalPredicateBase
    {
        private List<MessageHookEntry> m_MessageHookEntries;
        /// <summary>
        /// When <see cref="true"/>, all messages have been received.
        /// </summary>
        public bool AllMessagesReceived { get; internal set; }

        /// <summary>
        /// Returns the number of messages received.
        /// </summary>
        public int NumberOfMessagesReceived
        {
            get
            {
                return m_MessageHookEntries.Where((c) => !c.MessageHooks.IsWaiting).Count();
            }
        }

        /// <summary>
        /// For debug logging purposes, this returns the <see cref="MessageHooks"/> still remaining as a string.
        /// </summary>
        /// <returns>A list of the remaining message hooks.</returns>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override void OnFinished()
        {
            base.OnFinished();
        }

        /// <summary>
        /// Resets the instance.
        /// </summary>
        public void Reset()
        {
            foreach (var entry in m_MessageHookEntries)
            {
                entry.Initialize();
            }
        }

        /// <summary>
        /// Constructor that takes a list of <see cref="MessageHookEntry"/> class instances.
        /// </summary>
        /// <param name="messageHookEntries">The list of <see cref="MessageHookEntry"/> class instances that defines the messages to wait for.</param>
        public MessageHooksConditional(List<MessageHookEntry> messageHookEntries)
        {
            m_MessageHookEntries = messageHookEntries;
        }
    }

    /// <summary>
    /// Enum used with <see cref="MessageHooksConditional"/> and integration testing.
    /// </summary>
    public enum ReceiptType
    {
        /// <summary>
        /// Denotes a message has been received.
        /// </summary>
        Received,
        /// <summary>
        /// Denotes a message has been handled.
        /// </summary>
        Handled
    }

    /// <summary>
    /// Class that defines the <see cref="MessageHooks"/> to use with <see cref="MessageHooksConditional"/>.
    /// </summary>
    public class MessageHookEntry
    {
        internal MessageHooks MessageHooks;
        /// <summary>
        /// The relative <see cref="NetworkManager"/> instance for the integration test.
        /// </summary>
        protected NetworkManager m_NetworkManager;
        private MessageHooks.MessageReceiptCheck m_MessageReceiptCheck;
        private MessageHooks.MessageHandleCheck m_MessageHandleCheck;
        internal string MessageType;
        private ReceiptType m_ReceiptType;

        /// <summary>
        /// Initializes the <see cref="MessageHookEntry"/>.
        /// </summary>
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

        /// <summary>
        /// Constructor that takes a <see cref="NetworkManager"/> and optional <see cref="ReceiptType"/> to check for.
        /// </summary>
        /// <param name="networkManager">The <see cref="NetworkManager"/> instance specific to this <see cref="MessageHookEntry"/> instance.</param>
        /// <param name="type">The <see cref="ReceiptType"/> to check for.</param>
        public MessageHookEntry(NetworkManager networkManager, ReceiptType type = ReceiptType.Handled)
        {
            m_NetworkManager = networkManager;
            m_ReceiptType = type;
        }
    }
}
