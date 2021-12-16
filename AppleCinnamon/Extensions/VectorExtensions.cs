using SharpDX;

namespace AppleCinnamon.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 Rotate(this Vector3 vector, Vector3 axis, float angle)
            => Vector3.Transform(vector, Quaternion.RotationAxis(axis, angle));
    }
}
