using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    public static class BitCounter
    {
        // Since we don't have access to BitOperations.LeadingZeroCount() (which would have been the fastest)
        // we use the De Bruijn sequence to do this calculation
        // See https://en.wikipedia.org/wiki/De_Bruijn_sequence and https://www.chessprogramming.org/De_Bruijn_Sequence
        private const ulong k_DeBruijnMagic64 = 0x37E84A99DAE458F;
        private const uint k_DeBruijnMagic32 = 0x06EB14F9;

        // We're counting bytes, not bits, so these have all had the operation x/8 + 1 applied
        private static readonly int[] k_DeBruijnTableBytes64 =
        {
            0/8+1,  1/8+1, 17/8+1,  2/8+1, 18/8+1, 50/8+1,  3/8+1, 57/8+1,
            47/8+1, 19/8+1, 22/8+1, 51/8+1, 29/8+1,  4/8+1, 33/8+1, 58/8+1,
            15/8+1, 48/8+1, 20/8+1, 27/8+1, 25/8+1, 23/8+1, 52/8+1, 41/8+1,
            54/8+1, 30/8+1, 38/8+1,  5/8+1, 43/8+1, 34/8+1, 59/8+1,  8/8+1,
            63/8+1, 16/8+1, 49/8+1, 56/8+1, 46/8+1, 21/8+1, 28/8+1, 32/8+1,
            14/8+1, 26/8+1, 24/8+1, 40/8+1, 53/8+1, 37/8+1, 42/8+1,  7/8+1,
            62/8+1, 55/8+1, 45/8+1, 31/8+1, 13/8+1, 39/8+1, 36/8+1,  6/8+1,
            61/8+1, 44/8+1, 12/8+1, 35/8+1, 60/8+1, 11/8+1, 10/8+1,  9/8+1,
        };

        private static readonly int[] k_DeBruijnTableBytes32 =
        {
            0/8+1,  1/8+1, 16/8+1,  2/8+1, 29/8+1, 17/8+1,  3/8+1, 22/8+1,
            30/8+1, 20/8+1, 18/8+1, 11/8+1, 13/8+1,  4/8+1,  7/8+1, 23/8+1,
            31/8+1, 15/8+1, 28/8+1, 21/8+1, 19/8+1, 10/8+1, 12/8+1,  6/8+1,
            14/8+1, 27/8+1,  9/8+1,  5/8+1, 26/8+1,  8/8+1, 25/8+1, 24/8+1,
        };

        // And here we're counting the number of set bits, not the position of the highest set,
        // so these still have +1 applied - unfortunately 0 and 1 both return the same value.
        private static readonly int[] k_DeBruijnTableBits64 =
        {
            0+1,  1+1, 17+1,  2+1, 18+1, 50+1,  3+1, 57+1,
            47+1, 19+1, 22+1, 51+1, 29+1,  4+1, 33+1, 58+1,
            15+1, 48+1, 20+1, 27+1, 25+1, 23+1, 52+1, 41+1,
            54+1, 30+1, 38+1,  5+1, 43+1, 34+1, 59+1,  8+1,
            63+1, 16+1, 49+1, 56+1, 46+1, 21+1, 28+1, 32+1,
            14+1, 26+1, 24+1, 40+1, 53+1, 37+1, 42+1,  7+1,
            62+1, 55+1, 45+1, 31+1, 13+1, 39+1, 36+1,  6+1,
            61+1, 44+1, 12+1, 35+1, 60+1, 11+1, 10+1,  9+1,
        };

        private static readonly int[] k_DeBruijnTableBits32 =
        {
            0+1,  1+1, 16+1,  2+1, 29+1, 17+1,  3+1, 22+1,
            30+1, 20+1, 18+1, 11+1, 13+1,  4+1,  7+1, 23+1,
            31+1, 15+1, 28+1, 21+1, 19+1, 10+1, 12+1,  6+1,
            14+1, 27+1,  9+1,  5+1, 26+1,  8+1, 25+1, 24+1,
        };

        /// <summary>
        /// Get the minimum number of bytes required to represent the given value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The number of bytes required</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUsedByteCount(uint value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value = value & ~(value >> 1);
            return k_DeBruijnTableBytes32[value * k_DeBruijnMagic32 >> 27];
        }

        /// <summary>
        /// Get the minimum number of bytes required to represent the given value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The number of bytes required</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUsedByteCount(ulong value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            value = value & ~(value >> 1);
            return k_DeBruijnTableBytes64[value * k_DeBruijnMagic64 >> 58];
        }

        /// <summary>
        /// Get the minimum number of bits required to represent the given value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The number of bits required</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUsedBitCount(uint value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value = value & ~(value >> 1);
            return k_DeBruijnTableBits32[value * k_DeBruijnMagic32 >> 27];
        }

        /// <summary>
        /// Get the minimum number of bits required to represent the given value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The number of bits required</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUsedBitCount(ulong value)
        {
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            value = value & ~(value >> 1);
            return k_DeBruijnTableBits64[value * k_DeBruijnMagic64 >> 58];
        }
    }
}
