using System;
using SharpDX;

namespace AppleCinnamon.System
{
    public struct Bool3
    {
        public static readonly Bool3 True = new(true);
        public static readonly Bool3 False = new(false);

        public static readonly Bool3 UnitX = new(true, false, false);
        public static readonly Bool3 UnitY = new(false, true, false);
        public static readonly Bool3 UnitZ = new(false, false, true);

        public readonly bool X;
        public readonly bool Y;
        public readonly bool Z;

        public readonly byte Bytes;

        private readonly bool _isTrue;

        public Bool3(bool xyz) 
            : this(xyz, xyz, xyz) { }

        public Bool3(bool x, bool y, bool z)
        {
            X = x;
            Y = y;
            Z = z;
            _isTrue = X && Y && Z;

            byte bytes = 0;
            if (x) bytes += 1;
            if (y) bytes += 2;
            if (z) bytes += 4;

            Bytes = bytes;
        }

        public Bool3(Int3 direction)
            : this(Math.Sign(direction.X) != 0, Math.Sign(direction.Y) != 0, Math.Sign(direction.Z) != 0) { }

        public static bool operator ==(Bool3 lhs, Bool3 rhs) => lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
        public static bool operator !=(Bool3 lhs, Bool3 rhs) => lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
        public static bool operator ==(Bool3 lhs, bool rhs) => lhs._isTrue == rhs;
        public static bool operator !=(Bool3 lhs, bool rhs) => lhs._isTrue != rhs;
        public static bool operator ==(bool lhs, Bool3 rhs) => rhs._isTrue == lhs;
        public static bool operator !=(bool lhs, Bool3 rhs) => rhs._isTrue != lhs;
        public static Bool3 operator &(Bool3 lhs, Bool3 rhs) => new(lhs.X && rhs.X, lhs.Y && rhs.Y, lhs.Z && rhs.Z);


        public bool Equals(Bool3 other)
        {
            return _isTrue == other._isTrue && X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is Bool3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _isTrue.GetHashCode();
                hashCode = (hashCode * 397) ^ X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }
    }
}
