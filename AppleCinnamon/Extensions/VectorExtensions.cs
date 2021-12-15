using SharpDX;

namespace AppleCinnamon.Extensions
{
    public static class VectorExtensions
    {
        public static Int3 ToGlobal(this Int3 relative, Chunk chunk)
            => new Int3(chunk.ChunkIndex.X * Chunk.SizeXy + relative.X, relative.Y,
                chunk.ChunkIndex.Y * Chunk.SizeXy + relative.Z);


        public static Vector3 Rotate(this Vector3 vector, Vector3 axis, float angle)
            => Vector3.Transform(vector, Quaternion.RotationAxis(axis, angle));
    }
}
