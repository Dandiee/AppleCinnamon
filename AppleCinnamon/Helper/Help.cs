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
        public static Double3 ToDouble3(this Vector3 vector) => new(vector);
        public static Vector3 ToVector3(this Vector4 v) => new(v.X, v.Y, v.Z);
        public static float Distance(this float lhs, float rhs) => Math.Abs(Math.Abs(lhs) - Math.Abs(rhs));

        public static Int3 Round(this Vector3 vector) => new((int) Math.Round(vector.X), (int) Math.Round(vector.Y), (int) Math.Round(vector.Z));

        public static string ToNonRetardedString(this Vector3 vector) => $"{vector.X:F2}, {vector.Y:F2}, {vector.Z:F2}";



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int3 ToIndex(this int index, int height)
        {
            var k = index / (Chunk.SizeXy * height);
            var j = (index - k * Chunk.SizeXy * height) / Chunk.SizeXy;
            var i = index - (k * Chunk.SizeXy * height + j * Chunk.SizeXy);

            return new Int3(i, j, k);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToFlatIndex(this Int3 index, int height) => index.X + Chunk.SizeXy * (index.Y + height * index.Z);

        [InlineMethod.Inline]
        public static int GetFlatIndex(int i, int j, int k, int height) => i + Chunk.SizeXy * (j + height * k);

        [InlineMethod.Inline]
        public static int GetChunkFlatIndex(int i, int j) => 3 * i + j + 4;

        [InlineMethod.Inline]
        public static int GetChunkFlatIndex(Int2 ij) => 3 * ij.X + ij.Y + 4;
    }
}
