using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// ** Currently used for position synchronization **
    /// The current approach to optimizing transform synchronization.
    /// </summary>
    [BurstCompile]
    internal unsafe struct Vector3State : ITransformStateComponent<Vector3State>
    {
        public byte AxisWritten;
        public Vector3UInt Axis;
        public Vector3UInt AxisDelta;

        public readonly int CompressedSize => m_CompressedSize;
        private int m_CompressedSize;

        private Vector3 m_Position;
        private Vector3 m_Delta;


        private float m_Precision;
        private float m_InvPrecision;

        internal readonly Vector3 Delta => m_Delta;
        internal readonly Vector3 Position => m_Position;

#if DEBUG_TRANSFORMSTATE
        public string CompressValuesAsString()
        {
            return $"(X: {Axis[0]}, Y: {Axis[1]}, Z:{Axis[2]})";
        }
#endif

        #region Compress and Decompress are not needed
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Compress()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Decompress()
        {
        }
        #endregion

        public void ApplyState(Vector3State state)
        {
            for (int i = 0; i < 3; i++)
            {
                Axis[i] = state.Axis[i];
            }
            m_Position = state.m_Position;
            AxisWritten = state.AxisWritten;
            m_Delta = state.m_Delta;
            m_Precision = state.m_Precision;
            m_InvPrecision = state.m_InvPrecision;
        }

        #region ZigZag - Keeping here in case it is needed (for now)
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

        #endregion

        /// <summary>
        /// Called by the <see cref="TransformState.ProcessCurrentState(int, TransformAccess, int, bool)"/> by a job.
        /// This creates the delta between current and previous states.
        /// </summary>
        /// <param name="current">current network tick transform state</param>
        /// <param name="previous">previous network tick transform state</param>
        public void ToDelta(Vector3State current, Vector3State previous, bool fullSynch)
        {
            Clear();
            m_Precision = current.m_Precision;
            m_Delta = current.m_Position - previous.m_Position;
            var currentPos = current.m_Position;
            var negativeMask = (byte)(1 << 3);

            var adjustPrecision = !fullSynch && math.abs(currentPos.magnitude) <= 0.75f;
            // If the delta is relatively small, then increase precision by 1 decimal place
            var precisionAdjusted = adjustPrecision ? m_Precision * 10f : m_Precision;
            var precisionAdjustMask = adjustPrecision ? (byte)(1 << 6) : (byte)0;
            var axisValue = (uint)0;
            var axisDelta = (uint)0;

            for (int i = 0; i < 3; i++)
            {
                axisDelta = (((uint)(math.abs(m_Delta[i] * precisionAdjusted))) & 0x7FFFFF);
                AxisDelta[i] = axisDelta;
            }

            var negativeCheck = fullSynch ? current.m_Position : m_Delta;

            for (int i = 0; i < 3; i++)
            {
                negativeMask = (byte)(negativeMask << i);
                axisValue = fullSynch ? (((uint)(math.abs(currentPos[i] * precisionAdjusted))) & 0x7FFFFF) : AxisDelta[i];

                axisDelta = AxisDelta[i];


                // TODO-MAYBE: Combine the signed bit flag in the axis information and not the number;

                // If this axis has a delta, then convert the delta value, shift the delta
                // by 1 bit, and then apply the negative sign bit to the 1st bit position.
                //if (fullSynch || (!fullSynch && axisDelta > 0))
                if (fullSynch && axisDelta > 0)
                {
                    //var axis = (((uint)(math.abs(currentAxis * m_Precision))) & 0x7FFFFF);
                    //axis = (axis << 1) | (uint)((currentAxis < 0.0f) ? 0b1 : 0b0);

                    Axis[i] = axisValue;

                    var axisMask = (byte)(1 << i);
                    AxisWritten |= axisMask;
                    AxisWritten |= negativeCheck[i] < 0.0f ? negativeMask : (byte)0;
                    AxisWritten |= precisionAdjustMask;
                }
                else
                {
                    Axis[i] = 0;
                }
            }

#if TO_DELTA_REFERENCE
            var deltaX = (((uint)(math.abs(m_Delta.x * m_Precision))) & 0x7FFFFF);
            var deltaY = (((uint)(math.abs(m_Delta.y * m_Precision))) & 0x7FFFFF);
            var deltaZ = (((uint)(math.abs(m_Delta.z * m_Precision))) & 0x7FFFFF);

            if (deltaX > 0)
            {
                X = (((uint)(math.abs(current.m_Position.x * m_Precision))) & 0x7FFFFF);
                X = (X << 1) | (uint)((current.m_Position.x < 0.0f) ? 0b1 : 0b0);
            }
            else
            {
                X = 0;
            }

            if (deltaY > 0)
            {
                Y = (((uint)(math.abs(current.m_Position.y * m_Precision))) & 0x7FFFFF);
                Y = (Y << 1) | (uint)((current.m_Position.y < 0.0f) ? 0b1 : 0b0);
            }
            else
            {
                Y = 0;
            }

            if (deltaZ > 0)
            {
                Z = (((uint)(math.abs(current.m_Position.z * m_Precision))) & 0x7FFFFF);
                Z = (Z << 1) | (uint)((current.m_Position.z < 0.0f) ? 0b1 : 0b0);
            }
            else
            {
                Z = 0;
            }
#endif
            // Primarily for debugging purposes
            if (HasDelta())
            {
                m_Delta = current.m_Position;
            }
        }

        public void ApplyCurrent(Vector3 position, float precision)
        {
            m_Position = position;
            m_Precision = precision;
        }

        /// <summary>
        /// Extracts the state update's axis and applies.
        /// </summary>
        /// <param name="invPrecision"></param>
        /// <returns></returns>
        public Vector3 ToVector3(float invPrecision)
        {
            m_Precision = invPrecision;
            var vector = Vector3.zero;
            // Pull out the negative flag, shift the axis value by 1, and clamp maximum grid space region
            // relative to precision.
            // 1/10th: +/- 838,860.7 Unity world space units
            // 1/100th: +/- 83,886.07 Unity world space units 
            // 1/1000th: +/- 8,388.607 Unity world space units
            // Note:
            // This is what will define the total Unity world space volume of each node within the "grid space".
            // The grid-space provides a means to knowing location of transforms along with being able to keep the
            // maximum +/- range within the bounds as defined by the precision selected.
            // TODO:
            // Add the octree-based tracking (for now the above ranges are the limits based on the precision selected).

            var isNegative = 0.0f;
            var negativeMask = 1 << 3;
            var mask = (byte)0;
            var precisionAdjustMask = (byte)(1 << 6);
            // If the precision mask is set, then adjust by 1 decimal place for the current precision
            var precision = (AxisWritten & precisionAdjustMask) == precisionAdjustMask ? invPrecision * 0.10f : invPrecision;
            for (int i = 0; i < 3; i++)
            {
                mask = (byte)(0b1 << i);
                negativeMask = negativeMask << i;
                if ((mask & AxisWritten) == mask)
                {
                    var axisValue = Axis[i];
                    isNegative = (AxisWritten & negativeMask) == negativeMask ? -1.0f : 1.0f;
                    vector[i] = (axisValue & 0x7FFFFF) * invPrecision * isNegative;
                }
                else
                {
                    vector[i] = 0.0f;
                }
            }
#if TO_VECTOR3_REFERENCE
            var isNegative = (X & 0b1) == 1 ? -1.0f : 1.0f;
            vector.x = ((X >> 1) & 0x7FFFFF) * invPrecision * isNegative;
            isNegative = (Y & 0b1) == 1 ? -1.0f : 1.0f;
            vector.y = ((Y >> 1) & 0x7FFFFF) * invPrecision * isNegative;
            isNegative = (Z & 0b1) == 1 ? -1.0f : 1.0f;
            vector.z = ((Z >> 1) & 0x7FFFFF) * invPrecision * isNegative;
#endif

            return vector;
        }

        public bool HasDelta()
        {
            for (int i = 0; i < 3; i++)
            {
                if (Axis[i] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < 3; i++)
            {
                Axis[i] = 0;
            }
            m_Position = Vector3.zero;
            AxisWritten = 0;
            m_Delta = Vector3.zero;
            m_Precision = 0.0f;
            m_InvPrecision = 0.0f;
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
            }
        }

        public void Initialize()
        {
            // If any initialization is needed in the future
        }

        public void Dispose()
        {
            // If any disposing is needed in the future
        }

        public void WriteState(FastBufferWriter writer)
        {
            writer.WriteByteSafe(AxisWritten);
            // We use bit packing to handle compressing down each axis value.
            // Note: (Optimization pass 2)
            // Originally it wrote only the bytes needed,
            // but now uses bit packing. When we get interpolation
            // running in a job, we will also want to create a
            // job friendly version of WriteValueBitPacked and
            // handle this all in a job where each receiving client has their
            // own stream/writer.
            // Note: (Optimization pass 3)
            // Make the stream/writer integrated with the outbound job-friendly buffer in UnityTransport
            // (i.e. like NetworkMessageManager but as opposed to copying on the main thread it does it
            // in the job).
       
            for (int i = 0; i < 3; i++)
            {
                var axisValue = Axis[i];
                var axisDirty = 1 << i;
                if ((AxisWritten & axisDirty) == axisDirty)
                {
                    BytePacker.WriteValueBitPacked(writer, axisValue);
                }
            }
#if DEBUG_TRANSFORMSTATE
            Debug.Log($"[Vector3State][Write][AxisWritten ({AxisWritten})][X = {Axis[0]}][Y = {Axis[1]}][Z = {Axis[2]}]");
#endif
        }

        //var mask = (byte)0;
        //    for (int i = 0; i< 3; i++)
        //    {
        //        mask = (byte) (0b1 << i);
        //        if ((mask & offsetAxiswritten) == mask)
        //        {
        //            var axisValue = Axis[i];
        ////isNegative = (axisValue & 0b1) == 1 ? -1.0f : 1.0f;
        //isNegative = (offsetAxiswritten & 0b1) == 1 ? -1.0f : 1.0f;
        public void ReadState(FastBufferReader reader)
        {
            // Read which axis changed
            reader.ReadByteSafe(out AxisWritten);
            var mask = (byte)0;
            m_Delta = Vector3.zero;
            var axisValue = (uint)0;

            for (int i = 0; i < 3; i++)
            {
                mask = (byte)(1 << i);
                if ((mask & AxisWritten) == mask)
                {
                    ByteUnpacker.ReadValueBitPacked(reader, out axisValue);
                    Axis[i] = axisValue;
                }
                else
                {
                    Axis[i] = 0;
                }
            }
#if TO_READ_AXIS_REFERENCE
            // Get the bytes written for each axis
            var xBytes = AxisWritten & 0b11;
            var yBytes = (AxisWritten >> 2) & 0b11;
            var zBytes = (AxisWritten >> 4) & 0b11;
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
#if DEBUG_TRANSFORMSTATE
            Debug.Log($"[Vector3State][Read][AxisWritten ({AxisWritten})][X = {X}][Y = {Y}][Z = {Z}]");
#endif
#endif
        }
    }
}


