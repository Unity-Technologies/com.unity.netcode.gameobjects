using System;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.RuntimeTests.Profiling
{
    public sealed class NetworkVariableNameTests
    {
        private NetworkManager m_NetworkManager;
        private NetworkVariableNameComponent m_NetworkVariableNameComponent;

        [SetUp]
        public void SetUp()
        {
            NetworkManagerHelper.StartNetworkManager(out m_NetworkManager);

            var gameObjectId = NetworkManagerHelper.AddGameNetworkObject(Guid.NewGuid().ToString());
            m_NetworkVariableNameComponent= NetworkManagerHelper.AddComponentToObject<NetworkVariableNameComponent>(gameObjectId);
            NetworkManagerHelper.SpawnNetworkObject(gameObjectId);
        }

        [TearDown]
        public void TearDown()
        {
            NetworkManagerHelper.ShutdownNetworkManager();
        }

        [Test]
        public void EnsureNetworkVariableNameInitialization()
        {
            StringAssert.Contains(nameof(NetworkVariableNameComponent.NetworkVarString), m_NetworkVariableNameComponent.NetworkVarString.Name);
            StringAssert.Contains(nameof(NetworkVariableNameComponent.NetworkVarSet), m_NetworkVariableNameComponent.NetworkVarSet.Name);
            StringAssert.Contains(nameof(NetworkVariableNameComponent.NetworkVarList), m_NetworkVariableNameComponent.NetworkVarList.Name);
            StringAssert.Contains(nameof(NetworkVariableNameComponent.NetworkVarDictionary), m_NetworkVariableNameComponent.NetworkVarDictionary.Name);
        }

        private class NetworkVariableNameComponent : NetworkBehaviour
        {
            public NetworkVariableString NetworkVarString { get; } = new NetworkVariableString();
            
            public NetworkSet<ulong> NetworkVarSet { get; } = new NetworkSet<ulong>();
            
            public NetworkList<ulong> NetworkVarList { get; } = new NetworkList<ulong>();
            
            public NetworkDictionary<ulong, ulong> NetworkVarDictionary { get; } = new NetworkDictionary<ulong, ulong>();
        }
    }
}