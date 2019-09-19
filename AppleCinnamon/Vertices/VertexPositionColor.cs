using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public struct VertexPositionColor
    {
        public const int Size = sizeof(int) * 6;

        public Vector3 Position;
        public Color3 Color;

        public static readonly InputElement[] InputElements =
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new InputElement("TEXCOORD", 0, Format.R32G32B32_Float, 12, 0) //3+2
        };

        public VertexPositionColor(Vector3 position, Color3 color)
        {
            Position = position;
            Color = color;
        }
    }
}
