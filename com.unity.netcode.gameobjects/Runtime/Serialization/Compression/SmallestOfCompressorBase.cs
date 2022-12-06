using System;
using UnityEngine;

namespace Unity.Netcode
{
    public struct SmallestOfNCompressed
    {
        //Bits 6-7: Largest value's index (maximum 4)
        //Bits 3-5: Sign value
        //Bits 0-2:
        public byte Header;
        public byte Overflow;
        public byte[] Data;
    }

    public class SmallestOfCompressorBase
    {
        protected SmallestOfNCompressed m_SmallestOfNCompressed;

        // Square root of 2 over 2 (Mathf.Sqrt(2.0f) / 2.0f == 1.0f / Mathf.Sqrt(2.0f))
        // This provides encoding the smallest three components into a (+/-) Mathf.Sqrt(2.0f) / 2.0f range
        internal const float SqrtTwoOverTwoEncoding = 0.70710678118654752440084436210485f;

        protected byte Precision { get; private set; }

        protected uint PrecisionMask { get; private set; }

        // We can further improve the encoding compression by dividing k_SqrtTwoOverTwo into 1.0f and multiplying that
        // by the precision mask (minor reduction of runtime calculations)
        protected float CompressionEncodingMask { get; private set; }

        // We can do the same for our decoding and decompression by dividing k_PrecisionMask into 1.0 and multiplying
        // that by k_SqrtTwoOverTwo (minor reduction of runtime calculations)
        protected float DecompressionDecodingMask { get; private set; }

        protected byte BitsPerValue { get; private set; }

        protected byte BytesPerValue { get; private set; }

        protected int BytePosition { get; private set; }


        // Negative bit set values
        internal const ushort True = 1;
        internal const ushort False = 0;

        private const byte k_LowerSixBits = 0x3F;


        protected void StoreCompressedValue(int index, uint value)
        {
            //var blockOffset = index * BytePosition;
            //var bitsMaximum = (index * sizeof(ushort) * 8);
            //var bitPosition = index * Precision;
            //var precisionIndex = (ushort)((bitsMaximum - bitPosition) % 8);
            //precisionIndex = (ushort)((8 - precisionIndex) % 8);


            var blockOffset = index * BytePosition;
            //var bitsMaximum = (index * sizeof(ushort) * 8);
            var bitShift = (index * Precision) - (blockOffset * 8);

            var arrayBlockSize = (m_SmallestOfNCompressed.Data.Length - blockOffset) < sizeof(uint) ? 2 : 4;
            var currentValue = (uint)0;
            if (arrayBlockSize == 2)
            {
                currentValue = BitConverter.ToUInt16(m_SmallestOfNCompressed.Data, blockOffset);
            }
            else
            {
                currentValue = BitConverter.ToUInt32(m_SmallestOfNCompressed.Data, blockOffset);
            }

            currentValue |= (PrecisionMask & value) << bitShift;
            var adjustedBlock = BitConverter.GetBytes(currentValue);

            //if (BytesPerValue == sizeof(ushort))
            //{
            //    var currentValue = BitConverter.ToUInt16(m_SmallestOfNCompressed.Data, blockIndex);

            //    //var adjustedValue = (ushort)(currentValue | (ushort)((PrecisionMask & value) << precisionIndex));
            //    var adjustedValue = (ushort)(currentValue | (ushort)(value << precisionIndex));

            //    //var adjustedValue = (ushort)(currentValue | value);


            //    // Closer - But off
            //    //var adjustedValue = (ushort)((ushort)((currentValue & ValueMask) >> precisionIndex) | value);
            //    //adjustedValue |= currentValue;
            //    adjustedBlock = BitConverter.GetBytes(adjustedValue);
            //}
            //else
            //{
            //    var adjustedValue = BitConverter.ToUInt32(m_SmallestOfNCompressed.Data, blockIndex) | (PrecisionMask & value);
            //    adjustedBlock = BitConverter.GetBytes(adjustedValue);
            //}
            for (int i = 0; i < arrayBlockSize; i++)
            {
                var dataIndex = blockOffset + i;
                m_SmallestOfNCompressed.Data[dataIndex] = adjustedBlock[i];
            }
        }