//using System.Runtime.CompilerServices;
//using Unity.Burst;
//using Unity.Mathematics;
//using Unity.Netcode.Components;
//using UnityEngine;

//namespace Unity.Netcode
//{
//    /// <summary>
//    /// ** Currently used for position synchronization **
//    /// The current approach to optimizing transform synchronization.
//    /// </summary>
//    [BurstCompile]
//    internal unsafe struct Vector3State : ITransformStateComponent<Vector3State>
//    {
//        public byte AxisWritten;
//        public Vector3UInt Axis;

//        public readonly int CompressedSize => m_CompressedSize;
//        private int m_CompressedSize;

//        private Vector3 m_Position;
//        private Vector3 m_Delta;


//        private float m_Precision;
//        private float m_InvPrecision;

//        internal readonly Vector3 Delta => m_Delta;

//#if DEBUG_TRANSFORMSTATE
//        public string CompressValuesAsString()
//        {
//            return $"(X: {Axis[0]}, Y: {Axis[1]}, Z:{Axis[2]})";
//        }
//#endif

//        #region Compress and Decompress are not needed
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public unsafe void Compress()
//        {
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public unsafe void Decompress()
//        {
//        }
//        #endregion

//        public void ApplyState(Vector3State state)
//        {
//            for (int i = 0; i < 3; i++)
//            {
//                Axis[i] = state.Axis[i];
//            }
//            m_Position = state.m_Position;
//            AxisWritten = state.AxisWritten;
//            m_Delta = state.m_Delta;
//            m_Precision = state.m_Precision;
//            m_InvPrecision = state.m_InvPrecision;
//        }

