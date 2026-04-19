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
    internal struct Vector3UInt : IEquatable<Vector3UInt>
    {
        public uint X;
        public uint Y;
        public uint Z;

        public Vector3UInt(uint x, uint y, uint z)
        {
            X = x;
            Y = y;
            Z = z;
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
        public Vector3 ToVector3() => new Vector3(X, Y, Z);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
