using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode.GameObjects.Editor.Cli;
using Unity.Netcode.GameObjects.Editor.Configuration;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.GameObjects.EditorTests
{
    /// <summary>
    /// Covers the query layer behind the netcode.gameobjects CLI commands. It lives in the editor assembly and
    /// compiles without com.unity.pipeline, so these run in every test configuration.
    /// </summary>
    internal class NgoCliQueriesTests
    {
        private const string k_Folder = "Assets/NgoCliTests";
        private const string k_ListPath = k_Folder + "/CliTestPrefabs.asset";

        private bool m_GenerateDefaultNetworkPrefabs;
        private string m_DefaultNetworkPrefabsPath;
        private bool m_PlayModeOptionsChanged;
        private bool m_EnterPlayModeOptionsEnabled;
        private EnterPlayModeOptions m_EnterPlayModeOptions;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = NetcodeForGameObjectsProjectSettings.instance;
            m_GenerateDefaultNetworkPrefabs = settings.GenerateDefaultNetworkPrefabs;
            m_DefaultNetworkPrefabsPath = NetworkPrefabProcessor.DefaultNetworkPrefabsPath;
            // Register prefabs explicitly in these tests, into a list of their own.
            settings.GenerateDefaultNetworkPrefabs = false;
            if (!AssetDatabase.IsValidFolder(k_Folder))
            {
                AssetDatabase.CreateFolder("Assets", "NgoCliTests");
            }
            NetworkPrefabProcessor.DefaultNetworkPrefabsPath = k_ListPath;
            ResetSimulator();
        }

        [TearDown]
        public void TearDown()
        {
            NetcodeForGameObjectsProjectSettings.instance.GenerateDefaultNetworkPrefabs = m_GenerateDefaultNetworkPrefabs;
            NetworkPrefabProcessor.DefaultNetworkPrefabsPath = m_DefaultNetworkPrefabsPath;
            AssetDatabase.DeleteAsset(k_Folder);
            ResetSimulator();
        }

        /// <summary>
        /// Leaves play mode even when a play mode test failed part way, so the tests after it start in edit mode,
        /// and puts back the enter play mode options a test changed.
        /// </summary>
        [UnityTearDown]
        public IEnumerator ExitPlayModeAndRestoreSettings()
        {
            if (EditorApplication.isPlaying)
            {
                foreach (var manager in NgoCliQueries.FindManagers())
                {
                    if (manager.IsListening)
                    {
                        manager.Shutdown(true);
                    }
                }
                yield return WaitForCondition(() =>
                {
                    foreach (var manager in NgoCliQueries.FindManagers())
                    {
                        if (manager.IsListening || manager.ShutdownInProgress)
                        {
                            return false;
                        }
                    }
                    return true;
                });
                yield return new ExitPlayMode();
            }
            foreach (var manager in NgoCliQueries.FindManagers())
            {
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }
            if (m_PlayModeOptionsChanged)
            {
                EditorSettings.enterPlayModeOptionsEnabled = m_EnterPlayModeOptionsEnabled;
                EditorSettings.enterPlayModeOptions = m_EnterPlayModeOptions;
                m_PlayModeOptionsChanged = false;
            }
        }

        /// <summary>
        /// Enters play mode without a domain or scene reload, so the scene and prefabs a test sets up survive. The
        /// options are only changed (and later restored) when they differ, so the project settings aren't re-saved.
        /// </summary>
        private void UsePlayModeWithoutReload()
        {
            const EnterPlayModeOptions options = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            if (EditorSettings.enterPlayModeOptionsEnabled && EditorSettings.enterPlayModeOptions == options)
            {
                return;
            }
            m_EnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            m_EnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            m_PlayModeOptionsChanged = true;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = options;
        }

        /// <summary>A UDP port nothing is bound to, from the OS, so concurrent jobs on a CI agent don't collide.</summary>
        private static ushort GetFreeUdpPort()
        {
            using var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            return (ushort)((IPEndPoint)socket.Client.LocalEndPoint).Port;
        }

        private static void ResetSimulator()
        {
            NgoCliSimulator.Enabled = false;
            NgoCliSimulator.Preset = "None";
            NgoCliSimulator.DelayMs = 0;
            NgoCliSimulator.JitterMs = 0;
            NgoCliSimulator.DropPercent = 0;
            NgoCliSimulator.SetTimeout(false);
        }

        private static GameObject CreatePrefab(string name, params Type[] components)
        {
            var gameObject = new GameObject(name, components);
            var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, $"{k_Folder}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(gameObject);
            return prefab;
        }

        private static NetworkManager CreateManager(NetworkTopologyTypes topology = NetworkTopologyTypes.ClientServer, int port = 7777)
        {
            NgoCliQueries.CreateNetworkManager("NetworkManager", topology, "127.0.0.1", port, false);
            return NgoCliQueries.FindManagers().Single();
        }

        // --- Status and resolution ---

        [Test]
        public void Status_WithoutManager_NamesTheNextStep()
        {
            var status = NgoCliQueries.GetStatus();

            Assert.IsFalse(status.IsPlaying);
            CollectionAssert.IsEmpty(status.Managers);
            StringAssert.Contains("create_network_manager", status.Hint);
        }

        [Test]
        public void Status_InEditMode_ReportsTheConfiguration()
        {
            CreateManager(port: 7801);

            var manager = NgoCliQueries.GetStatus().Managers.Single();

            Assert.AreEqual("none", manager.Role);
            Assert.IsFalse(manager.IsListening);
            Assert.AreEqual("ClientServer", manager.Topology);
            Assert.AreEqual(nameof(UnityTransport), manager.Transport.Type);
            Assert.AreEqual(7801, manager.Transport.Port);
            Assert.IsNull(manager.LocalClientId, "Runtime values are null outside play mode.");
        }

        [Test]
        public void RuntimeQueries_OutsidePlayMode_AskForPlayMode()
        {
            CreateManager();

            var error = Assert.Throws<InvalidOperationException>(() => NgoCliQueries.ListClients(null));
            StringAssert.Contains("editor.playmode start", error.Message);
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.ListObjects(null, null, -1, 10));
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.StartLagSpike(100));
        }

        [Test]
        public void ResolveManager_PicksByNameOrAsksWhenAmbiguous()
        {
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.ResolveManager(null));

            var first = new GameObject("ServerManager").AddComponent<NetworkManager>();
            var second = new GameObject("ClientManager").AddComponent<NetworkManager>();

            Assert.AreSame(second, NgoCliQueries.ResolveManager("client"));
            var ambiguous = Assert.Throws<ArgumentException>(() => NgoCliQueries.ResolveManager(null));
            StringAssert.Contains("ServerManager", ambiguous.Message);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.ResolveManager("Nope"));
            Assert.AreSame(first, NgoCliQueries.ResolveManager("ServerManager"));

            var longer = new GameObject("ServerManager2").AddComponent<NetworkManager>();
            Assert.AreSame(first, NgoCliQueries.ResolveManager("ServerManager"), "An exact name wins over a partial match.");
            Assert.AreSame(longer, NgoCliQueries.ResolveManager("ServerManager2"));
        }

        // --- Authoring ---

        [Test]
        public void CreateNetworkManager_SetsUpTransportAndTopology()
        {
            var dryRun = NgoCliQueries.CreateNetworkManager("Net", NetworkTopologyTypes.DistributedAuthority, "10.0.0.5", 9000, true);
            Assert.IsFalse(dryRun.Created);
            CollectionAssert.IsEmpty(NgoCliQueries.FindManagers(), "dry_run changes nothing.");
            Assert.IsTrue(dryRun.Notes.Any(n => n.Contains("Distributed authority")));

            var result = NgoCliQueries.CreateNetworkManager("Net", NetworkTopologyTypes.DistributedAuthority, "10.0.0.5", 9000, false);
            Assert.IsTrue(result.Created);

            var manager = NgoCliQueries.FindManagers().Single();
            var transport = (UnityTransport)manager.NetworkConfig.NetworkTransport;
            Assert.AreSame(manager.GetComponent<UnityTransport>(), transport);
            Assert.AreEqual(NetworkTopologyTypes.DistributedAuthority, manager.NetworkConfig.NetworkTopology);
            Assert.AreEqual("10.0.0.5", transport.ConnectionData.Address);
            Assert.AreEqual(9000, transport.ConnectionData.Port);
            Assert.AreEqual("0.0.0.0", transport.ConnectionData.ServerListenAddress, "A server for a remote address listens on every interface.");
            Assert.AreEqual(string.Empty, NgoCliQueries.DefaultListenAddress("127.0.0.1"));
            Assert.AreEqual(string.Empty, NgoCliQueries.DefaultListenAddress("localhost"));
            Assert.AreEqual("::", NgoCliQueries.DefaultListenAddress("2001:db8::1"));

            var again = Assert.Throws<InvalidOperationException>(() => NgoCliQueries.CreateNetworkManager("Net", NetworkTopologyTypes.ClientServer, "127.0.0.1", 7777, false));
            StringAssert.Contains("already have a NetworkManager", again.Message);
            StringAssert.Contains("in an unsaved scene", again.Message, "An unsaved scene has no name to show.");
        }

        [Test]
        public void MakePrefab_AddsANetworkObjectWithAPrefabId()
        {
            CreatePrefab("Plain");
            var path = $"{k_Folder}/Plain.prefab";

            var dryRun = NgoCliQueries.MakePrefab(path, true);
            Assert.IsFalse(dryRun.AddedNetworkObject);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkObject>());

            var result = NgoCliQueries.MakePrefab("Plain", false);
            Assert.IsTrue(result.AddedNetworkObject);
            var networkObject = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkObject>();
            Assert.IsNotNull(networkObject);
            if (result.PrefabIdHash.HasValue)
            {
                Assert.AreEqual(networkObject.GlobalObjectIdHash, (uint)result.PrefabIdHash.Value);
            }

            Assert.IsTrue(NgoCliQueries.MakePrefab(path, false).AlreadyNetworkObject);
        }

        [Test]
        public void RegisterPrefab_AddsOnceAndListsIt()
        {
            var prefab = CreatePrefab("Networked", typeof(NetworkObject));
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, k_ListPath);

            var dryRun = NgoCliQueries.RegisterPrefab("Networked", k_ListPath, true);
            Assert.IsFalse(dryRun.Registered);
            Assert.IsFalse(list.Contains(prefab));

            var result = NgoCliQueries.RegisterPrefab("Networked", k_ListPath, false);
            Assert.IsTrue(result.Registered);
            Assert.IsTrue(list.Contains(prefab));

            var again = NgoCliQueries.RegisterPrefab($"{k_Folder}/Networked.prefab", null, false);
            Assert.IsTrue(again.AlreadyRegistered, "With no NetworkManager, the default list (here the test list) is the target.");

            var listed = NgoCliQueries.ListPrefabs("CliTestPrefabs").Lists.Single();
            Assert.AreEqual(k_ListPath, listed.Path);
            Assert.AreEqual($"{k_Folder}/Networked.prefab", listed.Prefabs.Single().Path);
        }

        [Test]
        public void RegisterPrefab_OverrideSourceCountsAsRegistered()
        {
            var source = CreatePrefab("Source", typeof(NetworkObject));
            var target = CreatePrefab("Target", typeof(NetworkObject));
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, k_ListPath);
            // As in the inspector's override entries: Prefab differs from the source being overridden.
            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.Prefab, Prefab = target, SourcePrefabToOverride = source, OverridingTargetPrefab = target });

            var result = NgoCliQueries.RegisterPrefab("Source", k_ListPath, false);

            Assert.IsTrue(result.AlreadyRegistered);
            Assert.IsFalse(result.Registered);
            Assert.AreEqual(1, list.PrefabList.Count);
        }

        [Test]
        public void MakePrefab_WithChildNetworkObjects_ChangesNothing()
        {
            var parent = new GameObject("Parent");
            new GameObject("Child", typeof(NetworkObject)).transform.SetParent(parent.transform);
            PrefabUtility.SaveAsPrefabAsset(parent, $"{k_Folder}/Parent.prefab");
            UnityEngine.Object.DestroyImmediate(parent);

            var error = Assert.Throws<ArgumentException>(() => NgoCliQueries.MakePrefab("Parent", false));

            StringAssert.Contains("nested NetworkObjects", error.Message);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{k_Folder}/Parent.prefab").GetComponent<NetworkObject>());
        }

        [Test]
        public void AuditPrefabs_ReportsBrokenOverridesAndEmptyEntries()
        {
            var source = CreatePrefab("Source", typeof(NetworkObject));
            var plain = CreatePrefab("Plain");
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, k_ListPath);
            list.Add(null);
            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = source });
            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.Prefab, SourcePrefabToOverride = source, OverridingTargetPrefab = plain });
            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = 0, OverridingTargetPrefab = source });

            var result = NgoCliQueries.AuditPrefabs(k_Folder, 50);

            var entries = result.Issues.Where(i => i.Code == "LIST_ENTRY_INVALID" && i.Asset == k_ListPath).ToList();
            var found = string.Join(" | ", entries.Select(i => $"{i.Severity}: {i.Message}"));
            Assert.AreEqual(4, entries.Count, found);
            Assert.AreEqual(1, entries.Count(i => i.Severity == "warning"), "Netcode ignores an empty entry: " + found);
            Assert.IsTrue(entries.Any(i => i.Message.Contains("no target prefab")), found);
            Assert.IsTrue(entries.Any(i => i.Message.Contains("Plain.prefab has no NetworkObject")), found);
            Assert.IsTrue(entries.Any(i => i.Message.Contains("source hash is 0")), found);

            var listed = NgoCliQueries.ListPrefabs("CliTestPrefabs").Lists.Single();
            Assert.AreEqual(4, listed.Prefabs.Count, "Listing doesn't stop at an empty entry.");
        }

        [Test]
        public void AuditPrefabs_CountsAHashOverrideAsRegistration()
        {
            var source = CreatePrefab("Source", typeof(NetworkObject));
            var target = CreatePrefab("Target", typeof(NetworkObject));
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, k_ListPath);
            list.Add(new NetworkPrefab { Prefab = target });
            // The inspector keeps Prefab after an entry switches to Hash; NGO ignores it, and so does the audit.
            var leftover = CreatePrefab("Leftover");
            list.Add(new NetworkPrefab { Override = NetworkPrefabOverride.Hash, SourceHashToOverride = source.GetComponent<NetworkObject>().GlobalObjectIdHash, OverridingTargetPrefab = target, Prefab = leftover });

            var result = NgoCliQueries.AuditPrefabs(k_Folder, 50);

            var found = string.Join(", ", result.Issues.Select(i => $"{i.Code} {i.Asset}"));
            Assert.IsFalse(result.Issues.Any(i => i.Code == "NOT_REGISTERED"), found);
            Assert.IsFalse(result.Issues.Any(i => i.Code == "LIST_ENTRY_INVALID"), found);
            Assert.IsTrue(NgoCliQueries.RegisterPrefab("Source", k_ListPath, true).AlreadyRegistered, "register_prefab and audit agree.");
        }

        [Test]
        public void AuditPrefabs_CountsThePlayerPrefabAsRegistration()
        {
            var player = CreatePrefab("Player", typeof(NetworkObject));
            var manager = CreateManager();
            manager.NetworkConfig.PlayerPrefab = player;

            var result = NgoCliQueries.AuditPrefabs(k_Folder, 50);

            Assert.IsFalse(result.Issues.Any(i => i.Code == "NOT_REGISTERED"), string.Join(", ", result.Issues.Select(i => $"{i.Code} {i.Asset}")));
        }

        [Test]
        public void SetConfig_OnAPrefabInstance_RecordsAnOverride()
        {
            CreateManager();
            var managerPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(NgoCliQueries.FindManagers().Single().gameObject, $"{k_Folder}/Manager.prefab", InteractionMode.AutomatedAction);
            Assert.IsNotNull(managerPrefab);

            NgoCliQueries.SetConfig("tick_rate", "60", null, false);

            var instance = NgoCliQueries.FindManagers().Single();
            Assert.IsTrue(PrefabUtility.GetPropertyModifications(instance).Any(m => m.target == managerPrefab.GetComponent<NetworkManager>() && m.propertyPath.EndsWith("TickRate") && m.value == "60"),
                "The change is kept as a prefab override.");
        }

        [Test]
        public void RegisterPrefab_WithoutNetworkObject_PointsToMakePrefab()
        {
            CreatePrefab("Plain");

            var error = Assert.Throws<ArgumentException>(() => NgoCliQueries.RegisterPrefab("Plain", null, false));
            StringAssert.Contains("make_prefab", error.Message);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.RegisterPrefab("NoSuchPrefab", null, false));
        }

        [Test]
        public void AuditPrefabs_FindsSetupProblems()
        {
            CreatePrefab("Orphan", typeof(Components.NetworkTransform));
            var nested = new GameObject("Nested", typeof(NetworkObject));
            new GameObject("Child", typeof(NetworkObject)).transform.SetParent(nested.transform);
            PrefabUtility.SaveAsPrefabAsset(nested, $"{k_Folder}/Nested.prefab");
            UnityEngine.Object.DestroyImmediate(nested);
            CreatePrefab("Unregistered", typeof(NetworkObject));
            CreatePrefab("NotNetworked");

            var result = NgoCliQueries.AuditPrefabs(k_Folder, 50);
            var codes = result.Issues.Select(i => (i.Code, i.Asset)).ToList();

            var found = string.Join(", ", codes);
            CollectionAssert.Contains(codes, ("BEHAVIOUR_WITHOUT_NETWORKOBJECT", $"{k_Folder}/Orphan.prefab"), found);
            CollectionAssert.Contains(codes, ("NESTED_NETWORKOBJECT", $"{k_Folder}/Nested.prefab"), found);
            CollectionAssert.Contains(codes, ("NOT_REGISTERED", $"{k_Folder}/Unregistered.prefab"), found);
            Assert.AreEqual(result.Issues.Count(i => i.Severity == "error"), result.Errors);
            Assert.AreEqual(3, result.PrefabsScanned, $"The prefab without netcode scripts isn't loaded. Found: {found}");

            var truncated = NgoCliQueries.AuditPrefabs(k_Folder, 1);
            Assert.AreEqual(1, truncated.Issues.Count);
            Assert.IsTrue(truncated.Truncated);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.AuditPrefabs("Assets/NoSuchFolder", 10));

            // Without a NetworkManager open, the project audit still checks the prefabs and says so.
            var all = NgoCliQueries.Audit("all", k_Folder, 50, null);
            Assert.AreEqual(3, all.PrefabsScanned);
            Assert.AreEqual(0, all.ManagersChecked);
            Assert.AreEqual("NO_NETWORKMANAGER", all.Issues[0].Code);
            Assert.IsTrue(all.Issues.Skip(1).All(i => i.Area == "prefabs"));
            Assert.AreEqual(result.Warnings + 1, all.Warnings);
            Assert.IsNull(NgoCliQueries.Audit("prefabs", k_Folder, 50, null).ManagersChecked);
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.Audit("config", null, 50, null), "Asking for the NetworkManager checks needs one.");
        }

        // --- Config ---

        [Test]
        public void GetConfig_ReportsSnakeCaseFields()
        {
            CreateManager(port: 7802);

            var config = NgoCliQueries.GetConfig(null);

            Assert.AreEqual(30u, config.NetworkConfig["tick_rate"]);
            Assert.AreEqual("ClientServer", config.NetworkConfig["network_topology"]);
            Assert.IsTrue(config.NetworkConfig.ContainsKey("use_cmb_service"));
            CollectionAssert.Contains(config.SettableFields, "port");
            CollectionAssert.Contains(config.SettableFields, "log_level");
            Assert.AreEqual(7802, config.Transport.Port);
        }

        [Test]
        public void SetConfig_ValidatesAndApplies()
        {
            var manager = CreateManager();

            var dryRun = NgoCliQueries.SetConfig("tick_rate", "60", null, true);
            Assert.AreEqual(30u, dryRun.Previous);
            Assert.AreEqual(30u, manager.NetworkConfig.TickRate, "dry_run changes nothing.");

            NgoCliQueries.SetConfig("tick_rate", "60", null, false);
            Assert.AreEqual(60u, manager.NetworkConfig.TickRate);
            NgoCliQueries.SetConfig("connection_approval", "true", null, false);
            Assert.IsTrue(manager.NetworkConfig.ConnectionApproval);
            NgoCliQueries.SetConfig("network_topology", "distributedauthority", null, false);
            Assert.AreEqual(NetworkTopologyTypes.DistributedAuthority, manager.NetworkConfig.NetworkTopology);
            var transport = (UnityTransport)manager.NetworkConfig.NetworkTransport;
            Assert.AreEqual(string.Empty, transport.ConnectionData.ServerListenAddress, "The listen address isn't set to the connect address.");
            transport.ConnectionData.WebSocketPath = "/game";
            NgoCliQueries.SetConfig("port", "9100", null, false);
            Assert.AreEqual(9100, transport.ConnectionData.Port);
            Assert.AreEqual("/game", transport.ConnectionData.WebSocketPath, "Other connection settings are kept.");

            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("load_scene_time_out", "0", null, false));
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("tick_rate", "0", null, false));
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("tick_rate", "fast", null, false));
            var unknown = Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("no_such_field", "1", null, false));
            StringAssert.Contains("tick_rate", unknown.Message);

            Undo.PerformUndo();
            Assert.AreEqual(7777, ((UnityTransport)manager.NetworkConfig.NetworkTransport).ConnectionData.Port, "Each change is its own undo step.");
            Assert.AreEqual(60u, manager.NetworkConfig.TickRate);

            var serialized = new SerializedObject(transport);
            serialized.FindProperty("m_ProtocolType").enumValueIndex = (int)UnityTransport.ProtocolType.RelayUnityTransport;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var relay = Assert.Throws<InvalidOperationException>(() => NgoCliQueries.SetConfig("port", "9200", null, false));
            StringAssert.Contains("Relay", relay.Message);
        }

        [Test]
        public void SetConfig_RejectsOutOfRangeAndNonFiniteFloats()
        {
            var manager = CreateManager();
            var spawnTimeout = manager.NetworkConfig.SpawnTimeout;

            StringAssert.Contains("between", Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("spawn_timeout", "0", null, false)).Message);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("spawn_timeout", "4000", null, false));
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("spawn_timeout", "NaN", null, false));
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("network_id_recycle_delay", "Infinity", null, false));
            Assert.AreEqual(spawnTimeout, manager.NetworkConfig.SpawnTimeout, "A rejected value changes nothing.");

            NgoCliQueries.SetConfig("spawn_timeout", "30", null, false);
            Assert.AreEqual(30f, manager.NetworkConfig.SpawnTimeout);
        }

        [Test]
        public void SetConfig_LogLevel_ChangesTheManager()
        {
            var manager = CreateManager();

            var result = NgoCliQueries.SetConfig("log_level", "developer", null, false);

            Assert.AreEqual("Normal", result.Previous);
            Assert.AreEqual(LogLevel.Developer, manager.LogLevel);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetConfig("log_level", "Verbose", null, false));
        }

        [Test]
        public void ValidateConfig_ReportsMissingTransportAndDistributedAuthorityService()
        {
            var manager = CreateManager(NetworkTopologyTypes.DistributedAuthority);
            manager.NetworkConfig.NetworkTransport = null;

            var audit = NgoCliQueries.Audit("config", null, 50, null);
            var codes = audit.Issues.Select(i => i.Code).ToList();

            Assert.AreEqual(1, audit.ManagersChecked);
            Assert.IsTrue(audit.Issues.All(i => i.Area == "config"));
            CollectionAssert.Contains(codes, "NO_TRANSPORT");
            CollectionAssert.Contains(codes, "DA_WITHOUT_SERVICE");
            CollectionAssert.Contains(codes, "NO_NETWORK_PREFABS");
            CollectionAssert.Contains(codes, "SCENE_NOT_SAVED", "Scene management is on in an unsaved scene, which opens a dialog in play mode.");
        }

        [Test]
        public void ToSnakeCase_KeepsAcronymsTogether()
        {
            Assert.AreEqual("tick_rate", NgoCliQueries.ToSnakeCase("TickRate"));
            Assert.AreEqual("use_cmb_service", NgoCliQueries.ToSnakeCase("UseCMBService"));
            Assert.AreEqual("rpc_hash_size", NgoCliQueries.ToSnakeCase("RpcHashSize"));
        }

        // --- Simulator ---

        [Test]
        public void SetSimulator_StoresValuesForTheNextSession()
        {
            var result = NgoCliQueries.SetSimulator(true, null, 120, 15, 3);

            Assert.IsFalse(result.Previous.Enabled);
            Assert.IsTrue(result.Current.Enabled);
            Assert.AreEqual(NgoCliSimulator.CustomPreset, result.Current.Preset);
            Assert.AreEqual(120, result.Current.PacketDelayMs);
            Assert.AreEqual(15, result.Current.PacketJitterMs);
            Assert.AreEqual(3, result.Current.PacketDropPercent);
            CollectionAssert.IsEmpty(result.AppliedLiveTo);

            var partial = NgoCliQueries.SetSimulator(true, null, -1, -1, 10);
            Assert.AreEqual(120, partial.Current.PacketDelayMs, "-1 leaves a value unchanged.");
            Assert.AreEqual(10, partial.Current.PacketDropPercent);

            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetSimulator(true, null, 5000, -1, -1));
            Assert.Throws<ArgumentException>(() => NgoCliQueries.SetSimulator(true, "NoSuchPreset", -1, -1, -1));
        }

        [Test]
        public void SimulatorPresets_ComeFromMultiplayerTools()
        {
            var presets = NgoCliQueries.ListSimulatorPresets();
            if (presets.Source == null)
            {
                CollectionAssert.IsEmpty(presets.Presets);
                StringAssert.Contains("com.unity.multiplayer.tools", presets.Note);
                return;
            }

            Assert.IsNotEmpty(presets.Presets);
            var preset = presets.Presets.First(p => p.PacketDelayMs > 0);
            var result = NgoCliQueries.SetSimulator(true, preset.Name.ToUpperInvariant(), -1, -1, -1);
            Assert.AreEqual(preset.Name, result.Current.Preset);
            Assert.AreEqual(preset.PacketDelayMs, result.Current.PacketDelayMs);

            // Preset names carry bracketed details; the name before them is enough.
            var detailed = presets.Presets.FirstOrDefault(p => p.Name.Contains(" ["));
            if (detailed != null)
            {
                var shortName = detailed.Name.Substring(0, detailed.Name.IndexOf(" [", StringComparison.Ordinal));
                Assert.AreEqual(detailed.Name, NgoCliQueries.SetSimulator(true, shortName, -1, -1, -1).Current.Preset);
            }
        }

        // --- A running host ---

        /// <summary>
        /// Starts a host in play mode and drives the runtime queries against it. Play mode is entered without a
        /// domain or scene reload so the scene and prefab set up here survive.
        /// </summary>
        [UnityTest]
        public IEnumerator RunningHost_SupportsTheRuntimeQueries()
        {
            UsePlayModeWithoutReload();
            var prefab = CreatePrefab("CliSpawnable", typeof(NetworkObject));
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, k_ListPath);
            NgoCliQueries.RegisterPrefab("CliSpawnable", k_ListPath, false);
            var manager = CreateManager(port: GetFreeUdpPort());
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            // The unsaved test scene isn't in the build, which with scene management on opens a dialog in play mode.
            manager.NetworkConfig.EnableSceneManagement = false;

            yield return new EnterPlayMode();

            var started = NgoCliQueries.Start(NgoCliQueries.SessionMode.Host, null, null, -1);
            Assert.AreEqual("host", started.Status.Role);
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.Start(NgoCliQueries.SessionMode.Host, null, null, -1));

            var ready = NgoCliQueries.WaitReadyAsync(null, 0, 10);
            yield return WaitFor(ready);
            Assert.IsTrue(ready.Result.Ready, ready.Result.Reason);

            var client = NgoCliQueries.ListClients(null).Clients.Single();
            Assert.AreEqual(0, client.ClientId);
            Assert.IsTrue(client.IsLocal);

            var spawned = NgoCliQueries.Spawn("CliSpawnable", new[] { 1f, 2f, 3f }, -1, false, null);
            var id = spawned.Object.NetworkObjectId;
            CollectionAssert.AreEqual(new[] { 1f, 2f, 3f }, spawned.Object.Position);
            Assert.AreEqual(1, NgoCliQueries.ListObjects(null, "CliSpawnable", -1, 10).Total);
            var detail = NgoCliQueries.GetObject(id, null);
            Assert.AreEqual(0, detail.OwnerClientId);
            Assert.AreEqual("The server or host.", detail.Ownership.WhoCanChange);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.GetObject(id + 1000, null));

            var unchanged = NgoCliQueries.ChangeOwner(id, 0, null);
            Assert.AreEqual(0, unchanged.Owner);
            Assert.Throws<ArgumentException>(() => NgoCliQueries.ChangeOwner(id, 42, null));
            Assert.Throws<ArgumentException>(() => NgoCliQueries.Kick(0, null, null), "The host can't kick its own client.");

            var driver = ((UnityTransport)NgoCliQueries.ResolveManager(null).NetworkConfig.NetworkTransport).GetNetworkDriver();
            driver.CurrentSettings.TryGet<SimulatorUtility.Parameters>(out var original);
            var simulator = NgoCliQueries.SetSimulator(true, null, 50, 0, 0);
            CollectionAssert.IsNotEmpty(simulator.AppliedLiveTo, "The running transport gets the settings now.");
            driver.CurrentSettings.TryGet<SimulatorUtility.Parameters>(out var applied);
            Assert.AreEqual(50, applied.PacketDelayMs);

            var timeout = NgoCliQueries.SetTimeout(true);
            Assert.IsTrue(timeout.Current.TimeoutActive);
            var spike = NgoCliQueries.StartLagSpike(10);
            Assert.IsTrue(spike.Current.TimeoutActive, "A lag spike leaves the timeout on.");
            yield return WaitForCondition(() => !NgoCliSimulator.LagSpikeActive);
            Assert.IsFalse(NgoCliSimulator.LagSpikeActive);
            Assert.IsTrue(NgoCliQueries.GetSimulator().Settings.TimeoutActive, "The timeout outlasts the lag spike.");
            NgoCliQueries.SetTimeout(false);

            NgoCliQueries.SetSimulator(false, null, -1, -1, -1);
            driver.CurrentSettings.TryGet<SimulatorUtility.Parameters>(out var restored);
            Assert.AreEqual(original.PacketDelayMs, restored.PacketDelayMs, "Turning the simulator off restores the transport's own settings.");

            var hooks = (IList)typeof(NetworkMessageManager).GetField("m_Hooks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(NgoCliQueries.ResolveManager(null).MessageManager);
            var hookCount = hooks.Count;
            var stats = NgoCliQueries.SampleStatsAsync(null, 3, 10);
            Assert.AreEqual(hookCount + 1, hooks.Count, "Sampling hooks the message manager.");
            yield return WaitFor(stats);
            Assert.AreEqual("host", stats.Result.Role);
            Assert.GreaterOrEqual(stats.Result.Frames, 1);
            Assert.AreEqual(stats.Result.Messages.Sum(m => m.Sent), stats.Result.MessagesSent);
            Assert.AreEqual(hookCount, hooks.Count, "Sampling unhooks the message manager when done.");

            var despawned = NgoCliQueries.Despawn(id, true, null);
            Assert.IsTrue(despawned.Despawned);
            Assert.AreEqual(0, NgoCliQueries.ListObjects(null, "CliSpawnable", -1, 10).Total);

            NgoCliQueries.Stop(null, false);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Runs a host and a second NetworkManager as its client in the same play session, and drives the commands
        /// that act on a remote client: start with a transport override, ownership, kick, reconnect, and a stats
        /// sample that the host's shutdown cuts short.
        /// </summary>
        [UnityTest]
        public IEnumerator HostWithARemoteClient_SupportsTheClientQueries()
        {
            UsePlayModeWithoutReload();
            CreatePrefab("CliSpawnable", typeof(NetworkObject));
            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, k_ListPath);
            NgoCliQueries.RegisterPrefab("CliSpawnable", k_ListPath, false);
            var port = GetFreeUdpPort();
            var host = CreateManager(port: port);
            host.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            host.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(list);
            // The unsaved test scene isn't in the build, which with scene management on opens a dialog in play mode.
            host.NetworkConfig.EnableSceneManagement = false;
            // A copy of the host's setup, pointed at the wrong port so start's override has something to fix.
            var client = UnityEngine.Object.Instantiate(host.gameObject).GetComponent<NetworkManager>();
            client.name = "ClientManager";
            var clientTransport = (UnityTransport)client.NetworkConfig.NetworkTransport;
            Assert.AreSame(client.gameObject, clientTransport.gameObject, "The copy uses its own transport.");
            clientTransport.SetConnectionData("127.0.0.1", (ushort)(port + 1));

            yield return new EnterPlayMode();

            NgoCliQueries.Start(NgoCliQueries.SessionMode.Host, "NetworkManager", null, -1);
            var clientStart = NgoCliQueries.Start(NgoCliQueries.SessionMode.Client, "ClientManager", null, port);
            Assert.AreEqual(port, clientTransport.ConnectionData.Port, "start's port overrides the transport's.");
            StringAssert.Contains($":{port}", clientStart.Notes[0]);

            var clientReady = NgoCliQueries.WaitReadyAsync("ClientManager", 0, 10);
            yield return WaitFor(clientReady);
            Assert.IsTrue(clientReady.Result.Ready, clientReady.Result.Reason);
            var hostReady = NgoCliQueries.WaitReadyAsync("NetworkManager", 1, 10);
            yield return WaitFor(hostReady);
            Assert.IsTrue(hostReady.Result.Ready, hostReady.Result.Reason);

            var remoteId = (long)client.LocalClientId;
            var clients = NgoCliQueries.ListClients("NetworkManager").Clients;
            Assert.AreEqual(2, clients.Count);
            Assert.IsFalse(clients.Single(c => c.ClientId == remoteId).IsLocal);

            var id = NgoCliQueries.Spawn("CliSpawnable", null, -1, false, "NetworkManager").Object.NetworkObjectId;
            var changed = NgoCliQueries.ChangeOwner(id, remoteId, "NetworkManager");
            Assert.AreEqual(0, changed.PreviousOwner);
            Assert.AreEqual(remoteId, changed.Owner);
            yield return WaitForCondition(() => client.SpawnManager.SpawnedObjects.ContainsKey((ulong)id));
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.ChangeOwner(id, 0, "ClientManager"), "A client-server client can't change ownership.");
            Assert.Throws<InvalidOperationException>(() => NgoCliQueries.Kick(remoteId, null, "ClientManager"), "Only the server or host kicks.");

            NgoCliQueries.Kick(remoteId, "Kicked by the CLI test", "NetworkManager");
            yield return WaitForCondition(() => !client.IsListening && !client.ShutdownInProgress);
            Assert.IsFalse(client.IsListening, "The kicked client shuts down.");
            StringAssert.Contains("Kicked by the CLI test", client.DisconnectReason);

            var reconnected = NgoCliQueries.ReconnectAsync("ClientManager", 10);
            yield return WaitFor(reconnected);
            Assert.IsTrue(reconnected.Result.Ready, reconnected.Result.Reason);
            Assert.IsTrue(client.IsConnectedClient);
            Assert.ThrowsAsync<InvalidOperationException>(() => NgoCliQueries.ReconnectAsync("NetworkManager", 5), "reconnect is for clients.");

            var stats = NgoCliQueries.SampleStatsAsync("NetworkManager", 10_000, 60);
            NgoCliQueries.Stop("NetworkManager", false);
            yield return WaitForCondition(() => stats.IsCompleted);
            Assert.IsTrue(stats.IsFaulted, "Stopping the host ends the sample with an error.");
            StringAssert.Contains("session ended while sampling", stats.Exception.InnerException.Message);

            NgoCliQueries.Stop("ClientManager", false);
            yield return WaitForCondition(() => !client.IsListening && !client.ShutdownInProgress);
        }

        /// <summary>
        /// A distributed authority client that hasn't connected yet can't spawn: the command refuses before it
        /// instantiates anything.
        /// </summary>
        [UnityTest]
        public IEnumerator Spawn_DistributedAuthorityClientNotConnected_Refuses()
        {
            UsePlayModeWithoutReload();
            CreatePrefab("CliSpawnable", typeof(NetworkObject));
            // Nothing listens on this port, so the client stays connecting.
            var manager = CreateManager(NetworkTopologyTypes.DistributedAuthority, GetFreeUdpPort());
            manager.NetworkConfig.EnableSceneManagement = false;

            yield return new EnterPlayMode();

            NgoCliQueries.Start(NgoCliQueries.SessionMode.Client, null, null, -1);
            Assert.IsTrue(manager.IsListening);
            Assert.IsFalse(manager.IsConnectedClient);

            var refused = Assert.Throws<InvalidOperationException>(() => NgoCliQueries.Spawn("CliSpawnable", null, -1, false, null));
            StringAssert.Contains("wait_ready", refused.Message);
            Assert.IsNull(GameObject.Find("CliSpawnable(Clone)"), "Nothing is instantiated.");

            NgoCliQueries.Stop(null, true);
            yield return WaitForCondition(() => !manager.IsListening && !manager.ShutdownInProgress);
        }

        private static IEnumerator WaitForCondition(Func<bool> condition)
        {
            var deadline = EditorApplication.timeSinceStartup + 20;
            while (!condition() && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitFor(Task task)
        {
            yield return WaitForCondition(() => task.IsCompleted);
            Assert.IsTrue(task.IsCompleted, "The query didn't finish.");
            if (task.IsFaulted)
            {
                throw task.Exception.InnerException;
            }
        }
    }
}
