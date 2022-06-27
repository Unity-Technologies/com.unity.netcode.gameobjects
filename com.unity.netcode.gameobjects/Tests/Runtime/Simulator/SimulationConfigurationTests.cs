using NUnit.Framework;
using UnityEngine;
using Unity.Netcode.TestHelpers.Runtime;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Utilities;

namespace Unity.Netcode.RuntimeTests
{
    public class SimulationConfigurationTests
    {
        [Test]
        public void GivenSimulationConfiguration_WhenUpdatedAtRuntime_ThenSimulatorParamsUpdated()
        {
            int newValue = new System.Random().Next(1, 99);
            NetcodeIntegrationTestHelpers.Create(1, out var server, out var clients);

            var gameObject = new GameObject(nameof(NetworkSimulator));
            var networkSimulator = gameObject.AddComponent<NetworkSimulator>();

            networkSimulator.SimulatorConfiguration = NetworkTypePresets.HomeBroadband;

            NetcodeIntegrationTestHelpers.Start(false, server, clients);

            networkSimulator.SimulatorConfiguration.PacketDelayMs = newValue;
            networkSimulator.SimulatorConfiguration.PacketJitterMs = newValue;
            networkSimulator.SimulatorConfiguration.PacketLossInterval = newValue;
            networkSimulator.SimulatorConfiguration.PacketLossPercent = newValue;
            networkSimulator.SimulatorConfiguration.PacketDuplicationPercent = newValue;

            networkSimulator.UpdateLiveParameters();

            var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            var settings = transport.NetworkSettings;
            var simulatorParameters = settings.GetSimulatorStageParameters();

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
            NetcodeIntegrationTestHelpers.Create(1, out var server, out var clients);

            var gameObject = new GameObject(nameof(NetworkSimulator));
            var networkSimulator = gameObject.AddComponent<NetworkSimulator>();

            networkSimulator.SimulatorConfiguration = NetworkSimulatorConfiguration.Create(
                name: "Test Config",
                description:"Test Config Description",
                packetDelayMs: value,
                packetJitterMs: value,
                packetLossInterval: value,
                packetLossPercent: value,
                packetDuplicationPercent: value);

            NetcodeIntegrationTestHelpers.Start(false, server, clients);

            networkSimulator.SimulatorConfiguration.PacketDelayMs = value;
            networkSimulator.SimulatorConfiguration.PacketJitterMs = value;
            networkSimulator.SimulatorConfiguration.PacketLossInterval = value;
            networkSimulator.SimulatorConfiguration.PacketLossPercent = value;
            networkSimulator.SimulatorConfiguration.PacketDuplicationPercent = value;

            networkSimulator.UpdateLiveParameters();

            var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

            var settings = transport.NetworkSettings;
            var simulatorParameters = settings.GetSimulatorStageParameters();

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
