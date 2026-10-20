using System.Collections.Generic;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    // Result models for the Netcode for GameObjects CLI commands. Public fields in PascalCase; NgoCliOutput.ToWire
    // turns them into camelCase JSON objects. A null field means "not available", not "zero".

    internal class StatusResult
    {
        public bool IsPlaying;
        public List<ManagerStatus> Managers = new List<ManagerStatus>();
        public string Hint;
    }

    internal class ManagerStatus
    {
        public string Name;
        public string Path;
        public string Scene;
        public bool IsSingleton;
        public bool IsListening;
        /// <summary>host, server, client, or none when the manager isn't started.</summary>
        public string Role;
        public bool IsConnectedClient;
        public bool IsApproved;
        public long? LocalClientId;
        public string Topology;
        public bool DistributedAuthority;
        public long? CurrentSessionOwner;
        public bool IsSessionOwner;
        public int? ConnectedClientCount;
        public int? SpawnedObjectCount;
        public uint TickRate;
        public int? ServerTick;
        public int? LocalTick;
        public bool ShuttingDown;
        public string DisconnectReason;
        public string LogLevel;
        public TransportSummary Transport;
        public UnifiedNetcodeSummary UnifiedNetcode;
    }

    internal class TransportSummary
    {
        public string Type;
        public string Protocol;
        public string Address;
        public int? Port;
        public string ListenAddress;
    }

    internal class UnifiedNetcodeSummary
    {
        /// <summary>True when the package was compiled with Netcode for Entities 7.2.0 or newer (UNIFIED_NETCODE).</summary>
        public bool Available;
        /// <summary>True while this manager runs a hybrid prefab session on a Netcode for Entities world.</summary>
        public bool? HybridSession;
    }

    internal class ClientListResult
    {
        public string Manager;
        public string Role;
        public List<ClientInfo> Clients = new List<ClientInfo>();
        public string Note;
    }

    internal class ClientInfo
    {
        public long ClientId;
        public bool IsLocal;
        public bool IsServer;
        public bool IsSessionOwner;
        public long? PlayerObjectId;
        public int? OwnedObjectCount;
        public long? RttMs;
    }

    internal class ObjectListResult
    {
        public string Manager;
        public string Role;
        public int Total;
        public int Returned;
        public List<ObjectSummary> Objects = new List<ObjectSummary>();
    }

    internal class ObjectSummary
    {
        public long NetworkObjectId;
        public string Name;
        public long PrefabIdHash;
        public long OwnerClientId;
        public bool IsOwner;
        public bool HasAuthority;
        public bool IsPlayerObject;
        public bool InScenePlaced;
        public bool Hybrid;
        public float[] Position;
    }

    internal class ObjectDetail : ObjectSummary
    {
        public string Manager;
        public string Scene;
        public bool ActiveInHierarchy;
        public bool DestroyWithScene;
        public bool DontDestroyWithOwner;
        public OwnershipInfo Ownership;
        public List<long> Observers;
        public List<string> Behaviours = new List<string>();
    }

    internal class OwnershipInfo
    {
        public List<string> Status = new List<string>();
        public bool Transferable;
        public bool Distributable;
        public bool SessionOwnerOnly;
        public bool RequestRequired;
        public bool Locked;
        public bool RequestInProgress;
        /// <summary>Which peer may change ownership under the manager's topology.</summary>
        public string WhoCanChange;
    }

    internal class SpawnResult
    {
        public string Manager;
        public string Prefab;
        public ObjectDetail Object;
        public List<string> Notes = new List<string>();
    }

    internal class DespawnResult
    {
        public string Manager;
        public long NetworkObjectId;
        public string Name;
        public bool Despawned;
        public bool Destroyed;
    }

    internal class OwnershipChangeResult
    {
        public string Manager;
        public long NetworkObjectId;
        public string Name;
        public long PreviousOwner;
        public long Owner;
        public string Topology;
        public List<string> Notes = new List<string>();
    }

    internal class CreateNetworkManagerResult
    {
        public bool DryRun;
        public bool Created;
        public string Name;
        public string Scene;
        public string Transport;
        public string Topology;
        public string Address;
        public int Port;
        public List<string> PrefabsLists = new List<string>();
        public List<string> Notes = new List<string>();
    }

    internal class PrefabListResult
    {
        public bool GenerateDefaultNetworkPrefabs;
        public string DefaultNetworkPrefabsPath;
        public List<PrefabsListInfo> Lists = new List<PrefabsListInfo>();
        public List<ManagerPrefabs> Managers = new List<ManagerPrefabs>();
    }

    internal class PrefabsListInfo
    {
        public string Path;
        public bool IsDefault;
        public int Count;
        public List<PrefabEntry> Prefabs = new List<PrefabEntry>();
    }

    internal class PrefabEntry
    {
        public string Name;
        public string Path;
        public long PrefabIdHash;
        public string Override;
        public string OverrideSource;
        public string OverrideTarget;
        public bool Hybrid;
        public string Error;
    }

    internal class ManagerPrefabs
    {
        public string Manager;
        public string Scene;
        public string PlayerPrefab;
        public List<string> Lists = new List<string>();
    }

    internal class RegisterPrefabResult
    {
        public bool DryRun;
        public string Prefab;
        public string List;
        public bool AlreadyRegistered;
        public bool Registered;
        public List<string> Notes = new List<string>();
    }

    internal class MakePrefabResult
    {
        public bool DryRun;
        public string Prefab;
        public bool AlreadyNetworkObject;
        public bool AddedNetworkObject;
        public long? PrefabIdHash;
        public string RegisteredIn;
        public List<string> Notes = new List<string>();
    }

    internal class AuditResult
    {
        public int PrefabsScanned;
        public int Errors;
        public int Warnings;
        public List<AuditIssue> Issues = new List<AuditIssue>();
        public bool Truncated;
    }

    internal class ValidationResult
    {
        public int ManagersChecked;
        public int Errors;
        public int Warnings;
        public List<AuditIssue> Issues = new List<AuditIssue>();
    }

    internal class ProjectAuditResult
    {
        /// <summary>Set when NetworkManagers were checked.</summary>
        public int? ManagersChecked;
        /// <summary>Set when network prefabs were checked.</summary>
        public int? PrefabsScanned;
        public int Errors;
        public int Warnings;
        public bool Truncated;
        public List<AuditIssue> Issues = new List<AuditIssue>();
    }

    internal class AuditIssue
    {
        /// <summary>error or warning.</summary>
        public string Severity;
        public string Code;
        /// <summary>config (NetworkManagers) or prefabs.</summary>
        public string Area;
        public string Asset;
        public string Message;
    }

    internal class ConfigResult
    {
        public string Manager;
        public string Scene;
        public bool IsPlaying;
        public Dictionary<string, object> NetworkConfig = new Dictionary<string, object>();
        public TransportSummary Transport;
        public string LogLevel;
        public string PlayerPrefab;
        public List<string> PrefabsLists = new List<string>();
        public bool GenerateDefaultNetworkPrefabs;
        public string DefaultNetworkPrefabsPath;
        public List<string> SettableFields = new List<string>();
    }

    internal class ConfigSetResult
    {
        public bool DryRun;
        public string Manager;
        public string Field;
        public object Previous;
        public object Value;
        public string AppliesWhen;
        public List<string> Notes = new List<string>();
    }

    internal class SimulatorSettings
    {
        public bool Enabled;
        public string Preset;
        public int PacketDelayMs;
        public int PacketJitterMs;
        public int PacketDropPercent;
        public bool TimeoutActive;
        public int LagSpikeMsLeft;
    }

    internal class SimulatorResult
    {
        public SimulatorSettings Previous;
        public SimulatorSettings Current;
        public List<string> AppliedLiveTo = new List<string>();
        public string AppliesWhen;
        public List<string> Notes = new List<string>();
    }

    internal class SimulatorStatusResult
    {
        public SimulatorSettings Settings;
        public bool MultiplayerToolsInstalled;
        /// <summary>True when the installed Multiplayer Tools has the Network Simulator and its presets (2.0 or later).</summary>
        public bool NetworkSimulatorAvailable;
        public List<string> NetworkSimulatorComponents = new List<string>();
        public List<string> LiveTransports = new List<string>();
        public string AppliesWhen;
    }

    internal class SimulatorPresetInfo
    {
        public string Name;
        public string Description;
        public int PacketDelayMs;
        public int PacketJitterMs;
        public int PacketLossPercent;
        public int PacketLossInterval;
    }

    internal class SimulatorPresetListResult
    {
        public string Source;
        public List<SimulatorPresetInfo> Presets = new List<SimulatorPresetInfo>();
        public string Note;
    }

    internal class SessionResult
    {
        public string Action;
        public ManagerStatus Status;
        public List<string> Notes = new List<string>();
    }

    internal class WaitReadyResult
    {
        public bool Ready;
        public bool TimedOut;
        public double WaitedSeconds;
        public string Reason;
        public ManagerStatus Status;
    }

    internal class StatsResult
    {
        public string Manager;
        public string Role;
        public int Frames;
        public double Seconds;
        public bool MultiplayerToolsInstalled;
        /// <summary>Counters with traffic during the sample, largest first.</summary>
        public List<CounterStat> Counters = new List<CounterStat>();
        /// <summary>Names of the counters that stayed at zero.</summary>
        public List<string> IdleCounters = new List<string>();
        public List<ClientInfo> Clients = new List<ClientInfo>();
        public int? SpawnedObjectCount;
        public string Note;
    }

    internal class CounterStat
    {
        public string Name;
        public long Total;
        public double PerSecond;
        public long MaxPerFrame;
    }
}
