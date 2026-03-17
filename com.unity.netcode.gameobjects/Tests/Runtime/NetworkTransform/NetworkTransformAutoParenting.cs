using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{

    [TestFixture(HostOrServer.DAHost)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.Server)]
    internal class NetworkTransformAutoParenting : IntegrationTestWithApproximation
    {
        public enum TransformSpace
        {
            World,
            Local
        }

        protected override int NumberOfClients => 4;

        private List<NetworkObject> m_PrefabsToSpawn = new List<NetworkObject>();
        private NetworkObject m_ParentToSpawn;

        private List<NetworkObject> m_ParentInstances = new List<NetworkObject>();
        private NetworkObject m_ChildInstance;
        private NetworkObject m_FinalParent;
        private ulong m_NetworkObjectIdToValidate;

        private TransformSpace m_ParentWorldOrLocal;


        public NetworkTransformAutoParenting(HostOrServer host) : base(host)
        {
        }

        public class NetworkTransformStateMonitor : NetworkTransform
        {
            public static bool VerboseDebug;
            public int DetectedMotionCount { get; private set; }

            private bool m_HasTeleported;
            private Vector3 m_LastKnownPosition;

            private void Log(string msg)
            {
                if (VerboseDebug)
                {
                    Debug.Log(msg);
                }
            }

            protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
            {
                if (newState.IsTeleportingNextFrame)
                {
                    DetectedMotionCount = 0;
                    m_HasTeleported = true;
                    NetworkManager.NetworkTickSystem.Tick += OnNetworkTick;
                    m_LastKnownPosition = transform.position;
                }
                base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
            }

            private void OnNetworkTick()
            {
                NetworkManager.NetworkTickSystem.Tick -= OnNetworkTick;
                Log("Teleporting tick completed.");
            }

            protected string GetVector3Values(Vector3 vector3)
            {
                return $"({vector3.x:F6},{vector3.y:F6},{vector3.z:F6})";
            }


            protected bool Approximately(Vector3 a, Vector3 b)
            {
                var deltaVariance = 0.0001f;
                return System.Math.Round(Mathf.Abs(a.x - b.x), 4) <= deltaVariance &&
                    System.Math.Round(Mathf.Abs(a.y - b.y), 4) <= deltaVariance &&
                    System.Math.Round(Mathf.Abs(a.z - b.z), 4) <= deltaVariance;
            }

            private void Update()
            {
                if (CanCommitToTransform)
                {
                    return;
                }

                if (m_HasTeleported)
                {
                    if (Approximately(transform.position, m_LastKnownPosition))
                    {
                        DetectedMotionCount++;
                        Log($"[{DetectedMotionCount}] Moving from {GetVector3Values(m_LastKnownPosition)} to {GetVector3Values(transform.position)}");
                    }
                }
            }
        }

        protected override IEnumerator OnTearDown()
        {
            m_PrefabsToSpawn.Clear();
            return base.OnTearDown();
        }

        private NetworkObject CreatePrefabToSpawn(TransformSpace transformSpace, bool useHalfPrecision, bool useQuaternion, bool compressQuaternion)
        {
            var prefabToSpawn = CreateNetworkObjectPrefab($"SeqObj[{m_PrefabsToSpawn.Count}]").GetComponent<NetworkObject>();
            var networkTransform = prefabToSpawn.gameObject.AddComponent<NetworkTransformStateMonitor>();
            networkTransform.SwitchTransformSpaceWhenParented = true;
            // Validates that even if you try to set local space it will be reset to world when 1st spawned
            networkTransform.InLocalSpace = transformSpace == TransformSpace.Local;
            networkTransform.UseHalfFloatPrecision = useHalfPrecision;
            networkTransform.UseQuaternionSynchronization = useQuaternion;
            networkTransform.UseQuaternionCompression = compressQuaternion;
            return prefabToSpawn;
        }

        /// <summary>
        /// Generates objects to spawn.
        /// </summary>
        protected override void OnServerAndClientsCreated()
        {
            m_ParentToSpawn = CreateNetworkObjectPrefab("SeqParent").GetComponent<NetworkObject>();

            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.World, false, false, false));
            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.Local, false, false, false));

            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.World, true, false, false));
            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.Local, true, false, false));

            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.World, true, true, false));
            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.Local, true, true, false));

            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.World, true, true, true));
            m_PrefabsToSpawn.Add(CreatePrefabToSpawn(TransformSpace.Local, true, true, true));

            base.OnServerAndClientsCreated();
        }

        private bool AllClientsSpawnedParentObject(StringBuilder errorLog)
        {
            var hadError = false;
            foreach (var networkManager in m_NetworkManagers)
            {
                foreach (var parent in m_ParentInstances)
                {
                    if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(parent.NetworkObjectId))
                    {
                        errorLog.AppendLine($"Client-{networkManager.LocalClientId}] Has not spawned {parent.name}!");
                        hadError = true;
                    }
                }
            }
            return !hadError;
        }

        private bool AllClientsDespawnedObject()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_NetworkObjectIdToValidate))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AllClientsParented(StringBuilder errorLog)
        {
            var hadError = false;
            foreach (var networkManager in m_NetworkManagers)
            {
                var localNetworkObject = networkManager.SpawnManager.SpawnedObjects[m_NetworkObjectIdToValidate];
                if (localNetworkObject.transform.parent == null)
                {
                    errorLog.AppendLine($"Client-{networkManager.LocalClientId}] {localNetworkObject.name} Has no parent when it should!");
                    hadError = true;
                    continue;
                }
                var parent = localNetworkObject.transform.parent.GetComponent<NetworkObject>();
                if (parent.NetworkObjectId != m_FinalParent.NetworkObjectId)
                {
                    errorLog.AppendLine($"Client-{networkManager.LocalClientId}] {localNetworkObject.name} Should be parented under {m_FinalParent.name}-{m_FinalParent.NetworkObjectId} but is parented under {parent.name}-{parent.NetworkObjectId}!");
                    hadError = true;
                }
            }
            return !hadError;
        }

        private const int k_ParentsToSpawn = 7;
        private const int k_ParentingIterations = 3;

        /// <summary>
        /// Validates that when SwitchTransformSpaceWhenParented is enabled and parenting occurs multiple times
        /// that all non-authority instances are properly synchronized with parenting and their final transform values.
        /// </summary>
        [UnityTest]
        public IEnumerator SwitchTransformSpaceWhenParented()
        {
            var authority = GetAuthorityNetworkManager();
            for (int i = 0; i < k_ParentsToSpawn; i++)
            {
                m_ParentInstances.Add(SpawnObject(m_ParentToSpawn.gameObject, authority).GetComponent<NetworkObject>());
            }
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedParentObject);
            AssertOnTimeout($"Timed out waiting for all clients to spawn parent instances!");

            foreach (var prefabToSpawn in m_PrefabsToSpawn)
            {
                yield return SpawnAndTest(prefabToSpawn, true);
                yield return SpawnAndTest(prefabToSpawn, false);
            }
        }

        /// <summary>
        /// This runs through the entire spawn and parenting validation tests
        /// for the prefab passed in while also adjusting whether to parent
        /// with world position stays enabled or disabled.
        /// </summary>
        private IEnumerator SpawnAndTest(NetworkObject prefabToSpawn, bool worldPositionStays)
        {
            var authority = GetAuthorityNetworkManager();
            m_ChildInstance = SpawnObject(prefabToSpawn.gameObject, authority).GetComponent<NetworkObject>();
            var networkTransform = m_ChildInstance.GetComponent<NetworkTransformStateMonitor>();
            m_ParentWorldOrLocal = worldPositionStays ? TransformSpace.World : TransformSpace.Local;
            Assert.False(networkTransform.InLocalSpace, $"{m_ChildInstance.name} should never be in local space when not parented and SwitchTransformSpaceWhenParented is enabled!");

            m_EnableVerboseDebug = true;
            VerboseDebug($"[Testing][Parenting: {m_ParentWorldOrLocal}][HalfFloat: {networkTransform.UseHalfFloatPrecision}][Quaternion: {networkTransform.UseQuaternionSynchronization}][Compressed Quaternion: {networkTransform.UseQuaternionCompression}]");
            m_EnableVerboseDebug = false;
            m_NetworkObjectIdToValidate = m_ChildInstance.NetworkObjectId;

            var startingParentIndex = Random.Range(0, k_ParentsToSpawn - 1);

            // Iterate several times setting the parent, handling parent-to-parent, and removing the parent
            // in order to validate we can handle back-to-back world to local, local to local, and local to
            // world transformations in the same frame (and that it synchronizes properly).
            for (int i = 0; i < k_ParentingIterations; i++)
            {
                if (m_ChildInstance.transform.parent)
                {
                    m_ChildInstance.TryRemoveParent();
                }
                for (int j = 0; j < k_ParentsToSpawn; j++)
                {
                    var parentIndex = (j + startingParentIndex) % k_ParentsToSpawn;
                    var parent = m_ParentInstances[parentIndex];
                    m_ChildInstance.TrySetParent(parent, m_ParentWorldOrLocal == TransformSpace.World);
                    m_FinalParent = parent;
                }
            }
            yield return WaitForSpawnedOnAllOrTimeOut(m_NetworkObjectIdToValidate);
            AssertOnTimeout($"Timed out waiting for all clients to spawn {m_ChildInstance.name} instance!");

            yield return WaitForConditionOrTimeOut(AllClientsParented);
            AssertOnTimeout($"Timed out waiting for all clients to parent {m_ChildInstance.name} under the final parent instance {m_FinalParent.name}!");

            yield return WaitForConditionOrTimeOut(TransformsMatch);
            AssertOnTimeout($"Timed out waiting for all non-authority transforms of the child to match the authority transform of the child {m_ChildInstance.name}!");

            var name = m_ChildInstance.name;
            m_ChildInstance.Despawn();

            yield return WaitForConditionOrTimeOut(AllClientsDespawnedObject);
            AssertOnTimeout($"Timed out waiting for all clients to despawn {name}!");
        }


        protected bool TransformsMatch(StringBuilder errorLog)
        {
            return InternalTransformsMatch(errorLog, TransformSpace.World) && InternalTransformsMatch(errorLog, TransformSpace.Local);
        }

        private bool InternalTransformsMatch(StringBuilder errorLog, TransformSpace transformSpace)
        {
            var hasErrors = false;
            var useWorldSpace = transformSpace == TransformSpace.World ? true : false;
            var authorityEulerRotation = useWorldSpace ? m_ChildInstance.transform.eulerAngles : m_ChildInstance.transform.localEulerAngles;
            var authorityPosition = useWorldSpace ? m_ChildInstance.transform.position : m_ChildInstance.transform.localPosition;

            foreach (var networkManager in m_NetworkManagers)
            {

                var nonAuthorityInstance = networkManager.SpawnManager.SpawnedObjects[m_ChildInstance.NetworkObjectId];
                var nonAuthorityEulerRotation = useWorldSpace ? nonAuthorityInstance.transform.eulerAngles : nonAuthorityInstance.transform.localEulerAngles;

                var xIsEqual = ApproximatelyEuler(authorityEulerRotation.x, nonAuthorityEulerRotation.x);
                var yIsEqual = ApproximatelyEuler(authorityEulerRotation.y, nonAuthorityEulerRotation.y);
                var zIsEqual = ApproximatelyEuler(authorityEulerRotation.z, nonAuthorityEulerRotation.z);
                if (!xIsEqual || !yIsEqual || !zIsEqual)
                {
                    errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] Rotation {GetVector3Values(nonAuthorityEulerRotation)} does not match the authority rotation {GetVector3Values(authorityEulerRotation)}!");
                    hasErrors = true;
                }
                var nonAuthorityPosition = useWorldSpace ? nonAuthorityInstance.transform.position : nonAuthorityInstance.transform.localPosition;
                xIsEqual = Approximately(authorityPosition.x, nonAuthorityPosition.x);
                yIsEqual = Approximately(authorityPosition.y, nonAuthorityPosition.y);
                zIsEqual = Approximately(authorityPosition.z, nonAuthorityPosition.z);

                if (!xIsEqual || !yIsEqual || !zIsEqual)
                {
                    errorLog.AppendLine($"[Client-{nonAuthorityInstance.NetworkManager.LocalClientId}][{nonAuthorityInstance.gameObject.name}] Position {GetVector3Values(nonAuthorityPosition)} does not match the authority position {GetVector3Values(authorityPosition)}!");
                    hasErrors = true;
                }
            }
            return !hasErrors;
        }
    }
}
