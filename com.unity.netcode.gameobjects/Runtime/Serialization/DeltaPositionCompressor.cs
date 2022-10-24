using UnityEngine;
using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    /// <summary>
    /// Compresses a delta position using a smallest two approach and using the 3rd 10 bit slot
    /// to store the magnitude.
    /// <see cref="QuaternionCompressor"/> for more details
    /// </summary>
    public static class DeltaPositionCompressor
    {
        private const ushort k_PrecisionMask = (1 << 9) - 1;

        // Square root of 2 over 2 (Mathf.Sqrt(2.0f) / 2.0f == 1.0f / Mathf.Sqrt(2.0f))
        // This provides encoding the smallest three components into a (+/-) Mathf.Sqrt(2.0f) / 2.0f range
        private const float k_SqrtTwoOverTwoEncoding = 0.70710678118654752440084436210485f;

        // We can further improve the encoding compression by dividing k_SqrtTwoOverTwo into 1.0f and multiplying that
        // by the precision mask (minor reduction of runtime calculations)
        private const float k_CompressionEcodingMask = (1.0f / k_SqrtTwoOverTwoEncoding) * k_PrecisionMask;

        // Used to shift the negative bit to the 10th bit position when compressing and encoding
        private const ushort k_ShiftNegativeBit = 9;

        // We can do the same for our decoding and decompression by dividing k_PrecisionMask into 1.0 and multiplying
        // that by k_SqrtTwoOverTwo (minor reduction of runtime calculations)
        private const float k_DcompressionDecodingMask = (1.0f / k_PrecisionMask) * k_SqrtTwoOverTwoEncoding;

        // The sign bit position (10th bit) used when decompressing and decoding
        private const ushort k_NegShortBit = 0x200;

        // Negative bit set values
        private const ushort k_True = 1;
        private const ushort k_False = 0;

        // Used to store the absolute value of the delta position value
        private static Vector3 s_AbsValues = Vector3.zero;

        /// <summary>
        /// Compress the delta position between a previous and current position
        /// </summary>
        /// <param name="previousPosition">the previous position</param>
        /// <param name="currentPosition">the current position</param>
        /// <returns>compressed delta position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public uint CompressDeltaPosition(ref Vector3 previousPosition, ref Vector3 currentPosition)
        {
            var directionTowards = (currentPosition - previousPosition);
            return CompressDeltaPosition(ref directionTowards);
        }

        /// <summary>
        /// Compress a delta position
        /// </summary>
        /// <param name="positionDelta">the delta between two positions</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public uint CompressDeltaPosition(ref Vector3 positionDelta)
        {
            var normalizedDir = positionDelta.normalized;
            var magnitude = positionDelta.magnitude;

            // Store off the absolute value for each Quaternion element
            s_AbsValues[0] = Mathf.Abs(normalizedDir[0]);
            s_AbsValues[1] = Mathf.Abs(normalizedDir[1]);
            s_AbsValues[2] = Mathf.Abs(normalizedDir[2]);

            // Get the largest element value of the position delta to know what the remaining "Smallest Three" values are
            var vectMax = Mathf.Max(s_AbsValues[0], s_AbsValues[1], s_AbsValues[2]);

            // Find the index of the largest element so we can skip that element while compressing and decompressing
            var indexToSkip = (ushort)(s_AbsValues[0] == vectMax ? 0 : s_AbsValues[1] == vectMax ? 1 : 2);

            // Get the sign of the largest element
            var vectMaxSign = (normalizedDir[indexToSkip] < 0 ? k_True : k_False);

            // Start with the largest value's index (shifted to the highest two bits)
            var compressed = (uint)indexToSkip;

            // Store the sign of the largest value with the magnitude
            var shortMag = (ushort)((vectMaxSign << k_ShiftNegativeBit) | (ushort)(k_PrecisionMask & (ushort)Mathf.Round(k_CompressionEcodingMask * magnitude)));
            compressed = compressed << 10 | shortMag;

            // Step 1: Start with the first element
            var currentIndex = 0;

            // Step 2: If we are on the index to skip preserve the current compressed value, otherwise proceed to step 3 and 4
            // Step 3: Get the sign of the element we are processing. If it is the not the same as the largest value's sign bit then we set the bit
            // Step 4: Get the compressed and encoded value by multiplying the absolute value of the current element by k_CompressionEcodingMask and round that result up
            compressed = currentIndex != indexToSkip ? (compressed << 10) | (uint)((normalizedDir[currentIndex] < 0 ? k_True : k_False)) << k_ShiftNegativeBit | (ushort)Mathf.Round(k_CompressionEcodingMask * s_AbsValues[currentIndex]) : compressed;
            currentIndex++;
            // Repeat the last 3 steps for the remaining elements
            compressed = currentIndex != indexToSkip ? (compressed << 10) | (uint)((normalizedDir[currentIndex] < 0 ? k_True : k_False)) << k_ShiftNegativeBit | (ushort)Mathf.Round(k_CompressionEcodingMask * s_AbsValues[currentIndex]) : compressed;
            currentIndex++;
            return currentIndex != indexToSkip ? (compressed << 10) | (uint)((normalizedDir[currentIndex] < 0 ? k_True : k_False)) << k_ShiftNegativeBit | (ushort)Mathf.Round(k_CompressionEcodingMask * s_AbsValues[currentIndex]) : compressed;
        }

        /// <summary>
        /// Decompresses a compressed delta position
        /// </summary>
        /// <param name="deltaPosition">the target vector to store the decompressed delta position</param>
        /// <param name="compressed">the compressed delta position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DecompressDeltaPosition(ref Vector3 deltaPosition, uint compressed)
        {
            // Get the last two bits for the index to skip (0-3)
            var indexToSkip = (int)(compressed >> 30);

            // Reverse out the values while skipping over the largest value index
            var sumOfSquaredMagnitudes = 0.0f;
            for (int i = 2; i >= 0; --i)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                // Check the negative bit and multiply that result with the decompressed and decoded value
                deltaPosition[i] = ((compressed & k_NegShortBit) > 0 ? -1.0f : 1.0f) * Mathf.Round((compressed & k_PrecisionMask) * k_DcompressionDecodingMask);
                sumOfSquaredMagnitudes += deltaPosition[i] * deltaPosition[i];
                compressed = compressed >> 10;
            }
            // Get the magnitude of the delta position
            var magnitude = k_DcompressionDecodingMask * (ushort)(compressed & k_PrecisionMask);

            // Calculate the largest value from the sum of squares of the two smallest axis values
            deltaPosition[indexToSkip] = Mathf.Sqrt(1.0f - sumOfSquaredMagnitudes) * ((compressed & k_NegShortBit) > 0 ? -1.0f : 1.0f);

            // Apply the magnitude
            deltaPosition *= magnitude;
        }
    }
}
