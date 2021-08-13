// <copyright file="XXHash.cs" company="Sedat Kapanoglu">
// Copyright (c) 2015-2019 Sedat Kapanoglu
// MIT License (see LICENSE file for details)
// </copyright>

// @mfatihmar (Unity): Modified for Unity support

using System.Text;
using System.Runtime.CompilerServices;

namespace Unity.Netcode
{
    /// <summary>
    /// XXHash implementation.
    /// </summary>
    internal static class XXHash
    {
        private const ulong k_Prime64v1 = 11400714785074694791ul;
        private const ulong k_Prime64v2 = 14029467366897019727ul;
        private const ulong k_Prime64v3 = 1609587929392839161ul;
        private const ulong k_Prime64v4 = 9650029242287828579ul;
        private const ulong k_Prime64v5 = 2870177450012600261ul;

        private const uint k_Prime32v1 = 2654435761u;
        private const uint k_Prime32v2 = 2246822519u;
        private const uint k_Prime32v3 = 3266489917u;
        private const uint k_Prime32v4 = 668265263u;
        private const uint k_Prime32v5 = 374761393u;

        public static uint Hash32(string text) => Hash32(text, Encoding.UTF8);
        public static uint Hash32(string text, Encoding encoding) => Hash32(encoding.GetBytes(text));
        public static uint Hash32(byte[] buffer)
        {
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    return Hash32(ptr, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Generate a 32-bit xxHash value.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="bufferLength">Input buffer length.</param>
        /// <param name="seed">Optional seed.</param>
        /// <returns>32-bit hash value.</returns>
        public static unsafe uint Hash32(byte* buffer, int bufferLength, uint seed = 0)
        {
            const int stripeLength = 16;

            int len = bufferLength;
            int remainingLen = len;
            uint acc;

            byte* pInput = buffer;
            if (len >= stripeLength)
            {
                uint acc1 = seed + k_Prime32v1 + k_Prime32v2;
                uint acc2 = seed + k_Prime32v2;
                uint acc3 = seed;
                uint acc4 = seed - k_Prime32v1;

                do
                {
                    acc = processStripe32(ref pInput, ref acc1, ref acc2, ref acc3, ref acc4);
                    remainingLen -= stripeLength;
                } while (remainingLen >= stripeLength);
            }
            else
            {
                acc = seed + k_Prime32v5;
            }

            acc += (uint)len;
            acc = processRemaining32(pInput, acc, remainingLen);

            return avalanche32(acc);
        }

        public static ulong Hash64(string text) => Hash64(text, Encoding.UTF8);
        public static ulong Hash64(string text, Encoding encoding) => Hash64(encoding.GetBytes(text));
        public static ulong Hash64(byte[] buffer)
        {
            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    return Hash64(ptr, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Generate a 64-bit xxHash value.
        /// </summary>
        /// <param name="buffer">Input buffer.</param>
        /// <param name="bufferLength">Input buffer length.</param>
        /// <param name="seed">Optional seed.</param>
        /// <returns>Computed 64-bit hash value.</returns>
        public static unsafe ulong Hash64(byte* buffer, int bufferLength, ulong seed = 0)
        {
            const int stripeLength = 32;

            int len = bufferLength;
            int remainingLen = len;
            ulong acc;

            byte* pInput = buffer;
            if (len >= stripeLength)
            {
                ulong acc1 = seed + k_Prime64v1 + k_Prime64v2;
                ulong acc2 = seed + k_Prime64v2;
                ulong acc3 = seed;
                ulong acc4 = seed - k_Prime64v1;

                do
                {
                    acc = processStripe64(ref pInput, ref acc1, ref acc2, ref acc3, ref acc4);
                    remainingLen -= stripeLength;
                } while (remainingLen >= stripeLength);
            }
            else
            {
                acc = seed + k_Prime64v5;
            }

            acc += (ulong)len;
            acc = processRemaining64(pInput, acc, remainingLen);


            return avalanche64(acc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong processStripe64(
            ref byte* pInput,
            ref ulong acc1,
            ref ulong acc2,
            ref ulong acc3,
            ref ulong acc4)
        {
            processLane64(ref acc1, ref pInput);
            processLane64(ref acc2, ref pInput);
            processLane64(ref acc3, ref pInput);
            processLane64(ref acc4, ref pInput);

            ulong acc = Bits.RotateLeft(acc1, 1)
                        + Bits.RotateLeft(acc2, 7)
                        + Bits.RotateLeft(acc3, 12)
                        + Bits.RotateLeft(acc4, 18);

            mergeAccumulator64(ref acc, acc1);
            mergeAccumulator64(ref acc, acc2);
            mergeAccumulator64(ref acc, acc3);
            mergeAccumulator64(ref acc, acc4);
            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void processLane64(ref ulong accn, ref byte* pInput)
        {
            ulong lane = *(ulong*)pInput;
            accn = round64(accn, lane);
            pInput += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe ulong processRemaining64(
            byte* pInput,
            ulong acc,
            int remainingLen)
        {
            for (ulong lane; remainingLen >= 8; remainingLen -= 8, pInput += 8)
            {
                lane = *(ulong*)pInput;

                acc ^= round64(0, lane);
                acc = Bits.RotateLeft(acc, 27) * k_Prime64v1;
                acc += k_Prime64v4;
            }

            for (uint lane32; remainingLen >= 4; remainingLen -= 4, pInput += 4)
            {
                lane32 = *(uint*)pInput;

                acc ^= lane32 * k_Prime64v1;
                acc = Bits.RotateLeft(acc, 23) * k_Prime64v2;
                acc += k_Prime64v3;
            }

            for (byte lane8; remainingLen >= 1; remainingLen--, pInput++)
            {
                lane8 = *pInput;
                acc ^= lane8 * k_Prime64v5;
                acc = Bits.RotateLeft(acc, 11) * k_Prime64v1;
            }

            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong avalanche64(ulong acc)
        {
            acc ^= acc >> 33;
            acc *= k_Prime64v2;
            acc ^= acc >> 29;
            acc *= k_Prime64v3;
            acc ^= acc >> 32;
            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong round64(ulong accn, ulong lane)
        {
            accn += lane * k_Prime64v2;
            return Bits.RotateLeft(accn, 31) * k_Prime64v1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void mergeAccumulator64(ref ulong acc, ulong accn)
        {
            acc ^= round64(0, accn);
            acc *= k_Prime64v1;
            acc += k_Prime64v4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint processStripe32(
            ref byte* pInput,
            ref uint acc1,
            ref uint acc2,
            ref uint acc3,
            ref uint acc4)
        {
            processLane32(ref pInput, ref acc1);
            processLane32(ref pInput, ref acc2);
            processLane32(ref pInput, ref acc3);
            processLane32(ref pInput, ref acc4);

            return Bits.RotateLeft(acc1, 1)
                   + Bits.RotateLeft(acc2, 7)
                   + Bits.RotateLeft(acc3, 12)
                   + Bits.RotateLeft(acc4, 18);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void processLane32(ref byte* pInput, ref uint accn)
        {
            uint lane = *(uint*)pInput;
            accn = round32(accn, lane);
            pInput += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint processRemaining32(
            byte* pInput,
            uint acc,
            int remainingLen)
        {
            for (uint lane; remainingLen >= 4; remainingLen -= 4, pInput += 4)
            {
                lane = *(uint*)pInput;
                acc += lane * k_Prime32v3;
                acc = Bits.RotateLeft(acc, 17) * k_Prime32v4;
            }

            for (byte lane; remainingLen >= 1; remainingLen--, pInput++)
            {
                lane = *pInput;
                acc += lane * k_Prime32v5;
                acc = Bits.RotateLeft(acc, 11) * k_Prime32v1;
            }

            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint round32(uint accn, uint lane)
        {
            accn += lane * k_Prime32v2;
            accn = Bits.RotateLeft(accn, 13);
            accn *= k_Prime32v1;
            return accn;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint avalanche32(uint acc)
        {
            acc ^= acc >> 15;
            acc *= k_Prime32v2;
            acc ^= acc >> 13;
            acc *= k_Prime32v3;
            acc ^= acc >> 16;
            return acc;
        }

        /// <summary>
        /// Bit operations.
        /// </summary>
        private static class Bits
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static ulong RotateLeft(ulong value, int bits)
            {
                return (value << bits) | (value >> (64 - bits));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static uint RotateLeft(uint value, int bits)
            {
                return (value << bits) | (value >> (32 - bits));
            }
        }
    }
}
