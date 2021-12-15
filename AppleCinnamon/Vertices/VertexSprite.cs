using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
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

        public VertexSprite(Vector3 position, int u, int v, byte metaData, byte compositeLight)
        {
            Position = position;

            var l = compositeLight;
            var h = metaData & 15;

            MetaData = 0;
            MetaData |= (uint)(u <<  0); // 5 bits
            MetaData |= (uint)(v <<  5); // 5 bits
            MetaData |= (uint)(l << 10); // 8 bits
            MetaData |= (uint)(h << 18); // 4 bits
        }

        public Vector3 Position;
        public uint MetaData;

    }
}