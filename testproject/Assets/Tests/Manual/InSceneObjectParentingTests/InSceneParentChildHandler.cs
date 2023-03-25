using System;
using System.Collections.Generic;
using TestProject.ManualTests;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TestProject.RuntimeTests
{
    public class InSceneParentChildHandler : NetworkBehaviour
    {
        public static InSceneParentChildHandler ServerRootParent;
        public static bool EnableVerboseDebug = true;
        public static bool AddNetworkTransform;
        public static bool WorldPositionStays;
        public static InSceneParentChildHandler RootParent { get; private set; }
        private static bool s_GenerateRandomValues;

        public bool ParentHasNoNetworkObject;
        public bool IsRootParent;
        public bool IsLastChild;
        public bool PreserveLocalSpace;

        public Vector3 PositionMax = new Vector3(5.0f, 4.0f, 5.0f);
        public Vector3 PositionMin = new Vector3(-5.00f, 0.5f, -5.0f);
        public Vector3 RotationMax = new Vector3(359.99f, 359.99f, 359.99f);
        public Vector3 RotationMin = new Vector3(0.01f, 0.01f, 0.01f);
        public float ScaleMax = 3.0f;
        public float ScaleMin = 0.75f;


        private InSceneParentChildHandler m_Parent;
        private InSceneParentChildHandler m_Child;
        private Vector3 m_TargetLocalPosition;
        private Vector3 m_TargetLocalRotation;
        private Vector3 m_TargetLocalScale;

        private NetworkTransform m_NetworkTransform;

        public static Dictionary<ulong, InSceneParentChildHandler> ServerRelativeInstances = new Dictionary<ulong, InSceneParentChildHandler>();
        public static Dictionary<ulong, Dictionary<ulong, InSceneParentChildHandler>> ClientRelativeInstances = new Dictionary<ulong, Dictionary<ulong, InSceneParentChildHandler>>();

        public static void ResetInstancesTracking(bool enableVerboseDebug)
        {
            EnableVerboseDebug = enableVerboseDebug;
            ServerRelativeInstances.Clear();
            ClientRelativeInstances.Clear();
        }

        public InSceneParentChildHandler GetChild()
        {
            return m_Child;
        }

        private Vector3 GenerateVector3(Vector3 min, Vector3 max)
        {
            var result = Vector3.zero;
            result.x = Random.Range(min.x, max.y);
            result.y = Random.Range(min.x, max.y);
            result.z = Random.Range(min.x, max.y);
            return result;
        }

        private void LogMessage(string message)
        {
            if (EnableVerboseDebug)
            {
                Debug.Log(message);
            }
        }

        private void Start()
        {
            if (IsRootParent)
            {
                Random.InitState((int)Random.Range(Time.deltaTime, Time.realtimeSinceStartup));
                RootParent = this;
            }
        }

        private int CountNestedChildren(Transform currentParent, int count = 0)
        {
            if (currentParent.childCount > 0)
            {
                var child = currentParent.GetChild(0);
                count++;
                CountNestedChildren(child, count);
            }
            return count;
        }

        private Transform GetLastChild(Transform currentParent)
        {
            if (currentParent.childCount > 0)
            {
                var child = currentParent.GetChild(0);
                var childHandler = child.GetComponent<InSceneParentChildHandler>();
                var parentHandler = currentParent.GetComponent<InSceneParentChildHandler>();
                parentHandler.m_Child = childHandler;
                childHandler.m_Parent = parentHandler;
                return GetLastChild(child);
            }
            return currentParent;
        }

        private void RemoveParent(Transform child, bool worldPositionStays = true)
        {
            var childNetworkObject = child.GetComponent<NetworkObject>();
            var parentOfChild = child.parent;
            if (parentOfChild != null)
            {
                if (!childNetworkObject.TryRemoveParent(worldPositionStays))
                {
                    throw new Exception($"[RemoveParent] {child.name} Failed to remove itself from parent {parentOfChild.name}!");
                }
                else
                {
                    RemoveParent(parentOfChild, worldPositionStays);
                }
            }
        }

        public void DeparentAllChildren(bool worldPositionStays = true)
        {
            if (IsRootParent && IsServer)
            {
                var lastChild = GetLastChild(transform);
                if (lastChild != null)
                {
                    RemoveParent(lastChild, worldPositionStays);
                }
            }
        }

        private void ParentChild(InSceneParentChildHandler child, bool worldPositionStays = true)
        {
            if (child != null)
            {
                if (child.m_Parent != null)
                {
                    var childNetworkObject = child.GetComponent<NetworkObject>();
                    if (!childNetworkObject.TrySetParent(child.m_Parent.transform, worldPositionStays))
                    {
                        throw new Exception($"[Parent] {child.name} Failed to parent itself under parent {child.m_Parent.name}!");
                    }
                    else
                    {
                        ParentChild(child.m_Child, worldPositionStays);
                    }
                }
            }
        }

        public void ReParentAllChildren(bool worldPositionStays = true)
        {
            if (IsRootParent && IsServer)
            {
                ParentChild(m_Child, worldPositionStays);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                LogMessage($"[{NetworkObjectId}] Pos = ({m_TargetLocalPosition}) | Rotation ({m_TargetLocalRotation}) | Scale ({m_TargetLocalScale})");
                if (AddNetworkTransform)
                {
                    m_NetworkTransform = gameObject.AddComponent<NetworkTransform>();
                    m_NetworkTransform.InLocalSpace = PreserveLocalSpace;
                }
                if (IsRootParent)
                {
                    ServerRootParent = this;
                }
            }
            else
            {
                if (!ClientRelativeInstances.ContainsKey(NetworkManager.LocalClientId))
                {
                    ClientRelativeInstances.Add(NetworkManager.LocalClientId, new Dictionary<ulong, InSceneParentChildHandler>());
                }
                if (!ClientRelativeInstances[NetworkManager.LocalClientId].ContainsKey(NetworkObjectId))
                {
                    ClientRelativeInstances[NetworkManager.LocalClientId].Add(NetworkObjectId, this);
                }
            }

            base.OnNetworkSpawn();
        }

        public void DeparentSetValuesAndReparent()
        {
            if (IsServer && IsRootParent)
            {
                // Back to back de-parenting and re-parenting
                s_GenerateRandomValues = true;
                DeparentAllChildren(WorldPositionStays);
                s_GenerateRandomValues = false;
                ReParentAllChildren(WorldPositionStays);
            }
        }

        /// <summary>
        /// This handles applying the final desired transform values before the ParentSyncMessage
        /// is created and sent.
        /// </summary>
        public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
        {
            if (!IsServer || !IsSpawned || parentNetworkObject != null || !s_GenerateRandomValues)
            {
                return;
            }

            m_TargetLocalPosition = GenerateVector3(PositionMin, PositionMax);
            m_TargetLocalRotation = GenerateVector3(RotationMin, RotationMax);
            var scale = Random.Range(ScaleMin, ScaleMax);
            m_TargetLocalScale = Vector3.one * scale;
            transform.position = m_TargetLocalPosition;
            transform.rotation = Quaternion.Euler(m_TargetLocalRotation);
            transform.localScale = m_TargetLocalScale;

            base.OnNetworkObjectParentChanged(parentNetworkObject);
        }


        private void LateUpdate()
        {
            if (!IsSpawned || !IsServer || NetworkManagerTestDisabler.IsIntegrationTest)
            {
                return;
            }

            // De-parent
            if (Input.GetKeyDown(KeyCode.D) && IsRootParent)
            {
                DeparentAllChildren(WorldPositionStays);
            }

            // Re-parent
            if (Input.GetKeyDown(KeyCode.R) && IsRootParent)
            {
                ReParentAllChildren(WorldPositionStays);
            }

            // De-parent, initialize with new values, and re-parent
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                RootParent.DeparentSetValuesAndReparent();
            }
        }
    }
}
