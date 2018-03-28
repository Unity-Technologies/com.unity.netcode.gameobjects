using System;
using IntXLib;
using System.Text;

namespace ECDH
{
    public class EllipticDiffieHellman
    {
        protected static readonly Random rand = new Random();

        protected readonly EllipticCurve curve;
        public readonly IntX priv;
        protected readonly Point generator, pub;


        public EllipticDiffieHellman(EllipticCurve curve, Point generator, IntX order, byte[] priv = null)
        {
            this.curve = curve;
            this.generator = generator;

            // Generate private key
            if (priv == null)
            {
                byte[] max = order.ToArray();
                do
                {
                    byte[] p1 = new byte[5 /*rand.Next(max.Length) + 1*/];

                    rand.NextBytes(p1);

                    if (p1.Length == max.Length) p1[p1.Length - 1] %= max[max.Length - 1];
                    else p1[p1.Length - 1] &= 127;

                    this.priv = Helper.FromArray(p1);
                } while (this.priv<2);
            }
            else this.priv = Helper.FromArray(priv);

            // Generate public key
            pub = curve.Multiply(generator, this.priv);
        }

        public byte[] GetPublicKey()
        {
            byte[] p1 = pub.X.ToArray();
            byte[] p2 = pub.Y.ToArray();

            byte[] ser = new byte[4 + p1.Length + p2.Length];
            ser[0] = (byte)(p1.Length & 255);
            ser[1] = (byte)((p1.Length >> 8) & 255);
            ser[2] = (byte)((p1.Length >> 16) & 255);
            ser[3] = (byte)((p1.Length >> 24) & 255);
            Array.Copy(p1, 0, ser, 4, p1.Length);
            Array.Copy(p2, 0, ser, 4 + p1.Length, p2.Length);

            return ser;
        }

        public byte[] GetPrivateKey() => priv.ToArray();

        public byte[] GetSharedSecret(byte[] pK)
        {
            byte[] p1 = new byte[pK[0] | (pK[1]<<8) | (pK[2]<<16) | (pK[3]<<24)]; // Reconstruct x-axis size
            byte[] p2 = new byte[pK.Length - p1.Length - 4];
            Array.Copy(pK, 4, p1, 0, p1.Length);
            Array.Copy(pK, 4 + p1.Length, p2, 0, p2.Length);

            Point remotePublic = new Point(Helper.FromArray(p1), Helper.FromArray(p2));

            byte[] secret = curve.Multiply(remotePublic, priv).X.ToArray(); // Use the x-coordinate as the shared secret

            // PBKDF2-HMAC-SHA1 (Common shared secret generation method)
            return PBKDF2(HMAC_SHA1, secret, Encoding.UTF8.GetBytes("P1sN0R4inb0wPl5P1sPls"), 1024, 32);
        }


