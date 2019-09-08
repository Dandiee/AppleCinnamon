namespace AppleCinnamon.System
{
	public struct Int2
	{
        public static readonly Int2 UniX = new Int2(1, 0);
        public static readonly Int2 UniY = new Int2(0, 1);

        public bool Equals(Int2 other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Int2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public int X;
		public int Y;

        public override string ToString()
        {
            return $"{X}; {Y}";
        }

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }
        public Int2(int xy)
        {
            X = Y = xy;
        }


        public static Int2 operator -(Int2 lhs)
        {
            return new Int2(-lhs.X, -lhs.Y);
        }


        public static bool operator ==(Int2 lhs, Int2 rhs)
		{
			return lhs.X == rhs.X && lhs.Y == rhs.Y;
		}

		public static bool operator !=(Int2 lhs, Int2 rhs)
		{
			return lhs.X != rhs.X || lhs.Y != rhs.Y;
		}

		public static Int2 operator +(Int2 lhs, Int2 rhs)
		{
			return new Int2(lhs.X + rhs.X, lhs.Y + rhs.Y);
		}

		public static Int2 operator -(Int2 lhs, Int2 rhs)
		{
			return new Int2(lhs.X - rhs.X, lhs.Y - rhs.Y);
		}

		public static Int2 operator *(Int2 lhs, Int2 rhs)
		{
			return new Int2(lhs.X * rhs.X, lhs.Y * rhs.Y);
		}

        public static Int2 operator *(Int2 lhs, int rhs)
        {
            return new Int2(lhs.X * rhs, lhs.Y * rhs);
        }

        public static readonly Int2 Zero = new Int2();
    }
}
