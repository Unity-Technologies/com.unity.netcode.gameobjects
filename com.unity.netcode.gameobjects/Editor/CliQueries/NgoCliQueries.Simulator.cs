using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEditor;
using UnityEngine;
#if MULTIPLAYER_TOOLS
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
#endif

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    /// <summary>
    /// The CLI network simulator: packet delay, jitter and loss applied to every UnityTransport driver in the
    /// editor through Unity Transport's simulator pipeline stage (compiled into the editor and development builds),
    /// the same mechanism Multiplayer Tools' Network Simulator uses. Settings last for the editor session and are
    /// applied to drivers created later, so they can be set before entering play mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class NgoCliSimulator
    {
        private const string k_Prefix = "Unity.Netcode.GameObjects.Cli.Simulator.";
        internal const string CustomPreset = "Custom";

        internal static bool Enabled
        {
            get => SessionState.GetBool(k_Prefix + "Enabled", false);
            set => SessionState.SetBool(k_Prefix + "Enabled", value);
        }

        internal static string Preset
        {
            get => SessionState.GetString(k_Prefix + "Preset", "None");
            set => SessionState.SetString(k_Prefix + "Preset", value);
        }

        internal static int DelayMs
        {
            get => SessionState.GetInt(k_Prefix + "DelayMs", 0);
            set => SessionState.SetInt(k_Prefix + "DelayMs", value);
        }

        internal static int JitterMs
        {
            get => SessionState.GetInt(k_Prefix + "JitterMs", 0);
            set => SessionState.SetInt(k_Prefix + "JitterMs", value);
        }

        internal static int DropPercent
        {
            get => SessionState.GetInt(k_Prefix + "DropPercent", 0);
            set => SessionState.SetInt(k_Prefix + "DropPercent", value);
        }

        /// <summary>True while every packet is dropped (timeout or lag spike).</summary>
        internal static bool Blackout { get; private set; }
        private static double s_LagSpikeEnd;

        internal static bool LagSpikeActive => s_LagSpikeEnd > 0;

        internal static int LagSpikeMsLeft => s_LagSpikeEnd > 0 ? Math.Max(0, (int)((s_LagSpikeEnd - EditorApplication.timeSinceStartup) * 1000)) : 0;

        static NgoCliSimulator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // UnityTransport clears its driver events when play mode starts, so subscribe once the runtime is up.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void SubscribeToDrivers()
        {
#if UNITY_6000_2_OR_NEWER
            UnityTransport.OnDriverInitialized -= OnDriverInitialized;
            UnityTransport.OnDriverInitialized += OnDriverInitialized;
#endif
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                SubscribeToDrivers();
                // Drivers created before the subscription (a NetworkManager started in Awake) still get the settings.
                ApplyToLiveTransports();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Blackout = false;
                s_LagSpikeEnd = 0;
                EditorApplication.update -= EndLagSpikeWhenDue;
            }
        }

#if UNITY_6000_2_OR_NEWER
        private static void OnDriverInitialized(EntityId transportId, NetworkDriver driver)
        {
            if (Enabled || Blackout)
            {
                Apply(driver);
            }
        }
