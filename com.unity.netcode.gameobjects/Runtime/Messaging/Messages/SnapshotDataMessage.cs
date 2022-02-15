using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode
{
    internal struct SnapshotDataMessage : INetworkMessage
    {
        public int CurrentTick;
        public ushort Sequence;

        public ushort Range;

        public byte[] SendMainBuffer;
        public NativeArray<byte> ReceiveMainBuffer;

        public struct AckData
        {
            public ushort LastReceivedSequence;
            public ushort ReceivedSequenceMask;
        }

        public AckData Ack;

        public struct EntryData
        {
            public ulong NetworkObjectId;
            public ushort BehaviourIndex;
            public ushort VariableIndex;
            public int TickWritten;
            public ushort Position;
            public ushort Length;
        }

        public NativeList<EntryData> Entries;

        public struct SpawnData
        {
            public ulong NetworkObjectId;
            public uint Hash;
            public bool IsSceneObject;

            public bool IsPlayerObject;
            public ulong OwnerClientId;
            public ulong ParentNetworkId;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public int TickWritten;
        }

        public NativeList<SpawnData> Spawns;

        public struct DespawnData
        {
            public ulong NetworkObjectId;
            public int TickWritten;
        }

        public NativeList<DespawnData> Despawns;

        public unsafe void Serialize(FastBufferWriter writer)
        {
            if (!writer.TryBeginWrite(
                FastBufferWriter.GetWriteSize(CurrentTick) +
                FastBufferWriter.GetWriteSize(Sequence) +
                FastBufferWriter.GetWriteSize(Range) + Range +
                FastBufferWriter.GetWriteSize(Ack) +
                FastBufferWriter.GetWriteSize<ushort>() +
                Entries.Length * sizeof(EntryData) +
                FastBufferWriter.GetWriteSize<ushort>() +
                Spawns.Length * sizeof(SpawnData) +
                FastBufferWriter.GetWriteSize<ushort>() +
                Despawns.Length * sizeof(DespawnData)
            ))
            {
                throw new OverflowException($"Not enough space to serialize {nameof(SnapshotDataMessage)}");
            }
            writer.WriteValue(CurrentTick);
            writer.WriteValue(Sequence);

            writer.WriteValue(Range);
            writer.WriteBytes(SendMainBuffer, Range);
            writer.WriteValue(Ack);

            writer.WriteValue((ushort)Entries.Length);
            writer.WriteBytes((byte*)Entries.GetUnsafePtr(), Entries.Length * sizeof(EntryData));

            writer.WriteValue((ushort)Spawns.Length);
            writer.WriteBytes((byte*)Spawns.GetUnsafePtr(), Spawns.Length * sizeof(SpawnData));

            writer.WriteValue((ushort)Despawns.Length);
            writer.WriteBytes((byte*)Despawns.GetUnsafePtr(), Despawns.Length * sizeof(DespawnData));
        }

        public unsafe bool Deserialize(FastBufferReader reader, ref NetworkContext context)
        {
            if (!reader.TryBeginRead(
                FastBufferWriter.GetWriteSize(CurrentTick) +
                FastBufferWriter.GetWriteSize(Sequence) +
                FastBufferWriter.GetWriteSize(Range)
            ))
            {
                throw new OverflowException($"Not enough space to deserialize {nameof(SnapshotDataMessage)}");
            }
            reader.ReadValue(out CurrentTick);
            reader.ReadValue(out Sequence);

            reader.ReadValue(out Range);
            ReceiveMainBuffer = new NativeArray<byte>(Range, Allocator.Temp);
            reader.ReadBytesSafe((byte*)ReceiveMainBuffer.GetUnsafePtr(), Range);
            reader.ReadValueSafe(out Ack);

            reader.ReadValueSafe(out ushort length);
            Entries = new NativeList<EntryData>(length, Allocator.Temp) { Length = length };
            reader.ReadBytesSafe((byte*)Entries.GetUnsafePtr(), Entries.Length * sizeof(EntryData));

            reader.ReadValueSafe(out length);
            Spawns = new NativeList<SpawnData>(length, Allocator.Temp) { Length = length };
            reader.ReadBytesSafe((byte*)Spawns.GetUnsafePtr(), Spawns.Length * sizeof(SpawnData));

            reader.ReadValueSafe(out length);
            Despawns = new NativeList<DespawnData>(length, Allocator.Temp) { Length = length };
            reader.ReadBytesSafe((byte*)Despawns.GetUnsafePtr(), Despawns.Length * sizeof(DespawnData));

            return true;
        }

        public void Handle(ref NetworkContext context)
        {
            using (ReceiveMainBuffer)
            using (Entries)
            using (Spawns)
            using (Despawns)
            {
                var systemOwner = context.SystemOwner;
                var senderId = context.SenderId;
                if (systemOwner is NetworkManager networkManager)
                {
                    // todo: temporary hack around bug
                    if (!networkManager.IsServer)
                    {
                        senderId = networkManager.ServerClientId;
                    }

                    var snapshotSystem = networkManager.SnapshotSystem;
                    snapshotSystem.HandleSnapshot(senderId, this);
                }
                else
                {
                    var ownerData = (Tuple<SnapshotSystem, ulong>)systemOwner;
                    var snapshotSystem = ownerData.Item1;
                    snapshotSystem.HandleSnapshot(ownerData.Item2, this);
                }
            }
        }
    }
}
