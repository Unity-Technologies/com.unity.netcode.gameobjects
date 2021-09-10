using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.EditorTests
{
    public class InternalMessageHandlerProfilingDecoratorTests
    {
        private InternalMessageHandlerProfilingDecorator m_Decorator;

        [SetUp]
        public void Setup()
        {
            m_Decorator = new InternalMessageHandlerProfilingDecorator(new DummyMessageHandler(null));
        }

        [Test]
        public void HandleSceneEventCallsUnderlyingHandler()
        {
            m_Decorator.HandleSceneEvent(0, null);

            LogAssert.Expect(LogType.Log, nameof(m_Decorator.HandleSceneEvent));
        }

        [Test]
        public void HandleUnnamedMessageCallsUnderlyingHandler()
        {
            m_Decorator.HandleUnnamedMessage(0, null);

            LogAssert.Expect(LogType.Log, nameof(m_Decorator.HandleUnnamedMessage));
        }

        [Test]
        public void HandleNamedMessageCallsUnderlyingHandler()
        {
            m_Decorator.HandleNamedMessage(0, null);

            LogAssert.Expect(LogType.Log, nameof(m_Decorator.HandleNamedMessage));
        }

        [Test]
        public void MessageReceiveQueueItemCallsUnderlyingHandler()
        {
            m_Decorator.MessageReceiveQueueItem(0, null, 0.0f, MessageQueueContainer.MessageType.None);

            LogAssert.Expect(LogType.Log, nameof(m_Decorator.MessageReceiveQueueItem));
        }
    }
}
