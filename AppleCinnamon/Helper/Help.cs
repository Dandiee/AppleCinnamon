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


        public static Vector4 ToVector4(this Vector3 v, float w) => new(v, w);
        public static float Distance(this float lhs, float rhs) => Math.Abs(Math.Abs(lhs) - Math.Abs(rhs));

        public static Int3 Round(this Vector3 vector) => new((int) Math.Round(vector.X), (int) Math.Round(vector.Y), (int) Math.Round(vector.Z));

        public static string ToNonRetardedString(this Vector3 vector) => $"{vector.X:F2}, {vector.Y:F2}, {vector.Z:F2}";



        [InlineMethod.Inline]
        public static int GetChunkFlatIndex(int i, int j) => 3 * i + j + 4;

        public static bool TryGetChunkIndexByAbsoluteVoxelIndex(Int3 absoluteVoxelIndex, out Int2 chunkIndex)
        {
            if (absoluteVoxelIndex.Y < 0)
            {
                chunkIndex = Int2.Zero;
                return false;
            }

            chunkIndex = new Int2(
                absoluteVoxelIndex.X < 0
                    ? ((absoluteVoxelIndex.X + 1) / Chunk.SizeXy) - 1
                    : absoluteVoxelIndex.X / Chunk.SizeXy,
                absoluteVoxelIndex.Z < 0
                    ? ((absoluteVoxelIndex.Z + 1) / Chunk.SizeXy) - 1
                    : absoluteVoxelIndex.Z / Chunk.SizeXy);
            return true;
        }
    }
}
