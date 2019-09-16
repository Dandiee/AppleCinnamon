using SharpDX;

namespace AppleCinnamon.Vertices
{
    public struct VertexPosition
    {
        public const int Size = 12;

        public Vector3 Position;

        public VertexPosition(Vector3 position)
        {
            Position = position;
        }
    }
}
