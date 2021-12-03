using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SharpDX;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace AppleCinnamon.System
{
    public static class Help
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



        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Int3 ToIndex(this int index)
        //{
        //    var k = index / (Chunk.SizeXy * Chunk.Height);
        //    var j = (index - k * Chunk.SizeXy * Chunk.Height) / Chunk.SizeXy;
        //    var i = index - (k * Chunk.SizeXy * Chunk.Height + j * Chunk.SizeXy);

        //    return new Int3(i, j, k);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static int ToFlatIndex(this Int3 index) => index.X + Chunk.SizeXy * (index.Y + Chunk.Height * index.Z);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static int GetFlatIndex(int i, int j, int k) => i + Chunk.SizeXy * (j + Chunk.Height * k);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int3 ToIndex1(this int index)
        {
            var slice = index / Chunk.SliceArea;
            var sliceIndex = index - slice * Chunk.SliceArea;

            var k = sliceIndex / (Chunk.SizeXy * Chunk.SliceHeight);
            var j = (sliceIndex - k * Chunk.SizeXy * Chunk.SliceHeight) / Chunk.SizeXy;
            var i = sliceIndex - (k * Chunk.SizeXy * Chunk.SliceHeight + j * Chunk.SizeXy);

            return new Int3(i, j + slice * Chunk.SliceHeight, k);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToFlatIndex1(this Int3 index)
        {
            var slice = index.Y / Chunk.SliceHeight;
            return slice * Chunk.SliceArea + index.X + Chunk.SizeXy * (index.Y - slice * Chunk.SliceHeight + Chunk.SliceHeight * index.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatIndex1(int i, int j, int k)
        {
            return j / Chunk.SliceHeight * Chunk.SliceArea + i + Chunk.SizeXy * (j - j / Chunk.SliceHeight * Chunk.SliceHeight + k * Chunk.SliceHeight);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int3 ToIndex(this int index, int height)
        {
            var k = index / (Chunk.SizeXy * height);
            var j = (index - k * Chunk.SizeXy * height) / Chunk.SizeXy;
            var i = index - (k * Chunk.SizeXy * height + j * Chunk.SizeXy);

            return new Int3(i, j, k);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToIndex(this int index, int height, out int i, out int j, out int k)
        {
            k = index / (Chunk.SizeXy * height);
            j = (index - k * Chunk.SizeXy * height) / Chunk.SizeXy;
            i = index - (k * Chunk.SizeXy * height + j * Chunk.SizeXy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToFlatIndex(this Int3 index, int height) => index.X + Chunk.SizeXy * (index.Y + height * index.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatIndex(int i, int j, int k, int height) => i + Chunk.SizeXy * (j + height * k);
    }
}
