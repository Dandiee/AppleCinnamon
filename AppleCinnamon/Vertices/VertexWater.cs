﻿using SharpDX;
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
}
