using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode.Components;
using Unity.Netcode.TestHelpers.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Netcode.RuntimeTests
{
    [TestFixture(HostOrServer.Server)]
    [TestFixture(HostOrServer.Host)]
    [TestFixture(HostOrServer.DAHost)]
    internal class NetworkTransformNonAuthorityTests : IntegrationTestWithApproximation
    {
        private const int k_NumberOfPasses = 3;
        protected override int NumberOfClients => 2;

        private StringBuilder m_ErrorMsg = new StringBuilder();

        private GameObject m_PrefabToSpawn;

        private NetworkObject m_AuthorityInstance;

        public NetworkTransformNonAuthorityTests(HostOrServer hostOrServer) : base(hostOrServer) { }

        public class NetworkTransformTestComponent : NetworkTransform, INetworkUpdateSystem
        {
            public static NetworkTransformTestComponent AuthorityInstance { get; private set; }
            public static readonly List<NetworkTransformTestComponent> AllInstances = new List<NetworkTransformTestComponent>();

            public void SetDirValues(Vector3 positionMotion = default, Vector3 rotationMotion = default,
                Vector3 scaleMotion = default, bool shouldMove = false)
            {
                m_UpdateNonSynchronizedAxis = shouldMove;
                if (m_UpdateNonSynchronizedAxis)
                {
                    m_PositionDir = GetSynchronizedPosition(positionMotion);
                    m_RotationDir = GetSynchronizedRotation(rotationMotion);
                    m_ScaleDir = GetSynchronizedScale(scaleMotion);
                    m_TargetPosition = GetSynchronizedPosition(m_PositionDir + transform.position);
                    var quat = Quaternion.identity;
                    quat = transform.rotation;
                    quat.eulerAngles = GetSynchronizedRotation(m_RotationDir + transform.rotation.eulerAngles);
                    m_TargetRotation = quat.eulerAngles;
                    m_TargetScale = GetSynchronizedScale(m_ScaleDir + transform.localScale);
                    NonSynchronizedPositionReached = false;
                    NonSynchronizedRotationReached = false;
                    NonSynchronizedScaleReached = false;

                    m_PosMag = 1.0f / m_TargetPosition.magnitude;
                    m_RotMag = 1.0f / m_TargetRotation.magnitude;
                    m_ScaleMag = 1.0f / m_TargetScale.magnitude;
                }
            }

            private float m_PosMag;
            private float m_RotMag;
            private float m_ScaleMag;

            public Vector3 MovePosition(Vector3 position)
            {
                if (!CanCommitToTransform)
                {
                    return Vector3.zero;
                }

                transform.position += GetSynchronizedPosition(position, false);
                return transform.position;
            }

            public Vector3 MoveRotation(Vector3 eulerAngles)
            {
                if (!CanCommitToTransform)
                {
                    return Vector3.zero;
                }
                var rotation = transform.rotation;
                rotation.eulerAngles += GetSynchronizedRotation(eulerAngles, false);
                transform.rotation = rotation;
                return rotation.eulerAngles;
            }

            public Vector3 MoveScale(Vector3 scale)
            {
                if (!CanCommitToTransform)
                {
                    return Vector3.zero;
                }

                transform.localScale += GetSynchronizedScale(scale, false);
                return transform.localScale;
            }

            public bool NonSynchronizedPositionReached { get; private set; }
            public bool NonSynchronizedRotationReached { get; private set; }
            public bool NonSynchronizedScaleReached { get; private set; }

            private bool m_UpdateNonSynchronizedAxis;
            private Vector3 m_PositionDir;
            private Vector3 m_RotationDir;
            private Vector3 m_ScaleDir;
            private Vector3 m_TargetPosition;
            private Vector3 m_TargetRotation;
            private Vector3 m_TargetScale;


            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                if (CanCommitToTransform)
                {
                    NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Update);
                    AuthorityInstance = this;
                }
                AllInstances.Add(this);
            }

            public override void OnNetworkDespawn()
            {
                NetworkUpdateLoop.UnregisterNetworkUpdate(this, NetworkUpdateStage.Update);
                base.OnNetworkDespawn();
            }

            public void NetworkUpdate(NetworkUpdateStage updateStage)
            {
                MoveObjectLocally();
            }

            private Vector3 GetSynchronizedPosition(Vector3 position, bool invert = true)
            {
                if (invert)
                {
                    position.x *= !SyncPositionX ? 1 : 0;
                    position.y *= !SyncPositionY ? 1 : 0;
                    position.z *= !SyncPositionZ ? 1 : 0;
                }
                else
                {
                    position.x *= SyncPositionX ? 1 : 0;
                    position.y *= SyncPositionY ? 1 : 0;
                    position.z *= SyncPositionZ ? 1 : 0;
                }
                return position;
            }

            private Vector3 GetSynchronizedRotation(Vector3 rotation, bool invert = true)
            {
                if (invert)
                {
                    rotation.x *= !SyncRotAngleX ? 1 : 0;
                    rotation.y *= !SyncRotAngleY ? 1 : 0;
                    rotation.z *= !SyncRotAngleZ ? 1 : 0;
                }
                else
                {
                    rotation.x *= SyncRotAngleX ? 1 : 0;
                    rotation.y *= SyncRotAngleY ? 1 : 0;
                    rotation.z *= SyncRotAngleZ ? 1 : 0;
                }
                return rotation;
            }

            private Vector3 GetSynchronizedScale(Vector3 scale, bool invert = true)
            {
                if (invert)
                {
                    scale.x *= !SyncScaleX ? 1 : 0;
                    scale.y *= !SyncScaleY ? 1 : 0;
                    scale.z *= !SyncScaleZ ? 1 : 0;
                }
                else
                {
                    scale.x *= SyncScaleX ? 1 : 0;
                    scale.y *= SyncScaleY ? 1 : 0;
                    scale.z *= SyncScaleZ ? 1 : 0;
                }
                return scale;
            }

            public bool HasCompletedMotion()
            {
                return NonSynchronizedPositionReached && NonSynchronizedRotationReached && NonSynchronizedScaleReached;
            }

            public void GetUnSynchronizedTargetInfo(StringBuilder builder)
            {
                if (!NonSynchronizedPositionReached)
                {
                    builder.Append($"[Position] Current: {GetSynchronizedPosition(transform.position)} | Target: {m_TargetPosition}");
                }
                if (!NonSynchronizedRotationReached)
                {
                    builder.Append($"[Rotation] Current: {GetSynchronizedRotation(transform.rotation.eulerAngles)} | Target: {m_TargetRotation}");
                }
                if (!NonSynchronizedScaleReached)
                {
                    builder.Append($"[Scale] Current: {GetSynchronizedScale(transform.localScale)} | Target: {m_TargetScale}");
                }

                builder.Append("\n");
            }

            private void MoveObjectLocally()
            {
                if (!m_UpdateNonSynchronizedAxis || HasCompletedMotion())
                {
                    return;
                }
                if (!NonSynchronizedPositionReached)
                {
                    var lerpAmount = Mathf.Clamp(1.0f - (Vector3.Distance(transform.position, m_TargetPosition) * m_PosMag), 0.25f, 1.0f);
                    transform.position = Vector3.Lerp(transform.position, m_TargetPosition, lerpAmount);
                    NonSynchronizedPositionReached = Approximately(GetSynchronizedPosition(transform.position), GetSynchronizedPosition(m_TargetPosition));
                }

                if (!NonSynchronizedRotationReached)
                {
                    var rotation = transform.rotation;
                    var eulerRotation = rotation.eulerAngles;
                    var lerpAmount = Mathf.Clamp(1.0f - (Vector3.Distance(eulerRotation, m_TargetRotation) * m_RotMag), 0.25f, 1.0f);
                    eulerRotation = Vector3.Lerp(eulerRotation, m_TargetRotation, lerpAmount);
                    rotation.eulerAngles = eulerRotation;
                    transform.rotation = rotation;
                    NonSynchronizedRotationReached = Approximately(GetSynchronizedRotation(transform.rotation.eulerAngles), m_TargetRotation);
                }

                if (!NonSynchronizedScaleReached)
                {
                    var lerpFactor = Vector3.Distance(transform.localScale, m_TargetScale);
                    var lerpAmount = Mathf.Clamp(1.0f - (Vector3.Distance(transform.localScale, m_TargetScale) * m_ScaleMag), 0.25f, 1.0f);
                    transform.localScale = Vector3.Lerp(transform.localScale, m_TargetScale, lerpAmount);
                    NonSynchronizedScaleReached = Approximately(GetSynchronizedScale(transform.localScale), m_TargetScale);
                }
            }

            public override void OnUpdate()
            {
                base.OnUpdate();

                MoveObjectLocally();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected bool Approximately(Vector3 a, Vector3 b)
            {
                var deltaVariance = 0.01f;
                return System.Math.Round(Mathf.Abs(a.x - b.x), 2) <= deltaVariance &&
                    System.Math.Round(Mathf.Abs(a.y - b.y), 2) <= deltaVariance &&
                    System.Math.Round(Mathf.Abs(a.z - b.z), 2) <= deltaVariance;
            }
        }

        protected override void OnServerAndClientsCreated()
        {
            m_PrefabToSpawn = CreateNetworkObjectPrefab("TestObject");
            var networkTransform = m_PrefabToSpawn.AddComponent<NetworkTransformTestComponent>();
            networkTransform.SyncPositionX = true;
            networkTransform.SyncPositionY = false;
            networkTransform.SyncPositionZ = true;
            
            networkTransform.SyncRotAngleX = true;
            networkTransform.SyncRotAngleY = false;
            networkTransform.SyncRotAngleZ = false;

            networkTransform.SyncScaleX = false;
            networkTransform.SyncScaleY = true;
            networkTransform.SyncScaleZ = false;

            base.OnServerAndClientsCreated();
        }

        private bool AllTransformsAreApproximatelyTheSame()
        {
            m_ErrorMsg.Clear();
            var authorityInstance = NetworkTransformTestComponent.AuthorityInstance;

            foreach (var instance in NetworkTransformTestComponent.AllInstances)
            {
                if (instance ==  authorityInstance)
                {
                    continue;
                }
                if (!Approximately(instance.transform.position, authorityInstance.transform.position))
                {
                    m_ErrorMsg.AppendLine($"[{instance.name}] Position ({instance.transform.position}) is not " +
                        $"equal to authority's ({authorityInstance.transform.position})! ");
                }
                if (!Approximately(instance.transform.rotation, authorityInstance.transform.rotation))
                {
                    m_ErrorMsg.AppendLine($"[{instance.name}] Rotation ({instance.transform.rotation.eulerAngles}) is not " +
                        $"equal to authority's ({authorityInstance.transform.rotation.eulerAngles})! ");
                }
                if (!Approximately(instance.transform.localScale, authorityInstance.transform.localScale))
                {
                    m_ErrorMsg.AppendLine($"[{instance.name}] Scale ({instance.transform.localScale}) is not " +
                        $"equal to authority's ({authorityInstance.transform.localScale})! ");
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        private bool AllNonSynchronizedMotionCompleted()
        {
            m_ErrorMsg.Clear();
            foreach (var instance in NetworkTransformTestComponent.AllInstances)
            {
                if (!instance.HasCompletedMotion())
                {
                    m_ErrorMsg.Append($"[{instance.name}] Has not completed local motion!\n");
                    instance.GetUnSynchronizedTargetInfo(m_ErrorMsg);
                }
            }
            return m_ErrorMsg.Length == 0;
        }

        private bool AllClientsSpawnedObject()
        {
            foreach(var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_AuthorityInstance.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator NonAuthorityUpdateNonSynchronizedAxis()
        {
            var authority = GetNonAuthorityNetworkManager();
            m_AuthorityInstance = SpawnObject(m_PrefabToSpawn, authority).GetComponent<NetworkObject>();
            yield return WaitForConditionOrTimeOut(AllClientsSpawnedObject);
            AssertOnTimeout($"All clients did not spawn {m_AuthorityInstance.name}!");

            for(int i = 0; i < k_NumberOfPasses; i++)
            {
                var positionDelta = GetRandomVector3(-4, 4);
                var rotationDelta = GetRandomVector3(-20, 20);
                var scaleDelta = GetRandomVector3(-2, 2);

                var movePosition = NetworkTransformTestComponent.AuthorityInstance.MovePosition(GetRandomVector3(-4, 4));
                var moveRotation = NetworkTransformTestComponent.AuthorityInstance.MoveRotation(GetRandomVector3(-20, 20));
                var moveScale = NetworkTransformTestComponent.AuthorityInstance.MoveScale(GetRandomVector3(-2, 2));

                foreach (var testTransform in NetworkTransformTestComponent.AllInstances)
                {
                    testTransform.SetDirValues(positionDelta, rotationDelta, scaleDelta, true);
                }

                // Wait for all instances to finish their local controlled changes
                yield return WaitForConditionOrTimeOut(AllNonSynchronizedMotionCompleted);
                AssertOnTimeout($"[Iteration: {i}] Not all instances completed local motion! {m_ErrorMsg}");

                // Wait for all instances' transforms to match
                yield return WaitForConditionOrTimeOut(AllTransformsAreApproximatelyTheSame);
                AssertOnTimeout($"[Iteration: {i}] Not all instances' transforms match! {m_ErrorMsg}");

                var builder = new StringBuilder();
                builder.AppendLine($"Final Expected Position: {movePosition + positionDelta}");
                foreach (var testTransform in NetworkTransformTestComponent.AllInstances)
                {
                    builder.AppendLine($"[Client-{testTransform.NetworkManager.LocalClientId}] Position: {testTransform.transform.position}");
                }
                Debug.Log(builder.ToString());
            }
        }

        private GameObject m_OwnershipObject;
        private NetworkObject m_OwnershipNetworkObject;
        private bool AllObjectsSpawnedOnClients()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (!networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_OwnershipNetworkObject.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        private bool ObjectHiddenOnNonAuthorityClients()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.LocalClientId == m_OwnershipNetworkObject.OwnerClientId)
                {
                    continue;
                }
                if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_OwnershipNetworkObject.NetworkObjectId))
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator NetworkShowWithChangeOwnershipTest()
        {
            var authority = GetAuthorityNetworkManager();

            m_OwnershipObject = SpawnObject(m_PrefabToSpawn, authority);
            m_OwnershipNetworkObject = m_OwnershipObject.GetComponent<NetworkObject>();

            yield return WaitForConditionOrTimeOut(AllObjectsSpawnedOnClients);
            AssertOnTimeout("Timed out waiting for all clients to spawn the ownership object!");

            VerboseDebug($"Hiding object {m_OwnershipNetworkObject.NetworkObjectId} on all clients");
            foreach (var client in m_NetworkManagers)
            {
                if (client == authority)
                {
                    continue;
                }
                m_OwnershipNetworkObject.NetworkHide(client.LocalClientId);
            }

            yield return WaitForConditionOrTimeOut(ObjectHiddenOnNonAuthorityClients);
            AssertOnTimeout("Timed out waiting for all clients to hide the ownership object!");

            m_NewOwner = GetNonAuthorityNetworkManager();
            Assert.AreNotEqual(m_OwnershipNetworkObject.OwnerClientId, m_NewOwner.LocalClientId, $"Client-{m_NewOwner.LocalClientId} should not have ownership of object {m_OwnershipNetworkObject.NetworkObjectId}!");
            Assert.False(m_NewOwner.SpawnManager.SpawnedObjects.ContainsKey(m_OwnershipNetworkObject.NetworkObjectId), $"Client-{m_NewOwner.LocalClientId} should not have object {m_OwnershipNetworkObject.NetworkObjectId} spawned!");

            // Run NetworkShow and ChangeOwnership directly after one-another
            VerboseDebug($"Calling {nameof(NetworkObject.NetworkShow)} on object {m_OwnershipNetworkObject.NetworkObjectId} for client {m_NewOwner.LocalClientId}");
            m_OwnershipNetworkObject.NetworkShow(m_NewOwner.LocalClientId);
            VerboseDebug($"Calling {nameof(NetworkObject.ChangeOwnership)} on object {m_OwnershipNetworkObject.NetworkObjectId} for client {m_NewOwner.LocalClientId}");
            m_OwnershipNetworkObject.ChangeOwnership(m_NewOwner.LocalClientId);
            m_ObjectId = m_OwnershipNetworkObject.NetworkObjectId;
            yield return WaitForConditionOrTimeOut(OwnershipHasChanged);
            AssertOnTimeout($"Timed out waiting for clients-{m_NewOwner.LocalClientId} to gain ownership of object {m_OwnershipNetworkObject.NetworkObjectId}!");
            VerboseDebug($"Client {m_NewOwner.LocalClientId} now owns object {m_OwnershipNetworkObject.NetworkObjectId}!");
        }

        private NetworkManager m_NewOwner;

        private bool OwnershipHasChanged()
        {
            if (!m_NewOwner.SpawnManager.SpawnedObjects.ContainsKey(m_ObjectId))
            {
                return false;
            }
            return m_NewOwner.SpawnManager.SpawnedObjects[m_ObjectId].OwnerClientId == m_NewOwner.LocalClientId;
        }

        private ulong m_ObjectId;
        private bool ObjectDespawned()
        {
            foreach (var networkManager in m_NetworkManagers)
            {
                if (networkManager.SpawnManager.SpawnedObjects.ContainsKey(m_ObjectId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
