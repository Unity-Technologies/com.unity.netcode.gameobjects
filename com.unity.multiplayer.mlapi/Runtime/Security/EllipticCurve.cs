#if !DISABLE_CRYPTOGRAPHY
using MLAPI.Serialization;
using System;
using System.Collections.Generic;

namespace MLAPI.Security
{
    internal class CurvePoint
    {
        public static readonly CurvePoint POINT_AT_INFINITY = new CurvePoint();
        public BigInteger X { get; private set; }
        public BigInteger Y { get; private set; }
        private readonly bool pai = false;

        public CurvePoint(BigInteger x, BigInteger y)
        {
            X = x;
            Y = y;
        }

        private CurvePoint()
        {
            pai = true;
        } // Accessing corrdinates causes undocumented behaviour

        public override string ToString()
        {
            return pai ? "(POINT_AT_INFINITY)" : "(" + X + ", " + Y + ")";
        }
    }

    internal class EllipticCurve
    {
        public enum CurveType { Weierstrass, Montgomery }

        protected readonly BigInteger a, b, modulo;
        protected readonly CurveType type;

        public EllipticCurve(BigInteger a, BigInteger b, BigInteger modulo, CurveType type = CurveType.Weierstrass)
        {
            if (
                (type == CurveType.Weierstrass && (4 * a * a * a) + (27 * b * b) == 0) || // Unfavourable Weierstrass curves
                (type == CurveType.Montgomery && b * (a * a - 4) == 0)                      // Unfavourable Montgomery curves
                ) throw new Exception("Unfavourable curve");
            this.a = a;
            this.b = b;
            this.modulo = modulo;
            this.type = type;
        }

        public CurvePoint Add(CurvePoint p1, CurvePoint p2)
        {
#if SAFE_MATH
            CheckOnCurve(p1);
            CheckOnCurve(p2);
#endif

            // Special cases
            if (p1 == CurvePoint.POINT_AT_INFINITY && p2 == CurvePoint.POINT_AT_INFINITY) return CurvePoint.POINT_AT_INFINITY;
            else if (p1 == CurvePoint.POINT_AT_INFINITY) return p2;
            else if (p2 == CurvePoint.POINT_AT_INFINITY) return p1;
            else if (p1.X == p2.X && p1.Y == Inverse(p2).Y) return CurvePoint.POINT_AT_INFINITY;

            BigInteger x3 = 0, y3 = 0;
            if (type == CurveType.Weierstrass)
            {
                BigInteger slope = p1.X == p2.X && p1.Y == p2.Y ? Mod((3 * p1.X * p1.X + a) * MulInverse(2 * p1.Y)) : Mod(Mod(p2.Y - p1.Y) * MulInverse(p2.X - p1.X));
                x3 = Mod((slope * slope) - p1.X - p2.X);
                y3 = Mod(-((slope * x3) + p1.Y - (slope * p1.X)));
            }
            else if (type == CurveType.Montgomery)
            {
                if ((p1.X == p2.X && p1.Y == p2.Y))
                {
                    BigInteger q = 3 * p1.X;
                    BigInteger w = q * p1.X;

                    BigInteger e = 2 * a;
                    BigInteger r = e * p1.X;

                    BigInteger t = 2 * b;
                    BigInteger y = t * p1.Y;

                    BigInteger u = MulInverse(y);

                    BigInteger o = w + e + 1;
                    BigInteger p = o * u;
                }
                BigInteger co = p1.X == p2.X && p1.Y == p2.Y ? Mod((3 * p1.X * p1.X + 2 * a * p1.X + 1) * MulInverse(2 * b * p1.Y)) : Mod(Mod(p2.Y - p1.Y) * MulInverse(p2.X - p1.X)); // Compute a commonly used coefficient
                x3 = Mod(b * co * co - a - p1.X - p2.X);
                y3 = Mod(((2 * p1.X + p2.X + a) * co) - (b * co * co * co) - p1.Y);
            }

            return new CurvePoint(x3, y3);
        }

        public CurvePoint Multiply(CurvePoint p, BigInteger scalar)
        {
            if (scalar <= 0) throw new Exception("Cannot multiply by a scalar which is <= 0");
            if (p == CurvePoint.POINT_AT_INFINITY) return CurvePoint.POINT_AT_INFINITY;

            CurvePoint p1 = new CurvePoint(p.X, p.Y);
            uint[] u = scalar.GetInternalState();
            long high_bit = -1;
            for (int i = u.Length - 1; i >= 0; --i)
                if (u[i] != 0)
                {
                    for (int j = 31; j >= 0; --j)
                        if ((u[i] & (1 << j)) != 0)
                        {
                            high_bit = j + i * 32;
                            goto Next;
                        }
                }
            Next:

            // Double-and-add method
            while (high_bit >= 0)
            {
                p1 = Add(p1, p1); // Double
                if ((u.BitAt(high_bit)))
                    p1 = Add(p1, p); // Add
                --high_bit;
            }

            return p1;
        }

        protected BigInteger MulInverse(BigInteger eq) => MulInverse(eq, modulo);

        public static BigInteger MulInverse(BigInteger eq, BigInteger modulo)
        {
            eq = Mod(eq, modulo);
            Stack<BigInteger> collect = new Stack<BigInteger>();
            BigInteger v = modulo; // Copy modulo
            BigInteger m;
            while ((m = v % eq) != 0)
            {
                collect.Push(-v / eq/*-(m.l_div)*/);
                v = eq;
                eq = m;
            }
            if (collect.Count == 0) return 1;
            v = 1;
            m = collect.Pop();
            while (collect.Count > 0)
            {
                eq = m;
                m = v + (m * collect.Pop());
                v = eq;
            }
            return Mod(m, modulo);
        }

        public CurvePoint Inverse(CurvePoint p) => Inverse(p, modulo);

        protected static CurvePoint Inverse(CurvePoint p, BigInteger modulo) => new CurvePoint(p.X, Mod(-p.Y, modulo));

        public bool IsOnCurve(CurvePoint p)
        {
            try { CheckOnCurve(p); }
            catch { return false; }
            return true;
        }

        protected void CheckOnCurve(CurvePoint p)
        {
            if (
                p != CurvePoint.POINT_AT_INFINITY &&                                                                      // The point at infinity is asserted to be on the curve
                (type == CurveType.Weierstrass && Mod(p.Y * p.Y) != Mod((p.X * p.X * p.X) + (p.X * a) + b)) ||          // Weierstrass formula
                (type == CurveType.Montgomery && Mod(b * p.Y * p.Y) != Mod((p.X * p.X * p.X) + (p.X * p.X * a) + p.X))  // Montgomery formula
                ) throw new Exception("Point is not on curve");
        }

        protected BigInteger Mod(BigInteger b) => Mod(b, modulo);

        private static BigInteger Mod(BigInteger x, BigInteger m)
        {
            BigInteger r = x.Abs() > m ? x % m : x;
            return r < 0 ? r + m : r;
        }

        protected static BigInteger ModPow(BigInteger x, BigInteger power, BigInteger prime)
        {
            BigInteger result = 1;
            bool setBit = false;
            while (power > 0)
            {
                x %= prime;
                setBit = (power & 1) == 1;
                power >>= 1;
                if (setBit) result *= x;
                x *= x;
            }

            return result;
        }
    }
}

#endif