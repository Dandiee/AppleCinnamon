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
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new("VISIBILITY", 0, Format.R32_UInt, 12, 0) //3+2
        };

        public VertexSolidBlock(Vector3 position, int u, int v, byte baseLight, byte totalneighborLights, int numberOfAmbientneighbors)
        {
            var lightCalc = (baseLight + totalneighborLights )/ 5;

            Position = position;
            Color = 0;
            Color |= (uint)(u << 0);
            Color |= (uint)(v << 4);
            Color |= (uint)(lightCalc << 8);
            Color |= (uint)(numberOfAmbientneighbors << 12);
        }

        public Vector3 Position;

        // 87654321|87654321|87654321|87654321
        // ????????|????????|??aallll|vvvvuuuu
        public uint Color;
    }
}
