using System;
using System.Collections;
#if NGO_TRANSFORM_DEBUG
using System.Text.RegularExpressions;
#endif
using Unity.Netcode.Components;
using NUnit.Framework;
// using Unity.Netcode.Samples;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Netcode.RuntimeTests
{
    // [TestFixture(true, true)]
    [TestFixture(true, false)]
    // [TestFixture(false, true)]
    [TestFixture(false, false)]
    public class NetworkTransformTests : BaseMultiInstanceTest
    {
        private NetworkObject m_ClientSideClientPlayer;
        private NetworkObject m_ServerSideClientPlayer;

        private readonly bool m_TestWithClientNetworkTransform;

        private readonly bool m_TestWithHost;

        public NetworkTransformTests(bool testWithHost, bool testWithClientNetworkTransform)
        {
            m_TestWithHost = testWithHost; // from test fixture
            m_TestWithClientNetworkTransform = testWithClientNetworkTransform;
        }

        protected override int NbClients => 1;

        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return StartSomeClientsAndServerWithPlayers(useHost: m_TestWithHost, nbClients: NbClients, updatePlayerPrefab: playerPrefab =>
            {
                if (m_TestWithClientNetworkTransform)
                {
                    // playerPrefab.AddComponent<ClientNetworkTransform>();
                }
                else
                {
                    playerPrefab.AddComponent<NetworkTransform>();
                }
            });

#if NGO_TRANSFORM_DEBUG
            // Log assert for writing without authority is a developer log...
            // TODO: This is why monolithic test base classes and test helpers are an anti-pattern - this is part of an individual test case setup but is separated from the code verifying it!
            m_ServerNetworkManager.LogLevel = LogLevel.Developer;
            m_ClientNetworkManagers[0].LogLevel = LogLevel.Developer;
#endif

            // This is the *SERVER VERSION* of the *CLIENT PLAYER*
            var serverClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ServerNetworkManager, serverClientPlayerResult));

            // This is the *CLIENT VERSION* of the *CLIENT PLAYER*
            var clientClientPlayerResult = new MultiInstanceHelpers.CoroutineResultWrapper<NetworkObject>();
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.GetNetworkObjectByRepresentation(x => x.IsPlayerObject && x.OwnerClientId == m_ClientNetworkManagers[0].LocalClientId, m_ClientNetworkManagers[0], clientClientPlayerResult));

            m_ServerSideClientPlayer = serverClientPlayerResult.Result;
            m_ClientSideClientPlayer = clientClientPlayerResult.Result;
        }

        // TODO: rewrite after perms & authority changes
        [UnityTest]
        public IEnumerator TestAuthoritativeTransformChangeOneAtATime([Values] bool testLocalTransform)
        {
            var waitResult = new MultiInstanceHelpers.CoroutineResultWrapper<bool>();

            NetworkTransform authoritativeNetworkTransform;
            NetworkTransform otherSideNetworkTransform;
            // if (m_TestWithClientNetworkTransform)
            // {
            //     // client auth net transform can write from client, not from server
            //     otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //     authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            // }
            // else
            {
                // server auth net transform can't write from client, not from client
                authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            }
            Assert.That(!otherSideNetworkTransform.CanCommitToTransform);
            Assert.That(authoritativeNetworkTransform.CanCommitToTransform);

            authoritativeNetworkTransform.Interpolate = false;
            otherSideNetworkTransform.Interpolate = false;

            if (authoritativeNetworkTransform.CanCommitToTransform)
            {
                authoritativeNetworkTransform.InLocalSpace = testLocalTransform;
            }

            if (otherSideNetworkTransform.CanCommitToTransform)
            {
                otherSideNetworkTransform.InLocalSpace = testLocalTransform;
            }

            float approximation = 0.05f;

            // test position
            var authPlayerTransform = authoritativeNetworkTransform.transform;
            authPlayerTransform.position = new Vector3(10, 20, 30);
            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "server side pos should be zero at first"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.position.x > approximation, waitResult, maxFrames: 120));
            if (!waitResult.Result)
            {
                throw new Exception("timeout while waiting for position change");
            }
            Assert.True(new Vector3(10, 20, 30) == otherSideNetworkTransform.transform.position, $"wrong position on ghost, {otherSideNetworkTransform.transform.position}"); // Vector3 already does float approximation with ==

            // test rotation
            authPlayerTransform.rotation = Quaternion.Euler(45, 40, 35); // using euler angles instead of quaternions directly to really see issues users might encounter
            Assert.AreEqual(Quaternion.identity, otherSideNetworkTransform.transform.rotation, "wrong initial value for rotation"); // sanity check
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.rotation.eulerAngles.x > approximation, waitResult, maxFrames: 120));
            if (!waitResult.Result)
            {
                throw new Exception("timeout while waiting for rotation change");
            }
            // approximation needed here since eulerAngles isn't super precise.
            Assert.LessOrEqual(Math.Abs(45 - otherSideNetworkTransform.transform.rotation.eulerAngles.x), approximation, $"wrong rotation on ghost on x, got {otherSideNetworkTransform.transform.rotation.eulerAngles.x}");
            Assert.LessOrEqual(Math.Abs(40 - otherSideNetworkTransform.transform.rotation.eulerAngles.y), approximation, $"wrong rotation on ghost on y, got {otherSideNetworkTransform.transform.rotation.eulerAngles.y}");
            Assert.LessOrEqual(Math.Abs(35 - otherSideNetworkTransform.transform.rotation.eulerAngles.z), approximation, $"wrong rotation on ghost on z, got {otherSideNetworkTransform.transform.rotation.eulerAngles.z}");

            // test scale
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.x, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.y, "wrong initial value for scale"); // sanity check
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(1f, otherSideNetworkTransform.transform.lossyScale.z, "wrong initial value for scale"); // sanity check
            authPlayerTransform.localScale = new Vector3(2, 3, 4);
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForCondition(() => otherSideNetworkTransform.transform.lossyScale.x > 1f + approximation, waitResult, maxFrames: 120));
            if (!waitResult.Result)
            {
                throw new Exception("timeout while waiting for scale change");
            }
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(2f, otherSideNetworkTransform.transform.lossyScale.x, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(3f, otherSideNetworkTransform.transform.lossyScale.y, "wrong scale on ghost");
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(4f, otherSideNetworkTransform.transform.lossyScale.z, "wrong scale on ghost");

            // todo reparent and test
            // todo test all public API
        }

        [UnityTest]
        // [Ignore("skipping for now, still need to figure weird multiinstance issue with hosts")]
        public IEnumerator TestCantChangeTransformFromOtherSideAuthority([Values] bool testClientAuthority)
        {
            // test server can't change client authoritative transform
            NetworkTransform authoritativeNetworkTransform;
            NetworkTransform otherSideNetworkTransform;

            // if (m_TestWithClientNetworkTransform)
            // {
            //     // client auth net transform can write from client, not from server
            //     otherSideNetworkTransform = m_ServerSideClientPlayer.GetComponent<ClientNetworkTransform>();
            //     authoritativeNetworkTransform = m_ClientSideClientPlayer.GetComponent<ClientNetworkTransform>();
            // }
            // else
            {
                // server auth net transform can't write from client, not from client
                authoritativeNetworkTransform = m_ServerSideClientPlayer.GetComponent<NetworkTransform>();
                otherSideNetworkTransform = m_ClientSideClientPlayer.GetComponent<NetworkTransform>();
            }

            authoritativeNetworkTransform.Interpolate = false;
            otherSideNetworkTransform.Interpolate = false;

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "other side pos should be zero at first"); // sanity check
            otherSideNetworkTransform.transform.position = new Vector3(4, 5, 6);

            yield return null; // one frame

            Assert.AreEqual(Vector3.zero, otherSideNetworkTransform.transform.position, "got authority error, but other side still moved!");
