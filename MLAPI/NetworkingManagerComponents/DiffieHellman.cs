using System;
using IntXLib;
using System.Text;
using System.Security.Cryptography;

namespace MLAPI.NetworkingManagerComponents
{
    public class EllipticDiffieHellman
    {
        protected static readonly RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
        public static readonly IntX DEFAULT_PRIME = (new IntX(1) << 255) - 19;
        public static readonly IntX DEFAULT_ORDER = (new IntX(1) << 252) + IntX.Parse("27742317777372353535851937790883648493");
        public static readonly EllipticCurve DEFAULT_CURVE = new EllipticCurve(486662, 1, DEFAULT_PRIME, EllipticCurve.CurveType.Montgomery);
        public static readonly CurvePoint DEFAULT_GENERATOR = new CurvePoint(9, IntX.Parse("14781619447589544791020593568409986887264606134616475288964881837755586237401"));

        protected readonly EllipticCurve curve;
        public readonly IntX priv;
        protected readonly CurvePoint generator, pub;


        public EllipticDiffieHellman(EllipticCurve curve, CurvePoint generator, IntX order, byte[] priv = null)
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

                    rand.GetBytes(p1);

                    if (p1.Length == max.Length) p1[p1.Length - 1] %= max[max.Length - 1];
                    else p1[p1.Length - 1] &= 127;

                    this.priv = DHHelper.FromArray(p1);
                } while (this.priv<2);
            }
            else this.priv = DHHelper.FromArray(priv);

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

            CurvePoint remotePublic = new CurvePoint(DHHelper.FromArray(p1), DHHelper.FromArray(p2));

            byte[] secret = curve.Multiply(remotePublic, priv).X.ToArray(); // Use the x-coordinate as the shared secret

            // PBKDF2-HMAC-SHA1 (Common shared secret generation method)
            return new Rfc2898DeriveBytes(secret, Encoding.UTF8.GetBytes("P1sN0R4inb0wPl5P1sPls"), 1000).GetBytes(32);
        }
    }

    public static class DHHelper
    {
        public static byte[] ToArray(this IntX v)
        {
            v.GetInternalState(out uint[] digits, out bool negative);
            byte[] b = DigitConverter.ToBytes(digits);
            byte[] b1 = new byte[b.Length + 1];
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
        public static bool BitAt(this uint[] data, long index) => (data[index / 32] & (1 << (int)(index % 32))) != 0;
        public static IntX Abs(this IntX i) => i < 0 ? -i : i;
    }
}
