using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI.Configuration;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLAPI.RuntimeTests
{
    internal static class MultiInstanceHelpers
    {
        public static bool Create(int clientCount, out NetworkManager server, out NetworkManager[] clients)
        {
            clients = new NetworkManager[clientCount];

            for (int i = 0; i < clientCount; i++)
            {
                // Create gameObject
                GameObject go = new GameObject("NetworkManager - Client - " + i);

                // Create networkManager component
                clients[i] = go.AddComponent<NetworkManager>();

                // Set config
                clients[i].NetworkConfig = new NetworkConfig()
                {
                    // Set the current scene to prevent unexpected log messages which would trigger a failure
                    RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name },
                    // Set transport
                    NetworkTransport = go.AddComponent<SIPTransport>()
                };
            }

            {
                // Create gameObject
                GameObject go = new GameObject("NetworkManager - Server");

                // Create networkManager component
                server = go.AddComponent<NetworkManager>();

                // Set config
                server.NetworkConfig = new NetworkConfig()
                {
                    // Set the current scene to prevent unexpected log messages which would trigger a failure
                    RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name },
                    // Set transport
                    NetworkTransport = go.AddComponent<SIPTransport>()
                };
            }

            return true;
        }

        public static bool Start(bool host, NetworkManager server, NetworkManager[] clients)
        {
            if (host)
            {
                server.StartHost();
            }
            else
            {
                server.StartClient();
            }

            for (int i = 0; i < clients.Length; i++)
            {
                clients[i].StartClient();
            }

            return true;
        }

        // Empty MonoBehaviour that is a holder of coroutine
        private class CoroutineRunner : MonoBehaviour
        {
        }

        private static CoroutineRunner s_CoroutineRunner;

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

        public static void MakeNetworkedObjectTestPrefab(NetworkObject networkObject, uint globalObjectIdHash = default)
        {
            // Set a globalObjectId for prefab
            if (globalObjectIdHash != default)
            {
                networkObject.TempGlobalObjectIdHashOverride = globalObjectIdHash;
            }

            // Force generation
            networkObject.GenerateGlobalObjectIdHash();

            // Prevent object from being snapped up as a scene object
            networkObject.IsSceneObject = false;
        }

        public static IEnumerator WaitForClientConnected(NetworkManager client, CoroutineResultWrapper<bool> result = null, int maxFrames = 64)
        {
            if (client.IsServer)
            {
                throw new InvalidOperationException("Cannot wait for connected as server");
            }

            int startFrame = Time.frameCount;

            while (Time.frameCount - startFrame <= maxFrames && !client.IsConnectedClient)
            {
                int nextFrameId = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameId);
            }

            bool res = client.IsConnectedClient;

            if (result != null)
            {
                result.Result = res;
            }
            else
            {
                Assert.True(res, "Client never connected");
            }
        }

        public static IEnumerator WaitForClientConnectedToServer(NetworkManager server, CoroutineResultWrapper<bool> result = null, int maxFrames = 64)
        {
            if (!server.IsServer)
            {
                throw new InvalidOperationException("Cannot wait for connected as client");
            }

            int startFrame = Time.frameCount;

            while (Time.frameCount - startFrame <= maxFrames && server.ConnectedClients.Count != (server.IsHost ? 2 : 1))
            {
                int nextFrameId = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameId);
            }

            bool res = server.ConnectedClients.Count == (server.IsHost ? 2 : 1);

            if (result != null)
            {
                result.Result = res;
            }
            else
            {
                Assert.True(res, "Client never connected to server");
            }
        }

        public static IEnumerator GetNetworkObjectByRepresentation(ulong networkObjectId, NetworkManager representation, CoroutineResultWrapper<NetworkObject> result, bool failIfNull = true, int maxFrames = 64)
        {
            if (result == null)
            {
                throw new ArgumentNullException("Result cannot be null");
            }

            int startFrame = Time.frameCount;

            while (Time.frameCount - startFrame <= maxFrames && representation.SpawnManager.SpawnedObjects.All(x => x.Value.NetworkObjectId != networkObjectId))
            {
                int nextFrameId = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameId);
            }

            result.Result = representation.SpawnManager.SpawnedObjects.First(x => x.Value.NetworkObjectId == networkObjectId).Value;

            if (failIfNull && result.Result == null)
            {
                Assert.Fail("NetworkObject could not be found");
            }
        }

        public static IEnumerator GetNetworkObjectByRepresentation(Func<NetworkObject, bool> predicate, NetworkManager representation, CoroutineResultWrapper<NetworkObject> result, bool failIfNull = true, int maxFrames = 64)
        {
            if (result == null)
            {
                throw new ArgumentNullException("Result cannot be null");
            }

            if (predicate == null)
            {
                throw new ArgumentNullException("Predicate cannot be null");
            }

            int startFrame = Time.frameCount;

            while (Time.frameCount - startFrame <= maxFrames && !representation.SpawnManager.SpawnedObjects.Any(x => predicate(x.Value)))
            {
                int nextFrameId = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameId);
            }

            result.Result = representation.SpawnManager.SpawnedObjects.FirstOrDefault(x => predicate(x.Value)).Value;

            if (failIfNull && result.Result == null)
            {
                Assert.Fail("NetworkObject could not be found");
            }
        }

        public static IEnumerator WaitForCondition(Func<bool> predicate, CoroutineResultWrapper<bool> result = null, int maxFrames = 64)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("Predicate cannot be null");
            }

            int startFrame = Time.frameCount;

            while (Time.frameCount - startFrame <= maxFrames && !predicate())
            {
                int nextFrameId = Time.frameCount + 1;
                yield return new WaitUntil(() => Time.frameCount >= nextFrameId);
            }

            bool res = predicate();

            if (result != null)
            {
                result.Result = res;
            }
            else
            {
                Assert.True(res, "PREDICATE CONDITION");
            }
        }
    }
}
