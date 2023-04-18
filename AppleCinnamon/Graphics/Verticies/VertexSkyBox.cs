using AppleCinnamon.Extensions;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Graphics.Verticies
{
    public struct VertexSkyBox : IVertex
    {
        public const int SIZE = 36;

        private static readonly InputElement[] _inputElements =
        {
            new("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            new("NORMAL", 0, Format.R32G32B32_Float, 16, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 28, 0)
        };

        public int Size => SIZE;
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