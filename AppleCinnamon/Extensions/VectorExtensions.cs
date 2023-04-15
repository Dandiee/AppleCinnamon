using SharpDX;
using System;

namespace AppleCinnamon.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 Rotate(this Vector3 vector, Vector3 axis, float angle) => Vector3.Transform(vector, Quaternion.RotationAxis(axis, angle));
        public static Vector3 ToVector3(this Int3 lhs) => new(lhs.X, lhs.Y, lhs.Z);
        public static Vector3 ToVector3(this Vector4 v) => new(v.X, v.Y, v.Z);
        public static Vector4 ToVector4(this Vector3 v, float w) => new(v, w);
        public static string ToNonRetardedString(this Vector3 vector) => $"{vector.X:F2}, {vector.Y:F2}, {vector.Z:F2}";
        public static Int3 Round(this Vector3 vector) => new((int)Math.Round(vector.X), (int)Math.Round(vector.Y), (int)Math.Round(vector.Z));
    }
}
