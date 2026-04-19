using Unity.Burst;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Job friendly version of synchronizing a quaternion in 4 bytes.
    /// </summary>
    [BurstCompile]
    internal unsafe struct QuaternionState : ITransformStateComponent<QuaternionState>
    {
        public bool IsDirty;
        public uint Compressed;

        public int X;
        public int Y;
        public int Z;
        public int W;

        public Quaternion Rotation;

        public void ApplyState(QuaternionState state)
        {
            X = state.X;
            Y = state.Y;
            Z = state.Z;
            W = state.W;
            Rotation = state.Rotation;
            Compressed = state.Compressed;
        }

        public bool HasDelta()
        {
            return !(X == 0 && Y == 0 && Z == 0 && W == 0);
        }

        public void Clear()
        {
            IsDirty = false;
            Compressed = 0;
            Rotation = Quaternion.identity;
        }

        public void Initialize()
        {
            Clear();
        }

        [BurstCompile]
        public void Dispose()
        {
            Clear();
        }

        [BurstCompile]
        public void Compress()
        {
            Compressed = QuaternionCompressorJob.CompressQuaternion(ref Rotation);
        }

        public void Decompress()
        {
            QuaternionCompressorJob.DecompressQuaternion(ref Rotation, Compressed);
        }

        public unsafe void WriteState(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Compressed);
        }

        public unsafe void ReadState(FastBufferReader reader)
        {
            reader.ReadValueSafe(out Compressed);
        }
    }
}
