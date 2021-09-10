using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Netcode.Messages
{
    internal struct SnapshotDataMessage: INetworkMessage
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

        public NativeArray<EntryData> Entries;

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

        public NativeArray<SpawnData> Spawns;

        public struct DespawnData
        {
            public ulong NetworkObjectId;
            public int TickWritten;
        }

        public NativeArray<DespawnData> Despawns;

        public unsafe void Serialize(ref FastBufferWriter writer)
        {
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

        public static unsafe void Receive(ref FastBufferReader reader, NetworkContext context)
        {
            NetworkManager networkManager = (NetworkManager) context.SystemOwner;
            var message = new SnapshotDataMessage();
            reader.ReadValue(out message.CurrentTick);
            reader.ReadValue(out message.Sequence);
            
            reader.ReadValue(out message.Range);
            message.ReceiveMainBuffer = new NativeArray<byte>(message.Range, Allocator.Temp);
            reader.ReadBytes((byte*)message.ReceiveMainBuffer.GetUnsafePtr(), message.Range);
            reader.ReadValue(out message.Ack);

            reader.ReadValue(out ushort length);
            message.Entries = new NativeArray<EntryData>(length, Allocator.Temp);
            reader.ReadBytes((byte*)message.Entries.GetUnsafePtr(), message.Entries.Length * sizeof(EntryData));
            
            reader.ReadValue(out length);
            message.Spawns = new NativeArray<SpawnData>(length, Allocator.Temp);
            reader.ReadBytes((byte*)message.Spawns.GetUnsafePtr(), message.Spawns.Length * sizeof(SpawnData));
            
            reader.ReadValue(out length);
            message.Despawns = new NativeArray<DespawnData>(length, Allocator.Temp);
            reader.ReadBytes((byte*)message.Despawns.GetUnsafePtr(), message.Despawns.Length * sizeof(DespawnData));

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