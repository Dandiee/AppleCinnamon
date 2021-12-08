using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public struct VertexWater
    {
        public const int Size = 24;

        public static readonly InputElement[] InputElements =
        {
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new("TEXCOORD", 0, Format.R32G32_Float, 12, 0), // 3
            new("COLOR", 0, Format.R32_Float, 20, 0), //3+2
        };

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

    public struct VertexBox
    {
        public const int Size = 36;

        public static readonly InputElement[] InputElements =
        {
            new("POSITION", 
                0, Format.R32G32B32_Float, 0, 0), //0
            new("POSITION", 
                1, Format.R32G32B32_Float, 12, 0), //0
            new("COLOR", 
                0, Format.R32G32B32_Float, 24, 0), //3+2
        };

        public VertexBox(Vector3 minimum, Vector3 maximum, Color3 color)
        {
            Minimum = minimum;
            Maximum = maximum;
            Color = color;
        }

        public Vector3 Minimum;
        public Vector3 Maximum;
        public Color3 Color;

    }
}
