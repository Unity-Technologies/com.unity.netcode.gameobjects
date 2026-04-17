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
        public TransformGridState GridStatePrevious;
        public TransformGridState GridStateCurrent;
        public TransformGridState GridStateDelta;

        public ulong EntityIdentifier;

        public void UpdateIds(TransformStateSync transformStateSync)
        {
            // We may not need the EntityIdentifier
            EntityIdentifier = EntityId.ToULong(transformStateSync.GetEntityId());
            GridStateDelta.TransformIdentifier = GridStatePrevious.TransformIdentifier = GridStateCurrent.TransformIdentifier = transformStateSync.TransformIdentifier;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ProcessCurrentState(int index, TransformAccess transformAccess, int precision, bool isNextTick)
        {
            if (isNextTick && transformAccess.isValid)
            {
                // Get and set the current transform state
                GridStateCurrent.Index = index;
                GridStateCurrent.Scale.X = (int)(transformAccess.localScale.x * precision);
                GridStateCurrent.Scale.Y = (int)(transformAccess.localScale.y * precision);
                GridStateCurrent.Scale.Z = (int)(transformAccess.localScale.z * precision);

                GridStateCurrent.Position.FromVector3(transformAccess.position, precision);
                //GridStateCurrent.Position.X = (int)(transformAccess.position.x * precision);
                //GridStateCurrent.Position.Y = (int)(transformAccess.position.y * precision);
                //GridStateCurrent.Position.Z = (int)(transformAccess.position.z * precision);

                //var forward = transformAccess.rotation.normalized * Vector3.forward;
                //GridStateCurrent.Forward.X = (int)(forward.x * precision);
                //GridStateCurrent.Forward.Y = (int)(forward.y * precision);
                //GridStateCurrent.Forward.Z = (int)(forward.z * precision);
                //GridStateCurrent.Forward.Forward = forward;

                GridStateCurrent.Rotation.X = (int)(transformAccess.rotation.x * precision);
                GridStateCurrent.Rotation.Y = (int)(transformAccess.rotation.y * precision);
                GridStateCurrent.Rotation.Z = (int)(transformAccess.rotation.z * precision);
                GridStateCurrent.Rotation.W = (int)(transformAccess.rotation.w * precision);
                GridStateCurrent.Rotation.Rotation = transformAccess.rotation;

                // Calculate the delta between the previous and current states.
                GridStateDelta.Index = index;
                GridStateDelta.Scale.X = GridStateCurrent.Scale.X - GridStatePrevious.Scale.X;
                GridStateDelta.Scale.Y = GridStateCurrent.Scale.Y - GridStatePrevious.Scale.Y;
                GridStateDelta.Scale.Z = GridStateCurrent.Scale.Z - GridStatePrevious.Scale.Z;

                GridStateDelta.Position.ToDelta(precision, GridStateCurrent.Position, GridStatePrevious.Position);
                //GridStateDelta.Position.X = GridStateCurrent.Position.X - GridStatePrevious.Position.X;
                //GridStateDelta.Position.Y = GridStateCurrent.Position.Y - GridStatePrevious.Position.Y;
                //GridStateDelta.Position.Z = GridStateCurrent.Position.Z - GridStatePrevious.Position.Z;

                GridStateDelta.Forward.X = GridStateCurrent.Forward.X - GridStatePrevious.Forward.X;
                GridStateDelta.Forward.Y = GridStateCurrent.Forward.Y - GridStatePrevious.Forward.Y;
                GridStateDelta.Forward.Z = GridStateCurrent.Forward.Z - GridStatePrevious.Forward.Z;

                GridStateDelta.Rotation.X = GridStateCurrent.Rotation.X - GridStatePrevious.Rotation.X;
                GridStateDelta.Rotation.Y = GridStateCurrent.Rotation.Y - GridStatePrevious.Rotation.Y;
                GridStateDelta.Rotation.Z = GridStateCurrent.Rotation.Z - GridStatePrevious.Rotation.Z;
                GridStateDelta.Rotation.W = GridStateCurrent.Rotation.W - GridStatePrevious.Rotation.W;

                // Check for and record deltas between the current and previous states
                GridStateDelta.DirtyScale = false;
                if (GridStateDelta.Scale.HasDelta())
                {
                    GridStateDelta.DirtyScale = true;
                    GridStateDelta.Scale.Axis = new half3(transformAccess.localScale);
                    // TODO: this could be removed
                    GridStateDelta.Scale.InvPrecision = 1.0f / precision;
                }

                GridStateDelta.DirtyPosition = false;
                if (GridStateDelta.Position.HasDelta())
                {
                    GridStateDelta.DirtyPosition = true;
                    GridStateDelta.Position.Compress();
                    //GridStateDelta.Position.Axis = new half3(transformAccess.position);

                    // TODO: this could be removed
                    //GridStateDelta.Position.InvPrecision = 1.0f / precision;

                }

                GridStateDelta.DirtyRotation = false;


                //if (GridStateDelta.Forward.HasDelta())
                //{
                //    GridStateDelta.DirtyRotation = true;
                //    GridStateDelta.Forward.ApplyState(GridStateCurrent.Forward);
                //}


                GridStateDelta.Rotation.IsDirty = false;
                if (GridStateDelta.Rotation.HasDelta())
                {
                    GridStateDelta.DirtyRotation = true;
                    GridStateDelta.Rotation.IsDirty = true;
                    GridStateDelta.Rotation.ApplyState(GridStateCurrent.Rotation);
                    GridStateDelta.Rotation.Compress();
                }

                // Apply the state even if there were no deltas so we 
                GridStatePrevious.ApplyState(GridStateCurrent);
            }

            return GridStateDelta.HasDelta();
        }
    }

    /// <summary>
    /// Keeping for reference purposes
    /// </summary>
    [BurstCompile]
    internal unsafe struct Vector3State : ITransformStateComponent<Vector3State>
    {
        public uint X;
        public uint Y;
        public uint Z;

        public byte AxisWritten;
        public readonly int CompressedSize => m_CompressedSize;
        public byte* Compressed;

        private int m_CompressedSize;

        private Vector3 m_RawState;

        private Vector3 m_Delta;

        internal readonly Vector3 Delta => m_Delta;

        public void ApplyState(Vector3State state)
        {
            X = state.X;
            Y = state.Y;
            Z = state.Z;
            m_RawState = state.m_RawState;
        }

        /// <summary>
        /// ZigZag encodes a signed integer and maps it to a unsigned integer
        /// </summary>
        /// <param name="value">The signed integer to encode</param>
        /// <returns>A ZigZag encoded version of the integer</returns>
        public static ulong ZigZagEncode(long value) => (ulong)((value >> 63) ^ (value << 1));

        /// <summary>
        /// Decides a ZigZag encoded integer back to a signed integer
        /// </summary>
        /// <param name="value">The unsigned integer</param>
        /// <returns>The signed version of the integer</returns>
        public static long ZigZagDecode(ulong value) => (((long)(value >> 1) & 0x7FFFFFFFFFFFFFFFL) ^ ((long)(value << 63) >> 63));

        public void ToDelta(float precision, Vector3State current, Vector3State previous)
        {
            m_Delta = current.m_RawState - previous.m_RawState;
            var deltaX = (((uint)(math.abs(m_Delta.x * precision))) & 0x7FFFFF);
            var deltaY = (((uint)(math.abs(m_Delta.y * precision))) & 0x7FFFFF);
            var deltaZ = (((uint)(math.abs(m_Delta.z * precision))) & 0x7FFFFF);
            if (deltaX > 0)
            {
                X = (((uint)(math.abs(m_Delta.x * precision))) & 0x7FFFFF);
                X = (X << 1) | (uint)((m_Delta.x < 0.0f) ? 0b1 : 0b0);
            }
            else
            {
                X = 0;
            }

            if (deltaY > 0)
            {
                Y = (((uint)(math.abs(m_Delta.y * precision))) & 0x7FFFFF);
                Y = (Y << 1) | (uint)((m_Delta.y < 0.0f) ? 0b1 : 0b0);
            }
            else
            {
                Y = 0;
            }

            if (deltaZ > 0)
            {
                Z = (((uint)(math.abs(m_Delta.z * precision))) & 0x7FFFFF);
                Z = (Z << 1) | (uint)((m_Delta.z < 0.0f) ? 0b1 : 0b0);
            }
            else
            {
                Z = 0;
            }

            //if (deltaX > 0)
            //{
            //    X = (((uint)(math.abs(current.m_RawState.x * precision))) & 0x7FFFFF);
            //    X = (X << 1) | (uint)((current.m_RawState.x < 0.0f) ? 0b1 : 0b0);
            //}
            //else
            //{
            //    X = 0;
            //}

            //if (deltaY > 0)
            //{
            //    Y = (((uint)(math.abs(current.m_RawState.y * precision))) & 0x7FFFFF);
            //    Y = (Y << 1) | (uint)((current.m_RawState.y < 0.0f) ? 0b1 : 0b0);
            //}
            //else
            //{
            //    Y = 0;
            //}

            //if (deltaZ > 0)
            //{
            //    Z = (((uint)(math.abs(current.m_RawState.z * precision))) & 0x7FFFFF);
            //    Z = (Z << 1) | (uint)((current.m_RawState.z < 0.0f) ? 0b1 : 0b0);
            //}
            //else
            //{
            //    Z = 0;
            //}
            if (HasDelta())
            {
                m_Delta = current.m_RawState;
            }
        }

        public void FromVector3(Vector3 position, float precision)
        {
            m_RawState = position;
        }

        public Vector3 ToVector3(float invPrecision)
        {
            var vector = Vector3.zero;
            var isNegative = (X & 0b1) == 1 ? -1.0f : 1.0f;
            vector.x = ((X >> 1) & 0x7FFFFF) * invPrecision * isNegative;
            isNegative = (Y & 0b1) == 1 ? -1.0f : 1.0f;
            vector.y = ((Y >> 1) & 0x7FFFFF) * invPrecision * isNegative;
            isNegative = (Z & 0b1) == 1 ? -1.0f : 1.0f;
            vector.z = ((Z >> 1) & 0x7FFFFF) * invPrecision * isNegative;

            return vector;
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
            //// Byte count is stored in lowest bit positions
            //var uX = X;
            //var uY = Y;
            //var uZ = Z;
            //var xBytes = BitCounter.GetUsedByteCount(X);
            //var yBytes = BitCounter.GetUsedByteCount(Y);
            //var zBytes = BitCounter.GetUsedByteCount(Z);

            //UnsafeUtility.MemSet(&Compressed[0], 0, sizeof(int) * 4);

            //// Get pointers to the values
            //var xPtr = (byte*)&uX;
            //var yPtr = (byte*)&uY;
            //var zPtr = (byte*)&uZ;

            //var offset = 1;
            //// Write the compressed values
            //if (X != 0)
            //{
            //    AxisWritten |= (byte)xBytes;

            //    CopyBytes(&Compressed[offset], xPtr, xBytes);
            //    //UnsafeUtility.MemCpy(&Compressed[offset], xPtr, xBytes);
            //    offset += xBytes;
            //}
            //if (Y != 0)
            //{
            //    AxisWritten |= (byte)(((byte)yBytes) << 2);
            //    CopyBytes(&Compressed[offset], xPtr, yBytes);
            //    //UnsafeUtility.MemCpy(&Compressed[offset], yPtr, yBytes);
            //    offset += yBytes;
            //}
            //if (Z != 0)
            //{
            //    AxisWritten |= (byte)(((byte)zBytes) << 4);
            //    CopyBytes(&Compressed[offset], xPtr, zBytes);
            //    //UnsafeUtility.MemCpy(&Compressed[offset], zPtr, zBytes);
            //    offset += zBytes;
            //}
            //m_CompressedSize = offset;
            //Compressed[0] = AxisWritten;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyBytes(byte* destination, byte* source, int numBytes)
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
            //AxisWritten = Compressed[0];
            //// Byte count is stored in lowest bit positions
            //var uX = X;
            //var uY = Y;
            //var uZ = Z;

            //var xBytes = AxisWritten & 0b11;
            //var yBytes = (AxisWritten >> 2) & 0b11;
            //var zBytes = (AxisWritten >> 4) & 0b11;

            //// Get pointers to the values
            //var xPtr = (byte*)&uX;
            //var yPtr = (byte*)&uY;
            //var zPtr = (byte*)&uZ;
            //var offset = 1;
            //if (xBytes > 0)
            //{
            //    CopyBytes(xPtr, &Compressed[offset], xBytes);
            //    offset += xBytes;
            //}

            //if (yBytes > 0)
            //{
            //    CopyBytes(yPtr, &Compressed[offset], yBytes);
            //    offset += yBytes;
            //}

            //if (zBytes > 0)
            //{
            //    CopyBytes(zPtr, &Compressed[offset], zBytes);
            //    offset += zBytes;
            //}
            //m_CompressedSize = offset;
            //X = uX;
            //Y = uY;
            //Z = uZ;
        }

        public void Initialize()
        {
            Compressed = (byte*)UnsafeUtility.Malloc((sizeof(int) * 3) + 1, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(Compressed, Allocator.Persistent);
        }

        //public void WriteState(FastBufferWriter writer)
        //{
        //    AxisWritten = 0;
        //    var position = writer.Position;
        //    writer.WriteByteSafe(AxisWritten);
        //    var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
        //    var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
        //    var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
        //    var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
        //    var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
        //    var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);
        //    if (xBytes > 0)
        //    {
        //        AxisWritten |= 0x01;
        //        BytePacker.WriteValuePacked(writer, X);
        //    }
        //    if (yBytes > 0)
        //    {
        //        AxisWritten |= 0x02;
        //        BytePacker.WriteValuePacked(writer, Y);
        //    }
        //    if (zBytes > 0)
        //    {
        //        AxisWritten |= 0x04;
        //        BytePacker.WriteValuePacked(writer, Z);
        //    }
        //    var tailPosition = writer.Position;
        //    writer.Seek(position);
        //    writer.WriteByteSafe(AxisWritten);
        //    writer.Seek(tailPosition);
        //}
        //public void ReadState(FastBufferReader reader)
        //{
        //    var position = reader.Position;
        //    reader.ReadByteSafe(out AxisWritten);
        //    if ((AxisWritten & 0x01) > 0)
        //    {
        //        ByteUnpacker.ReadValuePacked(reader, out X);
        //    }
        //    if ((AxisWritten & 0x02) > 0)
        //    {
        //        AxisWritten |= 0x02;
        //        ByteUnpacker.ReadValuePacked(reader, out Y);
        //    }
        //    if ((AxisWritten & 0x04) > 0)
        //    {
        //        ByteUnpacker.ReadValuePacked(reader, out Z);
        //    }
        //}

        public void WriteState(FastBufferWriter writer)
        {
            var xBytes = BitCounter.GetUsedByteCount(X);
            var yBytes = BitCounter.GetUsedByteCount(Y);
            var zBytes = BitCounter.GetUsedByteCount(Z);
            AxisWritten = 0;
            if (X != 0)
            {
                AxisWritten |= (byte)xBytes;
            }
            if (Y != 0)
            {
                AxisWritten |= (byte)(((byte)yBytes) << 2);
            }
            if (Z != 0)
            {
                AxisWritten |= (byte)(((byte)zBytes) << 4);
            }
            writer.WriteByteSafe(AxisWritten);
            if (X != 0)
            {
                BytePacker.WriteValueBitPacked(writer, X);
            }
            if (Y != 0)
            {
                BytePacker.WriteValueBitPacked(writer, Y);
            }
            if (Z != 0)
            {
                BytePacker.WriteValueBitPacked(writer, Z);
            }
#if DEBUG_TRANSFORMSTATE
            Debug.Log($"[Vector3State][Write][AxisWritten ({AxisWritten})][X = {X}][Y = {Y}][Z = {Z}]");
#endif

            //BytePacker.WriteValuePacked(writer,m_CompressedSize);
            //writer.WriteBytesSafe(&Compressed[0], m_CompressedSize);
        }
        public void ReadState(FastBufferReader reader)
        {
            reader.ReadByteSafe(out AxisWritten);
            
            var xBytes = AxisWritten & 0b11;
            var yBytes = (AxisWritten >> 2) & 0b11;
            var zBytes = (AxisWritten >> 4) & 0b11;
            try
            {
                if (xBytes > 0)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out X);
                }
                if (yBytes > 0)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out Y);
                }
                if (zBytes > 0)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out Z);
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
#if DEBUG_TRANSFORMSTATE
            Debug.Log($"[Vector3State][Read][AxisWritten ({AxisWritten})][X = {X}][Y = {Y}][Z = {Z}]");
