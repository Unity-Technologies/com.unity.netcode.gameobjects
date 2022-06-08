using UnityEngine;

namespace Unity.Netcode.Simulator
{
    [CreateAssetMenu(
        fileName = nameof(NetworkTypeConfiguration),
        menuName = "ScriptableObjects/" + nameof(NetworkTypeConfiguration))]
    public class NetworkTypeConfiguration : ScriptableObject
    {
        [field: SerializeField]
        public string Name { get; set; }

        [field: SerializeField]
        public string Description { get; set; }

        [field: SerializeField]
        public int PacketDelayMs { get; set; }

        [field: SerializeField]
        public int PacketJitterMs { get; set; }

        [field: SerializeField]
        public int PacketLossInterval { get; set; }

        [field: SerializeField]
        public int PacketLossPercent { get; set; }

        [field: SerializeField]
        public int PacketDuplicationPercent { get; set; }

        [field: SerializeField]
        public int PacketFuzzFactor { get; set; }

        [field: SerializeField]
        public int PacketFuzzOffset { get; set; }

        public static NetworkTypeConfiguration Create(
            string name,
            string description,
            int packetDelayMs,
            int packetJitterMs,
            int packetLossInterval,
            int packetLossPercent,
            int packetDuplicationPercent,
            int packetFuzzFactor,
            int packetFuzzOffset)
        {
            var configuration = CreateInstance<NetworkTypeConfiguration>();

            configuration.Name = name;
            configuration.Description = description;
            configuration.PacketDelayMs = packetDelayMs;
            configuration.PacketJitterMs = packetJitterMs;
            configuration.PacketLossInterval = packetLossInterval;
            configuration.PacketLossPercent = packetLossPercent;
            configuration.PacketDuplicationPercent = packetDuplicationPercent;
            configuration.PacketFuzzFactor = packetFuzzFactor;
            configuration.PacketFuzzOffset = packetFuzzOffset;

            return configuration;
        }
    }
}