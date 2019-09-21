using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public struct VertexSolidBlock
    {
        public const int Size = sizeof(int) * 4;

        public static readonly InputElement[] InputElements = 
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new InputElement("VISIBILITY", 0, Format.R32_UInt, 12, 0) //3+2
        };

        public VertexSolidBlock(Vector3 position, byte u, byte v, byte ao, byte baseLightness)
        {
            Position = position;
            Color = 0;
            Color |= (uint)(u << 0);
            Color |= (uint)(v << 4);
            Color |= (uint)(baseLightness << 8);
            Color |= (uint)(ao << 12);

        }

        public Vector3 Position;

        // 87654321|87654321|87654321|87654321
        // ????????|????????|??aallll|vvvvuuuu
        public uint Color;
    }
}
