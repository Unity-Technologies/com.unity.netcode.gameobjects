using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode.GameObjects.Editor.Configuration;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Netcode.GameObjects.Editor.Cli
{
    internal static partial class NgoCliQueries
    {
        private static readonly Regex k_GuidPattern = new Regex("^[0-9a-fA-F]{32}$");

        // --- NetworkManager ---

        /// <summary>
        /// Adds a NetworkManager with a UnityTransport to the active scene, set up the way the inspector would:
        /// the default network prefabs list attached, the transport selected, the topology and address set.
        /// Undoable; the scene is marked dirty but not saved.
        /// </summary>
        internal static CreateNetworkManagerResult CreateNetworkManager(string name, NetworkTopologyTypes topology, string address, int port, bool dryRun)
        {
            RequireEditMode("create a NetworkManager");
            RequireDefined(topology, "topology");
            if (port < 1 || port > ushort.MaxValue)
            {
                throw new ArgumentException($"port must be between 1 and {ushort.MaxValue}.");
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("address can't be empty, e.g. 127.0.0.1.");
            }
            var existing = FindManagers();
            if (existing.Count > 0)
            {
                throw new InvalidOperationException($"The open scenes already have a NetworkManager ({string.Join(", ", existing.Select(m => $"{GetHierarchyPath(m.transform)} in {m.gameObject.scene.name}"))}). A session uses one; inspect it with netcode.gameobjects.config get.");
            }

            var scene = SceneManager.GetActiveScene();
            var result = new CreateNetworkManagerResult
            {
                DryRun = dryRun,
                Name = string.IsNullOrWhiteSpace(name) ? "NetworkManager" : name.Trim(),
                Scene = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path,
                Transport = nameof(UnityTransport),
                Topology = topology.ToString(),
                Address = address.Trim(),
                Port = port,
            };
            if (topology == NetworkTopologyTypes.DistributedAuthority)
            {
                result.Notes.Add("Distributed authority needs a session service (UseCMBService, e.g. through the Multiplayer Services SDK) or a distributed authority host.");
            }
            if (dryRun)
            {
                return result;
            }

            Undo.IncrementCurrentGroup();
            var gameObject = new GameObject(result.Name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create NetworkManager");
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            // Adding the component runs NetworkManager.Reset, which attaches the default network prefabs list
            // when the project generates one.
            var manager = Undo.AddComponent<NetworkManager>(gameObject);
            var transport = Undo.AddComponent<UnityTransport>(gameObject);
            manager.NetworkConfig ??= new NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.NetworkTopology = topology;
            var listenAddress = DefaultListenAddress(result.Address);
            SetConnectionAddress(transport, result.Address, (ushort)port, listenAddress);
            if (!string.IsNullOrEmpty(listenAddress))
            {
                result.Notes.Add($"A server listens on every interface ({listenAddress}) so clients can reach it at {result.Address}. Change it with netcode.gameobjects.config set listen_address.");
            }
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(transport);
            EditorSceneManager.MarkSceneDirty(scene);

            result.Created = true;
            result.PrefabsLists = manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Where(l => l != null).Select(AssetDatabase.GetAssetPath).ToList();
            if (result.PrefabsLists.Count == 0)
            {
                result.Notes.Add("No network prefabs list attached (default prefab generation is off). Register prefabs with netcode.gameobjects register_prefab.");
            }
            result.Notes.Add("The scene is modified but not saved. Save it with scene save.");
            return result;
        }

        // --- Prefabs ---

        internal static PrefabListResult ListPrefabs(string listFilter)
        {
            var settings = NetcodeForGameObjectsProjectSettings.instance;
            var result = new PrefabListResult
            {
                GenerateDefaultNetworkPrefabs = settings.GenerateDefaultNetworkPrefabs,
                DefaultNetworkPrefabsPath = NetworkPrefabProcessor.DefaultNetworkPrefabsPath,
            };

            foreach (var list in FindPrefabsLists())
            {
                var path = AssetDatabase.GetAssetPath(list);
                if (!string.IsNullOrWhiteSpace(listFilter) && path.IndexOf(listFilter.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                var info = new PrefabsListInfo { Path = path, IsDefault = list.IsDefault, Count = list.PrefabList.Count };
                foreach (var prefab in list.PrefabList)
                {
                    info.Prefabs.Add(DescribePrefabEntry(prefab));
                }
                result.Lists.Add(info);
            }

            foreach (var manager in FindManagers())
            {
                var config = manager.NetworkConfig;
                result.Managers.Add(new ManagerPrefabs
                {
                    Manager = GetHierarchyPath(manager.transform),
                    Scene = manager.gameObject.scene.name,
                    PlayerPrefab = config?.PlayerPrefab != null ? AssetPathOrName(config.PlayerPrefab) : null,
                    Lists = config?.Prefabs?.NetworkPrefabsLists.Where(l => l != null).Select(AssetDatabase.GetAssetPath).ToList() ?? new List<string>(),
                });
            }
            return result;
        }

        private static PrefabEntry DescribePrefabEntry(NetworkPrefab prefab)
        {
            var entry = new PrefabEntry { Override = prefab.Override.ToString() };
            var source = prefab.Override switch
            {
                NetworkPrefabOverride.Prefab => prefab.SourcePrefabToOverride,
                _ => prefab.Prefab,
            };
            if (source == null && prefab.Override != NetworkPrefabOverride.Hash)
            {
                entry.Error = "Missing prefab reference.";
                return entry;
            }

            if (source != null)
            {
                entry.Name = source.name;
                entry.Path = AssetDatabase.GetAssetPath(source);
                if (source.TryGetComponent<NetworkObject>(out var networkObject))
                {
                    entry.PrefabIdHash = networkObject.GlobalObjectIdHash;
                    entry.Hybrid = IsHybridPrefab(source);
                }
                else
                {
                    entry.Error = "The prefab has no NetworkObject on its root.";
                }
            }
            if (prefab.Override == NetworkPrefabOverride.Hash)
            {
                entry.OverrideSource = prefab.SourceHashToOverride.ToString();
            }
            if (prefab.Override != NetworkPrefabOverride.None)
            {
                entry.OverrideSource ??= entry.Path;
                entry.OverrideTarget = prefab.OverridingTargetPrefab != null ? AssetDatabase.GetAssetPath(prefab.OverridingTargetPrefab) : null;
            }
            return entry;
        }

        /// <summary>
        /// Adds a prefab to a NetworkPrefabsList: the named one, else the list of the scene's NetworkManager, else
        /// the project's default list. With default prefab generation on, NetworkObject prefabs are usually in the
        /// default list already, which is reported rather than treated as an error.
        /// </summary>
        internal static RegisterPrefabResult RegisterPrefab(string prefab, string listPath, bool dryRun)
        {
            RequireEditMode("register a network prefab");
            var prefabAsset = ResolvePrefab(prefab);
            var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
            if (prefabAsset.GetComponent<NetworkObject>() == null)
            {
                throw new ArgumentException($"{prefabPath} has no NetworkObject on its root. Add one with netcode.gameobjects make_prefab.");
            }

            var result = new RegisterPrefabResult { DryRun = dryRun, Prefab = prefabPath };
            var registeredIn = FindPrefabsLists().Where(l => l.Contains(prefabAsset)).Select(AssetDatabase.GetAssetPath).ToList();
            var list = ResolveTargetList(listPath, dryRun, result.Notes);
            result.List = list != null ? AssetDatabase.GetAssetPath(list) : NetworkPrefabProcessor.DefaultNetworkPrefabsPath;

            if (list != null && list.Contains(prefabAsset))
            {
                result.AlreadyRegistered = true;
                if (list.IsDefault && NetcodeForGameObjectsProjectSettings.instance.GenerateDefaultNetworkPrefabs)
                {
                    result.Notes.Add("Default prefab generation keeps NetworkObject prefabs in this list automatically.");
                }
                return result;
            }
            if (registeredIn.Count > 0)
            {
                result.Notes.Add($"Also registered in: {string.Join(", ", registeredIn)}.");
            }
            if (dryRun)
            {
                return result;
            }

            Undo.RecordObject(list, "Register network prefab");
            list.Add(new NetworkPrefab { Prefab = prefabAsset });
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssetIfDirty(list);
            result.Registered = true;
            return result;
        }

        private static NetworkPrefabsList ResolveTargetList(string listPath, bool dryRun, List<string> notes)
        {
            if (!string.IsNullOrWhiteSpace(listPath))
            {
                var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath.Trim()) ?? throw new ArgumentException($"No NetworkPrefabsList at '{listPath}'. List them with netcode.gameobjects list_prefabs.");
                return list;
            }

            var managerLists = FindManagers()
                .SelectMany(m => m.NetworkConfig?.Prefabs?.NetworkPrefabsLists ?? new List<NetworkPrefabsList>())
                .Where(l => l != null)
                .Distinct()
                .ToList();
            if (managerLists.Count == 1)
            {
                return managerLists[0];
            }
            if (managerLists.Count > 1)
            {
                throw new ArgumentException($"The scene's NetworkManagers use several lists. Pass list with one of: {string.Join(", ", managerLists.Select(AssetDatabase.GetAssetPath))}.");
            }

            var defaultPath = NetworkPrefabProcessor.DefaultNetworkPrefabsPath;
            var defaultList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(defaultPath);
            if (defaultList == null)
            {
                notes.Add($"{(dryRun ? "Would create" : "Created")} the default list at {defaultPath}. Attach it to your NetworkManager's Network Prefabs Lists (or let netcode.gameobjects create_network_manager do it).");
                if (dryRun)
                {
                    return null;
                }
                defaultList = NetworkPrefabProcessor.GetOrCreateNetworkPrefabs(defaultPath, out _, false);
            }
            return defaultList;
        }

        /// <summary>
        /// Makes a prefab a network prefab by adding a NetworkObject to its root, and reports the prefab id hash
        /// the import generates.
        /// </summary>
        internal static MakePrefabResult MakePrefab(string prefab, bool dryRun)
        {
            RequireEditMode("make a network prefab");
            var prefabAsset = ResolvePrefab(prefab);
            var path = AssetDatabase.GetAssetPath(prefabAsset);
            var result = new MakePrefabResult { DryRun = dryRun, Prefab = path };

            if (prefabAsset.TryGetComponent<NetworkObject>(out var existing))
            {
                result.AlreadyNetworkObject = true;
                result.PrefabIdHash = existing.GlobalObjectIdHash;
                result.RegisteredIn = FindPrefabsLists().Where(l => l.Contains(prefabAsset)).Select(AssetDatabase.GetAssetPath).FirstOrDefault();
                return result;
            }
            if (prefabAsset.GetComponentInChildren<NetworkManager>(true) != null)
            {
                throw new ArgumentException($"{path} contains a NetworkManager; a NetworkObject can't share a hierarchy with it.");
            }
            var nested = prefabAsset.GetComponentsInChildren<NetworkObject>(true);
            if (nested.Length > 0)
            {
                result.Notes.Add($"It already has {nested.Length} child NetworkObject(s). Each spawns separately; the new root NetworkObject won't spawn them.");
            }
            if (dryRun)
            {
                return result;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                scope.prefabContentsRoot.AddComponent<NetworkObject>();
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            result.AddedNetworkObject = true;
            if (reloaded.TryGetComponent<NetworkObject>(out var added) && added.GlobalObjectIdHash != 0)
            {
                result.PrefabIdHash = added.GlobalObjectIdHash;
            }
            else
            {
                result.Notes.Add("The prefab id hash is generated on the next import; check it with netcode.gameobjects list_prefabs.");
            }
            result.RegisteredIn = FindPrefabsLists().Where(l => l.Contains(reloaded)).Select(AssetDatabase.GetAssetPath).FirstOrDefault();
            if (result.RegisteredIn == null)
            {
                result.Notes.Add("Not in a network prefabs list yet. Register it with netcode.gameobjects register_prefab.");
            }
            if (FindManagers().Count > 0)
            {
                result.Notes.Add("Instances already placed in scenes get their NetworkObject from the prefab; save the scenes so they keep it.");
            }
            return result;
        }

        // --- Audit ---

        /// <summary>
        /// Runs the NetworkManager checks (<see cref="ValidateConfig"/>), the network prefab checks
        /// (<see cref="AuditPrefabs"/>), or both, and returns their issues as one list, NetworkManagers first.
        /// </summary>
        internal static ProjectAuditResult Audit(string scope, string folder, int limit, string managerFilter)
        {
            var normalized = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();
            if (normalized is not ("all" or "config" or "prefabs"))
            {
                throw new ArgumentException("scope must be all, config or prefabs.");
            }
            if (limit < 1)
            {
                throw new ArgumentException("limit must be at least 1.");
            }

            var result = new ProjectAuditResult();
            var issues = new List<AuditIssue>();
            if (normalized != "prefabs")
            {
                // A project check shouldn't fail because the scene with the NetworkManager isn't open.
                if (normalized == "all" && string.IsNullOrWhiteSpace(managerFilter) && FindManagers().Count == 0)
                {
                    result.ManagersChecked = 0;
                    result.Warnings++;
                    issues.Add(new AuditIssue { Severity = "warning", Code = "NO_NETWORKMANAGER", Area = "config", Message = NoManagerMessage });
                }
                else
                {
                    var config = ValidateConfig(managerFilter);
                    result.ManagersChecked = config.ManagersChecked;
                    result.Errors += config.Errors;
                    result.Warnings += config.Warnings;
                    foreach (var issue in config.Issues)
                    {
                        issue.Area = "config";
                        issues.Add(issue);
                    }
                }
            }
            if (normalized != "config")
            {
                var prefabs = AuditPrefabs(folder, limit);
                result.PrefabsScanned = prefabs.PrefabsScanned;
                result.Errors += prefabs.Errors;
                result.Warnings += prefabs.Warnings;
                result.Truncated = prefabs.Truncated;
                foreach (var issue in prefabs.Issues)
                {
                    issue.Area = "prefabs";
                    issues.Add(issue);
                }
            }
            if (issues.Count > limit)
            {
                result.Truncated = true;
            }
            result.Issues = issues.Take(limit).ToList();
            return result;
        }

        /// <summary>
        /// Checks network prefabs for setup problems the editor otherwise reports one at a time, in the inspector or
        /// when a session starts: behaviours without a NetworkObject, nested or misplaced NetworkObjects,
        /// NetworkManagers inside network prefabs, duplicate or missing prefab ids, and list entries that don't
        /// resolve. Prefabs whose dependencies include no netcode script aren't loaded.
        /// </summary>
        internal static AuditResult AuditPrefabs(string folder, int limit)
        {
            if (limit < 1)
            {
                throw new ArgumentException("limit must be at least 1.");
            }
            if (!string.IsNullOrWhiteSpace(folder) && !AssetDatabase.IsValidFolder(folder.Trim()))
            {
                throw new ArgumentException($"No folder at '{folder}'. Pass a project folder such as Assets/Prefabs.");
            }

            var result = new AuditResult();
            var lists = FindPrefabsLists();
            var registered = new HashSet<GameObject>(lists.SelectMany(l => l.PrefabList).Select(p => p.Prefab).Where(p => p != null));
            var autoGenerate = NetcodeForGameObjectsProjectSettings.instance.GenerateDefaultNetworkPrefabs;
            var hashes = new Dictionary<uint, string>();
            var scriptCache = new Dictionary<string, bool>();

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
                if (result.Issues.Count < limit)
                {
                    result.Issues.Add(new AuditIssue { Severity = severity, Code = code, Asset = asset, Message = message });
                }
                else
                {
                    result.Truncated = true;
                }
            }

            var searchIn = string.IsNullOrWhiteSpace(folder) ? new[] { "Assets" } : new[] { folder.Trim() };
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", searchIn).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                if (!UsesNetcodeScripts(path, scriptCache))
                {
                    continue;
                }
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabAsset == null)
                {
                    continue;
                }
                result.PrefabsScanned++;
                AuditPrefab(prefabAsset, path, registered, autoGenerate, hashes, Add);
            }

            foreach (var list in lists)
            {
                var listPath = AssetDatabase.GetAssetPath(list);
                foreach (var entry in list.PrefabList)
                {
                    var described = DescribePrefabEntry(entry);
                    if (described.Error != null)
                    {
                        Add("error", "LIST_ENTRY_INVALID", listPath, $"{described.Name ?? "An entry"}: {described.Error}");
                    }
                }
            }
            return result;
        }

        private static void AuditPrefab(GameObject prefabAsset, string path, HashSet<GameObject> registered, bool autoGenerate, Dictionary<uint, string> hashes, Action<string, string, string, string> add)
        {
            var rootObject = prefabAsset.GetComponent<NetworkObject>();
            var networkObjects = prefabAsset.GetComponentsInChildren<NetworkObject>(true);
            var behaviours = prefabAsset.GetComponentsInChildren<NetworkBehaviour>(true);

            if (prefabAsset.GetComponentInChildren<NetworkManager>(true) != null && networkObjects.Length > 0)
            {
                add("error", "NETWORKMANAGER_WITH_NETWORKOBJECT", path, "A NetworkManager and a NetworkObject share this prefab's hierarchy. Keep the NetworkManager on its own GameObject.");
            }

            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || behaviour.GetComponentInParent<NetworkObject>(true) != null)
                {
                    continue;
                }
                add("error", "BEHAVIOUR_WITHOUT_NETWORKOBJECT", path, $"{behaviour.GetType().Name} on '{GetHierarchyPath(behaviour.transform)}' has no NetworkObject on itself or a parent, so it never spawns. Add one with netcode.gameobjects make_prefab.");
            }

            if (rootObject == null && networkObjects.Length > 0)
            {
                add("warning", "NETWORKOBJECT_NOT_ON_ROOT", path, $"The NetworkObject is on '{GetHierarchyPath(networkObjects[0].transform)}', not the root, so the prefab can't be registered or spawned as a whole.");
            }
            else if (rootObject != null && networkObjects.Length > 1)
            {
                add("warning", "NESTED_NETWORKOBJECT", path, $"{networkObjects.Length - 1} child NetworkObject(s). They aren't spawned with the prefab; spawn them separately or remove them.");
            }

            if (rootObject == null)
            {
                return;
            }

            var hash = rootObject.GlobalObjectIdHash;
            if (hash == 0)
            {
                add("error", "MISSING_PREFAB_ID", path, "The NetworkObject has no prefab id hash. Reimport the prefab (asset import) to generate it.");
            }
            else if (hashes.TryGetValue(hash, out var other))
            {
                add("error", "DUPLICATE_PREFAB_ID", path, $"Same prefab id hash ({hash}) as {other}. Reimport one of them (asset import) to regenerate it.");
            }
            else
            {
                hashes[hash] = path;
            }

            if (!registered.Contains(prefabAsset) && !autoGenerate)
            {
                add("warning", "NOT_REGISTERED", path, "Not in any network prefabs list, so it can't be spawned. Register it with netcode.gameobjects register_prefab.");
            }
        }

        /// <summary>Whether the prefab depends on NetworkObject, NetworkManager or a NetworkBehaviour script.</summary>
        private static bool UsesNetcodeScripts(string prefabPath, Dictionary<string, bool> cache)
        {
            foreach (var dependency in AssetDatabase.GetDependencies(prefabPath, true))
            {
                if (!dependency.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!cache.TryGetValue(dependency, out var isNetcode))
                {
                    var type = AssetDatabase.LoadAssetAtPath<MonoScript>(dependency)?.GetClass();
                    // A script whose class can't be resolved (a file holding several classes) might be netcode: check the prefab.
                    isNetcode = type == null || typeof(NetworkBehaviour).IsAssignableFrom(type) || type == typeof(NetworkObject) || type == typeof(NetworkManager);
                    cache[dependency] = isNetcode;
                }
                if (isNetcode)
                {
                    return true;
                }
            }
            return false;
        }

        // --- Helpers ---

        internal static List<NetworkPrefabsList> FindPrefabsLists()
        {
            return AssetDatabase.FindAssets($"t:{nameof(NetworkPrefabsList)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>)
                .Where(l => l != null)
                .ToList();
        }

        /// <summary>Resolves a prefab path, GUID or name to the prefab asset.</summary>
        internal static GameObject ResolvePrefab(string prefab)
        {
            if (string.IsNullOrWhiteSpace(prefab))
            {
                throw new ArgumentException("Pass the prefab as a path (Assets/…/Name.prefab), GUID or name. List network prefabs with netcode.gameobjects list_prefabs.");
            }

            prefab = prefab.Trim();
            string path;
            if (prefab.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                path = prefab;
            }
            else if (k_GuidPattern.IsMatch(prefab))
            {
                path = AssetDatabase.GUIDToAssetPath(prefab);
            }
            else
            {
                var matches = AssetDatabase.FindAssets($"{prefab} t:Prefab")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => string.Equals(Path.GetFileNameWithoutExtension(p), prefab, StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();
                if (matches.Count > 1)
                {
                    throw new ArgumentException($"Several prefabs are named '{prefab}': {string.Join(", ", matches)}. Pass the path instead.");
                }
                path = matches.FirstOrDefault();
            }

            var asset = (string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path)) ?? throw new ArgumentException($"No prefab found for '{prefab}'. Search with asset find, or list network prefabs with netcode.gameobjects list_prefabs.");
            return asset;
        }

        private static bool IsHybridPrefab(GameObject prefab)
        {
#if UNIFIED_NETCODE
            // NetworkObject serializes whether its GameObject has a GhostObject, which marks a hybrid prefab.
            return prefab.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.HasGhost;
#else
            return false;
#endif
        }

        private static string AssetPathOrName(UnityEngine.Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? asset.name : path;
        }

        private static void RequireEditMode(string action)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException($"Can't {action} in play mode; the change would be lost. Exit play mode with editor.playmode stop first.");
            }
        }
    }
}
