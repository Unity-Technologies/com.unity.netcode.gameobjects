using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using UnityEditor;
using UnityEngine;
#if MULTIPLAYER_TOOLS_NETWORK_SIMULATOR
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
    /// <remarks>
    /// No driver is touched, and no transport event subscribed to, until the CLI turns the simulator, a timeout or
    /// a lag spike on. Each driver's own simulator parameters (from the transport or a Network Simulator component)
    /// are saved before the first override and put back once all three are off. While the simulator is on, the
    /// console warns once per play session, when it first changes a driver.
    /// </remarks>
    [InitializeOnLoad]
    internal static class NgoCliSimulator
    {
        private const string k_Prefix = "Unity.Netcode.GameObjects.Cli.Simulator.";
        internal const string CustomPreset = "Custom";

        internal static bool Enabled
        {
            get => SessionState.GetBool(k_Prefix + "Enabled", false);
            set
            {
                SessionState.SetBool(k_Prefix + "Enabled", value);
                if (value)
                {
                    SubscribeWhilePlaying();
                }
            }
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

        /// <summary>True while every packet is dropped until the timeout is turned off.</summary>
        internal static bool TimeoutActive { get; private set; }
        private static double s_LagSpikeEnd;

        internal static bool LagSpikeActive => s_LagSpikeEnd > 0;

        internal static int LagSpikeMsLeft => s_LagSpikeEnd > 0 ? Math.Max(0, (int)((s_LagSpikeEnd - EditorApplication.timeSinceStartup) * 1000)) : 0;

        /// <summary>True while every packet is dropped (timeout or lag spike).</summary>
        internal static bool Blackout => TimeoutActive || LagSpikeActive;

        private static bool Active => Enabled || Blackout;

        /// <summary>A driver's own simulator parameters, saved before the CLI first overrides them.</summary>
        private struct Baseline
        {
            public SimulatorUtility.Parameters Stage;
            public NetworkSimulatorParameter Global;
        }

        // Keyed by the id UnityTransport passes to TransportInitialized (see TransportId).
        private static readonly Dictionary<int, Baseline> s_Baselines = new Dictionary<int, Baseline>();
        private static bool s_WarnedThisPlaySession;

        static NgoCliSimulator()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // UnityTransport clears its driver events when play mode starts, so subscribe once the runtime is up, and
        // only when the CLI simulator is on: in projects that never use it, no transport event is hooked.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void SubscribeToDrivers()
        {
            if (Enabled)
            {
                Subscribe();
            }
        }

        /// <summary>Hooks drivers started later in this play session, once the CLI turns something on.</summary>
        private static void SubscribeWhilePlaying()
        {
            if (EditorApplication.isPlaying)
            {
                Subscribe();
            }
        }

        private static void Subscribe()
        {
            UnityTransport.TransportInitialized -= OnTransportInitialized;
            UnityTransport.TransportInitialized += OnTransportInitialized;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && Active)
            {
                // Drivers created before the subscription (a NetworkManager started in Awake) still get the settings.
                ApplyToLiveTransports();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                TimeoutActive = false;
                s_LagSpikeEnd = 0;
                s_WarnedThisPlaySession = false;
                EditorApplication.update -= EndLagSpikeWhenDue;
                s_Baselines.Clear();
            }
        }

        private static void OnTransportInitialized(int transportId, NetworkDriver driver)
        {
            // A new driver starts from its transport's own settings.
            s_Baselines.Remove(transportId);
            Apply(transportId, driver);
        }

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

        /// <summary>
        /// The id UnityTransport passes to TransportInitialized and TransportDisposed for this transport. Mirrors
        /// UnityTransport's own computation; keep the two in step.
        /// </summary>
        private static int TransportId(UnityTransport utp)
        {
#if UNITY_6000_2_OR_NEWER
            return utp.GetEntityId().GetHashCode();
#else
            return utp.GetInstanceID();
#endif
        }

        /// <summary>Pushes the current settings to every running driver and returns the transports updated.</summary>
        internal static List<string> ApplyToLiveTransports(List<UnityTransport> live = null, bool clearBlackout = false)
        {
            var applied = new List<string>();
            foreach (var utp in live ?? LiveTransports())
            {
                if (Apply(TransportId(utp), utp.GetNetworkDriver(), clearBlackout))
                {
                    applied.Add(NgoCliQueries.GetHierarchyPath(utp.transform));
                }
            }
            return applied;
        }

        /// <summary>
        /// Overrides the driver's simulator parameters while the CLI simulator, a timeout or a lag spike is on,
        /// and restores the saved ones once none is. Returns false when the driver was left alone.
        /// </summary>
        /// <param name="clearBlackout">
        /// With nothing saved and nothing on, still lift a blackout (100% loss both ways) left on the driver, e.g.
        /// after a script recompile in play mode reset the saved state.
        /// </param>
        private static bool Apply(int transportId, NetworkDriver driver, bool clearBlackout = false)
        {
            if (!driver.IsCreated || !driver.CurrentSettings.TryGet<SimulatorUtility.Parameters>(out var stage))
            {
                return false;
            }
            if (!s_Baselines.TryGetValue(transportId, out var baseline))
            {
                if (!Active)
                {
                    return clearBlackout && ClearLeftoverBlackout(driver);
                }
                driver.CurrentSettings.TryGet<NetworkSimulatorParameter>(out var global);
                baseline = new Baseline { Stage = stage, Global = global };
                s_Baselines[transportId] = baseline;
            }

            stage = baseline.Stage;
            if (Enabled)
            {
                stage.PacketDelayMs = DelayMs;
                stage.PacketJitterMs = JitterMs;
                stage.PacketDropPercentage = DropPercent;
                stage.PacketDropInterval = 0;
                WarnOncePerPlaySession();
            }
            driver.ModifySimulatorStageParameters(stage);

            var parameters = baseline.Global;
            if (Blackout)
            {
                parameters.ReceivePacketLossPercent = 100f;
                parameters.SendPacketLossPercent = 100f;
            }
            driver.ModifyNetworkSimulatorParameters(parameters);
            if (!Active)
            {
                // Restored the driver's own settings.
                s_Baselines.Remove(transportId);
            }
            return true;
        }

        /// <summary>
        /// The simulator outlasts the play session that turned it on, so say so in the console, where someone who
        /// didn't run the command will look for the cause of the lag.
        /// </summary>
        private static void WarnOncePerPlaySession()
        {
            if (s_WarnedThisPlaySession)
            {
                return;
            }
            s_WarnedThisPlaySession = true;
            Debug.LogWarning($"[Netcode] The Unity CLI network simulator is on for this editor session: {DelayMs} ms delay, {JitterMs} ms jitter and {DropPercent}% packet loss on every UnityTransport. Turn it off with netcode.gameobjects.simulator set --enable false.");
        }

        private static bool ClearLeftoverBlackout(NetworkDriver driver)
        {
            if (!driver.CurrentSettings.TryGet<NetworkSimulatorParameter>(out var global)
                || global.ReceivePacketLossPercent < 100f || global.SendPacketLossPercent < 100f)
            {
                return false;
            }
            global.ReceivePacketLossPercent = 0f;
            global.SendPacketLossPercent = 0f;
            driver.ModifyNetworkSimulatorParameters(global);
            return true;
        }

        internal static void SetTimeout(bool on)
        {
            TimeoutActive = on;
            if (on)
            {
                SubscribeWhilePlaying();
            }
        }

        internal static void StartLagSpike(int durationMs)
        {
            SubscribeWhilePlaying();
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
            // A timeout that's on keeps dropping packets.
            ApplyToLiveTransports(clearBlackout: true);
        }

        internal static SimulatorSettings Snapshot() => new SimulatorSettings
        {
            Enabled = Enabled,
            Preset = Preset,
            PacketDelayMs = DelayMs,
            PacketJitterMs = JitterMs,
            PacketDropPercent = DropPercent,
            TimeoutActive = TimeoutActive,
            LagSpikeMsLeft = LagSpikeMsLeft,
        };
    }

    internal static partial class NgoCliQueries
    {
        private const string k_HybridSimulatorEffect = "the CLI simulator, timeout and lag_spike don't reach it";

        internal static SimulatorStatusResult GetSimulator()
        {
            var result = new SimulatorStatusResult
            {
                Settings = NgoCliSimulator.Snapshot(),
                MultiplayerToolsInstalled = MultiplayerToolsInstalled,
                NetworkSimulatorAvailable = NetworkSimulatorAvailable,
                LiveTransports = NgoCliSimulator.LiveTransports().Select(t => GetHierarchyPath(t.transform)).ToList(),
                AppliesWhen = EditorApplication.isPlaying
                    ? "Now, to the live transports listed, and to transports started later in this editor session."
                    : "When a transport starts in play mode, for the rest of this editor session.",
            };
            AddHybridSessionNotes(result.Notes, k_HybridSimulatorEffect);
#if MULTIPLAYER_TOOLS_NETWORK_SIMULATOR
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
                    throw new ArgumentException($"{PresetsUnavailableReason} Pass delay_ms, jitter_ms and drop_percent instead.");
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

            var live = NgoCliSimulator.LiveTransports();
            result.AppliedLiveTo = NgoCliSimulator.ApplyToLiveTransports(live);
            result.Current = NgoCliSimulator.Snapshot();
            result.AppliesWhen = result.AppliedLiveTo.Count > 0
                ? "Now, for the listed transports, and to transports started later in this editor session."
                : "When a transport starts in play mode, for the rest of this editor session.";
            if (enable && result.AppliedLiveTo.Count == 0 && live.Count > 0)
            {
                result.Notes.Add("The running transports have no simulator stage (it's compiled into the editor and development builds only).");
            }
            AddHybridSessionNotes(result.Notes, k_HybridSimulatorEffect);
#if MULTIPLAYER_TOOLS_NETWORK_SIMULATOR
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
#if MULTIPLAYER_TOOLS_NETWORK_SIMULATOR
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
                Note = $"{PresetsUnavailableReason} Without them, set delay_ms, jitter_ms and drop_percent directly.",
            };
#endif
        }

        /// <summary>
        /// Drops every packet for <paramref name="durationMs"/>, then restores the simulator settings. A timeout
        /// that's on keeps dropping packets after the spike.
        /// </summary>
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
            NgoCliSimulator.SetTimeout(enable);
            result.AppliedLiveTo = NgoCliSimulator.ApplyToLiveTransports(clearBlackout: !enable);
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
                foreach (var manager in FindManagers())
                {
                    if (manager.IsListening && IsHybridSession(manager))
                    {
                        throw new InvalidOperationException(HybridSessionNote(manager, $"the CLI simulator can't {action} on it"));
                    }
                }
                throw new InvalidOperationException($"No running UnityTransport to {action} on. Start a session with netcode.gameobjects.session start.");
            }
        }

        /// <summary>True when Multiplayer Tools has the Network Simulator and its presets (2.0 or later).</summary>
        internal static bool NetworkSimulatorAvailable
        {
            get
            {
#if MULTIPLAYER_TOOLS_NETWORK_SIMULATOR
                return true;
#else
                return false;
#endif
            }
        }

        private static string PresetsUnavailableReason => MultiplayerToolsInstalled
            ? "Presets come from the Network Simulator in Multiplayer Tools 2.0 or later; the installed version is older (upgrade com.unity.multiplayer.tools)."
            : "Presets come from Multiplayer Tools 2.0 or later, which isn't installed (package add com.unity.multiplayer.tools).";

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
