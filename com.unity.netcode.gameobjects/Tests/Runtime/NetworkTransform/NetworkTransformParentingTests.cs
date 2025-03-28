using System.Collections;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    internal class NetworkTransformParentingTests : IntegrationTestWithApproximation
    {
        /// <summary>
        /// A NetworkBehaviour that moves in space.
        /// When spawned on the client, an RPC is sent to the server to spawn a player object for that client.
        /// The server parents the player object to the spawner object. This gives a moving parent object and a non-moving child object.
        /// The child object should always be at {0,0,0} local space, while the parent object moves around.
        /// This NetworkBehaviour tests that parenting to a moving object works as expected.
        /// </summary>
        internal class PlayerSpawner : NetworkBehaviour
        {
            /// <summary>
            /// Prefab for the player
            /// </summary>
            public NetworkObject PlayerPrefab;

            /// <summary>
            /// The server side NetworkObject that was spawned when the client connected.
            /// </summary>
            public NetworkObject SpawnedPlayer;

            /// <summary>
            /// Represents the different movement states of the PlayerSpawner during the test lifecycle.
            /// </summary>
            public enum MoveState
            {
                // Initial state, PlayerSpawner will move without counting frames
                NotStarted,
                // The player object has been spawned, start counting frames
                PlayerSpawned,
                // We have moved far enough to test location
                ReachedPeak,
            }
            public MoveState State = MoveState.NotStarted;

            // A count of the number of updates since the player object was spawned.
            private int m_Count;

            // Movement offsets and targets.
            private const float k_PositionOffset = 5.0f;
            private const float k_RotationOffset = 25.0f;
            private readonly Vector3 m_PositionTarget = Vector3.one * k_PositionOffset * 10;
            private readonly Vector3 m_RotationTarget = Vector3.one * k_RotationOffset * 10;

            private void Update()
            {
                if (!IsServer)
                {
                    return;
                }

                transform.position = Vector3.Lerp(transform.position, m_PositionTarget, Time.deltaTime * 2);
                var rotation = transform.rotation;
                rotation.eulerAngles = Vector3.Slerp(rotation.eulerAngles, m_RotationTarget, Time.deltaTime * 2);
                transform.rotation = rotation;

                if (State != MoveState.PlayerSpawned)
                {
                    return;
                }

                // Move self for some time after player object is spawned
                // This ensures the parent object is moving throughout the spawn process.
                m_Count++;
                if (m_Count > 10)
                {
                    // Mark PlayerSpawner as having moved far enough to test.
                    State = MoveState.ReachedPeak;
                }
            }

            public override void OnNetworkSpawn()
            {
                if (IsOwner)
                {
                    // Owner initialises PlayerSpawner movement on spawn
                    transform.position = Vector3.one * k_PositionOffset;
                    var rotation = transform.rotation;
                    rotation.eulerAngles = Vector3.one * k_RotationOffset;
                    transform.rotation = rotation;
                }
                else
                {
                    // When spawned on a client, send the RPC to spawn the player object
                    // Using an RPC ensures the PlayerSpawner is moving for the entire spawning of the player object.
                    RequestPlayerObjectSpawnServerRpc();
                }
            }

            /// <summary>
            /// A ServerRpc that requests the server to spawn a player object for the client that invoked this RPC.
            /// </summary>
            /// <param name="rpcParams">Parameters for the ServerRpc, including the sender's client ID.</param>
            [ServerRpc(RequireOwnership = false)]
            private void RequestPlayerObjectSpawnServerRpc(ServerRpcParams rpcParams = default)
            {
                SpawnedPlayer = Instantiate(PlayerPrefab);
                SpawnedPlayer.SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
                SpawnedPlayer.TrySetParent(NetworkObject, false);
                State = MoveState.PlayerSpawned;
            }
        }

        // Client Authoritative NetworkTransform
        internal class ClientNetworkTransform : NetworkTransform
        {
            protected override bool OnIsServerAuthoritative()
            {
                return false;
            }
        }

        // Don't start with any clients, we will manually spawn a client inside the test
        protected override int NumberOfClients => 0;

        // Parent prefab with moving PlayerSpawner which will spawn the childPrefab
        private GameObject m_PlayerSpawnerPrefab;

        // Client and server instances
        private PlayerSpawner m_ServerPlayerSpawner;
        private NetworkObject m_NewClientPlayer;

        protected override void OnServerAndClientsCreated()
        {
            m_PlayerSpawnerPrefab = CreateNetworkObjectPrefab("Parent");
            var parentPlayerSpawner = m_PlayerSpawnerPrefab.AddComponent<PlayerSpawner>();
            m_PlayerSpawnerPrefab.AddComponent<NetworkTransform>();

            var playerPrefab = CreateNetworkObjectPrefab("Child");
            var childNetworkTransform = playerPrefab.AddComponent<ClientNetworkTransform>();
            childNetworkTransform.InLocalSpace = true;

            parentPlayerSpawner.PlayerPrefab = playerPrefab.GetComponent<NetworkObject>();

            base.OnServerAndClientsCreated();
        }

        private bool NewPlayerObjectSpawned()
        {
            return m_ServerPlayerSpawner.SpawnedPlayer &&
                   m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects.ContainsKey(m_ServerPlayerSpawner.SpawnedPlayer.NetworkObjectId);
        }

        private bool HasServerInstanceReachedPeakPoint()
        {
            VerboseDebug($"Client Local: {m_NewClientPlayer.transform.localPosition} Server Local: {m_ServerPlayerSpawner.SpawnedPlayer.transform.localPosition}");
            return m_ServerPlayerSpawner.State == PlayerSpawner.MoveState.ReachedPeak;
        }

        private bool ServerClientPositionMatches()
        {
            return Approximately(m_NewClientPlayer.transform.localPosition, m_ServerPlayerSpawner.SpawnedPlayer.transform.localPosition) &&
                Approximately(m_NewClientPlayer.transform.position, m_ServerPlayerSpawner.SpawnedPlayer.transform.position);
        }

        [UnityTest]
        public IEnumerator TestParentedPlayerUsingLocalSpace()
        {
            // Spawn the PlayerSpawner object and save the instantiated component
            // The PlayerSpawner object will start moving.
            m_ServerPlayerSpawner = SpawnObject(m_PlayerSpawnerPrefab, m_ServerNetworkManager).GetComponent<PlayerSpawner>();

            // Create a new client and connect to the server
            // The client will prompt the server to spawn a player object and parent it to the PlayerSpawner object.
            yield return CreateAndStartNewClient();

            yield return WaitForConditionOrTimeOut(NewPlayerObjectSpawned);
            AssertOnTimeout($"Client did not spawn new player object!");

            // Save the spawned player object
            m_NewClientPlayer = m_ClientNetworkManagers[0].SpawnManager.SpawnedObjects[m_ServerPlayerSpawner.SpawnedPlayer.NetworkObjectId];

            // Let the parent PlayerSpawner move for several ticks to get an offset
            yield return WaitForConditionOrTimeOut(HasServerInstanceReachedPeakPoint);
            AssertOnTimeout($"Server instance never reached peak point!");

            // Check that the client and server local positions match (they should both be at {0,0,0} local space)
            yield return WaitForConditionOrTimeOut(ServerClientPositionMatches);
            AssertOnTimeout($"Client local position {m_NewClientPlayer.transform.localPosition} does not match" +
                $" server local position {m_ServerPlayerSpawner.SpawnedPlayer.transform.localPosition}");
        }
    }
}