#if NGO_TRANSFORM_DEBUG
            // We are no longer emitting this warning, and we are banishing tests that rely on console output, so
            //  needs re-implementation
            // TODO: This should be a separate test - verify 1 behavior per test
            LogAssert.Expect(LogType.Warning, new Regex(".*without authority detected.*"));
#endif
        }

        /*
        * ownership change
        * test teleport with interpolation
        * test teleport without interpolation
        * test dynamic spawning -- done with NetworkTransformRespawnTests
        */

        [UnityTearDown]
        public override IEnumerator Teardown()
        {
            yield return base.Teardown();
            Object.DestroyImmediate(m_PlayerPrefab);
        }
    }

    /// <summary>
    /// This test simulates a pooled NetworkObject being re-used over time with a NetworkTransform
    /// This test validates that pooled NetworkObjects' NetworkTransforms are completely reset in
    /// order to properly start interpolating from the new spawn position and not the previous position
    /// when the registered Network Prefab was despawned.  This specifically tests the client side.
    /// </summary>
    public class NetworkTransformRespawnTests : BaseMultiInstanceTest
    {
        /// <summary>
        /// Our test object mover NetworkBehaviour
        /// </summary>
        public class DynamicObjectMover : NetworkBehaviour, INetworkPrefabInstanceHandler
        {
            private Rigidbody m_Rigidbody;
            private Vector3 m_MoveTowardsPosition = new Vector3(20, 0, 20);

            public Action<NetworkObject> OnInstantiate;
            public Action<NetworkObject> OnDestroyDespawn;



            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                transform.position = position;
                transform.rotation = rotation;
                gameObject.SetActive(true);
                OnInstantiate?.Invoke(NetworkObject);
                return NetworkObject;
            }

            public Vector3 GetFirstInterpolatedPosition()
            {
                return GetComponent<NetworkTransform>().GetInterpolatedPosition();
            }

            public void Destroy(NetworkObject networkObject)
            {
                Assert.AreEqual(networkObject, NetworkObject);
                OnDestroyDespawn?.Invoke(NetworkObject);
                gameObject.SetActive(false);
            }


            private void FixedUpdate()
            {
                if (!IsSpawned || !IsServer)
                {
                    return;
                }

                if (m_Rigidbody == null)
                {
                    m_Rigidbody = GetComponent<Rigidbody>();
                }
                if (m_Rigidbody != null)
                {
                    m_Rigidbody.MovePosition(transform.position + (m_MoveTowardsPosition * Time.fixedDeltaTime));
                }
            }
        }

        protected override int NbClients => 1;
        private GameObject m_ObjectToSpawn;
        private GameObject m_ClientSideObject;
        private NetworkObject m_DefaultNetworkObject;
        private Vector3 m_LastClientSidePosition;
        private bool m_ClientSideSpawned;

        private void OnClientInstantiate(NetworkObject networkObject)
        {
            m_ClientSideSpawned = true;
        }

        private void OnClientDestroy(NetworkObject networkObject)
        {
            m_ClientSideSpawned = false;
            m_LastClientSidePosition = networkObject.transform.position;
        }

        public override IEnumerator Setup()
        {
            m_BypassStartAndWaitForClients = true;
            yield return StartSomeClientsAndServerWithPlayers(true, NbClients);

            m_ObjectToSpawn = new GameObject("NetworkTransformDynamicObject");
            m_DefaultNetworkObject = m_ObjectToSpawn.AddComponent<NetworkObject>();
            m_ObjectToSpawn.AddComponent<NetworkTransform>();
            var rigidBody = m_ObjectToSpawn.AddComponent<Rigidbody>();
            rigidBody.useGravity = false;
            m_ObjectToSpawn.AddComponent<NetworkRigidbody>();
            var serverDynamicObjectMover = m_ObjectToSpawn.AddComponent<DynamicObjectMover>();
            MultiInstanceHelpers.MakeNetworkObjectTestPrefab(m_DefaultNetworkObject);

            var networkPrefab = new NetworkPrefab();
            networkPrefab.Prefab = m_ObjectToSpawn;
            m_ServerNetworkManager.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            m_ServerNetworkManager.NetworkConfig.EnableSceneManagement = false;
            m_DefaultNetworkObject.NetworkManagerOwner = m_ServerNetworkManager;
            m_ServerNetworkManager.PrefabHandler.AddHandler(m_ObjectToSpawn, serverDynamicObjectMover);

            m_ClientSideObject = Object.Instantiate(m_ObjectToSpawn);
            var clientDynamicObjectMover = m_ClientSideObject.GetComponent<DynamicObjectMover>();
            clientDynamicObjectMover.OnInstantiate += OnClientInstantiate;
            clientDynamicObjectMover.OnDestroyDespawn += OnClientDestroy;

            var client = m_ClientNetworkManagers[0];
            client.NetworkConfig.NetworkPrefabs.Add(networkPrefab);
            client.NetworkConfig.EnableSceneManagement = false;
            // Add a client side prefab handler for this NetworkObject
            client.PrefabHandler.AddHandler(m_ClientSideObject, clientDynamicObjectMover);
            m_ClientSideObject.SetActive(false);
            m_DefaultNetworkObject.gameObject.SetActive(false);
        }


        [UnityTest]
        public IEnumerator RespawnedPositionTest()
        {
            if (!MultiInstanceHelpers.Start(true, m_ServerNetworkManager, m_ClientNetworkManagers))
            {
                Debug.LogError("Failed to start instances");
                Assert.Fail("Failed to start instances");
            }

            // Wait for connection on client side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnected(m_ClientNetworkManagers));

            // Wait for connection on server side
            yield return MultiInstanceHelpers.Run(MultiInstanceHelpers.WaitForClientsConnectedToServer(m_ServerNetworkManager, NbClients + 1));
#if RESPAWNPOSITION_STRESS_TEST
            for (int i = 0; i < 100; i++)
            {
#endif
                Assert.True(m_ObjectToSpawn != null);
                Assert.True(m_DefaultNetworkObject != null);
                m_DefaultNetworkObject.gameObject.SetActive(true);
                m_DefaultNetworkObject.transform.position = Vector3.zero;
                m_DefaultNetworkObject.Spawn();
                yield return new WaitUntil(() => m_ClientSideSpawned);
                // Let the object move a bit
                yield return WaitForFrames(20);

                // Make sure it moved on the client side
                Assert.IsTrue(m_ClientSideObject.transform.position != Vector3.zero);

                // Spawn Test #1: Despawn
                m_ServerNetworkManager.SpawnManager.DespawnObject(m_DefaultNetworkObject);
                yield return new WaitUntil(() => !m_ClientSideSpawned);
                yield return new WaitForEndOfFrame();

                // Spawn Test #1: Verify the fix works
                m_DefaultNetworkObject.transform.position = Vector3.zero;
                m_DefaultNetworkObject.gameObject.SetActive(true);
                m_DefaultNetworkObject.Spawn();
                var clientDynamicObjectMover = m_ClientSideObject.GetComponent<DynamicObjectMover>();

                yield return new WaitUntil(() => m_ClientSideSpawned);

                var firstClientInterpolatedPosition = clientDynamicObjectMover.GetFirstInterpolatedPosition();
                Assert.IsFalse(firstClientInterpolatedPosition.magnitude > 2.0f);
                Debug.Log($"[Verify With Fix] First client interpolated position: {firstClientInterpolatedPosition}");

                yield return WaitForFrames(20);
                Assert.IsTrue(m_LastClientSidePosition != Vector3.zero);

                // Spawn Test #2: Despawn
                m_DefaultNetworkObject.GetComponent<NetworkTransform>().EnableDeferredClientInit = false;
                m_ServerNetworkManager.SpawnManager.DespawnObject(m_DefaultNetworkObject);
                var serverSidePositionAfterDespawn = m_DefaultNetworkObject.transform.position;

                yield return new WaitUntil(() => !m_ClientSideSpawned);

                // Spawn Test #2: Verify that this breaks without the fix
                m_ClientSideObject.GetComponent<NetworkTransform>().EnableDeferredClientInit = false;

                m_DefaultNetworkObject.transform.position = Vector3.zero;
                m_DefaultNetworkObject.gameObject.SetActive(true);
                m_DefaultNetworkObject.Spawn();

                yield return new WaitUntil(() => m_ClientSideSpawned);
                firstClientInterpolatedPosition = clientDynamicObjectMover.GetFirstInterpolatedPosition();
                Debug.Log($"[Without Fix] First client interpolated position: {firstClientInterpolatedPosition}");
                // At this point (*** the bug ***) the client should have close to the previous frame's position plus any delta sent from the server.
                // This should be a non-zero value on the client side
                Assert.IsTrue(firstClientInterpolatedPosition.magnitude > 2.0f);
                yield return new WaitForEndOfFrame();

                // The client side position magnitude should be greater than the current server-side position magnitude
                // This means the client-side would be interpolating from the last despawn position to the new server-side position
                Assert.IsTrue(firstClientInterpolatedPosition.magnitude > m_DefaultNetworkObject.transform.position.magnitude);

                // Done
                m_DefaultNetworkObject.Despawn();

#if RESPAWNPOSITION_STRESS_TEST
                yield return new WaitUntil(() => !m_ClientSideSpawned);
                m_ClientSideObject.GetComponent<NetworkTransform>().EnableDeferredClientInit = true;
                m_DefaultNetworkObject.GetComponent<NetworkTransform>().EnableDeferredClientInit = true;
            }
#endif
        }

        /// <summary>
        /// Waits for number of frames to pass
        /// </summary>
        /// <param name="framesToWait"></param>
        private IEnumerator WaitForFrames(int framesToWait)
        {
            var frameCountToWaitFor = Time.frameCount + framesToWait;
            var timeStarted = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => frameCountToWaitFor <= Time.frameCount);
        }

        public override IEnumerator Teardown()
        {
            if (m_ClientSideObject != null)
            {
                Object.Destroy(m_ClientSideObject);
                m_ClientSideObject = null;
            }

            if (m_ObjectToSpawn != null)
            {
                Object.Destroy(m_ObjectToSpawn);
                m_ObjectToSpawn = null;
            }

            return base.Teardown();
        }
    }
}
