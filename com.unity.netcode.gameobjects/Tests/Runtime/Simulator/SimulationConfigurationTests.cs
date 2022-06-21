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
        [Test]
        public void GivenSimulationConfiguration_WhenUpdatedAtRuntime_ThenSimulatorParamsUpdated()
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

            NetcodeIntegrationTestHelpers.StopOneClient(clients[0]);
            NetcodeIntegrationTestHelpers.Destroy();
        }

        [Test]
        public void GivenNetworkSimulator_WhenSimulatorConfigurationIsCreated_ThenSimulatorParamsUpdated()
        {
            int value = new System.Random().Next(1, 99);
            NetcodeIntegrationTestHelpers.Create(1, out NetworkManager server, out NetworkManager[] clients);

            var gameObject = new GameObject("NetworkSimulator");
            var networkSimulator = gameObject.AddComponent<NetworkSimulator>();

            networkSimulator.SimulationConfiguration = NetworkSimulationConfiguration.Create(
                name: "Test Config",
                description:"Test Config Description",
                packetDelayMs: value,
                packetJitterMs: value,
                packetLossInterval: value,
                packetLossPercent: value,
                packetDuplicationPercent: value,
                packetFuzzFactor: value,
                packetFuzzOffset: value);

            NetcodeIntegrationTestHelpers.Start(false, server, clients);

            networkSimulator.SimulationConfiguration.PacketDelayMs = value;
            networkSimulator.SimulationConfiguration.PacketJitterMs = value;
            networkSimulator.SimulationConfiguration.PacketLossInterval = value;
            networkSimulator.SimulationConfiguration.PacketLossPercent = value;
            networkSimulator.SimulationConfiguration.PacketDuplicationPercent = value;
            networkSimulator.SimulationConfiguration.PacketFuzzFactor = value;
            networkSimulator.SimulationConfiguration.PacketFuzzOffset = value;

            networkSimulator.UpdateLiveParameters();

            var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            var simulatorParameters = transport.GetSimulatorParameters();

            Assert.AreEqual(value, simulatorParameters.PacketDelayMs);
            Assert.AreEqual(value, simulatorParameters.PacketJitterMs);
            Assert.AreEqual(value, simulatorParameters.PacketDropInterval);
            Assert.AreEqual(value, simulatorParameters.PacketDropPercentage);
            Assert.AreEqual(value, simulatorParameters.PacketDuplicationPercentage);
            Assert.AreEqual(value, simulatorParameters.FuzzFactor);
            Assert.AreEqual(value, simulatorParameters.FuzzOffset);

            NetcodeIntegrationTestHelpers.StopOneClient(clients[0]);
            NetcodeIntegrationTestHelpers.Destroy();
        }

    }
}