//        #region ZigZag - Keeping here in case it is needed (for now)
//        /// <summary>
//        /// ZigZag encodes a signed integer and maps it to a unsigned integer
//        /// </summary>
//        /// <param name="value">The signed integer to encode</param>
//        /// <returns>A ZigZag encoded version of the integer</returns>
//        public static ulong ZigZagEncode(long value) => (ulong)((value >> 63) ^ (value << 1));

//        /// <summary>
//        /// Decides a ZigZag encoded integer back to a signed integer
//        /// </summary>
//        /// <param name="value">The unsigned integer</param>
//        /// <returns>The signed version of the integer</returns>
//        public static long ZigZagDecode(ulong value) => (((long)(value >> 1) & 0x7FFFFFFFFFFFFFFFL) ^ ((long)(value << 63) >> 63));

//        #endregion

//        /// <summary>
//        /// Called by the <see cref="TransformState.ProcessCurrentState(int, TransformAccess, int, bool)"/> by a job.
//        /// This creates the delta between current and previous states.
//        /// </summary>
//        /// <param name="current">current network tick transform state</param>
//        /// <param name="previous">previous network tick transform state</param>
//        public void ToDelta(Vector3State current, Vector3State previous)
//        {
//            m_Precision = current.m_Precision;
//            m_Delta = current.m_Position - previous.m_Position;
//            var halfVector3 = new HalfVector3(m_Delta);
//            for (int i = 0; i < 3; i++)
//            {
//                Axis[i] = halfVector3.Axis[i].value;
//            }
//            //var currentPos = current.m_Position;
//            //for (int i = 0; i < 3; i++)
//            //{
//            //    var currentAxis = currentPos[i];
//            //    var delta = (((uint)(math.abs(m_Delta[i] * m_Precision))) & 0x7FFFFF);
//            //    // If this axis has a delta, then convert the delta value, shift the delta
//            //    // by 1 bit, and then apply the negative sign bit to the 1st bit position.
//            //    if (delta > 0)
//            //    {
//            //        var axis = (((uint)(math.abs(currentAxis * m_Precision))) & 0x7FFFFF);
//            //        axis = (axis << 1) | (uint)((currentAxis < 0.0f) ? 0b1 : 0b0);
//            //        Axis[i] = axis;
//            //    }
//            //    else // Otherwise, clear this out
//            //    {
//            //        Axis[i] = 0;
//            //    }
//            //}

