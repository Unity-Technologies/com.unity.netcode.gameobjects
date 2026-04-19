using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Experimental synchronization of position using <see cref="half3"/>.
    /// </summary>
    [BurstCompile]
    internal unsafe struct Vector3HalfState : ITransformStateComponent<Vector3HalfState>
    {
        internal const int Length = 3;
        public int X;
        public int Y;
        public int Z;
        public half3 Axis;

        public float InvPrecision;
        public byte AxisWritten;
        public int CompressedSize;


        /// <summary>
        /// Gets the full precision value as a <see cref="Vector3"/>.
        /// </summary>
        /// <returns>a <see cref="Vector3"/> as the full precision value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToVector3()
        {
            return math.float3(Axis);
        }

        public void ApplyState(Vector3HalfState state)
        {
            X = state.X;
            Y = state.Y;
            Z = state.Z;
            InvPrecision = state.InvPrecision;
            Axis = state.Axis;
        }

        public bool HasDelta()
        {
            return !(X == 0 && Y == 0 && Z == 0);
        }

        public void Clear()
        {
            Axis = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Compress()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadBytes(byte* destination, byte* source, int numBytes)
        {
            switch (numBytes)
            {
                case 1:
                    destination[0] = source[0];
                    break;
                case 2:
                    destination[0] = source[0];
                    destination[1] = source[1];
                    break;
                case 3:
                    destination[0] = source[0];
                    destination[1] = source[1];
                    destination[2] = source[2];
                    break;
                case 4:
                    destination[0] = source[0];
                    destination[1] = source[1];
                    destination[2] = source[2];
                    destination[3] = source[3];
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Decompress()
        {
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }


        public static float ClampDecimalPlaces(float value, int decimalPlaces)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Must be non-negative.");
            }
            float factor = (float)math.pow(10, decimalPlaces);
            return (float)(math.trunc(value * factor) / factor);
        }

        public void WriteState(FastBufferWriter writer)
        {
            AxisWritten = 0;
            var position = writer.Position;
            var debugPosition = position;
            writer.WriteByteSafe(AxisWritten);
            //var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            //var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            //var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            //var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            //var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            //var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);
            // var debugInfo = "[Vector3Half]";
            //if (xBytes > 0)
            if (math.abs(X) > 0)
            {
                AxisWritten |= 0x01;
                BytePacker.WriteValuePacked(writer, Axis.x.value);
                //debugInfo += $"[X: {writer.Position - debugPosition} | {X}]";
                debugPosition = writer.Position;
            }
            //if (yBytes > 0)
            if (math.abs(Y) > 0)
            {
                AxisWritten |= 0x02;
                BytePacker.WriteValuePacked(writer, Axis.y.value);
                //debugInfo += $"[Y: {writer.Position - debugPosition} | {Y}]";
                debugPosition = writer.Position;
            }
            //if (zBytes > 0)
            if (math.abs(Z) > 0)
            {
                AxisWritten |= 0x04;
                BytePacker.WriteValuePacked(writer, Axis.z.value);
                //debugInfo += $"[Z: {writer.Position - debugPosition} | {Z}]";
                debugPosition = writer.Position;
            }
            var tailPosition = writer.Position;
            writer.Seek(position);
            writer.WriteByteSafe(AxisWritten);
            writer.Seek(tailPosition);
            //if ((xBytes + yBytes + zBytes) > 0)
            //{
            //    Debug.Log(debugInfo);
            //}
        }
        public void ReadState(FastBufferReader reader)
        {
            var position = reader.Position;
            reader.ReadByteSafe(out AxisWritten);
            var halfValue = (ushort)0;
            if ((AxisWritten & 0x01) == 0x01)
            {
                ByteUnpacker.ReadValuePacked(reader, out halfValue);
                Axis.x.value = halfValue;

            }
            if ((AxisWritten & 0x02) == 0x02)
            {
                ByteUnpacker.ReadValuePacked(reader, out halfValue);
                Axis.y.value = halfValue;
            }
            if ((AxisWritten & 0x04) == 0x04)
            {
                ByteUnpacker.ReadValuePacked(reader, out halfValue);
                Axis.z.value = halfValue;
            }
        }

        public Vector3 UpdateFromValue(Vector3 value)
        {
            var stateUpdate = ToVector3();
            if ((AxisWritten & 0x01) != 0x01)
            {
                stateUpdate.x = value.x;
            }
            if ((AxisWritten & 0x02) != 0x02)
            {
                stateUpdate.y = value.y;
            }
            if ((AxisWritten & 0x04) != 0x04)
            {
                stateUpdate.z = value.z;
            }
            return stateUpdate;
        }
    }
}
