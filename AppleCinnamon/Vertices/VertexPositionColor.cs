using SharpDX;

namespace AppleCinnamon.Vertices
{
    public struct VertexPositionColor
    {
        public const int Size = 24;

        public Vector3 Position;
        public Color3 Color;

        public VertexPositionColor(Vector3 position, Color3 color)
        {
            Position = position;
            Color = color;
        }
    }
}
