using Unity.Burst;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Experimental synchronization of a forward vector with magnitude.
    /// </summary>
    [BurstCompile]
    internal unsafe struct ForwardVector : ITransformStateComponent<ForwardVector>
    {
        internal const int Length = 3;
        public uint X;
        public uint Y;
        public uint Z;

        public Vector3 Forward;


        public Quaternion Rotation;


        public float InvPrecision;


        public void ApplyState(ForwardVector state)
        {
            X = state.X;
            Y = state.Y;
            Z = state.Z;
            Forward = state.Forward;
        }

        public bool HasDelta()
        {
            return !(X == 0 && Y == 0 && Z == 0);
        }

        public void Clear()
        {
            X = Y = Z = 0;
            Forward = Vector3.zero;
        }

        public unsafe void Compress()
        {

        }


        public unsafe void Decompress()
        {
        }

        public void Initialize()
        {

        }

        public void Dispose()
        {
        }
        public void WriteState(FastBufferWriter writer)
        {
            var scaleFactor = 127f;

            var forwardAsScaleFactor = stackalloc byte[3] { 0x00, 0x00, 0x00 };

            for (int i = 0; i < 3; i++)
            {
                var negativeMask = (byte)(Forward[i] < 0.0f ? 0x80 : 0x00);
                forwardAsScaleFactor[i] = (byte)(negativeMask | (0x7F & (byte)(Forward[i] * scaleFactor)));
            }
            Debug.Log($"[ForwardVector][WRITE][X: {Forward[0]} | {forwardAsScaleFactor[0]}][Y: {Forward[1]} | {forwardAsScaleFactor[1]}][Z: {Forward[2]} | {forwardAsScaleFactor[2]}]");
            writer.WriteBytesSafe(forwardAsScaleFactor, 3);

        }
        public void ReadState(FastBufferReader reader)
        {
            var scaleFactor = 1.0f / 127.0f;
            var forwardAsScaleFactor = stackalloc byte[3] { 0x00, 0x00, 0x00 };
            reader.ReadBytesSafe(forwardAsScaleFactor, 3);
            for (int i = 0; i < 3; i++)
            {
                var negative = ((0x80 & forwardAsScaleFactor[i]) == 0x80) ? -1.0f : 1.0f;
                Forward[i] = (forwardAsScaleFactor[i] & 0x7F) * scaleFactor * negative;
            }
            Debug.Log($"[ForwardVector][WRITE][X: {Forward[0]} | {forwardAsScaleFactor[0]}][Y: {Forward[1]} | {forwardAsScaleFactor[1]}][Z: {Forward[2]} | {forwardAsScaleFactor[2]}]");
        }
    }
}