//#if TO_DELTA_REFERENCE
//            var deltaX = (((uint)(math.abs(m_Delta.x * m_Precision))) & 0x7FFFFF);
//            var deltaY = (((uint)(math.abs(m_Delta.y * m_Precision))) & 0x7FFFFF);
//            var deltaZ = (((uint)(math.abs(m_Delta.z * m_Precision))) & 0x7FFFFF);

//            if (deltaX > 0)
//            {
//                X = (((uint)(math.abs(current.m_Position.x * m_Precision))) & 0x7FFFFF);
//                X = (X << 1) | (uint)((current.m_Position.x < 0.0f) ? 0b1 : 0b0);
//            }
//            else
//            {
//                X = 0;
//            }

//            if (deltaY > 0)
//            {
//                Y = (((uint)(math.abs(current.m_Position.y * m_Precision))) & 0x7FFFFF);
//                Y = (Y << 1) | (uint)((current.m_Position.y < 0.0f) ? 0b1 : 0b0);
//            }
//            else
//            {
//                Y = 0;
//            }

//            if (deltaZ > 0)
//            {
//                Z = (((uint)(math.abs(current.m_Position.z * m_Precision))) & 0x7FFFFF);
//                Z = (Z << 1) | (uint)((current.m_Position.z < 0.0f) ? 0b1 : 0b0);
//            }
//            else
//            {
//                Z = 0;
//            }
//#endif
//            // Primarily for debugging purposes
//            if (HasDelta())
//            {
//                m_Delta = current.m_Position;
//            }
//        }

