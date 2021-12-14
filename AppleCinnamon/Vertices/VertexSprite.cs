using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public struct VertexSprite : IVertex
    {
        private const int _size = 16;

        private static readonly InputElement[] _inputElements =
        {
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new("VISIBILITY", 0, Format.R32_UInt, 12, 0) //3+2
        };

        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexSprite(Vector3 position, int u, int v, byte baseLight, byte hueIndex)
        {
            
            Position = position;

            var l = baseLight;
            var h = hueIndex & 15;

            Color = 0;
            Color |= (uint)(u <<  0); // 5 bits
            Color |= (uint)(v <<  5); // 5 bits
            Color |= (uint)(l << 10); // 4 bits
            Color |= (uint)(h << 14); // 4 bits
        }

        public Vector3 Position;
        public uint Color;

    }
}