#endif

            //ByteUnpacker.ReadValuePacked(reader,out m_CompressedSize);
            //reader.ReadBytesSafe(&Compressed[0], m_CompressedSize);
        }
    }

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


    [BurstCompile]
    internal unsafe struct Vector3Half : ITransformStateComponent<Vector3Half>
    {
        internal const int Length = 3;
        public int X;
        public int Y;
        public int Z;
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
            //var uX = ((uint)Arithmetic.ZigZagEncode(X)) << 3;
            //var uY = ((uint)Arithmetic.ZigZagEncode(Y)) << 3;
            //var uZ = ((uint)Arithmetic.ZigZagEncode(Z)) << 3;
            //var xBytes = (uint)BitCounter.GetUsedByteCount(uX);
            //var yBytes = (uint)BitCounter.GetUsedByteCount(uY);
            //var zBytes = (uint)BitCounter.GetUsedByteCount(uZ);

            //CompressedSize = (int)(xBytes + yBytes + zBytes);

            //if (CompressedSize == 0)
            //{
            //    // Warning?
            //    return;
            //}

            //uX |= xBytes;
            //uY |= yBytes;
            //uZ |= zBytes;

            //UnsafeUtility.MemSet(&Compressed[0], 0, sizeof(int) * 4);


            //// Get pointers to the values
            //var xPtr = (byte*)&uX;
            //var yPtr = (byte*)&uY;
            //var zPtr = (byte*)&uZ;

            //var offset = 1;
            //// Write the compressed values
            //if (xBytes > 0)
            //{
            //    AxisWritten |= 0x01;
            //    UnsafeUtility.MemCpy(&Compressed[offset], xPtr, xBytes);
            //    offset += (int)xBytes;
            //}
            //if (yBytes > 0)
            //{
            //    AxisWritten |= (0x01 << 1);
            //    UnsafeUtility.MemCpy(&Compressed[offset], yPtr, yBytes);
            //    offset += (int)yBytes;
            //}
            //if (zBytes > 0)
            //{
            //    AxisWritten |= (0x01 << 2);
            //    UnsafeUtility.MemCpy(&Compressed[offset], zPtr, zBytes);
            //    offset += (int)zBytes;
            //}

            //Compressed[0] = AxisWritten;
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
            //AxisWritten = Compressed[0];
            //// Byte count is stored in lowest bit positions
            //var uX = X;
            //var uY = Y;
            //var uZ = Z;

            //// Get pointers to the values
            //var xPtr = (byte*)&uX;
            //var yPtr = (byte*)&uY;
            //var zPtr = (byte*)&uZ;
            //var offset = 1;
            //if ((AxisWritten & 0x01) > 0)
            //{
            //    int numBytes = (Compressed[offset] & 0b111);
            //    ReadBytes(xPtr, &Compressed[offset], numBytes);
            //    uX = uX >> 3;
            //    X = (ushort)Arithmetic.ZigZagDecode(uX);
            //    Axis.x = math.half(X * InvPrecision);
            //    offset += numBytes;
            //}

            //if ((AxisWritten & 0x02) > 0)
            //{
            //    int numBytes = (Compressed[offset] & 0b111);
            //    ReadBytes(yPtr, &Compressed[offset], numBytes);
            //    uY = uY >> 3;
            //    Y = (ushort)Arithmetic.ZigZagDecode(uY);
            //    Axis.y = math.half(Y * InvPrecision);
            //    offset += numBytes;
            //}

            //if ((AxisWritten & 0x04) > 0)
            //{
            //    int numBytes = (Compressed[offset] & 0b111);
            //    ReadBytes(zPtr, &Compressed[offset], numBytes);
            //    uZ = uZ >> 3;
            //    Z = (ushort)Arithmetic.ZigZagDecode(uZ);
            //    Axis.z = math.half(Z * InvPrecision);
            //    offset += numBytes;
            //}
        }

        public void Initialize()
        {
            Compressed = (byte*)UnsafeUtility.Malloc((sizeof(int) * 3) + 1, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(Compressed, Allocator.Persistent);
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
        public float Precision;
        public float InvPrecision;
        public bool DirtyScale;
        public bool DirtyPosition;
        public bool DirtyRotation;

        public int Header_Size;
        public int Payload_Size;
        public byte DirtyFlags { get; private set; }

        public Vector3Half Scale;
        public Vector3State Position;
        public ForwardVector Forward;
        public QuaternionState Rotation;

        public Vector3 ScaleFloat;
        public Vector3 PositionFloat;
        public int Index;
        public ushort TransformIdentifier;

        public Vector3 CurrentPosition;
        public Vector3 CurrentScale;

        public void ApplyState(TransformGridState state)
        {
            if (TransformIdentifier != state.TransformIdentifier)
            {
                Debug.Log($"MISMATCH CONFLICT IN STATE PROCESSING! Applying TID: {state.TransformIdentifier} to previous state for TID: {TransformIdentifier}!");
            }
            Index = state.Index;
            Precision = state.Precision;
            InvPrecision = state.InvPrecision;
            DirtyScale = state.DirtyScale;
            DirtyPosition = state.DirtyPosition;
            DirtyRotation = state.DirtyRotation;
            //if (DirtyScale)
            {
                Scale.ApplyState(state.Scale);
            }
            //if (DirtyPosition)
            {
                Position.ApplyState(state.Position);
            }
            //if (DirtyRotation)
            {
                Rotation.ApplyState(state.Rotation);
            }
            //Forward.ApplyState(state.Forward);
        }

        public bool HasDelta()
        {
            //return Scale.HasDelta() || Position.HasDelta() || Rotation.HasDelta();
            return Position.HasDelta() || Rotation.HasDelta();
            //return Position.HasDelta() || Forward.HasDelta();
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

        /// <summary>
        /// TODO: We may or may not need this.
        /// (Currently nothing uses this method when writing this)
        /// </summary>
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
                ScaleFloat = Scale.UpdateFromValue(CurrentScale);
            }

            if (DirtyPosition)
            {
                //Position.InvPrecision = InvPrecision;
                // TODO: Add grid offset here
                Position.Decompress();
                var update = Position.ToVector3(InvPrecision);
                PositionFloat = CurrentPosition;
                for(int i = 0; i < 3; i++)
                {
                    if (update[i] != 0.0f)
                    {
                        PositionFloat[i] += update[i];
                    }
                }
                //PositionFloat.x = (Position.X * InvPrecision);// + CurrentPosition.x;
                //PositionFloat.y = (Position.Y * InvPrecision);// + CurrentPosition.y;
                //PositionFloat.z = (Position.Z * InvPrecision);// + CurrentPosition.z;
                //PositionFloat = Position.UpdateFromValue(CurrentPosition);
            }

            if (DirtyRotation)
            {
                Rotation.Decompress();
            }
        }

        public unsafe (byte, int, int) DebugWriteState(FastBufferWriter writer)
        {
            WriteState(writer);
            return (DirtyFlags, Header_Size, Payload_Size);
        }

        public void WriteState(FastBufferWriter writer)
        {
            var dirtyFlags = (byte)0;
            var startPosition = writer.Position;
            BytePacker.WriteValueBitPacked(writer, TransformIdentifier);
            var dirtyPosition = writer.Position;
            writer.WriteByteSafe(dirtyFlags);
            Header_Size = writer.Position - startPosition;
            startPosition = writer.Position;

            if (Scale.HasDelta())
            {
                dirtyFlags |= 0x01;
                Scale.WriteState(writer);
            }

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

            //if (Forward.HasDelta())
            //{
            //    dirtyFlags |= 0x04;
            //    Forward.WriteState(writer);
            //}

            var tail = writer.Position;
            writer.Seek(dirtyPosition);
            writer.WriteValueSafe(dirtyFlags);
            writer.Seek(tail);
            Payload_Size = writer.Position - startPosition;
            DirtyFlags = dirtyFlags;

        }

        public unsafe void ReadState(FastBufferReader reader)
        {
            var dirtyFlags = (byte)0;
            var startPosition = reader.Position;
            ByteUnpacker.ReadValuePacked(reader, out TransformIdentifier);

            reader.ReadValueSafe(out dirtyFlags);
            Header_Size = reader.Position - startPosition;
            startPosition = reader.Position;
            if ((dirtyFlags & 0x01) == 0x01)
            {
                DirtyScale = true;
                Scale.ReadState(reader);
                ScaleFloat = math.float3(Scale.Axis);
            }

            if ((dirtyFlags & 0x02) == 0x02)
            {
                DirtyPosition = true;
                Position.ReadState(reader);
                //PositionFloat = math.float3(Position.Axis);
            }

            if ((dirtyFlags & 0x04) == 0x04)
            {
                DirtyRotation = true;
                Rotation.ReadState(reader);
                Rotation.Decompress();
            }

            //if ((dirtyFlags & 0x04) == 0x04)
            //{
            //    DirtyRotation = true;
            //    Forward.ReadState(reader);
            //}

            Payload_Size = reader.Position - startPosition;
            DirtyFlags = dirtyFlags;
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

        public bool HasDelta();

        public void Initialize();

        public unsafe void WriteState(FastBufferWriter writer);

        public unsafe void ReadState(FastBufferReader reader);
    }

}
