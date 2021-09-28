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
                Entries.Dispose();
                Spawns.Dispose();
                Despawns.Dispose();
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

            Entries.Dispose();
            Spawns.Dispose();
            Despawns.Dispose();
        }

        public static unsafe void Receive(FastBufferReader reader, in NetworkContext context)
        {
            var networkManager = (NetworkManager)context.SystemOwner;
            var message = new SnapshotDataMessage();
            if (!reader.TryBeginRead(
                FastBufferWriter.GetWriteSize(message.CurrentTick) +
                FastBufferWriter.GetWriteSize(message.Sequence) +
                FastBufferWriter.GetWriteSize(message.Range)
            ))
            {
                throw new OverflowException($"Not enough space to deserialize {nameof(SnapshotDataMessage)}");
            }
            reader.ReadValue(out message.CurrentTick);
            reader.ReadValue(out message.Sequence);

            reader.ReadValue(out message.Range);
            message.ReceiveMainBuffer = new NativeArray<byte>(message.Range, Allocator.Temp);
            reader.ReadBytesSafe((byte*)message.ReceiveMainBuffer.GetUnsafePtr(), message.Range);
            reader.ReadValueSafe(out message.Ack);

            reader.ReadValueSafe(out ushort length);
            message.Entries = new NativeList<EntryData>(length, Allocator.Temp);
            message.Entries.Length = length;
            reader.ReadBytesSafe((byte*)message.Entries.GetUnsafePtr(), message.Entries.Length * sizeof(EntryData));

            reader.ReadValueSafe(out length);
            message.Spawns = new NativeList<SpawnData>(length, Allocator.Temp);
            message.Spawns.Length = length;
            reader.ReadBytesSafe((byte*)message.Spawns.GetUnsafePtr(), message.Spawns.Length * sizeof(SpawnData));

            reader.ReadValueSafe(out length);
            message.Despawns = new NativeList<DespawnData>(length, Allocator.Temp);
            message.Despawns.Length = length;
            reader.ReadBytesSafe((byte*)message.Despawns.GetUnsafePtr(), message.Despawns.Length * sizeof(DespawnData));

            using (message.ReceiveMainBuffer)
            using (message.Entries)
            using (message.Spawns)
            using (message.Despawns)
            {
                message.Handle(context.SenderId, networkManager);
            }
        }

        public void Handle(ulong senderId, NetworkManager networkManager)
        {
            // todo: temporary hack around bug
            if (!networkManager.IsServer)
            {
                senderId = networkManager.ServerClientId;
            }

            var snapshotSystem = networkManager.SnapshotSystem;
            snapshotSystem.HandleSnapshot(senderId, this);
        }
    }
}
