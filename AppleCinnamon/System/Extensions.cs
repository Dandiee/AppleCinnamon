using System;
using SharpDX;

namespace AppleCinnamon.System
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this Int3 lhs)
        {
            return new Vector3(lhs.X, lhs.Y, lhs.Z);
        }

        public static bool IsEpsilon(this float number)
        {
            return Math.Abs(number) < 0.001f;
        }


        public static bool IsEpsilon(this double number)
        {
            return Math.Abs(number) < 0.00001f;
        }

        public static Double3 ToDouble3(this Vector3 vector)
        {
            return new Double3(vector);
        }

        public static Vector3 ToVector3(this Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static float Distance(this float lhs, float rhs)
        {
            return Math.Abs(Math.Abs(lhs) - Math.Abs(rhs));
        }

        public static Vector3 Add(this Vector3 vector, Int3 index)
        {
            return new Vector3(vector.X + index.X, vector.Y + index.Y, vector.Z + index.Z);
        }

        public static Int3 Round(this Vector3 vector) => new Int3(
            (int) Math.Round(vector.X),
            (int) Math.Round(vector.Y),
            (int) Math.Round(vector.Z));

        public static int Floor(this float value)
        {
            int valueI = (int)value;
            return value < valueI ? valueI - 1 : valueI;
        }

        public static string ToNonRetardedString(this Vector3 vector)
        {
            return $"{vector.X:F2}, {vector.Y:F2}, {vector.Z:F2}";
        }

        public static int ToIndex(this Int3 index)
        {
            return index.X + Chunk.SizeXy * (index.Y + Chunk.Height * index.Z);
        }
    }
}
