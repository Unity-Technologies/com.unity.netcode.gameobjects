using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode.GameObjects.Editor.Cli;
using Unity.Pipeline.Commands;
using UnityEditor.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Netcode.GameObjects.EditorTests
{
    /// <summary>
    /// Covers the netcode.gameobjects [CliGroupCommand] wrappers as the pipeline sees them: what is registered (the wire
    /// API) and what an invocation returns. The logic behind them is covered by NgoCliQueriesTests. Like the
    /// commands, this assembly only compiles when com.unity.pipeline is installed.
    /// </summary>
    internal class NetcodeGameObjectsCliCommandsTests
    {
        private const string k_Package = "Unity.Netcode.GameObjects.Editor.CLI";
        private const string k_Tag = "networking/netcode";

        // Parameters are wire API: clients bind positionals to required parameters in order, so renaming,
        // reordering or newly requiring one breaks them. A trailing '*' marks a required parameter.
        // Changing this table means changing the API; add new parameters at the end, as optional.
        private static readonly Dictionary<string, string[]> k_WireApi = new Dictionary<string, string[]>
        {
            ["netcode.gameobjects status"] = new string[0],
            ["netcode.gameobjects list_clients"] = new[] { "manager" },
            ["netcode.gameobjects list_objects"] = new[] { "name_contains", "owner", "limit", "manager" },
            ["netcode.gameobjects get_object"] = new[] { "id*", "manager" },
            ["netcode.gameobjects spawn"] = new[] { "prefab*", "position", "owner", "player_object", "manager" },
            ["netcode.gameobjects despawn"] = new[] { "id*", "destroy", "manager" },
            ["netcode.gameobjects change_owner"] = new[] { "id*", "owner*", "manager" },
            ["netcode.gameobjects create_network_manager"] = new[] { "name", "topology", "address", "port", "dry_run" },
            ["netcode.gameobjects list_prefabs"] = new[] { "list" },
            ["netcode.gameobjects register_prefab"] = new[] { "prefab*", "list", "dry_run" },
            ["netcode.gameobjects make_prefab"] = new[] { "prefab*", "dry_run" },
            ["netcode.gameobjects audit"] = new[] { "scope", "folder", "limit", "manager" },
            ["netcode.gameobjects.profiler get_stats"] = new[] { "frames", "manager", "timeout" },
            ["netcode.gameobjects.session start"] = new[] { "mode*", "address", "port", "manager" },
            ["netcode.gameobjects.session stop"] = new[] { "discard_message_queue", "manager" },
            ["netcode.gameobjects.session kick"] = new[] { "client_id*", "reason", "manager" },
            ["netcode.gameobjects.session reconnect"] = new[] { "manager", "timeout" },
            ["netcode.gameobjects.session wait_ready"] = new[] { "min_clients", "manager", "timeout" },
            ["netcode.gameobjects.simulator get"] = new string[0],
            ["netcode.gameobjects.simulator set"] = new[] { "enable", "preset", "delay_ms", "jitter_ms", "drop_percent" },
            ["netcode.gameobjects.simulator list_presets"] = new string[0],
            ["netcode.gameobjects.simulator lag_spike"] = new[] { "duration_ms" },
            ["netcode.gameobjects.simulator timeout"] = new[] { "enable" },
            ["netcode.gameobjects.config get"] = new[] { "manager" },
            ["netcode.gameobjects.config set"] = new[] { "field*", "value*", "manager", "dry_run" },
        };

        private static readonly string[] k_Groups =
        {
            "netcode.gameobjects", "netcode.gameobjects.session", "netcode.gameobjects.simulator",
            "netcode.gameobjects.config", "netcode.gameobjects.profiler",
        };

        [OneTimeSetUp]
        public void WarmCommandRegistry()
        {
            // Discovery logs an error for each invalid command in other packages (e.g. one written for an older
            // pipeline), which would fail whichever test discovers first. Discover once, ignoring those; the
            // registry caches the result, and the tests check only these commands.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                CommandRegistry.DiscoverCommands();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static List<CommandInfo> Commands() =>
            CommandRegistry.DiscoverCommands().Where(c => c.Package == k_Package).ToList();

        private static CommandInfo Command(string name)
        {
            var command = Commands().SingleOrDefault(c => c.FullName == name);
            Assert.IsNotNull(command, $"{name} is not registered.");
            return command;
        }

        /// <summary>
        /// Invokes a command's method the way the server does after binding: named arguments by CLI name, every
        /// other parameter at its C# default. Unwraps the reflection exception so tests see the command's own.
        /// </summary>
        private static object Invoke(string name, params (string Name, object Value)[] arguments)
        {
            var command = Command(name);
            var methodParameters = command.Method.GetParameters();
            var values = new object[methodParameters.Length];
            for (var i = 0; i < methodParameters.Length; i++)
            {
                var cliName = command.Parameters[i].Name;
                var supplied = arguments.Where(a => a.Name == cliName).ToList();
                values[i] = supplied.Count > 0 ? supplied[0].Value : methodParameters[i].DefaultValue;
            }

            object result;
            try
            {
                result = command.Method.Invoke(command.Target, values);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }

            Assert.IsNotInstanceOf<System.Threading.Tasks.Task>(result, "This helper is for synchronous commands.");
            return result;
        }

        private static Dictionary<string, object> InvokeObject(string name, params (string Name, object Value)[] arguments)
        {
            var result = Invoke(name, arguments);
            Assert.IsInstanceOf<Dictionary<string, object>>(result, $"{name} should return a camelCase object.");
            return (Dictionary<string, object>)result;
        }

        [Test]
        public void Registration_MatchesTheWireApi()
        {
            var commands = Commands();

            CollectionAssert.AreEquivalent(k_WireApi.Keys, commands.Select(c => c.FullName), "Registered commands (group name)");
            foreach (var command in commands)
            {
                var actual = command.Parameters.Select(p => p.Required ? p.Name + "*" : p.Name).ToArray();
                CollectionAssert.AreEqual(k_WireApi[command.FullName], actual, $"{command.FullName} parameters (name, order, required)");
            }
        }

        [Test]
        public void Registration_EveryCommandIsTaggedAndRunsOnTheMainThread()
        {
            foreach (var command in Commands())
            {
                CollectionAssert.Contains(command.Tags, k_Tag, command.FullName);
                CollectionAssert.Contains(k_Groups, command.Group, command.FullName);
                Assert.IsTrue(command.MainThreadRequired, $"{command.FullName} reads scene and network state, so it must run on the main thread.");
                Assert.IsNotEmpty(command.Description, command.FullName);
            }
        }

        [Test]
        public void Registration_TypedParametersKeepTheirTypes()
        {
            // Enum types let the pipeline's schema list the accepted values; a double timeout lets the server pass
            // the time left of the request.
            Assert.AreEqual(typeof(NgoCliQueries.SessionMode), Command("netcode.gameobjects.session start").Parameters.Single(p => p.Name == "mode").ParameterType);
            Assert.AreEqual(typeof(NetworkTopologyTypes), Command("netcode.gameobjects create_network_manager").Parameters.Single(p => p.Name == "topology").ParameterType);
            Assert.AreEqual(typeof(float[]), Command("netcode.gameobjects spawn").Parameters.Single(p => p.Name == "position").ParameterType);
            foreach (var name in new[] { "netcode.gameobjects.profiler get_stats", "netcode.gameobjects.session reconnect", "netcode.gameobjects.session wait_ready" })
            {
                Assert.AreEqual(typeof(double), Command(name).Parameters.Single(p => p.Name == "timeout").ParameterType, name);
            }
        }

        [Test]
        public void Status_WithoutManager_ReturnsAHint()
        {
            var result = InvokeObject("netcode.gameobjects status");

            Assert.AreEqual(false, result["isPlaying"]);
            CollectionAssert.IsEmpty((List<object>)result["managers"]);
            StringAssert.Contains("create_network_manager", (string)result["hint"]);
        }

        [Test]
        public void CreateNetworkManager_ThenConfigGet_ReturnCamelCaseResults()
        {
            var created = InvokeObject("netcode.gameobjects create_network_manager", ("port", 7811));
            Assert.AreEqual(true, created["created"]);
            Assert.AreEqual("ClientServer", created["topology"]);

            var config = InvokeObject("netcode.gameobjects.config get");
            var networkConfig = (Dictionary<string, object>)config["networkConfig"];
            Assert.IsTrue(networkConfig.ContainsKey("tick_rate"), "NetworkConfig fields keep the snake_case names config set takes.");
            Assert.AreEqual(7811, ((Dictionary<string, object>)config["transport"])["port"]);

            var audited = InvokeObject("netcode.gameobjects audit", ("scope", "config"));
            Assert.IsInstanceOf<List<object>>(audited["issues"]);
            Assert.AreEqual(1, audited["managersChecked"]);
            Assert.IsNull(audited["prefabsScanned"], "Prefabs aren't checked in the config scope.");
            Assert.Throws<ArgumentException>(() => Invoke("netcode.gameobjects audit", ("scope", "everything")));
        }

        [Test]
        public void Errors_NameTheNextStep()
        {
            var noManager = Assert.Throws<InvalidOperationException>(() => Invoke("netcode.gameobjects list_clients"));
            StringAssert.Contains("create_network_manager", noManager.Message);

            Invoke("netcode.gameobjects create_network_manager");
            var notPlaying = Assert.Throws<InvalidOperationException>(() => Invoke("netcode.gameobjects list_objects"));
            StringAssert.Contains("editor.playmode start", notPlaying.Message);

            var unknownPrefab = Assert.Throws<ArgumentException>(() => Invoke("netcode.gameobjects make_prefab", ("prefab", "NoSuchPrefab")));
            StringAssert.Contains("list_prefabs", unknownPrefab.Message);
        }

        [Test]
        public void WaitSeconds_StopsBeforeTheRequestTimeout()
        {
            Assert.AreEqual(28f, NetcodeGameObjectsCliCommands.WaitSeconds(30), 0.001);
            Assert.AreEqual(4.5f, NetcodeGameObjectsCliCommands.WaitSeconds(5), 0.001);
            Assert.AreEqual(600f, NetcodeGameObjectsCliCommands.WaitSeconds(double.PositiveInfinity));
        }
    }
}
