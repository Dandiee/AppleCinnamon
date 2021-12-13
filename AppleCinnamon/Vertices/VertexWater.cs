using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public struct VertexWater : IVertex
    {
        private const int _size = 24;

        private static readonly InputElement[] _inputElements =
        {
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new("TEXCOORD", 0, Format.R32G32_Float, 12, 0), // 3
            new("COLOR", 0, Format.R32_Float, 20, 0), //3+2
        };


        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexWater(Vector3 position, Vector2 textureCoordinate, int ambientOcclusion, float lightness)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
            AmbientOcclusion = 0.3f + (((1 / 16f) * lightness) - ambientOcclusion * .1f) * 0.8f;
        }

        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public float AmbientOcclusion;

    }

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
            Position = position; // 32
            Color = 0;
            Color |= (uint)(u << 0); // 4 bits
            Color |= (uint)(v << 4); // 4 bits
            Color |= (uint)(baseLight << 8);
            Color |= (uint)(hueIndex << 12);
        }

        public Vector3 Position;
        public uint Color;

    }

    public struct VertexBox : IVertex
    {
        private const int _size = 36;

        private static readonly InputElement[] _inputElements =
        {
            new("POSITION", 
                0, Format.R32G32B32_Float, 0, 0), //0
            new("POSITION", 
                1, Format.R32G32B32_Float, 12, 0), //0
            new("COLOR", 
                0, Format.R32G32B32_Float, 24, 0), //3+2
        };


        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexBox(Vector3 minimum, Vector3 maximum, Color3 color)
        {
            Minimum = minimum;
            Maximum = maximum;
            Color = color;
        }

        public VertexBox(ref BoundingBox bb, Color3 color)
        {
            Minimum = bb.Minimum;
            Maximum = bb.Maximum;
            Color = color;
        }

        public VertexBox(BoundingBox bb, Color3 color)
        {
            Minimum = bb.Minimum;
            Maximum = bb.Maximum;
            Color = color;
        }

        public Vector3 Minimum;
        public Vector3 Maximum;
        public Color3 Color;

    }
}
