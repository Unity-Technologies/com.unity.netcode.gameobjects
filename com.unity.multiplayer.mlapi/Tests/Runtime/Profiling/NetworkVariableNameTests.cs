using System;
using MLAPI.NetworkVariable;
using MLAPI.NetworkVariable.Collections;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.RuntimeTests.Profiling
{
    public sealed class NetworkVariableNameTests
    {
        NetworkManager m_NetworkManager;
        NetworkVariableNameComponent m_NetworkVariableNameComponent;

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
            Assert.True(m_NetworkVariableNameComponent.NetworkVarString.Name.Contains(nameof(NetworkVariableNameComponent.NetworkVarString)));
            Assert.True(m_NetworkVariableNameComponent.NetworkVarSet.Name.Contains(nameof(NetworkVariableNameComponent.NetworkVarSet)));
            Assert.True(m_NetworkVariableNameComponent.NetworkVarList.Name.Contains(nameof(NetworkVariableNameComponent.NetworkVarList)));
            Assert.True(m_NetworkVariableNameComponent.NetworkVarDictionary.Name.Contains(nameof(NetworkVariableNameComponent.NetworkVarDictionary)));
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