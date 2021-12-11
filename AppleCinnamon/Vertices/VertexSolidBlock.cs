﻿using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace AppleCinnamon.Vertices
{
    public interface IVertex
    {
        int Size { get; }
        InputElement[] InputElements { get; }
    }

    public struct VertexSolidBlock : IVertex
    {
        private const int _size = sizeof(int) * 4;
        private static readonly InputElement[] _inputElements = 
        {
            new("POSITION", 0, Format.R32G32B32_Float, 0, 0), //0
            new("VISIBILITY", 0, Format.R32_UInt, 12, 0) //3+2
        };

        public int Size => _size;
        public InputElement[] InputElements => _inputElements;

        public VertexSolidBlock(Vector3 position, int u, int v, byte baseLight, byte totalneighborLights, int numberOfAmbientneighbors, byte hueIndex)
        {
            var lightCalc = baseLight + totalneighborLights;

            Position = position; // 32
            Color = 0;
            Color |= (uint)(u << 0); // 4 bits
            Color |= (uint)(v << 4); // 4 bits
            Color |= (uint)(lightCalc << 8);
            Color |= (uint)(numberOfAmbientneighbors << 14);
            Color |= (uint)(hueIndex << 18);
        }

        public Vector3 Position;

        // 87654321|87654321|87654321|87654321
        // ????????|????????|??aallll|vvvvuuuu
        public uint Color;
    }
}
