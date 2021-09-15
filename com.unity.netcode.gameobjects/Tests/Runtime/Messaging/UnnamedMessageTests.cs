using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    public class UnnamedMessageTests : BaseMultiInstanceTest
    {
        protected override int NbClients => 2;

        private NetworkManager FirstClient => m_ClientNetworkManagers[0];
        private NetworkManager SecondClient => m_ClientNetworkManagers[1];

        internal struct HACK : INetworkMessage
        {
            public ulong NetworkObjectId;
            public ulong OwnerClientId;

            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValueSafe(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
            {
                var networkManager = (NetworkManager)context.SystemOwner;
                if (!networkManager.IsClient)
                {
                    return;
                }
//!!                reader.ReadValueSafe(out ChangeOwnershipMessage message);
//!!                message.Handle(context.SenderId, networkManager, reader.Length);
            }

            public void Handle(ulong senderId, NetworkManager networkManager, int messageSize)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjectId, out var networkObject))
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                    {
                        NetworkLog.LogWarning($"Trying to handle owner change but {nameof(NetworkObject)} #{NetworkObjectId} does not exist in {nameof(NetworkSpawnManager.SpawnedObjects)} anymore!");
                    }

                    return;
                }

                if (networkObject.OwnerClientId == networkManager.LocalClientId)
                {
                    //We are current owner.
                    networkObject.InvokeBehaviourOnLostOwnership();
                }

                networkObject.OwnerClientId = OwnerClientId;

                if (OwnerClientId == networkManager.LocalClientId)
                {
                    //We are new owner.
                    networkObject.InvokeBehaviourOnGainedOwnership();
                }

                networkManager.NetworkMetrics.TrackOwnershipChangeReceived(senderId, networkObject.NetworkObjectId, networkObject.name, messageSize);
            }
        }

        internal struct TestMessageOne : INetworkMessage
        {
            public int A;
            public int B;
            public int C;

            public void Serialize(ref FastBufferWriter writer)
            {
                writer.WriteValueSafe(this);
            }

            public static void Receive(ref FastBufferReader reader, NetworkContext context)
            {
                var networkManager = (NetworkManager)context.SystemOwner;
                if (!networkManager.IsClient)
                {
                    return;
                }
                reader.ReadValueSafe(out TestMessageOne message);
                message.Handle(context.SenderId, networkManager, reader.Length);
            }

            public void Handle(ulong senderId, NetworkManager networkManager, int messageSize)
            {
                Debug.Log("I'm here! with " + A + ", " + B + ", " + C);
            }
        }

        [UnityTest]
        public IEnumerator UnnamedIMESSAGEIsReceivedOnClientWithContent()
        {
//            var msg = new TestMessageOne { A = 1, B = 2, C = 3 };
            var msg = new HACK
            {
                NetworkObjectId = 123,
                OwnerClientId = 456
            };

            m_ServerNetworkManager.SendMessage(msg, NetworkDelivery.Unreliable, FirstClient.LocalClientId);

//            var messageContent = Guid.NewGuid();
//            var writer = new FastBufferWriter(1300, Allocator.Temp);
//            using (writer)
//            {
//                writer.WriteValueSafe(messageContent);
//                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
//                    FirstClient.LocalClientId,
//                    ref writer);
//            }
//
//            ulong receivedMessageSender = 0;
//            Guid receivedMessageContent;
//            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
//                (ulong sender, ref FastBufferReader reader) =>
//                {
//                    receivedMessageSender = sender;
//
//                    reader.ReadValueSafe(out receivedMessageContent);
//                };
//
            yield return new WaitForSeconds(0.2f);
//
//            Assert.AreEqual(messageContent, receivedMessageContent);
//            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnClientWithContent()
        {
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
                    FirstClient.LocalClientId,
                    ref writer);
            }

            ulong receivedMessageSender = 0;
            Guid receivedMessageContent;
            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, ref FastBufferReader reader) =>
                {
                    receivedMessageSender = sender;

                    reader.ReadValueSafe(out receivedMessageContent);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent, receivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, receivedMessageSender);
        }

        [UnityTest]
        public IEnumerator UnnamedMessageIsReceivedOnMultipleClientsWithContent()
        {
            var messageContent = Guid.NewGuid();
            var writer = new FastBufferWriter(1300, Allocator.Temp);
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                m_ServerNetworkManager.CustomMessagingManager.SendUnnamedMessage(
                    new List<ulong> { FirstClient.LocalClientId, SecondClient.LocalClientId },
                    ref writer);
            }

            ulong firstReceivedMessageSender = 0;
            Guid firstReceivedMessageContent;
            FirstClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, ref FastBufferReader reader) =>
                {
                    firstReceivedMessageSender = sender;

                    reader.ReadValueSafe(out firstReceivedMessageContent);
                };

            ulong secondReceivedMessageSender = 0;
            Guid secondReceivedMessageContent;
            SecondClient.CustomMessagingManager.OnUnnamedMessage +=
                (ulong sender, ref FastBufferReader reader) =>
                {
                    secondReceivedMessageSender = sender;

                    reader.ReadValueSafe(out secondReceivedMessageContent);
                };

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(messageContent, firstReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, firstReceivedMessageSender);

            Assert.AreEqual(messageContent, secondReceivedMessageContent);
            Assert.AreEqual(m_ServerNetworkManager.LocalClientId, secondReceivedMessageSender);
        }
    }
}
