using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Netcode.TestHelpers.Runtime
{
    /// <summary>
    /// Provides helpers for running multi instance tests.
    /// </summary>
    public static class NetcodeIntegrationTestHelpers
    {
        public const int DefaultMinFrames = 1;
        public const int DefaultMaxFrames = 64;
        private static List<NetworkManager> s_NetworkManagerInstances = new List<NetworkManager>();
        private static Dictionary<NetworkManager, MultiInstanceHooks> s_Hooks = new Dictionary<NetworkManager, MultiInstanceHooks>();
        private static bool s_IsStarted;
        private static int s_ClientCount;
        private static int s_OriginalTargetFrameRate = -1;

        public delegate bool MessageReceiptCheck(object receivedMessage);

        private class MultiInstanceHooks : INetworkHooks
        {
            public bool IsWaiting;

            public MessageReceiptCheck ReceiptCheck;

            public static bool CheckForMessageOfType<T>(object receivedMessage) where T : INetworkMessage
            {
                return receivedMessage is T;
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
                if (IsWaiting && (ReceiptCheck == null || ReceiptCheck.Invoke(message)))
                {
                    IsWaiting = false;
                }
            }
        }

        private const string k_FirstPartOfTestRunnerSceneName = "InitTestScene";

        public static List<NetworkManager> NetworkManagerInstances => s_NetworkManagerInstances;

        public enum InstanceTransport
        {
            SIP,
#if UTP_ADAPTER
            UTP
#endif
        }

        internal static IntegrationTestSceneHandler ClientSceneHandler = null;

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
        /// If deriving from <see cref="NetcodeIntegrationTest"/> or using <see cref="Destroy"/> then you
        /// typically won't need to do this.
        /// </summary>
        public static void CleanUpHandlers()
        {
            if (ClientSceneHandler != null)
            {
                ClientSceneHandler.Dispose();
                ClientSceneHandler = null;
            }
        }

        /// <summary>
        /// Call this to register scene validation and the IntegrationTestSceneHandler
        /// Note:
        /// If deriving from <see cref="NetcodeIntegrationTest"/> or using <see cref="Destroy"/> then you
        /// typically won't need to call this.
        /// </summary>
        public static void RegisterHandlers(NetworkManager networkManager, bool serverSideSceneManager = false)
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
                throw new InvalidOperationException($"{nameof(NetcodeIntegrationTestHelpers)} already thinks it is started. Did you forget to Destroy?");
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

        /// <summary>
        /// Used to return a value of type T from a wait condition
        /// </summary>
        public class ResultWrapper<T>
        {
            public T Result;
        }

        private static uint s_AutoIncrementGlobalObjectIdHashCounter = 111111;

        public static uint GetNextGlobalIdHashValue()
        {
            return ++s_AutoIncrementGlobalObjectIdHashCounter;
        }


        public static bool IsNetcodeIntegrationTestRunning { get; internal set; }
        public static void RegisterNetcodeIntegrationTest(bool registered)
        {
            IsNetcodeIntegrationTestRunning = registered;
        }


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

            // To avoid issues with integration tests that forget to clean up,
            // this feature only works with NetcodeIntegrationTest derived classes
            if (IsNetcodeIntegrationTestRunning)
            {
                // Add the object identifier component
                networkObject.gameObject.AddComponent<ObjectNameIdentifier>();
            }
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
        public static IEnumerator WaitForClientConnected(NetworkManager client, ResultWrapper<bool> result = null, int maxFrames = DefaultMaxFrames)
        {
            yield return WaitForClientsConnected(new NetworkManager[] { client }, result, maxFrames);
        }

        /// <summary>
        /// Similar to WaitForClientConnected, this waits for multiple clients to be connected.
        /// </summary>
        /// <param name="clients">The clients to be connected</param>
        /// <param name="result">The result. If null, it will automatically assert<</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForClientsConnected(NetworkManager[] clients, ResultWrapper<bool> result = null, int maxFrames = DefaultMaxFrames)
        {
            // Make sure none are the host client
            foreach (var client in clients)
            {
                if (client.IsServer)
                {
                    throw new InvalidOperationException("Cannot wait for connected as server");
                }
            }

            var startFrameNumber = Time.frameCount;
            var allConnected = true;
            while (Time.frameCount - startFrameNumber <= maxFrames)
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
        public static IEnumerator WaitForClientConnectedToServer(NetworkManager server, ResultWrapper<bool> result = null, int maxFrames = DefaultMaxFrames)
        {
            yield return WaitForClientsConnectedToServer(server, server.IsHost ? s_ClientCount + 1 : s_ClientCount, result, maxFrames);
        }

        /// <summary>
        /// Waits on the server side for 1 client to be connected
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="result">The result. If null, it will automatically assert</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForClientsConnectedToServer(NetworkManager server, int clientCount = 1, ResultWrapper<bool> result = null, int maxFrames = DefaultMaxFrames)
        {
            if (!server.IsServer)
            {
                throw new InvalidOperationException("Cannot wait for connected as client");
            }

            var startFrameNumber = Time.frameCount;

            while (Time.frameCount - startFrameNumber <= maxFrames && server.ConnectedClients.Count != clientCount)
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
        public static IEnumerator GetNetworkObjectByRepresentation(ulong networkObjectId, NetworkManager representation, ResultWrapper<NetworkObject> result, bool failIfNull = true, int maxFrames = DefaultMaxFrames)
        {
            if (result == null)
            {
                throw new ArgumentNullException("Result cannot be null");
            }

            var startFrameNumber = Time.frameCount;

            while (Time.frameCount - startFrameNumber <= maxFrames && representation.SpawnManager.SpawnedObjects.All(x => x.Value.NetworkObjectId != networkObjectId))
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
        public static IEnumerator GetNetworkObjectByRepresentation(Func<NetworkObject, bool> predicate, NetworkManager representation, ResultWrapper<NetworkObject> result, bool failIfNull = true, int maxFrames = DefaultMaxFrames)
        {
            if (result == null)
            {
                throw new ArgumentNullException("Result cannot be null");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("Predicate cannot be null");
            }

            var startFrame = Time.frameCount;

            while (Time.frameCount - startFrame <= maxFrames && !representation.SpawnManager.SpawnedObjects.Any(x => predicate(x.Value)))
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
        /// Waits for a predicate condition to be met
        /// </summary>
        /// <param name="predicate">The predicate to wait for</param>
        /// <param name="result">The result. If null, it will fail if the predicate is not met</param>
        /// <param name="minFrames">The min frames to wait for</param>
        /// <param name="maxFrames">The max frames to wait for</param>
        public static IEnumerator WaitForCondition(Func<bool> predicate, CoroutineResultWrapper<bool> result = null, int maxFrames = DefaultMaxFrames, int minFrames = DefaultMinFrames)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("Predicate cannot be null");
            }

            var startFrameNumber = Time.frameCount;

            if (minFrames > 0)
            {
                yield return new WaitUntil(() =>
                {
                    return Time.frameCount >= minFrames;
                });
            }

            while (Time.frameCount - startFrameNumber <= maxFrames &&
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
        internal static IEnumerator WaitForMessageOfType<T>(NetworkManager toBeReceivedBy, CoroutineResultWrapper<bool> result = null, float timeout = 0.5f) where T : INetworkMessage
        {
            var hooks = s_Hooks[toBeReceivedBy];
            hooks.ReceiptCheck = MultiInstanceHooks.CheckForMessageOfType<T>;
            if (result == null)
            {
                result = new CoroutineResultWrapper<bool>();
            }
            yield return ExecuteWaitForHook(hooks, result, timeout);

            Assert.True(result.Result, $"Expected message {typeof(T).Name} was not received within {timeout}s.");
        }

        /// <summary>
        /// Waits for a specific message, defined by a user callback, to be received
        /// </summary>
        /// <param name="requirement">Called for each received message to check if it's the right one</param>
        /// <param name="result">The result. If null, it will fail if the predicate is not met</param>
        /// <param name="timeout">The max time in seconds to wait for</param>
        internal static IEnumerator WaitForMessageMeetingRequirement(NetworkManager toBeReceivedBy, MessageReceiptCheck requirement, CoroutineResultWrapper<bool> result = null, float timeout = 0.5f)
        {
            var hooks = s_Hooks[toBeReceivedBy];
            hooks.ReceiptCheck = requirement;
            if (result == null)
            {
                result = new CoroutineResultWrapper<bool>();
            }
            yield return ExecuteWaitForHook(hooks, result, timeout);

            Assert.True(result.Result, $"Expected message meeting user requirements was not received within {timeout}s.");
        }

        private static IEnumerator ExecuteWaitForHook(MultiInstanceHooks hooks, CoroutineResultWrapper<bool> result, float timeout)
        {
            hooks.IsWaiting = true;

            var startTime = Time.realtimeSinceStartup;

            while (hooks.IsWaiting && Time.realtimeSinceStartup - startTime < timeout)
            {
                yield return null;
            }

            var res = !hooks.IsWaiting;
            hooks.IsWaiting = false;
            hooks.ReceiptCheck = null;
            result.Result = res;
        }
    }

    // Empty MonoBehaviour that is a holder of coroutine
    internal class CoroutineRunner : MonoBehaviour
    {
    }
}
