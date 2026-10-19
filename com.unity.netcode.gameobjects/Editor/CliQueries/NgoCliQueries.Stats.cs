using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    internal static partial class NgoCliQueries
    {
        /// <summary>
        /// Samples the profiler's Network counters for <paramref name="frames"/> frames: with Multiplayer Tools
        /// installed these are Netcode for GameObjects' message, RPC, NetworkVariable, spawn and byte counters.
        /// Adds the manager's clients with their round trip times. Doesn't block the main thread.
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

            var recorders = StartNetworkRecorders(frames);
            var completion = new TaskCompletionSource<StatsResult>();
            var startFrame = Time.frameCount;
            var startTime = EditorApplication.timeSinceStartup;
            var deadline = startTime + timeoutSeconds;

            void Finish()
            {
                EditorApplication.update -= Tick;
                try
                {
                    completion.TrySetResult(BuildStats(manager, recorders, Time.frameCount - startFrame, EditorApplication.timeSinceStartup - startTime));
                }
                catch (Exception e)
                {
                    completion.TrySetException(e);
                }
                finally
                {
                    foreach (var recorder in recorders.Values)
                    {
                        recorder.Dispose();
                    }
                }
            }

            void Tick()
            {
                if (!EditorApplication.isPlaying || manager == null)
                {
                    EditorApplication.update -= Tick;
                    foreach (var recorder in recorders.Values)
                    {
                        recorder.Dispose();
                    }
                    completion.TrySetException(new InvalidOperationException("Play mode stopped while sampling."));
                    return;
                }
                if (Time.frameCount - startFrame >= frames || EditorApplication.timeSinceStartup >= deadline)
                {
                    Finish();
                }
            }

            EditorApplication.update += Tick;
            return completion.Task;
        }

        private static Dictionary<string, ProfilerRecorder> StartNetworkRecorders(int capacity)
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            var recorders = new Dictionary<string, ProfilerRecorder>();
            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                // Counters only: the Network category also holds the editor connection's timing markers.
                if (description.Category != ProfilerCategory.Network || (description.Flags & MarkerFlags.Counter) == 0 || recorders.ContainsKey(description.Name))
                {
                    continue;
                }
                recorders[description.Name] = ProfilerRecorder.StartNew(ProfilerCategory.Network, description.Name, capacity);
            }
            return recorders;
        }

        private static StatsResult BuildStats(NetworkManager manager, Dictionary<string, ProfilerRecorder> recorders, int frames, double seconds)
        {
            var result = new StatsResult
            {
                Manager = manager.name,
                Role = GetRole(manager),
                Frames = frames,
                Seconds = Math.Round(seconds, 3),
                MultiplayerToolsInstalled = MultiplayerToolsInstalled,
                SpawnedObjectCount = manager.SpawnManager?.SpawnedObjects.Count,
            };

            foreach (var pair in recorders)
            {
                var recorder = pair.Value;
                if (!recorder.Valid)
                {
                    continue;
                }
                long total = 0;
                long max = 0;
                for (var i = 0; i < recorder.Count; i++)
                {
                    var value = recorder.GetSample(i).Value;
                    total += value;
                    max = Math.Max(max, value);
                }
                if (total == 0)
                {
                    result.IdleCounters.Add(pair.Key);
                    continue;
                }
                result.Counters.Add(new CounterStat
                {
                    Name = pair.Key,
                    Total = total,
                    MaxPerFrame = max,
                    PerSecond = seconds > 0 ? Math.Round(total / seconds, 1) : 0,
                });
            }
            result.IdleCounters.Sort(StringComparer.Ordinal);
            result.Counters = result.Counters.OrderByDescending(c => c.Total).ThenBy(c => c.Name, StringComparer.Ordinal).ToList();

            if (manager.IsListening)
            {
                result.Clients = ListClients(manager).Clients;
            }

            if (!MultiplayerToolsInstalled)
            {
                result.Note = "Netcode for GameObjects reports message and byte counters through Multiplayer Tools. Install it (package add com.unity.multiplayer.tools) for per-message-type traffic.";
            }
            else if (result.Counters.Count == 0)
            {
                result.Note = "No network traffic was recorded. A host on its own sends nothing; connect a client (a Multiplayer Play Mode player or another editor), and check that NetworkConfig network_message_metrics is on (netcode.gameobjects.config get).";
            }
            return result;
        }
    }
}
