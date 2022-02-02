using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    /// <summary>
    /// Provides helpers for running multi instance tests.
    /// </summary>
    public static class MultiInstanceHelpers
    {
        public const int DefaultMinFrames = 1;
        public const float DefaultTimeout = 1f;
        private static List<NetworkManager> s_NetworkManagerInstances = new List<NetworkManager>();
        private static Dictionary<NetworkManager, MultiInstanceHooks> s_Hooks = new Dictionary<NetworkManager, MultiInstanceHooks>();
        private static bool s_IsStarted;
        private static int s_ClientCount;
        private static int s_OriginalTargetFrameRate = -1;

        public delegate bool MessageHandleCheck(object receivedMessage);

        internal class MessageHandleCheckWithResult
        {
            public MessageHandleCheck Check;
            public bool Result;
        }

        private class MultiInstanceHooks : INetworkHooks
        {
            public Dictionary<Type, List<MessageHandleCheckWithResult>> HandleChecks = new Dictionary<Type, List<MessageHandleCheckWithResult>>();

            public static bool CheckForMessageOfType<T>(object receivedMessage) where T : INetworkMessage
            {
                return receivedMessage.GetType() == typeof(T);
            }


            public void OnBeforeSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery) where T : INetworkMessage
            {
            }

            public void OnAfterSendMessage<T>(ulong clientId, ref T message, NetworkDelivery delivery, int messageSizeBytes) where T : INetworkMessage
            {
            }

            public void OnBeforeReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
            {
            }

            public void OnAfterReceiveMessage(ulong senderId, Type messageType, int messageSizeBytes)
            {
            }

            public void OnBeforeSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
            {
            }

            public void OnAfterSendBatch(ulong clientId, int messageCount, int batchSizeInBytes, NetworkDelivery delivery)
            {
            }

            public void OnBeforeReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
            {
            }

            public void OnAfterReceiveBatch(ulong senderId, int messageCount, int batchSizeInBytes)
            {
            }

            public bool OnVerifyCanSend(ulong destinationId, Type messageType, NetworkDelivery delivery)
            {
                return true;
            }

            public bool OnVerifyCanReceive(ulong senderId, Type messageType)
            {
                return true;
            }

            public void OnBeforeHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
            {
            }

            public void OnAfterHandleMessage<T>(ref T message, ref NetworkContext context) where T : INetworkMessage
            {
                if (HandleChecks.ContainsKey(typeof(T)))
                {
                    foreach (var check in HandleChecks[typeof(T)])
                    {
                        if (check.Check(message))
                        {
                            check.Result = true;
                            HandleChecks[typeof(T)].Remove(check);
                            break;
                        }
                    }
                }
            }
        }

        private const string k_FirstPartOfTestRunnerSceneName = "InitTestScene";

        public static List<NetworkManager> NetworkManagerInstances => s_NetworkManagerInstances;

        internal static IntegrationTestSceneHandler ClientSceneHandler = null;

        public enum InstanceTransport
        {
            SIP,
#if UTP_ADAPTER
            UTP
#endif
        }

        /// <summary>
        /// Registers the IntegrationTestSceneHandler for integration tests.
        /// The default client behavior is to not load scenes on the client side.
        /// </summary>
        private static void RegisterSceneManagerHandler(NetworkManager networkManager)
        {
            if (!networkManager.IsServer)
            {
                if (ClientSceneHandler == null)
                {
                    ClientSceneHandler = new IntegrationTestSceneHandler();
                }
                networkManager.SceneManager.SceneManagerHandler = ClientSceneHandler;
            }
        }

        /// <summary>
        /// Call this to clean up the IntegrationTestSceneHandler and destroy the s_CoroutineRunner.
        /// Note:
        /// If deriving from BaseMultiInstanceTest or using MultiInstanceHelpers.Destroy then you
        /// typically won't need to do this.
        /// </summary>
        internal static void CleanUpHandlers()
        {
            if (ClientSceneHandler != null)
            {
                ClientSceneHandler.Dispose();
                ClientSceneHandler = null;
            }

            // Destroy the temporary GameObject used to run co-routines
            if (s_CoroutineRunner != null)
            {
                s_CoroutineRunner.StopAllCoroutines();
                Object.DestroyImmediate(s_CoroutineRunner.gameObject);
            }
        }

        /// <summary>
        /// Call this to register scene validation and the IntegrationTestSceneHandler
        /// Note:
        /// If deriving from BaseMultiInstanceTest or using MultiInstanceHelpers.Destroy then you
        /// typically won't need to call this.
        /// </summary>
        internal static void RegisterHandlers(NetworkManager networkManager, bool serverSideSceneManager = false)
        {
            SceneManagerValidationAndTestRunnerInitialization(networkManager);

            if (!networkManager.IsServer || networkManager.IsServer && serverSideSceneManager)
            {
                RegisterSceneManagerHandler(networkManager);
            }
        }

        /// <summary>
        /// Create the correct NetworkTransport, attach it to the game object and return it.
        /// Default value is SIPTransport.
        /// </summary>
        internal static NetworkTransport CreateInstanceTransport(InstanceTransport instanceTransport, GameObject go)
        {
            switch (instanceTransport)
            {
                case InstanceTransport.SIP:
                default:
                    return go.AddComponent<SIPTransport>();
#if UTP_ADAPTER
                case InstanceTransport.UTP:
                    return go.AddComponent<UnityTransport>();
#endif
            }
        }

        /// <summary>
        /// Creates NetworkingManagers and configures them for use in a multi instance setting.
        /// </summary>
        /// <param name="clientCount">The amount of clients</param>
        /// <param name="server">The server NetworkManager</param>
        /// <param name="clients">The clients NetworkManagers</param>
        /// <param name="targetFrameRate">The targetFrameRate of the Unity engine to use while the multi instance helper is running. Will be reset on shutdown.</param>
        /// <param name="instanceTransport">The NetworkTransport to use for the instances</param>
        public static bool Create(int clientCount, out NetworkManager server, out NetworkManager[] clients, int targetFrameRate = 60, InstanceTransport instanceTransport = InstanceTransport.SIP)
        {
            s_NetworkManagerInstances = new List<NetworkManager>();
            CreateNewClients(clientCount, out clients, instanceTransport);

            // Create gameObject
            var go = new GameObject("NetworkManager - Server");

            // Create networkManager component
            server = go.AddComponent<NetworkManager>();
            NetworkManagerInstances.Insert(0, server);

            // Set the NetworkConfig
            server.NetworkConfig = new NetworkConfig()
            {
                // Set transport
                NetworkTransport = CreateInstanceTransport(instanceTransport, go)
            };

            s_OriginalTargetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = targetFrameRate;

            return true;
        }

        /// <summary>
        /// Used to add a client to the already existing list of clients
        /// </summary>
        /// <param name="clientCount">The amount of clients</param>
        /// <param name="clients"></param>
        /// <returns></returns>
        public static bool CreateNewClients(int clientCount, out NetworkManager[] clients, InstanceTransport instanceTransport = InstanceTransport.SIP)
        {
            clients = new NetworkManager[clientCount];
            var activeSceneName = SceneManager.GetActiveScene().name;
            for (int i = 0; i < clientCount; i++)
            {
                // Create gameObject
                var go = new GameObject("NetworkManager - Client - " + i);
                // Create networkManager component
                clients[i] = go.AddComponent<NetworkManager>();

                // Set the NetworkConfig
                clients[i].NetworkConfig = new NetworkConfig()
                {
                    // Set transport
                    NetworkTransport = CreateInstanceTransport(instanceTransport, go)
                };
            }

            NetworkManagerInstances.AddRange(clients);
            return true;
        }

        /// <summary>
        /// Stops one single client and makes sure to cleanup any static variables in this helper
        /// </summary>
        /// <param name="clientToStop"></param>
        public static void StopOneClient(NetworkManager clientToStop)
        {
            clientToStop.Shutdown();
            s_Hooks.Remove(clientToStop);
            Object.Destroy(clientToStop.gameObject);
            NetworkManagerInstances.Remove(clientToStop);
        }

        /// <summary>
        /// Should always be invoked when finished with a single unit test
        /// (i.e. during TearDown)
        /// </summary>
        public static void Destroy()
        {
            if (s_IsStarted == false)
            {
                return;
            }

            s_IsStarted = false;

            // Shutdown the server which forces clients to disconnect
            foreach (var networkManager in NetworkManagerInstances)
            {
                networkManager.Shutdown();
                s_Hooks.Remove(networkManager);
            }

            // Destroy the network manager instances
            foreach (var networkManager in NetworkManagerInstances)
            {
                Object.DestroyImmediate(networkManager.gameObject);
            }

            NetworkManagerInstances.Clear();

            CleanUpHandlers();

            Application.targetFrameRate = s_OriginalTargetFrameRate;
        }

        /// <summary>
        /// We want to exclude the TestRunner scene on the host-server side so it won't try to tell clients to
        /// synchronize to this scene when they connect
        /// </summary>
        private static bool VerifySceneIsValidForClientsToLoad(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        {
            // exclude test runner scene
            if (sceneName.StartsWith(k_FirstPartOfTestRunnerSceneName))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// This registers scene validation callback for the server to prevent it from telling connecting
        /// clients to synchronize (i.e. load) the test runner scene.  This will also register the test runner
        /// scene and its handle for both client(s) and server-host.
        /// </summary>
        private static void SceneManagerValidationAndTestRunnerInitialization(NetworkManager networkManager)
        {
            // If VerifySceneBeforeLoading is not already set, then go ahead and set it so the host/server
            // will not try to synchronize clients to the TestRunner scene.  We only need to do this for the server.
            if (networkManager.IsServer && networkManager.SceneManager.VerifySceneBeforeLoading == null)
            {
                networkManager.SceneManager.VerifySceneBeforeLoading = VerifySceneIsValidForClientsToLoad;
                // If a unit/integration test does not handle this on their own, then Ignore the validation warning
                networkManager.SceneManager.DisableValidationWarnings(true);
            }

            // Register the test runner scene so it will be able to synchronize NetworkObjects without logging a
            // warning about using the currently active scene
            var scene = SceneManager.GetActiveScene();
            // As long as this is a test runner scene (or most likely a test runner scene)
            if (scene.name.StartsWith(k_FirstPartOfTestRunnerSceneName))
            {
                // Register the test runner scene just so we avoid another warning about not being able to find the
                // scene to synchronize NetworkObjects.  Next, add the currently active test runner scene to the scenes
                // loaded and register the server to client scene handle since host-server shares the test runner scene
                // with the clients.
                networkManager.SceneManager.GetAndAddNewlyLoadedSceneByName(scene.name);
                networkManager.SceneManager.ServerSceneHandleToClientSceneHandle.Add(scene.handle, scene.handle);
            }
        }

        public delegate void BeforeClientStartCallback();

        /// <summary>
        /// Starts NetworkManager instances created by the Create method.
        /// </summary>
        /// <param name="host">Whether or not to create a Host instead of Server</param>
        /// <param name="server">The Server NetworkManager</param>
        /// <param name="clients">The Clients NetworkManager</param>
        /// <param name="callback">called immediately after server is started and before client(s) are started</param>
        /// <returns></returns>
        public static bool Start(bool host, NetworkManager server, NetworkManager[] clients, BeforeClientStartCallback callback = null)
        {
            if (s_IsStarted)
            {
                throw new InvalidOperationException("MultiInstanceHelper already started. Did you forget to Destroy?");
            }

            s_IsStarted = true;
            s_ClientCount = clients.Length;

            if (host)
            {
                server.StartHost();
            }
            else
            {
                server.StartServer();
            }
            var hooks = new MultiInstanceHooks();
            server.MessagingSystem.Hook(hooks);
            s_Hooks[server] = hooks;

            // if set, then invoke this for the server
            RegisterHandlers(server);

            callback?.Invoke();

            if (ClientSceneHandler != null)
            {
                throw new Exception("Some how ClientSceneHandler did not get disposed when Destroy was called?");
            }

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].StartClient();
                hooks = new MultiInstanceHooks();
                clients[i].MessagingSystem.Hook(hooks);
                s_Hooks[clients[i]] = hooks;

                // if set, then invoke this for the client
                RegisterHandlers(clients[i]);
            }
            return true;
        }

        private static CoroutineRunner s_CoroutineRunner;

        /// <summary>
        /// Runs a IEnumerator as a Coroutine on a dummy GameObject. Used to get exceptions coming from the coroutine
        /// </summary>
        /// <param name="enumerator">The IEnumerator to run</param>
        public static Coroutine Run(IEnumerator enumerator)
        {
            if (s_CoroutineRunner == null)
            {
                s_CoroutineRunner = new GameObject(nameof(CoroutineRunner)).AddComponent<CoroutineRunner>();
            }

            return s_CoroutineRunner.StartCoroutine(enumerator);
        }

        public class CoroutineResultWrapper<T>
        {
            public T Result;
        }

        private static uint s_AutoIncrementGlobalObjectIdHashCounter = 111111;

        /// <summary>
        /// Normally we would only allow player prefabs to be set to a prefab. Not runtime created objects.
        /// In order to prevent having a Resource folder full of a TON of prefabs that we have to maintain,
        /// MultiInstanceHelper has a helper function that lets you mark a runtime created object to be
        /// treated as a prefab by the Netcode. That's how we can get away with creating the player prefab
        /// at runtime without it being treated as a SceneObject or causing other conflicts with the Netcode.
        /// </summary>
        /// <param name="networkObject">The networkObject to be treated as Prefab</param>
        /// <param name="globalObjectIdHash">The GlobalObjectId to force</param>
        public static void MakeNetworkObjectTestPrefab(NetworkObject networkObject, uint globalObjectIdHash = default)
        {
            // Override `GlobalObjectIdHash` if `globalObjectIdHash` param is set
            if (globalObjectIdHash != default)
            {
                networkObject.GlobalObjectIdHash = globalObjectIdHash;
            }

            // Fallback to auto-increment if `GlobalObjectIdHash` was never set
            if (networkObject.GlobalObjectIdHash == default)
            {
                networkObject.GlobalObjectIdHash = ++s_AutoIncrementGlobalObjectIdHashCounter;
            }

            // Prevent object from being snapped up as a scene object
            networkObject.IsSceneObject = false;
        }

        // We use GameObject instead of SceneObject to be able to keep hierarchy
        public static void MarkAsSceneObjectRoot(GameObject networkObjectRoot, NetworkManager server, NetworkManager[] clients)
        {
            networkObjectRoot.name += " - Server";

            NetworkObject[] serverNetworkObjects = networkObjectRoot.GetComponentsInChildren<NetworkObject>();

            for (int i = 0; i < serverNetworkObjects.Length; i++)
            {
                serverNetworkObjects[i].NetworkManagerOwner = server;
            }

            for (int i = 0; i < clients.Length; i++)
            {
                GameObject root = Object.Instantiate(networkObjectRoot);
                root.name += " - Client - " + i;

                NetworkObject[] clientNetworkObjects = root.GetComponentsInChildren<NetworkObject>();

                for (int j = 0; j < clientNetworkObjects.Length; j++)
                {
                    clientNetworkObjects[j].NetworkManagerOwner = clients[i];
                }
            }
        }

        /// <summary>
        /// Waits on the client side to be connected.
        /// </summary>
        /// <param name="client">The client</param>
        /// <param name="result">The result. If null, it will automatically assert</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForClientConnected(NetworkManager client, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout)
        {
            yield return WaitForClientsConnected(new NetworkManager[] { client }, result, timeout);
        }

        /// <summary>
        /// Similar to WaitForClientConnected, this waits for multiple clients to be connected.
        /// </summary>
        /// <param name="clients">The clients to be connected</param>
        /// <param name="result">The result. If null, it will automatically assert<</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        /// <returns></returns>
        public static IEnumerator WaitForClientsConnected(NetworkManager[] clients, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout)
        {
            // Make sure none are the host client
            foreach (var client in clients)
            {
                if (client.IsServer)
                {
                    throw new InvalidOperationException("Cannot wait for connected as server");
                }
            }

            var allConnected = true;
            var startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeout)
            {
                allConnected = true;
                foreach (var client in clients)
                {
                    if (!client.IsConnectedClient)
                    {
                        allConnected = false;
                        break;
                    }
                }
                if (allConnected)
                {
                    break;
                }
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            if (result != null)
            {
                result.Result = allConnected;
            }
            else
            {
                for (var i = 0; i < clients.Length; ++i)
                {
                    var client = clients[i];
                    // Logging i+1 because that's the local client ID they'll get (0 is server)
                    // Can't use client.LocalClientId because that doesn't get assigned until IsConnectedClient == true,
                    Assert.True(client.IsConnectedClient, $"Client {i + 1} never connected");
                }
            }
        }

        /// <summary>
        /// Waits on the server side for 1 client to be connected
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="result">The result. If null, it will automatically assert</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForClientConnectedToServer(NetworkManager server, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout)
        {
            yield return WaitForClientsConnectedToServer(server, server.IsHost ? s_ClientCount + 1 : s_ClientCount, result, timeout);
        }

        /// <summary>
        /// Waits on the server side for 1 client to be connected
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="result">The result. If null, it will automatically assert</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForClientsConnectedToServer(NetworkManager server, int clientCount = 1, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout)
        {
            if (!server.IsServer)
            {
                throw new InvalidOperationException("Cannot wait for connected as client");
            }

            var startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeout && server.ConnectedClients.Count != clientCount)
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            var res = server.ConnectedClients.Count == clientCount;

            if (result != null)
            {
                result.Result = res;
            }
            else
            {
                Assert.True(res, "A client never connected to server");
            }
        }

        /// <summary>
        /// Gets a NetworkObject instance as it's represented by a certain peer.
        /// </summary>
        /// <param name="networkObjectId">The networkObjectId to get</param>
        /// <param name="representation">The representation to get the object from</param>
        /// <param name="result">The result</param>
        /// <param name="failIfNull">Whether or not to fail if no object is found and result is null</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator GetNetworkObjectByRepresentation(ulong networkObjectId, NetworkManager representation, CoroutineResultWrapper<NetworkObject> result, bool failIfNull = true, float timeout = DefaultTimeout)
        {
            if (result == null)
            {
                throw new ArgumentNullException("Result cannot be null");
            }

            var startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeout && representation.SpawnManager.SpawnedObjects.All(x => x.Value.NetworkObjectId != networkObjectId))
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            result.Result = representation.SpawnManager.SpawnedObjects.First(x => x.Value.NetworkObjectId == networkObjectId).Value;

            if (failIfNull && result.Result == null)
            {
                Assert.Fail("NetworkObject could not be found");
            }
        }

        /// <summary>
        /// Gets a NetworkObject instance as it's represented by a certain peer.
        /// </summary>
        /// <param name="predicate">The predicate used to filter for your target NetworkObject</param>
        /// <param name="representation">The representation to get the object from</param>
        /// <param name="result">The result</param>
        /// <param name="failIfNull">Whether or not to fail if no object is found and result is null</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator GetNetworkObjectByRepresentation(Func<NetworkObject, bool> predicate, NetworkManager representation, CoroutineResultWrapper<NetworkObject> result, bool failIfNull = true, float timeout = DefaultTimeout)
        {
            if (result == null)
            {
                throw new ArgumentNullException("Result cannot be null");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("Predicate cannot be null");
            }

            var startTime = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - startTime < timeout && !representation.SpawnManager.SpawnedObjects.Any(x => predicate(x.Value)))
            {
                var nextFrameNumber = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameNumber);
            }

            result.Result = representation.SpawnManager.SpawnedObjects.FirstOrDefault(x => predicate(x.Value)).Value;

            if (failIfNull && result.Result == null)
            {
                Assert.Fail("NetworkObject could not be found");
            }
        }

        /// <summary>
        /// Runs some code, then verifies the condition (combines 'Run' and 'WaitForCondition')
        /// </summary>
        /// <param name="workload">Action / code to run</param>
        /// <param name="predicate">The predicate to wait for</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator RunAndWaitForCondition(Action workload, Func<bool> predicate, float timeout = DefaultTimeout, int minFrames = DefaultMinFrames)
        {
            var waitResult = new CoroutineResultWrapper<bool>();
            workload();

            yield return Run(WaitForCondition(
                predicate,
                waitResult,
                timeout: timeout,
                minFrames: minFrames));

            if (!waitResult.Result)
            {
                Assert.Fail("Predicate condition failed");
            }
        }

        /// <summary>
        /// Waits for a predicate condition to be met
        /// </summary>
        /// <param name="predicate">The predicate to wait for</param>
        /// <param name="result">The result. If null, it will fail if the predicate is not met</param>
        /// <param name="minFrames">The min frames to wait for</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForCondition(Func<bool> predicate, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout, int minFrames = DefaultMinFrames)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("Predicate cannot be null");
            }

            var startTime = Time.realtimeSinceStartup;

            if (minFrames > 0)
            {
                yield return new WaitUntil(() =>
                {
                    return Time.frameCount >= minFrames;
                });
            }

            while (Time.realtimeSinceStartup - startTime < timeout &&
                   !predicate())
            {
                // Changed to 2 frames to avoid the scenario where it would take 1+ frames to
                // see a value change (i.e. discovered in the NetworkTransformTests)
                var nextFrameNumber = Time.frameCount + 2;
                yield return new WaitUntil(() =>
                {
                    return Time.frameCount >= nextFrameNumber;
                });
            }

            var res = predicate();

            if (result != null)
            {
                result.Result = res;
            }
            else
            {
                Assert.True(res, "PREDICATE CONDITION");
            }
        }

        /// <summary>
        /// Waits for a message of the given type to be received
        /// </summary>
        /// <param name="result">The result. If null, it will fail if the predicate is not met</param>
        /// <param name="timeout">The max time in seconds to wait for</param>
        internal static IEnumerator WaitForMessageOfType<T>(NetworkManager toBeReceivedBy, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout) where T : INetworkMessage
        {
            var hooks = s_Hooks[toBeReceivedBy];
            if (!hooks.HandleChecks.ContainsKey(typeof(T)))
            {
                hooks.HandleChecks.Add(typeof(T), new List<MessageHandleCheckWithResult>());
            }
            var check = new MessageHandleCheckWithResult { Check = MultiInstanceHooks.CheckForMessageOfType<T> };
            hooks.HandleChecks[typeof(T)].Add(check);
            if (result == null)
            {
                result = new CoroutineResultWrapper<bool>();
            }
            yield return ExecuteWaitForHook(check, result, timeout);

            Assert.True(result.Result, $"Expected message {typeof(T).Name} was not received within {timeout}s.");
        }

        /// <summary>
        /// Waits for a specific message, defined by a user callback, to be received
        /// </summary>
        /// <param name="requirement">Called for each received message to check if it's the right one</param>
        /// <param name="result">The result. If null, it will fail if the predicate is not met</param>
        /// <param name="timeout">The max time in seconds to wait for</param>
        internal static IEnumerator WaitForMessageMeetingRequirement<T>(NetworkManager toBeReceivedBy, MessageHandleCheck requirement, CoroutineResultWrapper<bool> result = null, float timeout = DefaultTimeout)
        {
            var hooks = s_Hooks[toBeReceivedBy];
            if (!hooks.HandleChecks.ContainsKey(typeof(T)))
            {
                hooks.HandleChecks.Add(typeof(T), new List<MessageHandleCheckWithResult>());
            }
            var check = new MessageHandleCheckWithResult { Check = requirement };
            hooks.HandleChecks[typeof(T)].Add(check);
            if (result == null)
            {
                result = new CoroutineResultWrapper<bool>();
            }
            yield return ExecuteWaitForHook(check, result, timeout);

            Assert.True(result.Result, $"Expected message meeting user requirements was not received within {timeout}s.");
        }

        private static IEnumerator ExecuteWaitForHook(MessageHandleCheckWithResult check, CoroutineResultWrapper<bool> result, float timeout)
        {
            var startTime = Time.realtimeSinceStartup;

            while (!check.Result && Time.realtimeSinceStartup - startTime < timeout)
            {
                yield return null;
            }

            var res = check.Result;
            result.Result = res;
        }

        public static IEnumerator RunMultiple(IEnumerable<IEnumerator> waitFor)
        {
            var runningCoroutines = new List<Coroutine>();
            foreach (var enumerator in waitFor)
            {
                runningCoroutines.Add(Run(enumerator));
            }

            foreach (var coroutine in runningCoroutines)
            {
                yield return coroutine;
            }
        }
    }

    // Empty MonoBehaviour that is used for coroutines
    internal class CoroutineRunner : MonoBehaviour
    {
    }
}
