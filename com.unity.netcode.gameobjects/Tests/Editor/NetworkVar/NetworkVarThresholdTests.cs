using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Netcode.EditorTests.NetworkVar
{
    public class NetworkVarThresholdTests
    {
        public class NetworkVariableTest : NetworkVariableWithThreshold<int>
        {
            public bool IsAwaitingReplication()
            {
                var field = typeof(NetworkVariableBase).GetField("m_IsDirty", BindingFlags.NonPublic | BindingFlags.Instance);
                return (bool)field.GetValue(this);
            }

            protected override bool ShouldSetDirty(ref int previousValue, ref int newValue)
            {
                return Mathf.Abs(newValue - previousValue) > m_Threshold;
            }

            public NetworkVariableTest(int threshold, int value = default) : base(threshold, value)
            {
            }
        }

        public class NetworkVarComponent : NetworkBehaviour
        {
            public NetworkVariableTest networkVariableInt = new NetworkVariableTest(3, 31415926);
        }

        [Test]
        public void TestAssignmentIntUnchanged()
        {
            var gameObjectMan = new GameObject();
            var networkManager = gameObjectMan.AddComponent<NetworkManager>();
            networkManager.BehaviourUpdater = new NetworkBehaviourUpdater();
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = networkManager;
            var networkVarComponent = gameObject.AddComponent<NetworkVarComponent>();
            networkVarComponent.networkVariableInt.Initialize(networkVarComponent);
            networkVarComponent.networkVariableInt.ResetDirty();

            networkVarComponent.networkVariableInt.Value = 31415926;
            if (networkVarComponent.networkVariableInt.IsAwaitingReplication())
            {
                Assert.Fail("Network Variable get replicated despite threshold");
            }

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObjectMan);
        }


        [Test]
        public void TestAssignmentBelowThreshold()
        {
            var gameObjectMan = new GameObject();
            var networkManager = gameObjectMan.AddComponent<NetworkManager>();
            networkManager.BehaviourUpdater = new NetworkBehaviourUpdater();
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = networkManager;
            var networkVarComponent = gameObject.AddComponent<NetworkVarComponent>();
            networkVarComponent.networkVariableInt.Initialize(networkVarComponent);
            networkVarComponent.networkVariableInt.ResetDirty();

            for (int i = 0; i < 2; ++i)
            {
                networkVarComponent.networkVariableInt.Value++;
                if (networkVarComponent.networkVariableInt.IsAwaitingReplication())
                {
                    Assert.Fail("Network Variable get replicated despite threshold");
                }
            }

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObjectMan);
        }

        [Test]
        public void TestAssignmentAboveThreshold()
        {
            var gameObjectMan = new GameObject();
            var networkManager = gameObjectMan.AddComponent<NetworkManager>();
            networkManager.BehaviourUpdater = new NetworkBehaviourUpdater();
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = networkManager;
            var networkVarComponent = gameObject.AddComponent<NetworkVarComponent>();
            networkVarComponent.networkVariableInt.Initialize(networkVarComponent);
            networkVarComponent.networkVariableInt.ResetDirty();

            networkVarComponent.networkVariableInt.Value += 4;
            if (!networkVarComponent.networkVariableInt.IsAwaitingReplication())
            {
                Assert.Fail("Network Variable get replicated despite threshold");
            }

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObjectMan);
        }

        [Test]
        public void TestAssignmentAboveThresholdAndReverted()
        {
            var gameObjectMan = new GameObject();
            var networkManager = gameObjectMan.AddComponent<NetworkManager>();
            networkManager.BehaviourUpdater = new NetworkBehaviourUpdater();
            var gameObject = new GameObject();
            var networkObject = gameObject.AddComponent<NetworkObject>();
            networkObject.NetworkManagerOwner = networkManager;
            var networkVarComponent = gameObject.AddComponent<NetworkVarComponent>();
            networkVarComponent.networkVariableInt.Initialize(networkVarComponent);
            networkVarComponent.networkVariableInt.ResetDirty();

            networkVarComponent.networkVariableInt.Value += 4;
            networkVarComponent.networkVariableInt.Value -= 4;
            if (networkVarComponent.networkVariableInt.IsAwaitingReplication())
            {
                Assert.Fail("Network Variable get replicated despite threshold");
            }

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(gameObjectMan);
        }
    }
}