        protected uint GetDecompressedValue(int index)
        {
            //var blockOffset = index * BytePosition;
            //var bitsMaximum = (index * sizeof(ushort) * 8);
            //var bitPosition = index * Precision;
            //var precisionIndex = (ushort)((bitsMaximum - bitPosition) % 8);
            //precisionIndex = (ushort)((8 - precisionIndex) % 8);

            var blockOffset = index * BytePosition;
            //var bitsMaximum = (index * sizeof(ushort) * 8);
            var bitShift = (index * Precision) - (blockOffset * 8);
            //var precisionIndex = (ushort)((bitsMaximum - bitPosition) % 8);
            //precisionIndex = (ushort)((8 - precisionIndex) % 8);

            var currentValue = (uint)0;
            if ((m_SmallestOfNCompressed.Data.Length - blockOffset) < sizeof(uint))
            {
                currentValue = BitConverter.ToUInt16(m_SmallestOfNCompressed.Data, blockOffset);
            }
            else
            {
                currentValue = BitConverter.ToUInt32(m_SmallestOfNCompressed.Data, blockOffset);
            }

            return (currentValue >> bitShift) & PrecisionMask;

            //if (BytesPerValue == sizeof(ushort))
            //{
            //    var rawValue = BitConverter.ToUInt16(m_SmallestOfNCompressed.Data, blockIndex);
            //    var bitsMaximum = (index * sizeof(ushort) * 8);
            //    var bitPosition = index * Precision;
            //    var precisionIndex = (ushort)((bitsMaximum - bitPosition) % 8);
            //    precisionIndex = (ushort)((8 - precisionIndex) % 8);
            //    var adjustedValue = (ushort)((rawValue >> precisionIndex) & PrecisionMask);
            //    //var adjustedValue = (ushort)(rawValue & PrecisionMask);

            //    return (uint)adjustedValue;
            //}
            //else
            //{
            //    return BitConverter.ToUInt32(m_SmallestOfNCompressed.Data, blockIndex) & PrecisionMask;
            //}

        }

        /// <summary>
        /// Only applies the first two bits
        /// </summary>
        /// <remarks>
        /// Bits 0-2: Overflow
        /// Bits 3-5: Sign bits
        /// Bits 6-7: Largest index value
        /// </remarks>
        /// <param name="index"></param>
        protected void SetLargestIndex(int largestIndexPosition)
        {
            m_SmallestOfNCompressed.Header |= (byte)((largestIndexPosition << 6) | (m_SmallestOfNCompressed.Header & k_LowerSixBits));
        }

        protected int GetLargestIndex()
        {
            return (m_SmallestOfNCompressed.Header & ~k_LowerSixBits) >> 6;
        }

        /// <summary>
        /// Sets the sign bit
        /// </summary>
        /// <remarks>
        /// Bits 0-2: Overflow
        /// Bits 3-5: Sign bits
        /// Bits 6-7: Largest index value
        /// </remarks>
        /// <param name="index"></param>
        /// <param name="value"></param>
        protected void SetSignBit(int index, float value)
        {
            m_SmallestOfNCompressed.Header |= (byte)((value < 0 ? True : False) << (index + 3));
        }

        protected int GetSignBit(int index)
        {
            return ((m_SmallestOfNCompressed.Header >> (index + 3)) & 0x01) == True ? -1 : 1;
        }

        protected void SetOverFlowBits(uint value)
        {
            value &= 0xFF;
            m_SmallestOfNCompressed.Overflow |= (byte)value;
        }

        protected uint GetOverFlowBits()
        {
            return m_SmallestOfNCompressed.Overflow;
        }

        protected void PopulateNCompressed(ref SmallestOfNCompressed smallestOfCompressorBase)
        {
            m_SmallestOfNCompressed.Header = smallestOfCompressorBase.Header;
            smallestOfCompressorBase.Data.CopyTo(m_SmallestOfNCompressed.Data, 0);
        }

        protected void Clear()
        {
            m_SmallestOfNCompressed.Header = 0;
            m_ClearedData.CopyTo(m_SmallestOfNCompressed.Data, 0);
        }

        private byte[] m_ClearedData;

        public SmallestOfCompressorBase(byte precision)
        {
            Precision = precision;
            BytePosition = (precision / 8);
            BytesPerValue = (byte)((uint)((Precision % 8) > 0 ? 1 : 0) + (uint)BytePosition);

            //var precisionvalue = 1 << Precision;
            //PrecisionMask = (ushort)((precisionvalue - 1) | precisionvalue);
            PrecisionMask = (uint)((1 << Precision) - 1);
            CompressionEncodingMask = (1.0f / SqrtTwoOverTwoEncoding) * PrecisionMask;
            DecompressionDecodingMask = (1.0f / PrecisionMask) * SqrtTwoOverTwoEncoding;

            // Allocate memory for the final compressed values
            var totalBits = precision * 3;
            var totalBytes = ((totalBits % 8) > 0 ? 1 : 0) + (totalBits / 8);
            m_SmallestOfNCompressed = new SmallestOfNCompressed();
            m_SmallestOfNCompressed.Data = new byte[totalBytes];
            m_ClearedData = new byte[totalBytes];
        }
    }

