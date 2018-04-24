using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MLAPI.NetworkingManagerComponents.Cryptography
{
    public static class MessageDigest
    {
        public struct SHA1Result
        {
            public uint i0, i1, i2, i3, i4;
            public byte Get(int idx) => (byte)((idx < 4 ? i0 : idx < 8 ? i1 : idx < 12 ? i2 : idx < 16 ? i3 : i4) >> (8 * (idx % 4)));
            public byte[] ToArray()
            {
                byte[] b = new byte[20];
                for (int i = 0; i < 20; ++i) b[i] = Get(i);
                return b;
            }
            private ulong Collect(int bytes)
            {
                ulong u = 0;
                for (int i = 0; i < bytes; ++i) u |= (ulong)Get(i) << (8*i);
                return u;
            }
            public byte CastByte() => Get(0);
            public ushort CastUShort() => (ushort)Collect(2);
            public uint CastUInt() => (uint)Collect(4);
            public ulong CastULong() => (ulong)Collect(8);
        }
        public static SHA1Result SHA1_Opt(byte[] message)
        {
            SHA1Result result = new SHA1Result
            {
                // Initialize buffers
                i0 = 0x67452301,
                i1 = 0xEFCDAB89,
                i2 = 0x98BADCFE,
                i3 = 0x10325476,
                i4 = 0xC3D2E1F0
            };

            // Pad message
            long len = message.Length * 8;
            int
                ml = message.Length + 1,
                max = ml + ((960 - (ml * 8 % 512)) % 512) / 8 + 8;

            // Replaces the allocation of a lot of bytes
            byte GetMsg(int idx)
            {
                if (idx < message.Length)
                    return message[idx];
                else if (idx == message.Length)
                    return 0x80;
                else if (max - idx <= 8)
                    return (byte)((len >> ((max - 1 - idx) * 8)) & 255);
                return 0;
            }

            int chunks = max / 64;

            // Replaces the recurring allocation of 80 uints
            uint ComputeIndex(int block, int idx)
            {
                if (idx < 16)
                    return (uint)((GetMsg(block * 64 + idx * 4) << 24) | (GetMsg(block * 64 + idx * 4 + 1) << 16) | (GetMsg(block * 64 + idx * 4 + 2) << 8) | (GetMsg(block * 64 + idx * 4 + 3) << 0));
                else
                    return Rot(ComputeIndex(block, idx - 3) ^ ComputeIndex(block, idx - 8) ^ ComputeIndex(block, idx - 14) ^ ComputeIndex(block, idx - 16), 1);
            }

            // Perform hashing for each 512-bit block
            for (int i = 0; i < chunks; ++i)
            {

                // Initialize chunk-hash
                uint
                    a = result.i0,
                    b = result.i1,
                    c = result.i2,
                    d = result.i3,
                    e = result.i4;

                // Do hash rounds
                for (int t = 0; t < 80; ++t)
                {
                    uint tmp = Rot(a, 5) + func(t, b, c, d) + e + K(t) + ComputeIndex(i, t);
                    e = d;
                    d = c;
                    c = Rot(b, 30);
                    b = a;
                    a = tmp;
                }
                result.i0 += a;
                result.i1 += b;
                result.i2 += c;
                result.i3 += d;
                result.i4 += e;
            }
            result.i0 = Support.SwapEndian(result.i0);
            result.i1 = Support.SwapEndian(result.i1);
            result.i2 = Support.SwapEndian(result.i2);
            result.i3 = Support.SwapEndian(result.i3);
            result.i4 = Support.SwapEndian(result.i4);
            return result;
        }

        private static uint func(int t, uint b, uint c, uint d) =>
            t < 20 ? (b & c) | ((~b) & d) :
            t < 40 ? b ^ c ^ d :
            t < 60 ? (b & c) | (b & d) | (c & d) :
            /*t<80*/ b ^ c ^ d;

        private static uint K(int t) =>
            t < 20 ? 0x5A827999 :
            t < 40 ? 0x6ED9EBA1 :
            t < 60 ? 0x8F1BBCDC :
            /*t<80*/ 0xCA62C1D6;

        private static uint Rot(uint val, int by) => (val << by) | (val >> (32 - by));
    }
}
