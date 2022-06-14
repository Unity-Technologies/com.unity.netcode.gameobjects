using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;

namespace Unity.Netcode.RuntimeTests
{
    public class SimulationConfigurationTests
    {
        [UnityTest]
        public IEnumerator GivenSimulationConfiguration_WhenUpdatedAtRuntime_ThenSimulatorParamsUpdated()
        {
            int newValue = new System.Random().Next(1, 99);
            NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            var gameObject = new GameObject("NetworkSimulator");
            var networkSimulator = gameObject.AddComponent<NetworkSimulator>();

            networkSimulator.SimulationConfiguration = NetworkTypePresets.HomeBroadband;

            NetcodeIntegrationTestHelpers.Start(false, server, clients);

            networkSimulator.SimulationConfiguration.PacketDelayMs = newValue;
            networkSimulator.SimulationConfiguration.PacketJitterMs = newValue;
            networkSimulator.SimulationConfiguration.PacketLossInterval = newValue;
            networkSimulator.SimulationConfiguration.PacketLossPercent = newValue;
            networkSimulator.SimulationConfiguration.PacketDuplicationPercent = newValue;
            networkSimulator.SimulationConfiguration.PacketFuzzFactor = newValue;
            networkSimulator.SimulationConfiguration.PacketFuzzOffset = newValue;

            networkSimulator.UpdateLiveParameters();

            var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            var simulatorParameters = transport.GetSimulatorParameters();

            Assert.AreEqual(newValue, simulatorParameters.PacketDelayMs);
            Assert.AreEqual(newValue, simulatorParameters.PacketJitterMs);
            Assert.AreEqual(newValue, simulatorParameters.PacketDropInterval);
            Assert.AreEqual(newValue, simulatorParameters.PacketDropPercentage);
            Assert.AreEqual(newValue, simulatorParameters.PacketDuplicationPercentage);
            Assert.AreEqual(newValue, simulatorParameters.FuzzFactor);
            Assert.AreEqual(newValue, simulatorParameters.FuzzOffset);

            yield return null;
        }

    }
}