//        public void ApplyCurrent(Vector3 position, float precision)
//        {
//            m_Position = position;
//            m_Precision = precision;
//        }

//        /// <summary>
//        /// Extracts the state update's axis and applies.
//        /// </summary>
//        /// <param name="invPrecision"></param>
//        /// <returns></returns>
//        public Vector3 ToVector3(float invPrecision)
//        {
//            return m_Delta;
//#if TEMP
//            m_Precision = invPrecision;
//            var vector = Vector3.zero;
//            // Pull out the negative flag, shift the axis value by 1, and clamp maximum grid space region
//            // relative to precision.
//            // 1/10th: +/- 838,860.7 Unity world space units
//            // 1/100th: +/- 83,886.07 Unity world space units 
//            // 1/1000th: +/- 8,388.607 Unity world space units
//            // Note:
//            // This is what will define the total Unity world space volume of each node within the "grid space".
//            // The grid-space provides a means to knowing location of transforms along with being able to keep the
//            // maximum +/- range within the bounds as defined by the precision selected.
//            // TODO:
//            // Add the octree-based tracking (for now the above ranges are the limits based on the precision selected).


//            // !!!!!!!!!!! TODO-NEXT: 
//            // Convert to 3 bits and not use bytes.
//            // Use the 4th bit for varying decimal place based on rate of change.
//            // The remaining bits will be for 
//            var isNegative = 0.0f;
//            for (int i = 0; i < 3; i++)
//            {
//                var axisValue = Axis[i];
//                isNegative = (axisValue & 0b1) == 1 ? -1.0f : 1.0f;
//                vector[i] = ((axisValue >> 1) & 0x7FFFFF) * invPrecision * isNegative;
//            }
//#if TO_VECTOR3_REFERENCE
//            var isNegative = (X & 0b1) == 1 ? -1.0f : 1.0f;
//            vector.x = ((X >> 1) & 0x7FFFFF) * invPrecision * isNegative;
//            isNegative = (Y & 0b1) == 1 ? -1.0f : 1.0f;
//            vector.y = ((Y >> 1) & 0x7FFFFF) * invPrecision * isNegative;
//            isNegative = (Z & 0b1) == 1 ? -1.0f : 1.0f;
//            vector.z = ((Z >> 1) & 0x7FFFFF) * invPrecision * isNegative;
//#endif

//            return vector;
//#endif
//        }

//        public bool HasDelta()
//        {
//            for (int i = 0; i < 3; i++)
//            {
//                if (Axis[i] != 0)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        public void Clear()
//        {
//            for (int i = 0; i < 3; i++)
//            {
//                Axis[i] = 0;
//            }
//            m_Position = Vector3.zero;
//            AxisWritten = 0;
//            m_Delta = Vector3.zero;
//            m_Precision = 0.0f;
//            m_InvPrecision = 0.0f;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private void CopyBytes(byte* destination, byte* source, int numBytes)
//        {
//            switch (numBytes)
//            {
//                case 1:
//                    destination[0] = source[0];
//                    break;
//                case 2:
//                    destination[0] = source[0];
//                    destination[1] = source[1];
//                    break;
//                case 3:
//                    destination[0] = source[0];
//                    destination[1] = source[1];
//                    destination[2] = source[2];
//                    break;
//                case 4:
//                    destination[0] = source[0];
//                    destination[1] = source[1];
//                    destination[2] = source[2];
//                    destination[3] = source[3];
//                    break;
//            }
//        }

//        public void Initialize()
//        {
//            // If any initialization is needed in the future
//        }

//        public void Dispose()
//        {
//            // If any disposing is needed in the future
//        }

//        public void WriteState(FastBufferWriter writer)
//        {
//            // Sanity check and clear axis written.
//            AxisWritten = 0;

