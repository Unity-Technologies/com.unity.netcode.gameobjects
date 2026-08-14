using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// The Smallest Three Quaternion Compressor Implementation
    /// (Job friendly version)
    /// </summary>
    /// <remarks>
    /// Explanation of why "The smallest three":
    /// Since a normalized Quaternion's unit value is 1.0f:
    /// x*x + y*y + z*z + w*w = M*M (where M is the magnitude of the vector)
    /// If w was the largest value and the quaternion is normalized:
    /// M = 1.0f (which M * M would still yield 1.0f)
    /// w*w = M*M - (x*x + y*y + z*z) or Mathf.Sqrt(1.0f - (x*x + y*y + z*z))
    /// w = Math.Sqrt(1.0f - (x*x + y*y + z*z))
    /// Using the largest number avoids potential loss of precision in the smallest three values.
    /// </remarks>
    public static class QuaternionCompressor
    {
        private const ushort k_PrecisionMask = (1 << 9) - 1;

        // Square root of 2 over 2 (Mathf.Sqrt(2.0f) / 2.0f == 1.0f / Mathf.Sqrt(2.0f))
        // This provides encoding the smallest three components into a (+/-) Mathf.Sqrt(2.0f) / 2.0f range
        private const float k_SqrtTwoOverTwoEncoding = 0.70710678118654752440084436210485f;

        // We can further improve the encoding compression by dividing k_SqrtTwoOverTwo into 1.0f and multiplying that
        // by the precision mask (minor reduction of runtime calculations)
        private const float k_CompressionEncodingMask = (1.0f / k_SqrtTwoOverTwoEncoding) * k_PrecisionMask;

        // Used to shift the negative bit to the 10th bit position when compressing and encoding
        private const ushort k_ShiftNegativeBit = 9;

        // We can do the same for our decoding and decompression by dividing k_PrecisionMask into 1.0 and multiplying
        // that by k_SqrtTwoOverTwo (minor reduction of runtime calculations)
        private const float k_DecompressionDecodingMask = (1.0f / k_PrecisionMask) * k_SqrtTwoOverTwoEncoding;

        // The sign bit position (10th bit) used when decompressing and decoding
        private const ushort k_NegShortBit = 0x200;

        // Negative bit set values
        private const ushort k_True = 1;
        private const ushort k_False = 0;

        /// <summary>
        /// Compresses a Quaternion into an unsigned integer
        /// </summary>
        /// <param name="quaternion">the <see cref="Quaternion"/> to be compressed</param>
        /// <returns>the <see cref="Quaternion"/> compressed as an unsigned integer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CompressQuaternion(ref Quaternion quaternion)
        {
            return Compress(new float4(quaternion.x, quaternion.y, quaternion.z, quaternion.w));
        }

        /// <summary>
        /// Decompress an unsigned integer into a <see cref="Quaternion"/>.
        /// </summary>
        /// <param name="quaternion">quaternion to store the decompressed values within</param>
        /// <param name="compressed">the compressed quaternion</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DecompressQuaternion(ref Quaternion quaternion, uint compressed)
        {
            Decompress(out var decompressed, compressed);
            quaternion.x = decompressed.x;
            quaternion.y = decompressed.y;
            quaternion.z = decompressed.z;
            quaternion.w = decompressed.w;
        }

        /// <summary>
        /// The <see cref="float4"/> based implementation of <see cref="CompressQuaternion(ref Quaternion)"/>.
        /// </summary>
        /// <remarks>
        /// This is a job safe method to be used in place of <see cref="CompressQuaternion(ref Quaternion)"/>.
        /// </remarks>
        /// <param name="quaternion">the quaternion, as a <see cref="float4"/>, to be compressed</param>
        /// <returns>the quaternion compressed as an unsigned integer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Compress(in float4 quaternion)
        {
            // Store off the absolute value for each Quaternion element
            var quatAbsValues = math.abs(quaternion);

            // Get the largest element value of the quaternion to know what the remaining "Smallest Three" values are
            var quatMax = math.cmax(quatAbsValues);

            // Find the index of the largest element, so we can skip that element while compressing and decompressing
            var indexToSkip = (ushort)(quatAbsValues.x == quatMax ? 0 : quatAbsValues.y == quatMax ? 1 : quatAbsValues.z == quatMax ? 2 : 3);

            // Get the sign of the largest element which is all that is needed when calculating the sum of squares of a normalized quaternion.
            var maxValue = indexToSkip == 0 ? quaternion.x : indexToSkip == 1 ? quaternion.y : indexToSkip == 2 ? quaternion.z : quaternion.w;
            var quatMaxSign = maxValue < 0 ? k_True : k_False;

            // Start with the index to skip which will be shifted to the highest two bits
            var compressed = (uint)indexToSkip;

            // Step 1: If we are on the index to skip, preserve the current compressed value, otherwise proceed to step 2 and 3
            // Step 2: Get the sign of the element we are processing. If it is not the same as the largest value's sign bit then we set the bit
            // Step 3: Get the compressed and encoded value by multiplying the absolute value of the current element by k_CompressionEncodingMask and round that result up
            compressed = 0 != indexToSkip ? EncodeElement(compressed, quaternion.x, quatAbsValues.x, quatMaxSign) : compressed;
            // Repeat the 3 steps for the remaining elements
            compressed = 1 != indexToSkip ? EncodeElement(compressed, quaternion.y, quatAbsValues.y, quatMaxSign) : compressed;
            compressed = 2 != indexToSkip ? EncodeElement(compressed, quaternion.z, quatAbsValues.z, quatMaxSign) : compressed;
            compressed = 3 != indexToSkip ? EncodeElement(compressed, quaternion.w, quatAbsValues.w, quatMaxSign) : compressed;

            // Return the compress quaternion
            return compressed;
        }

        /// <summary>
        /// The ecoding algorithm broken down to its fundamental, easier to understand, elements.
        /// </summary>
        /// <param name="compressed">The current compressed value.</param>
        /// <param name="value">The value to be compressed into the compressed value.</param>
        /// <param name="absValue">The absolute value of the value to be compressed.</param>
        /// <param name="quatMaxSign">The sign of the largest value that is calculated upon decompression.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint EncodeElement(uint compressed, float value, float absValue, ushort quatMaxSign)
        {
            return (compressed << 10)
                | (uint)((value < 0 ? k_True : k_False) != quatMaxSign ? k_True : k_False) << k_ShiftNegativeBit
                | (ushort)math.round(k_CompressionEncodingMask * absValue);
        }

        /// <summary>
        /// The <see cref="float4"/> based implementation of <see cref="DecompressQuaternion(ref Quaternion, uint)"/>.
        /// </summary>
        /// <remarks>
        /// This is a job safe method to be used in place of <see cref="DecompressQuaternion(ref Quaternion, uint)"/>.
        /// </remarks>
        /// <param name="quaternion">the decompressed quaternion as a <see cref="float4"/></param>
        /// <param name="compressed">the compressed quaternion</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Decompress(out float4 quaternion, uint compressed)
        {
            quaternion = float4.zero;

            // Get the last two bits for the index to skip (0-3)
            var indexToSkip = (int)(compressed >> 30);

            // Reverse out the values while skipping over the largest value index
            var sumOfSquaredMagnitudes = 0.0f;
            for (int i = 3; i >= 0; --i)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                // Check the negative bit and multiply that result with the decompressed and decoded value
                var value = ((compressed & k_NegShortBit) > 0 ? -1.0f : 1.0f) * ((compressed & k_PrecisionMask) * k_DecompressionDecodingMask);
                SetAxis(ref quaternion, i, value);
                sumOfSquaredMagnitudes += value * value;
                compressed = compressed >> 10;
            }
            // Since a normalized quaternion's magnitude is 1.0f, we subtract the sum of the squared smallest three from the unit value and take
            // the square root of the difference to find the final largest value.
            SetAxis(ref quaternion, indexToSkip, math.sqrt(1.0f - sumOfSquaredMagnitudes));
        }

        /// <summary>
        /// Sets the value of the value directly as opposed to indexing into the array to avoid bounds checking cost.
        /// </summary>
        /// <param name="decompressed">The current decompressed quaternion.</param>
        /// <param name="index">The index of the decompressed quaternion to be set.</param>
        /// <param name="value">The axis value to apply to the decompressed quaternion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetAxis(ref float4 decompressed, int index, float value)
        {
            switch (index)
            {
                case 0:
                    decompressed.x = value;
                    break;
                case 1:
                    decompressed.y = value;
                    break;
                case 2:
                    decompressed.z = value;
                    break;
                default:
                    decompressed.w = value;
                    break;
            }
        }
    }
}
