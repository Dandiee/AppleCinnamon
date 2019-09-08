using SharpDX;

namespace AppleCinnamon
{
    public sealed class BoxDetails
    {
        public Vector3 Size { get; }
        public Vector3 Position { get; }
        public Color3 Color { get; }

        public BoxDetails(Vector3 size, Vector3 position, Color3 color)
        {
            Size = size;
            Position = position;
            Color = color;
        }
    }
}