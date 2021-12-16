using System;
using System.Runtime.CompilerServices;
using SharpDX;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace AppleCinnamon.Helper
{
    public static class Help
    {
        public static Vector3 ToVector3(this Int3 lhs) => new(lhs.X, lhs.Y, lhs.Z);
        public static bool IsEpsilon(this float number) => Math.Abs(number) < 0.001f;
        public static bool IsEpsilon(this double number) => Math.Abs(number) < 0.00001f;
        public static Vector3 ToVector3(this Vector4 v) => new(v.X, v.Y, v.Z);


        public static Vector4 ToVector4(this Vector3 v, float w) => new(v, w);
        public static float Distance(this float lhs, float rhs) => Math.Abs(Math.Abs(lhs) - Math.Abs(rhs));

        public static Int3 Round(this Vector3 vector) => new((int) Math.Round(vector.X), (int) Math.Round(vector.Y), (int) Math.Round(vector.Z));

        public static string ToNonRetardedString(this Vector3 vector) => $"{vector.X:F2}, {vector.Y:F2}, {vector.Z:F2}";
    }
}
