using System;
using SharpDX;

namespace AppleCinnamon.System
{
    public struct Double3
    {
        public static readonly Double3 Zero = new Double3();
        public static readonly Double3 UnitX = new Double3(1, 0, 0);
        public static readonly Double3 UnitY = new Double3(0, 1, 0);
        public static readonly Double3 UnitZ = new Double3(0, 0, 1);

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Double3(double xyz) : this(xyz, xyz, xyz) { }
        public Double3(Vector3 vector) : this(vector.X, vector.Y, vector.Z) { }
        public Double3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Double3 operator +(Double3 lhs, Double3 rhs) => new Double3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        public static Double3 operator +(Double3 lhs, Vector3 rhs) => new Double3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        public static Double3 operator +(Vector3 lhs, Double3 rhs) => new Double3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        public static Double3 operator -(Double3 lhs, Double3 rhs) => new Double3(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        public static Double3 operator *(Double3 lhs, Double3 rhs) => new Double3(lhs.X * rhs.X, lhs.Y * rhs.Y, lhs.Z * rhs.Z);
        public static Double3 operator *(Double3 lhs, double rhs) => rhs * lhs;
        public static Double3 operator *(double lhs, Double3 rhs) => new Double3(lhs * rhs.X, lhs * rhs.Y, lhs * rhs.Z);
        public static Double3 operator +(Double3 lhs, double rhs) => rhs + lhs;
        public static Double3 operator +(double lhs, Double3 rhs) => new Double3(lhs + rhs.X, lhs + rhs.Y, lhs + rhs.Z);
        public static Double3 operator -(Double3 lhs, double rhs) => rhs - lhs;
        public static Double3 operator -(double lhs, Double3 rhs) => new Double3(lhs - rhs.X, lhs - rhs.Y, lhs - rhs.Z);
        public static Double3 operator /(Double3 lhs, double rhs) => rhs / lhs;
        public static Double3 operator /(double lhs, Double3 rhs) => new Double3(lhs / rhs.X, lhs / rhs.Y, lhs / rhs.Z);

        public static Double3 operator /(Double3 lhs, Double3 rhs) => new Double3(lhs.X / rhs.X, lhs.Y / rhs.Y, lhs.Z / rhs.Z);
        public static bool operator ==(Double3 lhs, Double3 rhs) => lhs.X == rhs.X && lhs.Y ==rhs.Y && lhs.Z == rhs.Z;
        public static bool operator !=(Double3 lhs, Double3 rhs) => !(lhs == rhs);

        public Vector3 ToVector3() => new Vector3((float)X, (float)Y, (float)Z);

        public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

        public static Double3 Normalize(Double3 vector)
        {
            var length = vector.Length();
            if (length.IsEpsilon())
            {
                return Zero;
            }

            var num = 1d / length;

            return new Double3(vector.X * num, vector.Y * num, vector.Z * num);
        }

        public bool Equals(Double3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is Double3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"X:{{{X}}} Y:{{{Y}}} Z:{{{Z}}}";
        }
    }
}