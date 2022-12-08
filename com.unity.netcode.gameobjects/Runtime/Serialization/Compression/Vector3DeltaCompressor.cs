using UnityEngine;
using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    public struct CompressedVector3Delta : INetworkSerializable
    {
        /// <summary>
        /// Contains additional compression information
        /// </summary>
        /// <remarks>
        /// Bits 0 - 10: decimal place precision value of the magnitude
        /// Bits 11 - 13: Sign values of the largest axial value and the two smallest values
        /// bits 14 - 15: The index of the largest axial value
        /// </remarks>
        public ushort Header;

        /// <summary>
        /// Contains the compressed values
        /// </summary>
        /// <remarks>
        /// Bits 0-21: The smallest two normalized axial values with 11 bit precision
        /// Bits 22-30: The unsigned int value of the magnitude
        /// Bit 31: When set, the magnitude is < 0.22f
        /// When Magnitude is only a fractional value:
        /// Bits 22-30: high 9 bits of value (header is the lower 10bits giving 19 bit precision)
        /// </remarks>
        public uint Compressed;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Header);
            serializer.SerializeValue(ref Compressed);
        }
    }

    /// <summary>
    /// Compresses a <see cref="Vector3"/> delta using a "smallest two approach" and using the 3rd 10 bit slot
    /// to store the magnitude. This should only be used when determining the delta between two Vector3 values.
    /// </summary>
    /// <remarks>
    /// Specifications for usage:
    /// - This yields a 50% reduction in total size to maintain a reasonable precision
    ///   - 12 bytes (Vector3) to 6 bytes <see cref="CompressedVector3Delta"/>.
    /// - Min-Max delta ranges from (+/-) 255.00f to 0.001f
    /// - Decompressed value(s) precision accuracy ranges when used for delta positions:
    ///   [+Delta-][Precision][Unity WorldSpace Units(UWSU)] (USWU Values Calculated @30x +/-Delta)
    ///   [255.000][0.1124573][+/- 7650.000 UWSU per second] (Delta Ceiling)
    ///   [127.000][0.0557327][+/- 3810.000 UWSU per second]
    ///   [063.000][0.0276642][+/- 1890.000 UWSU per second]
    ///   [031.000][0.0139141][+/- 0930.000 UWSU per second]
    ///   [015.000][0.0067520][+/- 0450.000 UWSU per second]
    ///   [007.000][0.0028806][+/- 0210.000 UWSU per second]
    ///   [003.000][0.0012348][+/- 0090.000 UWSU per second]
    ///   [001.000][0.0004115][+/- 0030.000 UWSU per second]
    ///   [000.500][0.0002058][+/- 0015.000 UWSU per second]
    ///   [000.250][0.0001029][+/- 0007.500 UWSU per second]
    ///   [000.125][0.0000572][+/- 0003.750 UWSU per second]
    ///   [000.063][0.0000257][+/- 0001.875 UWSU per second]
    ///   [000.031][0.0000157][+/- 0000.938 UWSU per second]
    ///   [000.016][0.0000061][+/- 0000.469 UWSU per second]
    ///   [000.008][0.0000034][+/- 0000.234 UWSU per second]
    ///   [000.004][0.0000031][+/- 0000.117 UWSU per second]
    ///   [000.002][0.0000020][+/- 0000.058 UWSU per second] (Precision Floor)
    ///   [000.001][0.0000020][+/- 0000.029 UWSU per second] (Delta Floor)(i.e. Minimum NetworkTransform Thresholds for Scale and Position)
    ///
    /// Note: Objects with axial position deltas >= +/- 255.0f (per delta) range are moving so fast they most likely
    /// will be excluded from camera's default far clipping plane (1000 UWSU) on the 1st or 2nd network tick. When
    /// used for scale, this does not apply.  Of course, a change (delta) of scale greater than the maximum size is
    /// highly unlikely.
    /// </remarks>
    public static class Vector3DeltaCompressor
    {
        private const ushort k_PrecisionMask = (1 << 11) - 1;

        // Square root of 2 over 2 (Mathf.Sqrt(2.0f) / 2.0f == 1.0f / Mathf.Sqrt(2.0f))
        // This provides encoding the smallest three components into a (+/-) Mathf.Sqrt(2.0f) / 2.0f range
        private const float k_SqrtTwoOverTwoEncoding = 0.70710678118654752440084436210485f;

        // We can further improve the encoding compression by dividing k_SqrtTwoOverTwo into 1.0f and multiplying that
        // by the precision mask (minor reduction of runtime calculations)
        private const float k_CompressionEcodingMask = (1.0f / k_SqrtTwoOverTwoEncoding) * k_PrecisionMask;

        // We can do the same for our decoding and decompression by dividing k_PrecisionMask into 1.0 and multiplying
        // that by k_SqrtTwoOverTwo (minor reduction of runtime calculations)
        private const float k_DcompressionDecodingMask = (1.0f / k_PrecisionMask) * k_SqrtTwoOverTwoEncoding;

        private const ushort k_HeaderMagnitudeMask = 1023;
        private const ushort k_MagnitudeMask = 511;
        private const byte k_LargestIndexShift = 14;
        private const byte k_BaseSignShift = 11;
        private const uint k_FractionAdjustMask = 0x7FFFFFFF;

        // Negative bit set values
        private const ushort k_True = 1;
        private const ushort k_False = 0;

        // Used to store the absolute value of the delta Vector3 value
        private static Vector3 s_AbsValues = Vector3.zero;

        /// <summary>
        /// Compress the delta between two Vector3 values
        /// </summary>
        /// <param name="previous">previous Vector3</param>
        /// <param name="current">current Vector3</param>
        /// <param name="compressedVector3Delta">the compressed value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CompressDelta(ref Vector3 previous, ref Vector3 current, ref CompressedVector3Delta compressedVector3Delta)
        {
            var directionTowards = (current - previous);
            CompressDelta(ref directionTowards, ref compressedVector3Delta);
        }

        /// <summary>
        /// Compress a Vector3 delta
        /// </summary>
        /// <param name="delta">the delta between two Vector3 values</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CompressDelta(ref Vector3 delta, ref CompressedVector3Delta compressedVector3Delta)
        {
            compressedVector3Delta.Compressed = 0;
            compressedVector3Delta.Header = 0;
            var normalizedDir = delta.normalized;
            var magnitude = delta.magnitude;
            var magnitudeIsFractional = (magnitude < 0.22f);

            // Set the combined decimal place flag if the magnitude falls below 0.21f
            compressedVector3Delta.Compressed = (uint)((magnitudeIsFractional ? k_True : k_False) << 31);

            // Store off the absolute value for each Quaternion element
            s_AbsValues[0] = Mathf.Abs(normalizedDir[0]);
            s_AbsValues[1] = Mathf.Abs(normalizedDir[1]);
            s_AbsValues[2] = Mathf.Abs(normalizedDir[2]);

            // Get the largest element value of the Vector3 delta to know what the remaining "Smallest Three" values are
            var vectMax = Mathf.Max(s_AbsValues[0], s_AbsValues[1], s_AbsValues[2]);

            // Find the index of the largest element so we can skip that element while compressing and decompressing
            var indexToSkip = (ushort)(s_AbsValues[0] == vectMax ? 0 : s_AbsValues[1] == vectMax ? 1 : 2);

            // Get the sign of the largest element
            var vectMaxSign = (normalizedDir[indexToSkip] < 0 ? k_True : k_False);

            // Store the index, magnitude precision, and largest value's sign
            compressedVector3Delta.Header = (ushort)(indexToSkip << k_LargestIndexShift);
            // Get the header fractional magnitude value
            var headerMag = magnitudeIsFractional ? (magnitude * 100) - (uint)(magnitude * 100) : magnitude - (uint)magnitude;

            compressedVector3Delta.Header |= (ushort)((ushort)Mathf.Round(headerMag * 1000) & k_HeaderMagnitudeMask);
            compressedVector3Delta.Header |= (ushort)(vectMaxSign << (k_BaseSignShift + 2));

            // Store the magnitude value
            var magnitudeAdjusted = magnitudeIsFractional ? ((uint)(magnitude * 100) & k_MagnitudeMask) : (uint)(magnitude) & k_MagnitudeMask;

            compressedVector3Delta.Compressed |= ((uint)magnitudeAdjusted << 22) & k_FractionAdjustMask;

            var currentIndex = 0;
            var axialValues = (uint)0;
            for (int i = 0; i < 3; i++)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                // Store the axis sign in the header
                compressedVector3Delta.Header |= (ushort)((normalizedDir[i] < 0 ? k_True : k_False) << (k_BaseSignShift + currentIndex));

                // Add the axis compressed value to the existing compressed values
                axialValues = (axialValues << 11) | (ushort)Mathf.Round(k_CompressionEcodingMask * s_AbsValues[i]);
                currentIndex++;
            }
            compressedVector3Delta.Compressed |= axialValues;
        }

        /// <summary>
        /// Decompresses a compressed Vector3 delta
        /// </summary>
        /// <param name="delta">the target Vector3 to store the decompressed delta</param>
        /// <param name="compressed">the compressed Vector3 delta</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void DecompressDelta(ref Vector3 delta, ref CompressedVector3Delta compressedVector3Delta)
        {
            var compressed = compressedVector3Delta.Compressed;


            // Get the last two bits for the index to skip (0-3)
            var indexToSkip = compressedVector3Delta.Header >> k_LargestIndexShift;

            // Reverse out the values while skipping over the largest value index
            var sumOfSquaredMagnitudes = 0.0f;
            var currentIndex = 1;

            // Get the magnitude of the delta
            var magnitude = (float)((compressed >> 22) & k_MagnitudeMask);
            var magnitudeIsFractional = (compressed & (k_FractionAdjustMask + 1)) > 0;
            var magnitudeAdjusted = magnitudeIsFractional ? magnitude * 0.01f : magnitude;

            // Get the 1/1000th decimal place precision value of the magnitude
            magnitudeAdjusted += (compressedVector3Delta.Header & k_HeaderMagnitudeMask) * (magnitudeIsFractional ? 0.00001f : 0.001f);

            for (int i = 2; i >= 0; --i)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                // Check the negative bit and multiply that result with the decompressed and decoded value
                var axisSign = compressedVector3Delta.Header & (1 << (k_BaseSignShift + currentIndex));

                delta[i] = (axisSign > 0 ? -1.0f : 1.0f) * (compressed & k_PrecisionMask) * k_DcompressionDecodingMask;
                sumOfSquaredMagnitudes += delta[i] * delta[i];
                compressed = compressed >> 11;
                currentIndex--;
            }

            // Get the largest value's sign
            var largestSign = compressedVector3Delta.Header & (1 << (k_BaseSignShift + 2));

            // Calculate the largest value from the sum of squares of the two smallest axis values
            delta[indexToSkip] = Mathf.Sqrt(1.0f - sumOfSquaredMagnitudes) * (largestSign > 0 ? -1.0f : 1.0f);

            // Apply the magnitude
            delta *= magnitudeAdjusted;
        }
    }
}
