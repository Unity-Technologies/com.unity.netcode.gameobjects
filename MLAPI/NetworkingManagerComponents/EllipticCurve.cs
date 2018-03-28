using System;
using System.Collections.Generic;
using IntXLib;

namespace ECDH
{
    public class Point
    {
        public static readonly Point POINT_AT_INFINITY = new Point();
        public IntX X { get; private set; }
        public IntX Y { get; private set; }
        private bool pai = false;
        public Point(IntX x, IntX y)
        {
            X = x;
            Y = y;
        }
        private Point() { pai = true; } // Accessing corrdinates causes undocumented behaviour
        public override string ToString()
        {
            return pai ? "(POINT_AT_INFINITY)" : "(" + X + ", " + Y + ")";
        }
    }

    public class EllipticCurve
    {
        public enum CurveType { Weierstrass, Montgomery }

        protected readonly IntX a, b, modulo;
        protected readonly CurveType type;

        public EllipticCurve(IntX a, IntX b, IntX modulo, CurveType type = CurveType.Weierstrass)
        {
            if (
                (type==CurveType.Weierstrass && (4 * a * a * a) + (27 * b * b) == 0) || // Unfavourable Weierstrass curves
                (type==CurveType.Montgomery && b * (a * a - 4)==0)                      // Unfavourable Montgomery curves
                ) throw new Exception("Unfavourable curve");
            this.a = a;
            this.b = b;
            this.modulo = modulo;
            this.type = type;
        }

        public Point Add(Point p1, Point p2)
        {
#if SAFE_MATH
            CheckOnCurve(p1);
            CheckOnCurve(p2);
#endif

            // Special cases
            if (p1 == Point.POINT_AT_INFINITY && p2 == Point.POINT_AT_INFINITY) return Point.POINT_AT_INFINITY;
            else if (p1 == Point.POINT_AT_INFINITY) return p2;
            else if (p2 == Point.POINT_AT_INFINITY) return p1;
            else if (p1.X == p2.X && p1.Y == Inverse(p2).Y) return Point.POINT_AT_INFINITY;
            
            IntX x3 = 0, y3 = 0;
            if (type == CurveType.Weierstrass)
            {
                IntX slope = p1.X == p2.X && p1.Y == p2.Y ? Mod((3 * p1.X * p1.X + a) * MulInverse(2 * p1.Y)) : Mod(Mod(p2.Y - p1.Y) * MulInverse(p2.X - p1.X));
                x3 = Mod((slope * slope) - p1.X - p2.X);
                y3 = Mod(-((slope * x3) + p1.Y - (slope * p1.X)));
            }
            else if (type == CurveType.Montgomery)
            {
                if ((p1.X == p2.X && p1.Y == p2.Y))
                {
                    IntX q = 3 * p1.X;
                    IntX w = q * p1.X;

                    IntX e = 2 * a;
                    IntX r = e * p1.X;

                    IntX t = 2 * b;
                    IntX y = t * p1.Y;

                    IntX u = MulInverse(y);

                    IntX o = w + e + 1;
                    IntX p = o * u;
                }
                IntX co = p1.X == p2.X && p1.Y == p2.Y ? Mod((3 * p1.X * p1.X + 2 * a * p1.X + 1) * MulInverse(2 * b * p1.Y)) : Mod(Mod(p2.Y - p1.Y) * MulInverse(p2.X - p1.X)); // Compute a commonly used coefficient
                x3 = Mod(b * co * co - a - p1.X - p2.X);
                y3 = Mod(((2 * p1.X + p2.X + a) * co) - (b * co * co * co) - p1.Y);
            }
            
            return new Point(x3, y3);
        }

        public Point Multiply(Point p, IntX scalar)
        {
            if (scalar <= 0) throw new Exception("Cannot multiply by a scalar which is <= 0");
            if (p == Point.POINT_AT_INFINITY) return Point.POINT_AT_INFINITY;

            Point p1 = new Point(p.X, p.Y);
            scalar.GetInternalState(out uint[] u, out bool b);
            long high_bit = -1;
            for (int i = u.Length - 1; i>=0; --i)
                if (u[i] != 0)
                {
                    for(int j = 31; j>=0; --j)
                        if ((u[i] & (1<<j))!=0)
                        {
                            high_bit = j + i * 32;
                            goto Next;
                        }
                }
            Next:

            // Double-and-add method
            while(high_bit >= 0)
            {
                p1 = Add(p1, p1); // Double
                if ((u.BitAt(high_bit)))
                    p1 = Add(p1, p); // Add
                --high_bit;
            }

            return p1;
        }

        protected IntX MulInverse(IntX eq) => MulInverse(eq, modulo);
        public static IntX MulInverse(IntX eq, IntX modulo)
        {
            eq = Mod(eq, modulo);
            Stack<IntX> collect = new Stack<IntX>();
            IntX v = modulo; // Copy modulo
            IntX m;
            while((m = v % eq) != 0)
            {
                collect.Push(-v/eq/*-(m.l_div)*/);
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

        public Point Inverse(Point p) => Inverse(p, modulo);
        protected static Point Inverse(Point p, IntX modulo) => new Point(p.X, Mod(-p.Y, modulo));

        public bool IsOnCurve(Point p)
        {
            try { CheckOnCurve(p); }
            catch { return false; }
            return true;
        }
        protected void CheckOnCurve(Point p)
        {
            if (
                p!=Point.POINT_AT_INFINITY &&                                                                           // The point at infinity is asserted to be on the curve
                (type == CurveType.Weierstrass && Mod(p.Y * p.Y) != Mod((p.X * p.X * p.X) + (p.X * a) + b)) ||          // Weierstrass formula
                (type == CurveType.Montgomery && Mod(b * p.Y * p.Y) != Mod((p.X * p.X * p.X) + (p.X * p.X * a) + p.X))  // Montgomery formula
                ) throw new Exception("Point is not on curve");
        }

        protected IntX Mod(IntX b) => Mod(b, modulo);

        private static IntX Mod(IntX x, IntX m)
        {
            IntX r = x.Abs() > m ? x % m : x;
            return r < 0 ? r + m : r;
        }

        protected static IntX ModPow(IntX x, IntX power, IntX prime)
        {
            IntX result = 1;
            bool setBit = false;
            while(power > 0)
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
