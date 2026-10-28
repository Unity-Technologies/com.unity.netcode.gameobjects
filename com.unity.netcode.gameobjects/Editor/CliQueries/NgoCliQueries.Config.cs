using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Unity.Netcode.GameObjects.Editor.Configuration;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    internal static partial class NgoCliQueries
    {
        // Transport and manager settings that config set accepts besides NetworkConfig's own fields.
        private const string k_Address = "address";
        private const string k_Port = "port";
        private const string k_ListenAddress = "listen_address";
        private const string k_PlayerPrefab = "player_prefab";
        private const string k_LogLevel = "log_level";

        // --- Get ---

        internal static ConfigResult GetConfig(string managerFilter)
        {
            var manager = ResolveManager(managerFilter);
            var config = manager.NetworkConfig ?? new NetworkConfig();
            var result = new ConfigResult
            {
                Manager = GetHierarchyPath(manager.transform),
                Scene = manager.gameObject.scene.name,
                IsPlaying = EditorApplication.isPlaying,
                Transport = DescribeTransport(config.NetworkTransport),
                LogLevel = manager.LogLevel.ToString(),
                PlayerPrefab = config.PlayerPrefab != null ? AssetPathOrName(config.PlayerPrefab) : null,
                PrefabsLists = config.Prefabs?.NetworkPrefabsLists.Where(l => l != null).Select(AssetDatabase.GetAssetPath).ToList() ?? new List<string>(),
                GenerateDefaultNetworkPrefabs = NetcodeForGameObjectsProjectSettings.instance.GenerateDefaultNetworkPrefabs,
                DefaultNetworkPrefabsPath = NetworkPrefabProcessor.DefaultNetworkPrefabsPath,
            };
            foreach (var field in SettableConfigFields())
            {
                var value = field.GetValue(config);
                result.NetworkConfig[ToSnakeCase(field.Name)] = value is Enum ? value.ToString() : value;
            }
            result.SettableFields = SettableConfigFields().Select(f => ToSnakeCase(f.Name))
                .Concat(new[] { k_Address, k_Port, k_ListenAddress, k_PlayerPrefab, k_LogLevel })
                .ToList();
            return result;
        }

        // --- Set ---

        /// <summary>
        /// Sets one NetworkManager setting: a NetworkConfig field (snake_case name), the UnityTransport address,
        /// port or listen address, the player prefab, or the log level. In edit mode the change is undoable and
        /// the scene is marked dirty. In play mode it applies to the live NetworkManager, which must be stopped,
        /// and is lost when play mode ends.
        /// </summary>
        internal static ConfigSetResult SetConfig(string field, string value, string managerFilter, bool dryRun)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                throw new ArgumentException("Pass the field to set. List them with netcode.gameobjects.config get (settableFields).");
            }
            var manager = ResolveManager(managerFilter);
            var name = field.Trim().ToLowerInvariant();
            var playing = EditorApplication.isPlaying;
            if (playing && (manager.IsListening || manager.ShutdownInProgress) && name != k_LogLevel)
            {
                throw new InvalidOperationException($"{manager.name} is running; its configuration is read when it starts. Stop it (netcode.gameobjects.session stop), set the field, then start it again.");
            }

            if (manager.NetworkConfig == null)
            {
                throw new InvalidOperationException($"{manager.name} has no NetworkConfig. Add a NetworkManager with netcode.gameobjects create_network_manager.");
            }
            var result = new ConfigSetResult
            {
                DryRun = dryRun,
                Manager = GetHierarchyPath(manager.transform),
                Field = name,
                AppliesWhen = playing
                    ? "The next time this NetworkManager starts in this play session. Not saved: exit play mode and set it again to keep it."
                    : "The next time the NetworkManager starts. The scene is modified but not saved; save it with scene save.",
            };

            UnityEngine.Object target;
            Action apply;
            switch (name)
            {
                case k_Address:
                case k_Port:
                case k_ListenAddress:
                    {
                        if (manager.NetworkConfig.NetworkTransport is not UnityTransport utp)
                        {
                            throw new ArgumentException($"{name} is a UnityTransport setting, and {manager.name} uses {manager.NetworkConfig.NetworkTransport?.GetType().Name ?? "no transport"}.");
                        }
                        RequireDirectConnection(utp, name);
                        if (IsHybridSession(manager))
                        {
                            result.Notes.Add(HybridSessionNote(manager, $"{name} is saved on the UnityTransport but doesn't apply to it"));
                        }
                        var data = utp.ConnectionData;
                        target = utp;
                        if (name == k_Port)
                        {
                            var port = (ushort)ParseValue(typeof(ushort), value, name);
                            if (port == 0)
                            {
                                throw new ArgumentException("port must be between 1 and 65535.");
                            }
                            result.Previous = (int)data.Port;
                            result.Value = (int)port;
                            apply = () => SetConnectionAddress(utp, data.Address, port, data.ServerListenAddress);
                        }
                        else if (name == k_Address)
                        {
                            var address = RequireText(value, name);
                            result.Previous = data.Address;
                            result.Value = address;
                            var listen = ListenAddressFor(data.Address, data.ServerListenAddress, address);
                            if (listen != (data.ServerListenAddress ?? string.Empty))
                            {
                                result.Notes.Add(DescribeListenAddress(listen));
                            }
                            apply = () => SetConnectionAddress(utp, address, data.Port, listen);
                        }
                        else
                        {
                            var listen = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                            result.Previous = data.ServerListenAddress;
                            result.Value = listen;
                            apply = () => SetConnectionAddress(utp, data.Address, data.Port, listen);
                        }
                        break;
                    }
                case k_PlayerPrefab:
                    {
                        target = manager;
                        var prefab = string.IsNullOrWhiteSpace(value) || value.Trim() == "none" ? null : ResolvePrefab(value);
                        if (prefab != null && prefab.GetComponent<NetworkObject>() == null)
                        {
                            throw new ArgumentException($"{AssetDatabase.GetAssetPath(prefab)} has no NetworkObject on its root. Add one with netcode.gameobjects make_prefab.");
                        }
                        result.Previous = manager.NetworkConfig.PlayerPrefab != null ? AssetPathOrName(manager.NetworkConfig.PlayerPrefab) : null;
                        result.Value = prefab != null ? AssetDatabase.GetAssetPath(prefab) : null;
                        apply = () => manager.NetworkConfig.PlayerPrefab = prefab;
                        break;
                    }
                case k_LogLevel:
                    {
                        var level = (LogLevel)ParseValue(typeof(LogLevel), value, name);
                        target = manager;
                        result.Previous = manager.LogLevel.ToString();
                        result.Value = level.ToString();
                        apply = () => manager.LogLevel = level;
                        if (playing)
                        {
                            result.AppliesWhen = "Now. Not saved: exit play mode and set it again to keep it.";
                        }
                        break;
                    }
                default:
                    {
                        var configField = SettableConfigFields().FirstOrDefault(f => ToSnakeCase(f.Name) == name) ?? throw new ArgumentException($"Unknown field '{field}'. Settable: {string.Join(", ", GetConfig(managerFilter).SettableFields)}.");
                        var parsed = ParseValue(configField.FieldType, value, name);
                        ValidateConfigValue(configField.Name, name, parsed, result.Notes);
                        target = manager;
                        var previous = configField.GetValue(manager.NetworkConfig);
                        result.Previous = previous is Enum ? previous.ToString() : previous;
                        result.Value = parsed is Enum ? parsed.ToString() : parsed;
                        apply = () => configField.SetValue(manager.NetworkConfig, parsed);
                        break;
                    }
            }

            if (dryRun)
            {
                return result;
            }
            if (!playing)
            {
                Undo.IncrementCurrentGroup();
                Undo.RecordObject(target, $"Set NetworkManager {name}");
            }
            apply();
            if (!playing)
            {
                // A change made directly to a prefab instance is only kept once recorded as an override.
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                }
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
            return result;
        }

        private static void ValidateConfigValue(string fieldName, string name, object value, List<string> notes)
        {
            switch (fieldName)
            {
                case nameof(NetworkConfig.TickRate) when (uint)value == 0 || (uint)value > 1000:
                    throw new ArgumentException("tick_rate must be between 1 and 1000.");
                case nameof(NetworkConfig.TickRate) when (uint)value > 120:
                    notes.Add("Tick rates above 120 cost a lot of bandwidth and CPU; 30 to 60 is typical.");
                    break;
                case nameof(NetworkConfig.NetworkTopology) when (NetworkTopologyTypes)value == NetworkTopologyTypes.DistributedAuthority:
                    notes.Add("Distributed authority needs a session service (use_cmb_service, e.g. through the Multiplayer Services SDK) or a distributed authority host.");
                    break;
                case nameof(NetworkConfig.SpawnTimeout) when (float)value < NetworkConfig.MinSpawnTimeout || (float)value > NetworkConfig.MaxSpawnTimeout:
                    throw new ArgumentException($"{name} must be between {NetworkConfig.MinSpawnTimeout} and {NetworkConfig.MaxSpawnTimeout} seconds.");
                case nameof(NetworkConfig.ClientConnectionBufferTimeout) or nameof(NetworkConfig.LoadSceneTimeOut) or nameof(NetworkConfig.TimeResyncInterval) when (int)value < 1:
                    throw new ArgumentException($"{name} must be at least 1.");
            }
        }

        // --- Validate ---

        /// <summary>
        /// Checks NetworkManager setups for problems that otherwise only surface when a session starts.
        /// </summary>
        internal static ValidationResult ValidateConfig(string managerFilter)
        {
            var result = new ValidationResult();
            void Add(string severity, string code, string asset, string message)
            {
                if (severity == "error")
                {
                    result.Errors++;
                }
                else
                {
                    result.Warnings++;
                }
                result.Issues.Add(new AuditIssue { Severity = severity, Code = code, Asset = asset, Message = message });
            }

            var all = FindManagers();
            var managers = string.IsNullOrWhiteSpace(managerFilter) ? all : new List<NetworkManager> { ResolveManager(managerFilter, all) };
            if (managers.Count == 0)
            {
                throw new InvalidOperationException(NoManagerMessage);
            }
            if (all.Count > 1)
            {
                Add("warning", "SEVERAL_NETWORKMANAGERS", null, $"{all.Count} NetworkManagers in the open scenes. Only one becomes NetworkManager.Singleton; keep one per session.");
            }

            foreach (var manager in managers)
            {
                ValidateManager(manager, Add);
            }
            result.ManagersChecked = managers.Count;
            return result;
        }

        private static void ValidateManager(NetworkManager manager, Action<string, string, string, string> add)
        {
            var where = $"{SceneLabel(manager.gameObject.scene)}/{GetHierarchyPath(manager.transform)}";
            var config = manager.NetworkConfig;
            if (config == null)
            {
                add("error", "NO_NETWORK_CONFIG", where, "The NetworkManager has no NetworkConfig. Reset the component, or recreate it with netcode.gameobjects create_network_manager.");
                return;
            }

            if (manager.transform.parent != null)
            {
                add("error", "NETWORKMANAGER_NOT_ROOT", where, "The NetworkManager isn't a root GameObject. It moves to DontDestroyOnLoad when it starts, which a nested GameObject can't do.");
            }
            if (manager.GetComponentInChildren<NetworkObject>(true) != null || manager.GetComponentInParent<NetworkObject>(true) != null)
            {
                add("error", "NETWORKMANAGER_WITH_NETWORKOBJECT", where, "A NetworkObject shares the NetworkManager's hierarchy. Keep the NetworkManager on its own GameObject.");
            }

            if (config.NetworkTransport == null)
            {
                add("error", "NO_TRANSPORT", where, "No transport selected. Add a UnityTransport and select it (netcode.gameobjects create_network_manager sets one up).");
            }
            if (config.NetworkTopology == NetworkTopologyTypes.DistributedAuthority)
            {
                if (config.NetworkTransport != null && config.NetworkTransport is not UnityTransport)
                {
                    add("error", "DA_NEEDS_UNITYTRANSPORT", where, "Distributed authority only supports UnityTransport.");
                }
                if (!config.UseCMBService)
                {
                    add("warning", "DA_WITHOUT_SERVICE", where, "Distributed authority without use_cmb_service needs a distributed authority host; sessions usually go through the Multiplayer Services SDK.");
                }
            }

            // The dialog opens on entering play mode; in play mode the manager has moved to DontDestroyOnLoad.
            var scene = manager.gameObject.scene;
            var checkScene = config.EnableSceneManagement && !EditorApplication.isPlaying;
            if (checkScene && !string.IsNullOrEmpty(scene.path) && !EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path))
            {
                add("warning", "SCENE_NOT_IN_BUILD", where, $"Scene management is on and {scene.path} isn't in the build's scene list, so clients can't load it. Entering play mode opens a dialog asking to add it; add it first with scene add_to_build, or turn scene management off (netcode.gameobjects.config set enable_scene_management false).");
            }
            else if (checkScene && string.IsNullOrEmpty(scene.path))
            {
                add("warning", "SCENE_NOT_SAVED", where, "Scene management is on and the scene isn't saved, so it can't be in the build's scene list; entering play mode opens a dialog. Save it (scene save) and add it to the build (scene add_to_build).");
            }

            if (config.TickRate == 0)
            {
                add("error", "TICK_RATE_ZERO", where, "tick_rate is 0. Set it with netcode.gameobjects.config set tick_rate 30.");
            }

            var lists = config.Prefabs?.NetworkPrefabsLists.Where(l => l != null).ToList() ?? new List<NetworkPrefabsList>();
            if (config.Prefabs != null && config.Prefabs.NetworkPrefabsLists.Any(l => l == null))
            {
                add("error", "MISSING_PREFABS_LIST", where, "A Network Prefabs Lists entry is empty or points to a deleted asset.");
            }
            if (lists.Count == 0 && config.PlayerPrefab == null)
            {
                add("warning", "NO_NETWORK_PREFABS", where, "No network prefabs list and no player prefab: nothing can be spawned. Register prefabs with netcode.gameobjects register_prefab.");
            }
            if (config.PlayerPrefab != null && config.PlayerPrefab.GetComponent<NetworkObject>() == null)
            {
                add("error", "PLAYER_PREFAB_WITHOUT_NETWORKOBJECT", where, $"The player prefab {AssetPathOrName(config.PlayerPrefab)} has no NetworkObject on its root.");
            }

            var hybrid = ConfiguredHybridPrefabs(config);
            if (hybrid.Count > 0)
            {
                ValidateHybrid(config, hybrid, where, add);
            }
        }

        /// <summary>
        /// The hybrid (GhostObject) prefabs a config registers, through its lists or as the player prefab. NGO rejects
        /// overrides of hybrid prefabs (audit reports them), so only plain list entries can make a session hybrid.
        /// </summary>
        internal static List<UnityEngine.GameObject> ConfiguredHybridPrefabs(NetworkConfig config)
        {
            var hybrid = new List<UnityEngine.GameObject>();
            void Add(UnityEngine.GameObject prefab)
            {
                if (prefab != null && !hybrid.Contains(prefab) && IsHybridPrefab(prefab))
                {
                    hybrid.Add(prefab);
                }
            }

            if (config.Prefabs != null)
            {
                foreach (var list in config.Prefabs.NetworkPrefabsLists)
                {
                    if (list == null)
                    {
                        continue;
                    }
                    foreach (var entry in list.PrefabList)
                    {
                        if (entry != null && entry.Override == NetworkPrefabOverride.None)
                        {
                            Add(entry.Prefab);
                        }
                    }
                }
            }
            Add(config.PlayerPrefab);
            return hybrid;
        }

        private static void ValidateHybrid(NetworkConfig config, List<UnityEngine.GameObject> hybrid, string where, Action<string, string, string, string> add)
        {
#if UNIFIED_NETCODE
            var names = string.Join(", ", hybrid.Take(5).Select(AssetPathOrName));
            if (config.NetworkTopology == NetworkTopologyTypes.DistributedAuthority)
            {
                add("error", "HYBRID_WITH_DISTRIBUTED_AUTHORITY", where, $"{NetworkPrefabs.DistributedAuthorityHybridPrefabError} Hybrid prefabs: {names}.");
            }

            var netcodeConfig = FindNetcodeConfig();
            if (netcodeConfig == null)
            {
                add("error", "HYBRID_WITHOUT_NETCODECONFIG", where, $"Hybrid prefabs ({names}) run on Netcode for Entities and need a NetcodeConfig set to a single world. Create one in Project Settings > Multiplayer.");
            }
            else if (HybridNetcodeDefaults.IsMissingRequired(netcodeConfig, out var reason))
            {
                add("error", "HYBRID_NETCODECONFIG", AssetDatabase.GetAssetPath(netcodeConfig), $"The NetcodeConfig can't be used with hybrid prefabs: {reason}.");
            }
#endif
        }