#endif

        /// <summary>The UnityTransports in the open scenes whose driver is running.</summary>
        internal static List<UnityTransport> LiveTransports()
        {
            var transports = new List<UnityTransport>();
            if (!EditorApplication.isPlaying)
            {
                return transports;
            }
            foreach (var manager in NgoCliQueries.FindManagers())
            {
                if (manager.NetworkConfig?.NetworkTransport is UnityTransport utp && utp.GetNetworkDriver().IsCreated && !transports.Contains(utp))
                {
                    transports.Add(utp);
                }
            }
            return transports;
        }

        /// <summary>Pushes the current settings to every running driver and returns the transports updated.</summary>
        internal static List<string> ApplyToLiveTransports()
        {
            var applied = new List<string>();
            foreach (var utp in LiveTransports())
            {
                if (Apply(utp.GetNetworkDriver()))
                {
                    applied.Add(NgoCliQueries.GetHierarchyPath(utp.transform));
                }
            }
            return applied;
        }

        private static bool Apply(NetworkDriver driver)
        {
            if (!driver.IsCreated || !driver.CurrentSettings.TryGet<SimulatorUtility.Parameters>(out var parameters))
            {
                return false;
            }
            var on = Enabled;
            parameters.PacketDelayMs = on ? DelayMs : 0;
            parameters.PacketJitterMs = on ? JitterMs : 0;
            parameters.PacketDropPercentage = on ? DropPercent : 0;
            parameters.PacketDropInterval = 0;
            driver.ModifySimulatorStageParameters(parameters);

            var loss = Blackout ? 100f : 0f;
            driver.ModifyNetworkSimulatorParameters(new NetworkSimulatorParameter
            {
                ReceivePacketLossPercent = loss,
                SendPacketLossPercent = loss,
            });
            return true;
        }

        internal static void SetBlackout(bool on)
        {
            Blackout = on;
            if (!on)
            {
                s_LagSpikeEnd = 0;
                EditorApplication.update -= EndLagSpikeWhenDue;
            }
        }

        internal static void StartLagSpike(int durationMs)
        {
            Blackout = true;
            s_LagSpikeEnd = EditorApplication.timeSinceStartup + durationMs / 1000.0;
            EditorApplication.update -= EndLagSpikeWhenDue;
            EditorApplication.update += EndLagSpikeWhenDue;
        }

        private static void EndLagSpikeWhenDue()
        {
            if (EditorApplication.timeSinceStartup < s_LagSpikeEnd)
            {
                return;
            }
            EditorApplication.update -= EndLagSpikeWhenDue;
            s_LagSpikeEnd = 0;
            Blackout = false;
            ApplyToLiveTransports();
        }

        internal static SimulatorSettings Snapshot() => new SimulatorSettings
        {
            Enabled = Enabled,
            Preset = Preset,
            PacketDelayMs = DelayMs,
            PacketJitterMs = JitterMs,
            PacketDropPercent = DropPercent,
            TimeoutActive = Blackout && !LagSpikeActive,
            LagSpikeMsLeft = LagSpikeMsLeft,
        };
    }

    internal static partial class NgoCliQueries
    {
        internal static SimulatorStatusResult GetSimulator()
        {
            var result = new SimulatorStatusResult
            {
                Settings = NgoCliSimulator.Snapshot(),
                MultiplayerToolsInstalled = MultiplayerToolsInstalled,
                LiveTransports = NgoCliSimulator.LiveTransports().Select(t => GetHierarchyPath(t.transform)).ToList(),
                AppliesWhen = EditorApplication.isPlaying
                    ? "Now, to the live transports listed, and to transports started later in this editor session."
                    : "When a transport starts in play mode, for the rest of this editor session.",
            };
#if MULTIPLAYER_TOOLS
            foreach (var component in FindObjects.ByType<NetworkSimulator>(includeInactive: false))
            {
                result.NetworkSimulatorComponents.Add($"{component.gameObject.scene.name}/{GetHierarchyPath(component.transform)}");
            }
#endif
            return result;
        }

        /// <summary>
        /// Sets the simulated network conditions: a preset and/or packet delay, jitter and loss (values win over
        /// the preset; -1 leaves a value unchanged). Applies live to running transports and to transports started
        /// later in this editor session.
        /// </summary>
        internal static SimulatorResult SetSimulator(bool enable, string preset, int delayMs, int jitterMs, int dropPercent)
        {
            if (delayMs > 2000)
            {
                throw new ArgumentException("delay_ms must be at most 2000 (-1 leaves it unchanged).");
            }
            if (jitterMs > 200)
            {
                throw new ArgumentException("jitter_ms must be at most 200 (-1 leaves it unchanged).");
            }
            if (dropPercent > 100)
            {
                throw new ArgumentException("drop_percent must be at most 100 (-1 leaves it unchanged).");
            }

            SimulatorPresetInfo chosen = null;
            if (!string.IsNullOrWhiteSpace(preset))
            {
                var presets = ListSimulatorPresets().Presets;
                if (presets.Count == 0)
                {
                    throw new ArgumentException("Presets come from Multiplayer Tools, which isn't installed (package add com.unity.multiplayer.tools). Pass delay_ms, jitter_ms and drop_percent instead.");
                }
                chosen = MatchPreset(presets, preset.Trim()) ?? throw new ArgumentException($"Unknown preset '{preset}'. Presets: {string.Join(", ", presets.Select(p => p.Name))}.");
            }

            var result = new SimulatorResult { Previous = NgoCliSimulator.Snapshot() };
            NgoCliSimulator.Enabled = enable;
            if (chosen != null)
            {
                NgoCliSimulator.Preset = chosen.Name;
                NgoCliSimulator.DelayMs = chosen.PacketDelayMs;
                NgoCliSimulator.JitterMs = chosen.PacketJitterMs;
                NgoCliSimulator.DropPercent = chosen.PacketLossPercent;
                if (chosen.PacketLossInterval > 0)
                {
                    result.Notes.Add($"The preset also drops every {chosen.PacketLossInterval}th packet; the CLI simulator applies its loss percentage only.");
                }
            }
            if (delayMs >= 0 || jitterMs >= 0 || dropPercent >= 0)
            {
                if (delayMs >= 0)
                {
                    NgoCliSimulator.DelayMs = delayMs;
                }
                if (jitterMs >= 0)
                {
                    NgoCliSimulator.JitterMs = jitterMs;
                }
                if (dropPercent >= 0)
                {
                    NgoCliSimulator.DropPercent = dropPercent;
                }
                NgoCliSimulator.Preset = NgoCliSimulator.CustomPreset;
            }

            result.AppliedLiveTo = NgoCliSimulator.ApplyToLiveTransports();
            result.Current = NgoCliSimulator.Snapshot();
            result.AppliesWhen = result.AppliedLiveTo.Count > 0
                ? "Now, for the listed transports, and to transports started later in this editor session."
                : "When a transport starts in play mode, for the rest of this editor session.";
            if (EditorApplication.isPlaying && result.AppliedLiveTo.Count == 0 && NgoCliSimulator.LiveTransports().Count > 0)
            {
                result.Notes.Add("The running transports have no simulator stage (it's compiled into the editor and development builds only).");
            }
#if MULTIPLAYER_TOOLS
            if (FindObjects.ByType<NetworkSimulator>(includeInactive: false).Length > 0)
            {
                result.Notes.Add("A Multiplayer Tools Network Simulator component is in the scene; it reapplies its own preset when that preset changes.");
            }
#endif
            return result;
        }

        /// <summary>
        /// Matches a preset by its full name, its name without the bracketed details ("Mobile 4G" for
        /// "Mobile 4G [4G, LTE, '13]"), or a prefix that only one preset has.
        /// </summary>
        private static SimulatorPresetInfo MatchPreset(List<SimulatorPresetInfo> presets, string name)
        {
            static string ShortName(string full)
            {
                var bracket = full.IndexOf(" [", StringComparison.Ordinal);
                return bracket > 0 ? full.Substring(0, bracket) : full;
            }

            var exact = presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? presets.FirstOrDefault(p => string.Equals(ShortName(p.Name), name, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }
            var prefixed = presets.Where(p => p.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return prefixed.Count == 1 ? prefixed[0] : null;
        }

        internal static SimulatorPresetListResult ListSimulatorPresets()
        {
#if MULTIPLAYER_TOOLS
            var result = new SimulatorPresetListResult { Source = "com.unity.multiplayer.tools" };
            foreach (var preset in NetworkSimulatorPresets.Values)
            {
                result.Presets.Add(new SimulatorPresetInfo
                {
                    Name = preset.Name,
                    Description = preset.Description,
                    PacketDelayMs = preset.PacketDelayMs,
                    PacketJitterMs = preset.PacketJitterMs,
                    PacketLossPercent = preset.PacketLossPercent,
                    PacketLossInterval = preset.PacketLossInterval,
                });
            }
            return result;
#else
            return new SimulatorPresetListResult
            {
                Source = null,
                Note = "Presets come from Multiplayer Tools (package add com.unity.multiplayer.tools). Without it, set delay_ms, jitter_ms and drop_percent directly.",
            };
#endif
        }

        /// <summary>Drops every packet for <paramref name="durationMs"/>, then restores the simulator settings.</summary>
        internal static SimulatorResult StartLagSpike(int durationMs)
        {
            if (durationMs < 10 || durationMs > 120_000)
            {
                throw new ArgumentException("duration_ms must be between 10 and 120000.");
            }
            RequireLiveTransports("simulate a lag spike");
            var result = new SimulatorResult { Previous = NgoCliSimulator.Snapshot() };
            NgoCliSimulator.StartLagSpike(durationMs);
            result.AppliedLiveTo = NgoCliSimulator.ApplyToLiveTransports();
            result.Current = NgoCliSimulator.Snapshot();
            result.AppliesWhen = $"Now, for {durationMs} ms. Connections can time out if the spike outlasts the transport's disconnect timeout.";
            return result;
        }

        /// <summary>Drops every packet until turned off, so connections time out.</summary>
        internal static SimulatorResult SetTimeout(bool enable)
        {
            if (enable)
            {
                RequireLiveTransports("simulate a timeout");
            }
            var result = new SimulatorResult { Previous = NgoCliSimulator.Snapshot() };
            NgoCliSimulator.SetBlackout(enable);
            result.AppliedLiveTo = NgoCliSimulator.ApplyToLiveTransports();
            result.Current = NgoCliSimulator.Snapshot();
            result.AppliesWhen = enable
                ? "Now. Every packet is dropped until netcode.gameobjects.simulator timeout false; connections time out after the transport's disconnect timeout."
                : "Now. Packets flow again, subject to the simulator settings.";
            return result;
        }

        private static void RequireLiveTransports(string action)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException($"Can only {action} in play mode, with a session running. Enter play mode (editor.playmode start) and start one (netcode.gameobjects.session start).");
            }
            if (NgoCliSimulator.LiveTransports().Count == 0)
            {
                throw new InvalidOperationException($"No running UnityTransport to {action} on. Start a session with netcode.gameobjects.session start.");
            }
        }

        internal static bool MultiplayerToolsInstalled
        {
            get
            {
#if MULTIPLAYER_TOOLS
                return true;
#else
                return false;
#endif
            }
        }
    }
}
