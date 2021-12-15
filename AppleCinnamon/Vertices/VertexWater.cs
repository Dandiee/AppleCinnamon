using AppleCinnamon.Helper;
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
            new("VISIBILITY", 0, Format.R32_UInt, 20, 0), //3+2
        };


        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexWater(Vector3 position, Vector2 textureCoordinate, byte sunlight, byte customLight)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
            Asd = 0; //0.3f + (((1 / 16f) * lightness) - ambientOcclusion * .1f) * 0.8f;

            var l = sunlight;
            var c = customLight;

            Asd |= (uint)(l << 0); // 4 bits
            Asd |= (uint)(c << 4); // 4 bits
        }

        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public uint Asd;

    }


    public struct VertexSkyBox : IVertex
    {
        private const int _size = 36;

        private static readonly InputElement[] _inputElements =
        {
            new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            new("NORMAL", 0, Format.R32G32B32_Float, 16, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 28, 0)
        };


        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexSkyBox(Vector4 position, Vector3 normal, Vector2 texCoord)
        {
            Position = position;
            Normal = normal;
            TexCoord = texCoord;
        }

        public VertexSkyBox(Vector3 position, Vector3 normal, Vector2 texCoord)
        {
            Position = position.ToVector4(1);
            Normal = normal;
            TexCoord = texCoord;
        }

        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

    }
}
