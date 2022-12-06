using UnityEngine;
using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    public struct CompressedDeltaPosition
    {
        /// <summary>
        /// Contains additional compression information
        /// </summary>
        /// <remarks>
        /// Bits 0 - 10: 1/1000th decimal place precision value of magnitude
        /// Bits 11 - 13: Sign values of the largest axial value and the two smallest values
        /// bits 14 - 15: The index of the largest axial value
        /// </remarks>
        public ushort Header;

        /// <summary>
        /// Contains the compressed values
        /// </summary>
        public uint Compressed;
    }

    /// <summary>
    /// Compresses a delta position using a smallest two approach and using the 3rd 10 bit slot
    /// to store the magnitude.
    /// <see cref="QuaternionCompressor"/> for more details
    /// </summary>
    public static class DeltaPositionCompressor
    {
        private const ushort k_PrecisionMask = (1 << 10) - 1;

        // Square root of 2 over 2 (Mathf.Sqrt(2.0f) / 2.0f == 1.0f / Mathf.Sqrt(2.0f))
        // This provides encoding the smallest three components into a (+/-) Mathf.Sqrt(2.0f) / 2.0f range
        private const float k_SqrtTwoOverTwoEncoding = 0.70710678118654752440084436210485f;

        // We can further improve the encoding compression by dividing k_SqrtTwoOverTwo into 1.0f and multiplying that
        // by the precision mask (minor reduction of runtime calculations)
        private const float k_CompressionEcodingMask = (1.0f / k_SqrtTwoOverTwoEncoding) * k_PrecisionMask;

        // We can do the same for our decoding and decompression by dividing k_PrecisionMask into 1.0 and multiplying
        // that by k_SqrtTwoOverTwo (minor reduction of runtime calculations)
        private const float k_DcompressionDecodingMask = (1.0f / k_PrecisionMask) * k_SqrtTwoOverTwoEncoding;

        private const ushort k_MagnitudePrecisionMask = 1023;
        private const byte k_LargestIndexShift = 14;
        private const byte k_BaseSignShift = 11;

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
        static public void CompressDeltaPosition(ref Vector3 previousPosition, ref Vector3 currentPosition, ref CompressedDeltaPosition compressedDeltaPosition)
        {
            var directionTowards = (currentPosition - previousPosition);
            CompressDeltaPosition(ref directionTowards, ref compressedDeltaPosition);
        }

        /// <summary>
        /// Compress a delta position
        /// </summary>
        /// <param name="positionDelta">the delta between two positions</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CompressDeltaPosition(ref Vector3 positionDelta, ref CompressedDeltaPosition compressedDeltaPosition)
        {
            compressedDeltaPosition.Compressed = 0;
            compressedDeltaPosition.Header = 0;
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

            // Store the index, magnitude precision, and largest value's sign
            compressedDeltaPosition.Header = (ushort)(indexToSkip << k_LargestIndexShift);
            compressedDeltaPosition.Header |= (ushort)((ushort)Mathf.Round((magnitude - (uint)magnitude) * 1000) & k_MagnitudePrecisionMask);
            compressedDeltaPosition.Header |= (ushort)(vectMaxSign << (k_BaseSignShift + 2));

            // Store the non-decimal magnitude value
            var compressed = (uint)magnitude;

            var currentIndex = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                // Store the axis sign in the header
                compressedDeltaPosition.Header |= (ushort)((normalizedDir[i] < 0 ? k_True : k_False) << (k_BaseSignShift + currentIndex));
                // Add the axis compressed value to the existing compressed values
                compressed = (compressed << 10) | (ushort)Mathf.Round(k_CompressionEcodingMask * s_AbsValues[i]);
                currentIndex++;
            }
            compressedDeltaPosition.Compressed = compressed;
        }

        /// <summary>
        /// Decompresses a compressed delta position
        /// </summary>
        /// <param name="deltaPosition">the target vector to store the decompressed delta position</param>
        /// <param name="compressed">the compressed delta position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DecompressDeltaPosition(ref Vector3 deltaPosition, ref CompressedDeltaPosition compressedDeltaPosition)
        {
            var compressed = compressedDeltaPosition.Compressed;
            // Get the last two bits for the index to skip (0-3)
            var indexToSkip = compressedDeltaPosition.Header >> k_LargestIndexShift;

            // Reverse out the values while skipping over the largest value index
            var sumOfSquaredMagnitudes = 0.0f;
            var currentIndex = 1;
            for (int i = 2; i >= 0; --i)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                // Check the negative bit and multiply that result with the decompressed and decoded value
                var axisSign = compressedDeltaPosition.Header & (1 << (k_BaseSignShift + currentIndex));

                deltaPosition[i] = (axisSign > 0 ? -1.0f : 1.0f) * (compressed & k_PrecisionMask) * k_DcompressionDecodingMask;
                sumOfSquaredMagnitudes += deltaPosition[i] * deltaPosition[i];
                compressed = compressed >> 10;
                currentIndex--;
            }

            // Get the magnitude of the delta position
            var magnitude = (float)(compressed & k_PrecisionMask);

            // Get the 1/1000th decimal place precision value of the magnitude
            magnitude += (compressedDeltaPosition.Header & k_MagnitudePrecisionMask) * 0.001f;

            var largestSign = compressedDeltaPosition.Header & (1 << (k_BaseSignShift + 2));

            // Calculate the largest value from the sum of squares of the two smallest axis values
            deltaPosition[indexToSkip] = Mathf.Sqrt(1.0f - sumOfSquaredMagnitudes) * (largestSign > 0 ? -1.0f : 1.0f);

            // Apply the magnitude
            deltaPosition *= magnitude;
        }
    }
}