    public class PositionDeltaCompressor : SmallestOfCompressorBase
    {
        // Used to store the absolute value of the delta position value
        private Vector3 m_AbsValues = Vector3.zero;
        public SmallestOfNCompressed CompressDeltaPosition(ref Vector3 positionDelta)
        {
            Clear();


            var normalizedDir = positionDelta.normalized;
            var magnitude = positionDelta.magnitude;

            // Store off the absolute value for each Quaternion element
            m_AbsValues[0] = Mathf.Abs(normalizedDir[0]);
            m_AbsValues[1] = Mathf.Abs(normalizedDir[1]);
            m_AbsValues[2] = Mathf.Abs(normalizedDir[2]);

            // Get the largest element value of the position delta to know what the remaining "Smallest two" values are
            var vectMax = Mathf.Max(m_AbsValues[0], m_AbsValues[1], m_AbsValues[2]);

            // Find the index of the largest element so we can skip that element while compressing and decompressing
            var indexToSkip = (m_AbsValues[0] == vectMax ? 0 : m_AbsValues[1] == vectMax ? 1 : 2);

            // Store the sign of the largest value
            SetLargestIndex(indexToSkip);

            // Get the sign of the largest element
            SetSignBit(2, normalizedDir[indexToSkip]);

            // Add one decimal place of precision
            SetOverFlowBits((uint)Math.Round((magnitude - (uint)magnitude) * 100));

            // Start with the largest value's magnitude that will end up shifted to the highest value bit position range
            var compressed = (uint)magnitude;
            var compressedMagnitude = magnitude;
            StoreCompressedValue(2, compressed);
            //Debug.Log($"[Magnitude] Value = ({magnitude}) | Compressed ({compressed})");
            var decompressed = GetDecompressedValue(2);

            if (Mathf.Abs(magnitude - decompressed) >= 1.0f)
            {
                Debug.Log("Fail!");
            }

            var indexPosition = 0;
            // Step 1: Process each of the four elements
            for (int i = 0; i < 3; i++)
            {
                // Step 2: If we are on the index to skip preserve the current compressed value, otherwise proceed to step 3 and 4
                if (i == indexToSkip)
                {
                    continue;
                }
                // Step 3: Get the sign of the element we are processing. If it is the not the same as the largest value's sign bit then we set the bit
                // Step 4: Get the compressed and encoded value by multiplying the absolute value of the current element by k_CompressionEcodingMask and round that result up
                SetSignBit(indexPosition, normalizedDir[i]);
                compressed = (uint)Mathf.Round(CompressionEncodingMask * m_AbsValues[i]);
                //compressed = (uint)(CompressionEncodingMask * m_AbsValues[i]);
                StoreCompressedValue(indexPosition, compressed);
                indexPosition++;
            }
            decompressed = GetDecompressedValue(2);

            if (Mathf.Abs(compressedMagnitude - decompressed) >= 1.0f)
            {
                Debug.Log("Fail!");
            }

            return m_SmallestOfNCompressed;
        }

        public void DecompressDeltaPosition(ref Vector3 deltaPosition, ref SmallestOfNCompressed smallestOfNCompressed)
        {
            PopulateNCompressed(ref smallestOfNCompressed);

            // Get the last two bits for the index to skip (0-3)
            var indexToSkip = GetLargestIndex();

            // Reverse out the values while skipping over the largest value index
            var sumOfSquaredMagnitudes = 0.0f;
            var indexPosition = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i == indexToSkip)
                {
                    continue;
                }
                var signValue = GetSignBit(indexPosition);
                var compressed = GetDecompressedValue(indexPosition);
                // Check the negative bit and multiply that result with the decompressed and decoded value
                deltaPosition[i] = signValue * compressed * DecompressionDecodingMask;
                sumOfSquaredMagnitudes += deltaPosition[i] * deltaPosition[i];
                indexPosition++;
            }
            // Get the magnitude of the delta position
            var magnitude = (float)GetDecompressedValue(2);
            magnitude += 0.01f * GetOverFlowBits();
            var largestSign = GetSignBit(2);
            // Calculate the largest value from the sum of squares of the two smallest axis values
            deltaPosition[indexToSkip] = Mathf.Sqrt(Mathf.Abs(1.0f - sumOfSquaredMagnitudes)) * largestSign;
            //Debug.Log($"[DeltaPosition] Decompressed Normalized -> ({deltaPosition.x}, {deltaPosition.y},{deltaPosition.z})");
            // Apply the magnitude
            deltaPosition *= magnitude;
        }

        public PositionDeltaCompressor(byte precision) : base(precision)
        {
        }
    }
}