//            // Use the bytes used for each axis delta to
//            // determine if an axis is dirty when reading.
//            // Note:
//            // If we need extra flags, then we can make
//            // these the first 3 bits and have 5 remaining
//            // flags to use.

//            // Only apply the bytes used if the axis delta is
//            // greater than 0.
//            for (int i = 0; i < 3; i++)
//            {
//                var axisValue = Axis[i];
//                if (axisValue != 0)
//                {
//                    var bytesUsed = BitCounter.GetUsedByteCount(axisValue);
//                    // shift by 0b11 (2 bits) times the axis index
//                    AxisWritten |= (byte)(((byte)bytesUsed) << (i * 2));
//                }
//            }
//#if TO_AXIS_WRITTEN_REFERENCE
//            var xBytes = BitCounter.GetUsedByteCount(X);
//            var yBytes = BitCounter.GetUsedByteCount(Y);
//            var zBytes = BitCounter.GetUsedByteCount(Z);
//            AxisWritten = 0;
//            if (X != 0)
//            {
//                AxisWritten |= (byte)xBytes;
//            }
//            if (Y != 0)
//            {
//                AxisWritten |= (byte)(((byte)yBytes) << 2);
//            }
//            if (Z != 0)
//            {
//                AxisWritten |= (byte)(((byte)zBytes) << 4);
//            }
//#endif
//            writer.WriteByteSafe(AxisWritten);

//            var halfVector3 = new HalfVector3(m_Delta);
//            // We use bit packing to handle determining the
//            // size for each axis written.
//            // Note: (Optimization pass 2)
//            // Originally it wrote only the bytes needed,
//            // but now uses bit packing. When we get interpolation
//            // running in a job, we will also want to create a
//            // job friendly version of WriteValueBitPacked and
//            // handle this all in a job.
//            for (int i = 0; i < 3; i++)
//            {
//                var axisValue = Axis[i];
//                if (axisValue != 0)
//                {
//                    //var halfValue = halfVector3.Axis[i];
//                    //BytePacker.WriteValueBitPacked(writer, axisValue);
//                    BytePacker.WriteValueBitPacked(writer, halfVector3.Axis[i].value);
//                }
//            }
//#if DEBUG_TRANSFORMSTATE
//            Debug.Log($"[Vector3State][Write][AxisWritten ({AxisWritten})][X = {Axis[0]}][Y = {Axis[1]}][Z = {Axis[2]}]");
//#endif
//        }


//        public void ReadState(FastBufferReader reader)
//        {
//            // Read which axis changed
//            reader.ReadByteSafe(out AxisWritten);
//            m_Delta = Vector3.zero;
//            var axisValue = (ushort)0;
//            var halfValue = new half();
//            for (int i = 0; i < 3; i++)
//            {
//                var bytesWritten = (AxisWritten >> i * 2) & 0b11;
//                if (bytesWritten > 0)
//                {
//                    ByteUnpacker.ReadValueBitPacked(reader, out axisValue);
//                    Axis[i] = axisValue;
//                    halfValue.value = axisValue;
//                    m_Delta[i] = halfValue;
//                }
//                else
//                {
//                    Axis[i] = 0;
//                }
//            }
//#if TO_READ_AXIS_REFERENCE
//            // Get the bytes written for each axis
//            var xBytes = AxisWritten & 0b11;
//            var yBytes = (AxisWritten >> 2) & 0b11;
//            var zBytes = (AxisWritten >> 4) & 0b11;
//            if (xBytes > 0)
//            {
//                ByteUnpacker.ReadValueBitPacked(reader, out X);
//            }
//            if (yBytes > 0)
//            {
//                ByteUnpacker.ReadValueBitPacked(reader, out Y);
//            }
//            if (zBytes > 0)
//            {
//                ByteUnpacker.ReadValueBitPacked(reader, out Z);
//            }
//#if DEBUG_TRANSFORMSTATE
//            Debug.Log($"[Vector3State][Read][AxisWritten ({AxisWritten})][X = {X}][Y = {Y}][Z = {Z}]");
//#endif
//#endif
//        }
//    }
//}