        public delegate byte[] PRF(byte[] key, byte[] salt);
        private static byte[] PBKDF2(PRF function, byte[] password, byte[] salt, int iterations, int dklen)
        {
            byte[] dk = new byte[0]; // Create a placeholder for the derived key
            uint iter = 1; // Track the iterations
            while (dk.Length < dklen)
            {
                // F-function
                // The F-function (PRF) takes the amount of iterations performed in the opposite endianness format from what C# uses, so we have to swap the endianness
                byte[] u = function(password, Concatenate(salt, WriteToArray(new byte[4], SwapEndian(iter), 0)));
                byte[] ures = new byte[u.Length];
                Array.Copy(u, ures, u.Length);
                for (int i = 1; i < iterations; ++i)
                {
                    // Iteratively apply the PRF
                    u = function(password, u);
                    for (int j = 0; j < u.Length; ++j) ures[j] ^= u[j];
                }

                // Concatenate the result to the dk
                dk = Concatenate(dk, ures);

                ++iter;
            }

            // Clip all bytes past what we needed (yes, that's really what the standard is)
            if (dk.Length != dklen)
            {
                var t1 = new byte[dklen];
                Array.Copy(dk, t1, Math.Min(dklen, dk.Length));
                return t1;
            }
            return dk;
        }
        public delegate byte[] HashFunction(byte[] message);
        private static byte[] HMAC(byte[] key, byte[] message, HashFunction func, int blockSizeBytes)
        {
            if (key.Length > blockSizeBytes) key = func(key);
            else if (key.Length < blockSizeBytes)
            {
                byte[] b = new byte[blockSizeBytes];
                Array.Copy(key, b, key.Length);
                key = b;
            }

            byte[] o_key_pad = new byte[blockSizeBytes]; // Outer padding
            byte[] i_key_pad = new byte[blockSizeBytes]; // Inner padding
            for (int i = 0; i < blockSizeBytes; ++i)
            {
                // Combine padding with key
                o_key_pad[i] = (byte)(key[i] ^ 0x5c);
                i_key_pad[i] = (byte)(key[i] ^ 0x36);
            }
            return func(Concatenate(o_key_pad, func(Concatenate(message, i_key_pad))));
        }
        private static byte[] HMAC_SHA1(byte[] key, byte[] message) => HMAC(key, message, SHA1, 20);
        private static byte[] Concatenate(params byte[][] bytes)
        {
            int alloc = 0;
            foreach (byte[] b in bytes) alloc += b.Length;
            byte[] result = new byte[alloc];
            alloc = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                Array.Copy(bytes[i], 0, result, alloc, bytes[i].Length);
                alloc += bytes[i].Length;
            }
            return result;
        }
        public static byte[] SHA1(byte[] message)
        {
            // Initialize buffers
            uint h0 = 0x67452301;
            uint h1 = 0xEFCDAB89;
            uint h2 = 0x98BADCFE;
            uint h3 = 0x10325476;
            uint h4 = 0xC3D2E1F0;

            // Pad message
            int ml = message.Length + 1;
            byte[] msg = new byte[ml + ((960 - (ml * 8 % 512)) % 512) / 8 + 8];
            Array.Copy(message, msg, message.Length);
            msg[message.Length] = 0x80;
            long len = message.Length * 8;
            for (int i = 0; i < 8; ++i) msg[msg.Length - 1 - i] = (byte)((len >> (i * 8)) & 255);
            //Support.WriteToArray(msg, message.Length * 8, msg.Length - 8);
            //for (int i = 0; i <4; ++i) msg[msg.Length - 5 - i] = (byte)(((message.Length*8) >> (i * 8)) & 255);

            int chunks = msg.Length / 64;

            // Perform hashing for each 512-bit block
            for (int i = 0; i < chunks; ++i)
            {

                // Split block into words
                uint[] w = new uint[80];
                for (int j = 0; j < 16; ++j)
                    w[j] |= (uint)((msg[i * 64 + j * 4] << 24) | (msg[i * 64 + j * 4 + 1] << 16) | (msg[i * 64 + j * 4 + 2] << 8) | (msg[i * 64 + j * 4 + 3] << 0));

                // Expand words
                for (int j = 16; j < 80; ++j)
                    w[j] = Rot(w[j - 3] ^ w[j - 8] ^ w[j - 14] ^ w[j - 16], 1);

                // Initialize chunk-hash
                uint
                    a = h0,
                    b = h1,
                    c = h2,
                    d = h3,
                    e = h4;

                // Do hash rounds
                for (int t = 0; t < 80; ++t)
                {
                    uint tmp = ((a << 5) | (a >> (27))) +
                        ( // Round-function
                        t < 20 ? (b & c) | ((~b) & d) :
                        t < 40 ? b ^ c ^ d :
                        t < 60 ? (b & c) | (b & d) | (c & d) :
                        /*t<80*/ b ^ c ^ d
                        ) +
                        e +
                        ( // K-function
                        t < 20 ? 0x5A827999 :
                        t < 40 ? 0x6ED9EBA1 :
                        t < 60 ? 0x8F1BBCDC :
                        /*t<80*/ 0xCA62C1D6
                        ) +
                        w[t];
                    e = d;
                    d = c;
                    c = Rot(b, 30);
                    b = a;
                    a = tmp;
                }
                h0 += a;
                h1 += b;
                h2 += c;
                h3 += d;
                h4 += e;
            }

            return WriteContiguous(new byte[20], 0, SwapEndian(h0), SwapEndian(h1), SwapEndian(h2), SwapEndian(h3), SwapEndian(h4));
        }

        private static uint Rot(uint val, int by) => (val << by) | (val >> (32 - by));

        // Swap endianness of a given integer
        private static uint SwapEndian(uint value) => (uint)(((value >> 24) & (255 << 0)) | ((value >> 8) & (255 << 8)) | ((value << 8) & (255 << 16)) | ((value << 24) & (255 << 24)));

        private static byte[] WriteToArray(byte[] target, uint data, int offset)
        {
            for (int i = 0; i < 4; ++i)
                target[i + offset] = (byte)((data >> (i * 8)) & 255);
            return target;
        }

        private static byte[] WriteContiguous(byte[] target, int offset, params uint[] data)
        {
            for (int i = 0; i < data.Length; ++i) WriteToArray(target, data[i], offset + i * 4);
            return target;
        }
    }

    public static class Helper
    {
        public static byte[] ToArray(this IntX v)
        {
            v.GetInternalState(out uint[] digits, out bool negative);
            byte[] b = DigitConverter.ToBytes(digits);
            byte[] b1 = new byte[b.Length];
            Array.Copy(b, b1, b.Length);
            b1[b.Length] = (byte)(negative ? 1 : 0);
            return b1;
        }
        public static IntX FromArray(byte[] b)
        {
            if (b.Length == 0) return new IntX();
            byte[] b1 = new byte[b.Length - 1];
            Array.Copy(b, b1, b1.Length);
            uint[] u = DigitConverter.FromBytes(b1);
            return new IntX(u, b[b.Length - 1]==1);
        }
        public static bool BitAt(this uint[] data, long index) => (data[index/8]&(1<<(int)(index%8)))!=0;
        public static IntX Abs(this IntX i) => i < 0 ? -i : i;
    }
}
