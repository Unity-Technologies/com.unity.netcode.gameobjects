using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Netcode.EditorTests
{
    public class SnapshotTests
    {
        private const double k_Epsilon = 0.0001;

        internal int SendMessage(in SnapshotDataMessage message, NetworkDelivery delivery, ulong clientId)
        {
            Debug.Log("Snapshot Message sent");
            return 0;
        }

        [Test]
        public void TestSnapshot()
        {
            var config = new NetworkConfig();
            var tickSystem = new NetworkTickSystem(15, 0.0, 0.0);
            var timeSystem = new NetworkTimeSystem(0.2, 0.2, 1.0);

            config.UseSnapshotDelta = false;
            config.UseSnapshotSpawn = true;

            var snapshotSystem = new SnapshotSystem(null, config, tickSystem);

            snapshotSystem.IsServer = true;
            snapshotSystem.IsConnectedClient = false;
            snapshotSystem.ServerClientId = 0;
            snapshotSystem.ConnectedClientsId.Clear();
            snapshotSystem.ConnectedClientsId.Add(1);
            snapshotSystem.MockSendMessage = SendMessage;

            SnapshotSpawnCommand command = default;
            // identity
            command.NetworkObjectId = 0;
            // archetype
            command.GlobalObjectIdHash = 0;
            command.IsSceneObject = true;
            // parameters
            command.IsPlayerObject = false;
            command.OwnerClientId = 0;
            command.ParentNetworkId = 0;
            command.ObjectPosition = default;
            command.ObjectRotation = default;
            command.ObjectScale = new Vector3(1.0f, 1.0f, 1.0f);

            command.TargetClientIds = new List<ulong> { 1 };
            snapshotSystem.Spawn(command);

            timeSystem.Advance(0.1);
            tickSystem.UpdateTick(timeSystem.LocalTime, timeSystem.ServerTime);
            snapshotSystem.NetworkUpdate(NetworkUpdateStage.EarlyUpdate);
        }
    }
}

