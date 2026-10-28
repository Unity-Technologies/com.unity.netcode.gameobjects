using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    internal static partial class NgoCliQueries
    {
        /// <summary>
        /// Samples the manager's network traffic for <paramref name="frames"/> frames: the bytes and messages it sends
        /// and receives, per message type, read from its message manager as Netcode for GameObjects' own metrics
        /// are. Adds the manager's clients with their round trip times. Doesn't block the main thread.
        /// </summary>
        internal static Task<StatsResult> SampleStatsAsync(string managerFilter, int frames, float timeoutSeconds)
        {
            if (frames < 1 || frames > 10_000)
            {
                throw new ArgumentException("frames must be between 1 and 10000.");
            }
            ValidateTimeout(timeoutSeconds);
            var manager = ResolveRunningManager(managerFilter);
            if (EditorApplication.isPaused)
            {
                throw new InvalidOperationException("Play mode is paused, so no frames run. Unpause (editor.playmode pause) and retry.");
            }

            var messageManager = manager.MessageManager;
            var traffic = new TrafficHooks();
            messageManager.Hook(traffic);
            var completion = new TaskCompletionSource<StatsResult>();
            var startFrame = Time.frameCount;
            var startTime = EditorApplication.timeSinceStartup;
            var deadline = startTime + timeoutSeconds;

            void Stop()
            {
                EditorApplication.update -= Tick;
                // After a shutdown the hooked message manager is already disposed, and its hooks with it.
                if (manager != null && manager.MessageManager == messageManager)
                {
                    messageManager.Unhook(traffic);
                }
            }

            void Tick()
            {
                if (!EditorApplication.isPlaying || manager == null)
                {
                    Stop();
                    completion.TrySetException(new InvalidOperationException("Play mode stopped while sampling."));
                    return;
                }
                // A shutdown disposes the message manager and a restart creates a new one, so the hooks stop counting.
                if (!manager.IsListening || manager.MessageManager != messageManager)
                {
                    Stop();
                    completion.TrySetException(new InvalidOperationException($"{manager.name}'s session ended while sampling, so the traffic counts are incomplete. Run get_stats again once it's running."));
                    return;
                }
                if (Time.frameCount - startFrame < frames && EditorApplication.timeSinceStartup < deadline)
                {
                    return;
                }
                Stop();
                try
                {
                    completion.TrySetResult(BuildStats(manager, traffic, Time.frameCount - startFrame, EditorApplication.timeSinceStartup - startTime));
                }
                catch (Exception e)
                {
                    completion.TrySetException(e);
                }
            }

            EditorApplication.update += Tick;
            return completion.Task;
        }

        private static StatsResult BuildStats(NetworkManager manager, TrafficHooks traffic, int frames, double seconds)
        {
            var result = new StatsResult
            {
                Manager = manager.name,
                Role = GetRole(manager),
                Frames = frames,
                Seconds = Math.Round(seconds, 3),
                BytesSent = traffic.BytesSent,
                BytesReceived = traffic.BytesReceived,
                BytesSentPerSecond = seconds > 0 ? Math.Round(traffic.BytesSent / seconds, 1) : 0,
                BytesReceivedPerSecond = seconds > 0 ? Math.Round(traffic.BytesReceived / seconds, 1) : 0,
                MessagesSent = traffic.Messages.Values.Sum(m => m.Sent),
                MessagesReceived = traffic.Messages.Values.Sum(m => m.Received),
                Messages = traffic.Messages.Values
                    .OrderByDescending(m => m.BytesSent + m.BytesReceived)
                    .ThenBy(m => m.Message, StringComparer.Ordinal)
                    .ToList(),
                SpawnedObjectCount = manager.SpawnManager?.SpawnedObjects.Count,
            };

            if (manager.IsListening)
            {
                result.Clients = ListClients(manager).Clients;
            }
            if (result.MessagesSent == 0 && result.MessagesReceived == 0)
            {
                result.Note = "No messages were sent or received during the sample. A host on its own sends nothing over the network; connect a client (another editor or a Multiplayer Play Mode player).";
            }
            if (IsHybridSession(manager))
            {
                var hybrid = HybridSessionNote(manager, "the sample counts Netcode for GameObjects messages only, not GhostObject replication");
                result.Note = result.Note == null ? hybrid : $"{result.Note} {hybrid}";
            }
            return result;
        }

        /// <summary>
        /// Counts what the message manager sends and receives, from the same hooks that feed Netcode for GameObjects'
        /// metrics: message sizes per type, and batch sizes for the bytes handed to and read from the transport.
        /// </summary>
        private sealed class TrafficHooks : INetworkHooks
        {
            public long BytesSent;
            public long BytesReceived;
            public readonly Dictionary<Type, MessageStat> Messages = new Dictionary<Type, MessageStat>();

            private MessageStat For(Type type)
            {
                if (!Messages.TryGetValue(type, out var stat))
                {
                    stat = new MessageStat { Message = type.Name };
                    Messages[type] = stat;
                }
                return stat;
            }

            public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
            {
                var stat = For(typeof(T));
                stat.Sent++;
                stat.BytesSent += messageSizeBytes;
            }

            public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
            {
                var stat = For(messageType);
                stat.Received++;
                stat.BytesReceived += messageSizeBytes;
            }

            public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery) => BytesSent += batchSizeInBytes;

            public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes) => BytesReceived += batchSizeInBytes;

            public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
            {
            }

            public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
            {
            }

            public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
            {
            }

            public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
            {
            }

            public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery) => true;

            public bool OnVerifyCanReceive(ulong senderId, Type messageType, FastBufferReader messageContent, ref NetworkContext context) => true;

            public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
            {
            }

            public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
            {
            }
        }
    }
}
