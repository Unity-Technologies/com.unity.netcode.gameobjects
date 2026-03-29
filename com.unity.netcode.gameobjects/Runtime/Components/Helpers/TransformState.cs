using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Unity.Netcode
{

    [BurstCompile]
    internal struct TransformState
    {
        /// <summary>
        /// Needs to be converted to a byte
        /// </summary>
        public TransformIntState PreviousState;
        public TransformIntState CurrentState;
        public TransformIntState Delta;

        public ulong NetworkObjectId;
        public ushort NetworkBehaviourId;

        public ulong EntityIdentifier;

        public void UpdateIds(TransformStateSync transformStateSync)
        {
            EntityIdentifier = EntityId.ToULong(transformStateSync.GetEntityId());
            NetworkObjectId = transformStateSync.NetworkObjectId;
            NetworkBehaviourId = transformStateSync.NetworkBehaviourId;
            GridStateDelta.NetworkObjectId = GridStatePrevious.NetworkObjectId = GridStateCurrent.NetworkObjectId = NetworkObjectId;
            GridStateDelta.NetworkBehaviourId = GridStatePrevious.NetworkBehaviourId = GridStateCurrent.NetworkBehaviourId = NetworkBehaviourId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ProcessCurrentStateOriginal(TransformAccess transformAccess, int precision, bool isNextTick)
        {
            CurrentState.Scale.X = (int)(transformAccess.localScale.x * precision);
            CurrentState.Scale.Y = (int)(transformAccess.localScale.y * precision);
            CurrentState.Scale.Z = (int)(transformAccess.localScale.z * precision);

            CurrentState.Position.X = (int)(transformAccess.position.x * precision);
            CurrentState.Position.Y = (int)(transformAccess.position.y * precision);
            CurrentState.Position.Z = (int)(transformAccess.position.z * precision);

            CurrentState.Rotation.X = (int)(transformAccess.rotation.x * precision);
            CurrentState.Rotation.Y = (int)(transformAccess.rotation.y * precision);
            CurrentState.Rotation.Z = (int)(transformAccess.rotation.z * precision);
            CurrentState.Rotation.W = (int)(transformAccess.rotation.w * precision);

            if (isNextTick)
            {
                Delta.Scale.X = CurrentState.Scale.X - PreviousState.Scale.X;
                Delta.Scale.Y = CurrentState.Scale.Y - PreviousState.Scale.Y;
                Delta.Scale.Z = CurrentState.Scale.Z - PreviousState.Scale.Z;

                Delta.Position.X = CurrentState.Position.X - PreviousState.Position.X;
                Delta.Position.Y = CurrentState.Position.Y - PreviousState.Position.Y;
                Delta.Position.Z = CurrentState.Position.Z - PreviousState.Position.Z;

                Delta.Rotation.X = CurrentState.Rotation.X - PreviousState.Rotation.X;
                Delta.Rotation.Y = CurrentState.Rotation.Y - PreviousState.Rotation.Y;
                Delta.Rotation.Z = CurrentState.Rotation.Z - PreviousState.Rotation.Z;
                Delta.Rotation.W = CurrentState.Rotation.W - PreviousState.Rotation.W;
                PreviousState.ApplyState(CurrentState);
            }

            return Delta.HasDelta();
        }

        public TransformGridState GridStatePrevious;
        public TransformGridState GridStateCurrent;
        public TransformGridState GridStateDelta;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ProcessCurrentState(int index, TransformAccess transformAccess, int precision, bool isNextTick)
        {
            if (isNextTick && transformAccess.isValid)
            {

                GridStateCurrent.Index = index;
                GridStateCurrent.Scale.X = (uint)(transformAccess.localScale.x * precision);
                GridStateCurrent.Scale.Y = (uint)(transformAccess.localScale.y * precision);
                GridStateCurrent.Scale.Z = (uint)(transformAccess.localScale.z * precision);

                GridStateCurrent.Position.X = (uint)(transformAccess.position.x * precision);
                GridStateCurrent.Position.Y = (uint)(transformAccess.position.y * precision);
                GridStateCurrent.Position.Z = (uint)(transformAccess.position.z * precision);


                GridStateCurrent.Rotation.X = (uint)(transformAccess.rotation.x * precision);
                GridStateCurrent.Rotation.Y = (uint)(transformAccess.rotation.y * precision);
                GridStateCurrent.Rotation.Z = (uint)(transformAccess.rotation.z * precision);
                GridStateCurrent.Rotation.W = (uint)(transformAccess.rotation.w * precision);
                GridStateCurrent.Rotation.Rotation = transformAccess.rotation;


                GridStateDelta.Index = index;
                GridStateDelta.DirtyScale = false;
                GridStateDelta.Scale.X = GridStateCurrent.Scale.X - GridStatePrevious.Scale.X;
                GridStateDelta.Scale.Y = GridStateCurrent.Scale.Y - GridStatePrevious.Scale.Y;
                GridStateDelta.Scale.Z = GridStateCurrent.Scale.Z - GridStatePrevious.Scale.Z;

                GridStateDelta.Position.X = GridStateCurrent.Position.X - GridStatePrevious.Position.X;
                GridStateDelta.Position.Y = GridStateCurrent.Position.Y - GridStatePrevious.Position.Y;
                GridStateDelta.Position.Z = GridStateCurrent.Position.Z - GridStatePrevious.Position.Z;

                GridStateDelta.Rotation.X = GridStateCurrent.Rotation.X - GridStatePrevious.Rotation.X;
                GridStateDelta.Rotation.Y = GridStateCurrent.Rotation.Y - GridStatePrevious.Rotation.Y;
                GridStateDelta.Rotation.Z = GridStateCurrent.Rotation.Z - GridStatePrevious.Rotation.Z;
                GridStateDelta.Rotation.W = GridStateCurrent.Rotation.W - GridStatePrevious.Rotation.W;

                GridStateDelta.DirtyScale = false;
                if (GridStateDelta.Scale.HasDelta())
                {
                    GridStateDelta.DirtyScale = true;
                    GridStateDelta.Scale.Axis = new half3(transformAccess.localScale);
                    GridStateDelta.Scale.InvPrecision = 1.0f / precision;
                    //GridStateDelta.Scale.Compress();
                }

                GridStateDelta.DirtyPosition = false;
                if (GridStateDelta.Position.HasDelta())
                {
                    GridStateDelta.DirtyPosition = true;
                    GridStateDelta.Position.Axis = new half3(transformAccess.position);
                    GridStateDelta.Position.InvPrecision = 1.0f / precision;
                    //GridStateDelta.Position.Compress();
                }

                GridStateDelta.DirtyRotation = false;
                GridStateDelta.Rotation.IsDirty = false;
                if (GridStateDelta.Rotation.HasDelta())
                {
                    GridStateDelta.DirtyRotation = true;
                    GridStateDelta.Rotation.IsDirty = true;
                    GridStateDelta.Rotation.ApplyState(GridStateCurrent.Rotation);
                    GridStateDelta.Rotation.Compress();
                }
                GridStatePrevious.ApplyState(GridStateCurrent);
            }

            return GridStateDelta.HasDelta();
        }
    }

    [BurstCompile]
    internal unsafe struct Vector3State : ITransformStateComponent<Vector3State>
    {
        public int X;
        public int Y;
        public int Z;

        public byte AxisWritten;
        public int CompressedSize;
        public byte* Compressed;

        public void ApplyState(Vector3State state)
        {
            X = state.X;
            Y = state.Y;
            Z = state.Z;
        }

        public bool HasDelta()
        {
            return !(X == 0 && Y == 0 && Z == 0);
        }

        public void Clear()
        {
            X = Y = Z = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Compress()
        {
            var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);

            CompressedSize = (int)(xBytes + yBytes + zBytes);

            if (CompressedSize == 0)
            {
                // Warning?
                return;
            }

            uX |= xBytes;
            uY |= yBytes;
            uZ |= zBytes;

            UnsafeUtility.MemSet(&Compressed[0], 0, sizeof(int) * 4);


            // Get pointers to the values
            var xPtr = (byte*)&uX;
            var yPtr = (byte*)&uY;
            var zPtr = (byte*)&uZ;

            var offset = 1;
            // Write the compressed values
            if (xBytes > 0)
            {
                AxisWritten |= 0x01;
                UnsafeUtility.MemCpy(&Compressed[offset], xPtr, xBytes);
                offset += (int)xBytes;
            }
            if (yBytes > 0)
            {
                AxisWritten |= (0x01 << 1);
                UnsafeUtility.MemCpy(&Compressed[offset], yPtr, yBytes);
                offset += (int)yBytes;
            }
            if (zBytes > 0)
            {
                AxisWritten |= (0x01 << 2);
                UnsafeUtility.MemCpy(&Compressed[offset], zPtr, zBytes);
                offset += (int)zBytes;
            }

            Compressed[0] = AxisWritten;
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
                    //case 5:
                    //    // First byte contains no data, it's just a marker. The data is in the remaining two bytes.
                    //    destination[0] = source[1];
                    //    destination[1] = source[2];
                    //    destination[2] = source[3];
                    //    destination[3] = source[4];
                    //    value = returnValue;
                    //    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Decompress()
        {
            /// TODO: Implenent any form of grid compression here.

            AxisWritten = Compressed[0];
            // Byte count is stored in lowest bit positions
            var uX = (uint)X;
            var uY = (uint)Y;
            var uZ = (uint)Z;

            // Get pointers to the values
            var xPtr = (byte*)&uX;
            var yPtr = (byte*)&uY;
            var zPtr = (byte*)&uZ;
            var offset = 1;
            if ((AxisWritten & 0x01) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(xPtr, &Compressed[offset], numBytes);
                uX = uX >> 3;
                X = (int)Arithmetic.ZigZagDecode(uX);
                offset += numBytes;
            }

            if ((AxisWritten & 0x02) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(yPtr, &Compressed[offset], numBytes);
                uY = uY >> 3;
                Y = (int)Arithmetic.ZigZagDecode(uY);
                offset += numBytes;
            }

            if ((AxisWritten & 0x04) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(zPtr, &Compressed[offset], numBytes);
                uZ = uZ >> 3;
                Z = (int)Arithmetic.ZigZagDecode(uZ);
                offset += numBytes;
            }
        }

        public void Initialize()
        {
            Compressed = (byte*)UnsafeUtility.Malloc((sizeof(int) * 3) + 1, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(Compressed, Allocator.Persistent);
        }

        public void WriteState(FastBufferWriter writer)
        {
            AxisWritten = 0;
            var position = writer.Position;
            writer.WriteByteSafe(AxisWritten);
            var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);
            if (xBytes > 0)
            {
                AxisWritten |= 0x01;
                BytePacker.WriteValuePacked(writer, X);
            }
            if (yBytes > 0)
            {
                AxisWritten |= 0x02;
                BytePacker.WriteValuePacked(writer, Y);
            }
            if (zBytes > 0)
            {
                AxisWritten |= 0x04;
                BytePacker.WriteValuePacked(writer, Z);
            }
            var tailPosition = writer.Position;
            writer.Seek(position);
            writer.WriteByteSafe(AxisWritten);
            writer.Seek(tailPosition);
        }
        public void ReadState(FastBufferReader reader)
        {
            var position = reader.Position;
            reader.ReadByteSafe(out AxisWritten);
            if ((AxisWritten & 0x01) > 0)
            {
                ByteUnpacker.ReadValuePacked(reader, out X);
            }
            if ((AxisWritten & 0x02) > 0)
            {
                AxisWritten |= 0x02;
                ByteUnpacker.ReadValuePacked(reader, out Y);
            }
            if ((AxisWritten & 0x04) > 0)
            {
                ByteUnpacker.ReadValuePacked(reader, out Z);
            }
        }
    }

    internal unsafe struct Vector3Half : ITransformStateComponent<Vector3Half>
    {
        internal const int Length = 3;
        public uint X;
        public uint Y;
        public uint Z;
        public half3 Axis;

        public float InvPrecision;
        public byte AxisWritten;
        public int CompressedSize;
        public byte* Compressed;


        /// <summary>
        /// Gets the full precision value as a <see cref="Vector3"/>.
        /// </summary>
        /// <returns>a <see cref="Vector3"/> as the full precision value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 ToVector3()
        {
            return math.float3(Axis);
        }

        public void ApplyState(Vector3Half state)
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
            Axis = default(half3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Compress()
        {
            var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);

            CompressedSize = (int)(xBytes + yBytes + zBytes);

            if (CompressedSize == 0)
            {
                // Warning?
                return;
            }

            uX |= xBytes;
            uY |= yBytes;
            uZ |= zBytes;

            UnsafeUtility.MemSet(&Compressed[0], 0, sizeof(int) * 4);


            // Get pointers to the values
            var xPtr = (byte*)&uX;
            var yPtr = (byte*)&uY;
            var zPtr = (byte*)&uZ;

            var offset = 1;
            // Write the compressed values
            if (xBytes > 0)
            {
                AxisWritten |= 0x01;
                UnsafeUtility.MemCpy(&Compressed[offset], xPtr, xBytes);
                offset += (int)xBytes;
            }
            if (yBytes > 0)
            {
                AxisWritten |= (0x01 << 1);
                UnsafeUtility.MemCpy(&Compressed[offset], yPtr, yBytes);
                offset += (int)yBytes;
            }
            if (zBytes > 0)
            {
                AxisWritten |= (0x01 << 2);
                UnsafeUtility.MemCpy(&Compressed[offset], zPtr, zBytes);
                offset += (int)zBytes;
            }

            Compressed[0] = AxisWritten;
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
                    //case 5:
                    //    // First byte contains no data, it's just a marker. The data is in the remaining two bytes.
                    //    destination[0] = source[1];
                    //    destination[1] = source[2];
                    //    destination[2] = source[3];
                    //    destination[3] = source[4];
                    //    value = returnValue;
                    //    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Decompress()
        {
            AxisWritten = Compressed[0];
            // Byte count is stored in lowest bit positions
            var uX = X;
            var uY = Y;
            var uZ = Z;

            // Get pointers to the values
            var xPtr = (byte*)&uX;
            var yPtr = (byte*)&uY;
            var zPtr = (byte*)&uZ;
            var offset = 1;
            if ((AxisWritten & 0x01) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(xPtr, &Compressed[offset], numBytes);
                uX = uX >> 3;
                X = (ushort)Arithmetic.ZigZagDecode(uX);
                Axis.x = math.half(X * InvPrecision);
                offset += numBytes;
            }

            if ((AxisWritten & 0x02) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(yPtr, &Compressed[offset], numBytes);
                uY = uY >> 3;
                Y = (ushort)Arithmetic.ZigZagDecode(uY);
                Axis.y = math.half(Y * InvPrecision);
                offset += numBytes;
            }

            if ((AxisWritten & 0x04) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(zPtr, &Compressed[offset], numBytes);
                uZ = uZ >> 3;
                Z = (ushort)Arithmetic.ZigZagDecode(uZ);
                Axis.z = math.half(Z * InvPrecision);
                offset += numBytes;
            }
        }

        public void Initialize()
        {
            Compressed = (byte*)UnsafeUtility.Malloc((sizeof(int) * 3) + 1, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(Compressed, Allocator.Persistent);
        }

        public void WriteState(FastBufferWriter writer)
        {
            AxisWritten = 0;
            var position = writer.Position;
            writer.WriteByteSafe(AxisWritten);
            var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);
            if (xBytes > 0)
            {
                AxisWritten |= 0x01;
                BytePacker.WriteValuePacked(writer, Axis.x.value);
            }
            if (yBytes > 0)
            {
                AxisWritten |= 0x02;
                BytePacker.WriteValuePacked(writer, Axis.y.value);
            }
            if (zBytes > 0)
            {
                AxisWritten |= 0x04;
                BytePacker.WriteValuePacked(writer, Axis.z.value);
            }
            var tailPosition = writer.Position;
            writer.Seek(position);
            writer.WriteByteSafe(AxisWritten);
            writer.Seek(tailPosition);
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
                AxisWritten |= 0x02;
                ByteUnpacker.ReadValuePacked(reader, out halfValue);
                Axis.y.value = halfValue;
            }
            if ((AxisWritten & 0x04) == 0x04)
            {
                ByteUnpacker.ReadValuePacked(reader, out halfValue);
                Axis.z.value = halfValue;
            }
        }
    }

    [BurstCompile]
    internal unsafe struct QuaternionState : ITransformStateComponent<QuaternionState>
    {
        public bool IsDirty;
        public uint Compressed;

        public uint X;
        public uint Y;
        public uint Z;
        public uint W;

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

        public void Dispose()
        {
            Clear();
        }

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



    [BurstCompile]
    internal unsafe struct Vector4State : ITransformStateComponent<Vector4State>
    {
        public int X;
        public int Y;
        public int Z;
        public int W;

        public byte AxisWritten;
        public int CompressedSize;
        public byte* Compressed;

        public void ApplyState(Vector4State state)
        {
            X = state.X;
            Y = state.Y;
            Z = state.Z;
            W = state.W;
        }

        public bool HasDelta()
        {
            return !(X == 0 && Y == 0 && Z == 0 && W == 0);
        }

        public void Clear()
        {
            X = Y = Z = W = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Compress()
        {
            var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            var uW = ((uint)Arithmetic.ZigZagEncode(W)) << 3;
            var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);
            var wBytes = (uint)BitCounter.GetUsedByteCount(uW);

            CompressedSize = (int)(xBytes + yBytes + zBytes + wBytes);

            if (CompressedSize == 0)
            {
                // Warning?
                return;
            }

            uX |= xBytes;
            uY |= yBytes;
            uZ |= zBytes;
            uW |= wBytes;

            UnsafeUtility.MemSet(&Compressed[0], 0, sizeof(int) * 4);


            // Get pointers to the values
            var xPtr = (byte*)&uX;
            var yPtr = (byte*)&uY;
            var zPtr = (byte*)&uZ;
            var wPtr = (byte*)&uW;

            var offset = 1;
            // Write the compressed values
            if (xBytes > 0)
            {
                AxisWritten |= 0x01;
                UnsafeUtility.MemCpy(&Compressed[offset], xPtr, xBytes);
                offset += (int)xBytes;
            }
            if (yBytes > 0)
            {
                AxisWritten |= (0x01 << 1);
                UnsafeUtility.MemCpy(&Compressed[offset], yPtr, yBytes);
                offset += (int)yBytes;
            }
            if (zBytes > 0)
            {
                AxisWritten |= (0x01 << 2);
                UnsafeUtility.MemCpy(&Compressed[offset], zPtr, zBytes);
                offset += (int)zBytes;
            }
            if (wBytes > 0)
            {
                AxisWritten |= (0x01 << 3);
                UnsafeUtility.MemCpy(&Compressed[offset], wPtr, wBytes);
                offset += (int)wBytes;
            }

            Compressed[0] = AxisWritten;
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
                    //case 5:
                    //    // First byte contains no data, it's just a marker. The data is in the remaining two bytes.
                    //    destination[0] = source[1];
                    //    destination[1] = source[2];
                    //    destination[2] = source[3];
                    //    destination[3] = source[4];
                    //    value = returnValue;
                    //    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Decompress()
        {
            AxisWritten = Compressed[0];
            // Byte count is stored in lowest bit positions
            var uX = (uint)X;
            var uY = (uint)Y;
            var uZ = (uint)Z;
            var uW = (uint)W;

            // Get pointers to the values
            var xPtr = (byte*)&uX;
            var yPtr = (byte*)&uY;
            var zPtr = (byte*)&uZ;
            var wPtr = (byte*)&uW;
            var offset = 1;
            if ((AxisWritten & 0x01) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(xPtr, &Compressed[offset], numBytes);
                uX = uX >> 3;
                X = (int)Arithmetic.ZigZagDecode(uX);
                offset += numBytes;
            }

            if ((AxisWritten & 0x02) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(yPtr, &Compressed[offset], numBytes);
                uY = uY >> 3;
                Y = (int)Arithmetic.ZigZagDecode(uY);
                offset += numBytes;
            }

            if ((AxisWritten & 0x04) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(zPtr, &Compressed[offset], numBytes);
                uZ = uZ >> 3;
                Z = (int)Arithmetic.ZigZagDecode(uZ);
                offset += numBytes;
            }

            if ((AxisWritten & 0x08) > 0)
            {
                int numBytes = (Compressed[offset] & 0b111);
                ReadBytes(wPtr, &Compressed[offset], numBytes);
                uW = uW >> 3;
                W = (int)Arithmetic.ZigZagDecode(uW);
                offset += numBytes;
            }
        }

        public void Initialize()
        {
            Compressed = (byte*)UnsafeUtility.Malloc((sizeof(int) * 4) + 1, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(Compressed, Allocator.Persistent);
        }

        public void WriteState(FastBufferWriter writer)
        {
            AxisWritten = 0;
            var position = writer.Position;
            writer.WriteByteSafe(AxisWritten);
            var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            var uW = ((uint)Arithmetic.ZigZagEncode(W)) << 3;
            var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);
            var wBytes = (uint)BitCounter.GetUsedByteCount(uW);
            if (xBytes > 0)
            {
                AxisWritten |= 0x01;
                BytePacker.WriteValuePacked(writer, X);
            }
            if (yBytes > 0)
            {
                AxisWritten |= 0x02;
                BytePacker.WriteValuePacked(writer, Y);
            }
            if (zBytes > 0)
            {
                AxisWritten |= 0x04;
                BytePacker.WriteValuePacked(writer, Z);
            }
            if (wBytes > 0)
            {
                AxisWritten |= 0x08;
                BytePacker.WriteValuePacked(writer, W);
            }
            var tailPosition = writer.Position;
            writer.Seek(position);
            writer.WriteByteSafe(AxisWritten);
            writer.Seek(tailPosition);
        }
        public void ReadState(FastBufferReader reader)
        {
            var position = reader.Position;
            reader.ReadByteSafe(out AxisWritten);
            if ((AxisWritten & 0x01) > 0)
            {
                ByteUnpacker.ReadValuePacked(reader, out X);
            }
            if ((AxisWritten & 0x02) > 0)
            {
                AxisWritten |= 0x02;
                ByteUnpacker.ReadValuePacked(reader, out Y);
            }
            if ((AxisWritten & 0x04) > 0)
            {
                ByteUnpacker.ReadValuePacked(reader, out Z);
            }
            if ((AxisWritten & 0x08) > 0)
            {
                ByteUnpacker.ReadValuePacked(reader, out W);
            }
        }
    }


    internal struct TransformGridState : ITransformState<TransformGridState>
    {
        public ulong NetworkObjectId;
        public ushort NetworkBehaviourId;
        public float Precision;
        public float InvPrecision;
        public bool DirtyScale;
        public bool DirtyPosition;
        public bool DirtyRotation;
        public Vector3Half Scale;
        public Vector3Half Position;
        public QuaternionState Rotation;

        public Vector3 ScaleFloat;
        public Vector3 PositionFloat;
        public int Index;

        public void ApplyState(TransformGridState state)
        {
            if (NetworkObjectId != state.NetworkObjectId)
            {
                Debug.Log($"MISMATCH CONFLICT IN STATE PROCESSING! Applying NID: {state.NetworkObjectId} to previous state for NID: {NetworkObjectId}!");
            }
            Index = state.Index;
            Precision = state.Precision;
            InvPrecision = state.InvPrecision;
            DirtyScale = state.DirtyScale;
            DirtyPosition = state.DirtyPosition;
            DirtyRotation = state.DirtyRotation;

            Scale.ApplyState(state.Scale);
            Position.ApplyState(state.Position);
            Rotation.ApplyState(state.Rotation);
        }

        public bool HasDelta()
        {
            //return Scale.HasDelta() || Position.HasDelta() || Rotation.HasDelta();
            return Position.HasDelta() || Rotation.HasDelta();
        }

        public void Clear()
        {
            DirtyScale = false;
            DirtyPosition = false;
            DirtyRotation = false;
            InvPrecision = 0.0f;
            Precision = 0.0f;
            ScaleFloat = Vector3.zero;
            PositionFloat = Vector3.zero;
            Index = 0;
            Scale.Clear();
            Position.Clear();
            Rotation.Clear();
        }

        public void Initialize()
        {
            Scale.Initialize();
            Position.Initialize();
            Rotation.Initialize();
        }

        public void Dispose()
        {
            Scale.Dispose();
            Position.Dispose();
            Rotation.Dispose();
        }

        public void Compress()
        {
            if (Scale.HasDelta())
            {
                Scale.Compress();
            }
            if (Position.HasDelta())
            {
                Position.Compress();
            }
            if (Rotation.HasDelta())
            {
                Rotation.Compress();
            }
        }

        public void Decompress()
        {
            if (DirtyScale)
            {
                Scale.InvPrecision = InvPrecision;
                //Scale.Decompress();

                ScaleFloat = Scale.ToVector3();
            }

            if (DirtyPosition)
            {
                Position.InvPrecision = InvPrecision;
                //Position.Decompress();
                PositionFloat = Position.ToVector3();
            }

            if (DirtyRotation)
            {
                Rotation.Decompress();
            }
        }


        public int Header_Size;
        public int Payload_Size;

        public unsafe void WriteState(FastBufferWriter writer)
        {
            var dirtyFlags = (byte)0;
            var startPosition = writer.Position;
            BytePacker.WriteValuePacked(writer, NetworkObjectId);
            BytePacker.WriteValuePacked(writer, NetworkBehaviourId);
            var dirtyPosition = writer.Position;
            writer.WriteByteSafe(dirtyFlags);
            Header_Size = writer.Position - startPosition;
            startPosition = writer.Position;
            //if (Scale.HasDelta())
            //{
            //    dirtyFlags |= 0x01;
            //    Scale.WriteState(writer);
            //}

            if (Position.HasDelta())
            {
                dirtyFlags |= 0x02;
                Position.WriteState(writer);
            }

            if (Rotation.HasDelta())
            {
                dirtyFlags |= 0x04;
                Rotation.WriteState(writer);
            }
            var tail = writer.Position;
            writer.Seek(dirtyPosition);
            writer.WriteValueSafe(dirtyFlags);
            writer.Seek(tail);
            Payload_Size = writer.Position - startPosition;
            DirtyFlags = dirtyFlags;
        }

        public byte DirtyFlags { get; private set; }
        public unsafe void ReadState(FastBufferReader reader)
        {
            var dirtyFlags = (byte)0;
            var startPosition = reader.Position;
            ByteUnpacker.ReadValuePacked(reader, out NetworkObjectId);
            ByteUnpacker.ReadValuePacked(reader, out NetworkBehaviourId);
            reader.ReadValueSafe(out dirtyFlags);
            Header_Size = reader.Position - startPosition;
            startPosition = reader.Position;
            //if ((dirtyFlags & 0x01) == 0x01)
            //{
            //    DirtyScale = true;
            //    Scale.ReadState(reader);
            //    ScaleFloat = math.float3(Scale.Axis);
            //}

            if ((dirtyFlags & 0x02) == 0x02)
            {
                DirtyPosition = true;
                Position.ReadState(reader);
                PositionFloat = math.float3(Position.Axis);
            }

            if ((dirtyFlags & 0x04) == 0x04)
            {
                DirtyRotation = true;
                Rotation.ReadState(reader);
                Rotation.Decompress();
            }

            Payload_Size = reader.Position - startPosition;
            DirtyFlags = dirtyFlags;
        }
    }


    internal struct TransformIntState : ITransformState<TransformIntState>
    {
        public ulong NetworkObjectId;
        public ushort NetworkBehaviourId;
        public byte CompressFlags;
        public int TotalCompressedSize;

        public Vector3State Scale;
        public Vector3State Position;
        public Vector4State Rotation;


        public Vector3 OriginalPosition;

        public Vector3 DecompScale;
        public Vector3 DecompPosition;
        public Quaternion DecompRotation;

        public bool DirtyPosition { get; private set; }
        public bool DirtyRotation { get; private set; }
        public bool DirtyScale { get; private set; }

        public void ApplyState(TransformIntState state)
        {
            Scale.ApplyState(state.Scale);
            Position.ApplyState(state.Position);
            Rotation.ApplyState(state.Rotation);
        }

        public bool HasDelta()
        {
            return Scale.HasDelta() || Position.HasDelta() || Rotation.HasDelta();
        }

        public void Clear()
        {
            Scale.Clear();
            Position.Clear();
            Rotation.Clear();
        }

        public void Initialize()
        {
            Scale.Initialize();
            Position.Initialize();
            Rotation.Initialize();
        }

        public void Dispose()
        {
            Scale.Dispose();
            Position.Dispose();
            Rotation.Dispose();
        }

        public unsafe void Compress()
        {
            TotalCompressedSize = 0;
            CompressFlags = 0;

            if (Scale.HasDelta())
            {
                CompressFlags |= 0x01;
                Scale.Compress();
                TotalCompressedSize += Scale.CompressedSize;
            }
            if (Position.HasDelta())
            {
                CompressFlags |= 0x02;
                Position.Compress();
                TotalCompressedSize += Position.CompressedSize;
            }
            if (Rotation.HasDelta())
            {
                CompressFlags |= 0x04;
                Rotation.Compress();
                TotalCompressedSize += Rotation.CompressedSize;
            }
        }

        public unsafe void Decompress(int precision)
        {
            var precisionInvert = 1.0f / precision;

            if ((CompressFlags & 0x01) > 0)
            {
                Scale.Clear();
                Scale.Decompress();
                DecompScale.x = Scale.X * precisionInvert;
                DecompScale.y = Scale.X * precisionInvert;
                DecompScale.z = Scale.X * precisionInvert;
            }
            if ((CompressFlags & 0x02) > 0)
            {
                Position.Clear();
                Position.Decompress();
                DecompPosition.x = Position.X * precisionInvert;
                DecompPosition.y = Position.Y * precisionInvert;
                DecompPosition.z = Position.Z * precisionInvert;
            }
            if ((CompressFlags & 0x04) > 0)
            {
                Rotation.Clear();
                Rotation.Decompress();
                DecompRotation.x = Rotation.X * precisionInvert;
                DecompRotation.y = Rotation.Y * precisionInvert;
                DecompRotation.z = Rotation.Z * precisionInvert;
                DecompRotation.w = Rotation.W * precisionInvert;
            }
        }

        public unsafe void WriteState(FastBufferWriter writer)
        {
            if (!HasDelta())
            {
                //Warning?
                return;
            }
            BytePacker.WriteValuePacked(writer, NetworkObjectId);
            BytePacker.WriteValuePacked(writer, NetworkBehaviourId);
            writer.WriteByteSafe(CompressFlags);
            DirtyPosition = DirtyRotation = DirtyScale = false;
            // We can write the compressed size as a byte since we know
            // it will never exceed 255 bytes.
            if ((CompressFlags & 0x01) > 0)
            {
                DirtyScale = true;
                Scale.WriteState(writer);
                //writer.WriteByteSafe((byte)Scale.CompressedSize);
                //writer.WriteBytesSafe(&Scale.Compressed[0], Scale.CompressedSize);
            }
            if ((CompressFlags & 0x02) > 0)
            {
                DirtyPosition = true;
                Position.WriteState(writer);
                //writer.WriteByteSafe((byte)Position.CompressedSize);
                //writer.WriteBytesSafe(&Position.Compressed[0], Position.CompressedSize);
            }
            if ((CompressFlags & 0x04) > 0)
            {
                DirtyRotation = true;
                Rotation.WriteState(writer);
                //writer.WriteByteSafe((byte)Rotation.CompressedSize);
                //writer.WriteBytesSafe(&Rotation.Compressed[0], Rotation.CompressedSize);
            }
        }

        // TODO: We need to further compress this down by about 60%
        public int Header_Size;
        public int Payload_Size;

        /// <summary>
        /// TODO:  Next--> Switch to quaternion compression
        /// Should get us close to ~= 9-10 bytes (total) per state update when moving around a bunch.
        /// </summary>
        /// <param name="reader"></param>
        public unsafe void ReadState(FastBufferReader reader)
        {
            var position = reader.Position;
            ByteUnpacker.ReadValuePacked(reader, out NetworkObjectId);
            ByteUnpacker.ReadValuePacked(reader, out NetworkBehaviourId);
            reader.ReadByteSafe(out CompressFlags);
            Header_Size = reader.Position - position;
            position = reader.Position;
            //var readSize = (byte)0;
            DirtyPosition = DirtyRotation = DirtyScale = false;
            if ((CompressFlags & 0x01) > 0)
            {
                DirtyScale = true;
                Scale.ReadState(reader);
                //reader.ReadByteSafe(out readSize);
                //reader.ReadBytesSafe(&Scale.Compressed[0], readSize);
            }
            if ((CompressFlags & 0x02) > 0)
            {
                DirtyPosition = true;
                Position.ReadState(reader);
                //reader.ReadByteSafe(out readSize);
                //reader.ReadBytesSafe(&Position.Compressed[0], readSize);
            }
            if ((CompressFlags & 0x04) > 0)
            {
                DirtyRotation = true;
                Rotation.ReadState(reader);
                //reader.ReadByteSafe(out readSize);
                //reader.ReadBytesSafe(&Rotation.Compressed[0], readSize);
            }
            Payload_Size = reader.Position - position;
        }

    }


    internal interface ITransformStateComponent<T> : ITransformState<T>
    {
        public unsafe void Compress();

        public unsafe void Decompress();
    }

    internal interface ITransformState<T> : IDisposable
    {
        public void ApplyState(T state);

        public void Clear();

        public bool HasDelta();

        public void Initialize();

        public unsafe void WriteState(FastBufferWriter writer);

        public unsafe void ReadState(FastBufferReader reader);
    }

}