#if UNIFIED_NETCODE
        private static NetcodeConfig FindNetcodeConfig()
        {
            if (NetcodeConfig.Global != null)
            {
                return NetcodeConfig.Global;
            }
            var configs = AssetDatabase.FindAssets($"t:{nameof(NetcodeConfig)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NetcodeConfig>)
                .Where(c => c != null)
                .ToList();
            // A project has one NetcodeConfig (Netcode for Entities reports several as an error).
            return configs.FirstOrDefault();
        }
#endif

        // --- Helpers ---

        private static IEnumerable<FieldInfo> SettableConfigFields()
        {
            return typeof(NetworkConfig).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => !f.IsInitOnly && (f.FieldType.IsPrimitive || f.FieldType.IsEnum))
                .OrderBy(f => f.MetadataToken);
        }

        private static object ParseValue(Type type, string value, string name)
        {
            var text = RequireText(value, name);
            try
            {
                if (type.IsEnum)
                {
                    var match = Enum.GetNames(type).FirstOrDefault(n => string.Equals(n, text, StringComparison.OrdinalIgnoreCase)) ?? throw new FormatException();
                    return Enum.Parse(type, match);
                }
                if (type == typeof(bool))
                {
                    return text.ToLowerInvariant() switch
                    {
                        "true" or "1" or "on" or "yes" => true,
                        "false" or "0" or "off" or "no" => false,
                        _ => throw new FormatException(),
                    };
                }
                var parsed = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
                if (parsed is float f && !float.IsFinite(f) || parsed is double d && !double.IsFinite(d))
                {
                    throw new FormatException();
                }
                return parsed;
            }
            catch (Exception e) when (e is FormatException or OverflowException or InvalidCastException)
            {
                var expected = type.IsEnum ? $"one of {string.Join(", ", Enum.GetNames(type))}" : type == typeof(bool) ? "true or false" : $"a {type.Name} number";
                throw new ArgumentException($"{name} must be {expected}, not '{value}'.");
            }
        }

        private static string RequireText(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"Pass a value for {name}.");
            }
            return value.Trim();
        }

        internal static string ToSnakeCase(string name)
        {
            var builder = new System.Text.StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    // Keep acronyms together: "UseCMBService" -> "use_cmb_service".
                    var startsWord = i > 0 && (!char.IsUpper(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1])));
                    if (startsWord)
                    {
                        builder.Append('_');
                    }
                    builder.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
