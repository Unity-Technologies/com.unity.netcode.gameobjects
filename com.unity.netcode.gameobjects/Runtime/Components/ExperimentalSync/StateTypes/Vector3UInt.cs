using System;
using Unity.Burst;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// A <see cref="Vector3Int"/> as a <see cref="uint"/>.
    /// </summary>
    [Serializable]
    [BurstCompile]
    public struct Vector3UInt : IEquatable<Vector3UInt>
    {
        public uint X;
        public uint Y;
        public uint Z;

        private float m_InvPrecision;
        private uint m_Precision;

        private int m_SignX;
        private int m_SignY;
        private int m_SignZ;

        public Vector3UInt(uint x, uint y, uint z, uint precision = 1000)
        {
            m_InvPrecision = 1.0f / precision;
            m_Precision = precision;
            X = x;
            Y = y;
            Z = z;
            m_SignX = 1;
            m_SignY = 1;
            m_SignZ = 1;
        }

        public Vector3UInt(Vector3 vector3, uint precision = 1000)
        {
            precision = Math.Clamp(precision, 10, 10000);
            var digits = (int)Math.Floor(Math.Log10(Math.Abs(precision) / (precision % 10 == 0 ? Math.Pow(10, (int)Math.Log10(Math.Abs(precision) & ~(Math.Abs(precision) - 1))) : Math.Abs(precision))));
            m_InvPrecision = 1.0f / precision;
            m_Precision = precision;
            X = (uint)Math.Abs(Math.Round((double)(vector3.x * m_Precision), digits, MidpointRounding.AwayFromZero));
            Y = (uint)Math.Abs(Math.Round((double)(vector3.y * m_Precision), digits, MidpointRounding.AwayFromZero));
            Z = (uint)Math.Abs(Math.Round((double)(vector3.z * m_Precision), digits, MidpointRounding.AwayFromZero)); 
            m_SignX = vector3.x >= 0.0f ? 1 : -1;
            m_SignY = vector3.y >= 0.0f ? 1 : -1;
            m_SignZ = vector3.z >= 0.0f ? 1 : -1;
        }

        // Common static properties
        public static readonly Vector3UInt Zero = new Vector3UInt(0, 0, 0);
        public static readonly Vector3UInt One = new Vector3UInt(1, 1, 1);

        // Magnitude (as double to avoid overflow)
        public double Magnitude => Math.Sqrt((double)X * X + (double)Y * Y + (double)Z * Z);

        // Indexer for [0]=x, [1]=y, [2]=z
        public uint this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException("Invalid Vector3UInt index!")
                };
            }
            set
            {
                switch (index)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    default: throw new IndexOutOfRangeException("Invalid Vector3UInt index!");
                }
            }
        }

        // Operator overloads
        public static Vector3UInt operator +(Vector3UInt a, Vector3UInt b) =>
            new Vector3UInt(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3UInt operator -(Vector3UInt a, Vector3UInt b) =>
            new Vector3UInt(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3UInt operator *(Vector3UInt a, uint d) =>
            new Vector3UInt(a.X * d, a.Y * d, a.Z * d);

        public static Vector3UInt operator /(Vector3UInt a, uint d) =>
            new Vector3UInt(a.X / d, a.Y / d, a.Z / d);

        public static bool operator ==(Vector3UInt lhs, Vector3UInt rhs) =>
            lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;

        public static bool operator !=(Vector3UInt lhs, Vector3UInt rhs) => !(lhs == rhs);

        // Equality
        public bool Equals(Vector3UInt other) => this == other;
        public override bool Equals(object obj) => obj is Vector3UInt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        // Conversion to Unity's Vector3Int (clamped to int range)
        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(
                (int)Math.Min(X, int.MaxValue),
                (int)Math.Min(Y, int.MaxValue),
                (int)Math.Min(Z, int.MaxValue)
            );
        }

        // Conversion to Vector3 (float)
        public Vector3 ToVector3() => new Vector3(X * m_InvPrecision * m_SignX, Y * m_InvPrecision * m_SignY, Z * m_InvPrecision * m_SignZ);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
