using System.Collections.Generic;
using System.Linq;


namespace Unity.Netcode.TestHelpers.Runtime
{
    public class RpcMessageHooks : ConditionalPredicateBase
    {
        private MessageHooks m_ServerMessageHook;
        private List<MessageHooks> m_ClientMessageHooks = new List<MessageHooks>();

        public bool ServerMessageReceived { get; internal set; }
        public bool ClientMessagesReceived { get; internal set; }

        protected override bool OnHasConditionBeenReached()
        {
            ServerMessageReceived = !m_ServerMessageHook.IsWaiting;
            ClientMessagesReceived = m_ClientMessageHooks.Where((c) => !c.IsWaiting).Count() == m_ClientMessageHooks.Count;
            return ServerMessageReceived & ClientMessagesReceived;
        }

        protected override void OnFinished()
        {
            m_ClientMessageHooks.Clear();
            m_ServerMessageHook = null;
            base.OnFinished();
        }

        public RpcMessageHooks(NetworkManager server, NetworkManager[] clients)
        {
            m_ServerMessageHook = new MessageHooks();
            m_ServerMessageHook.ReceiptCheck = MessageHooks.CheckForMessageOfType<ServerRpcMessage>;
            server.MessagingSystem.Hook(m_ServerMessageHook);
            foreach (var client in clients)
            {
                var clientMessageHook = new MessageHooks();
                clientMessageHook.ReceiptCheck = MessageHooks.CheckForMessageOfType<ClientRpcMessage>;
                client.MessagingSystem.Hook(clientMessageHook);
            }
        }
    }
}

