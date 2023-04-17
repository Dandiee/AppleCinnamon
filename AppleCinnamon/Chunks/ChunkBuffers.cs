﻿using System;
using AppleCinnamon.Grfx;
using AppleCinnamon.Vertices;

namespace AppleCinnamon.Chunks
{
    public sealed class ChunkBuffers : IDisposable
    {
        public BufferDefinition<VertexSolidBlock> BufferSolid;
        public BufferDefinition<VertexWater> BufferWater;
        public BufferDefinition<VertexSprite> BufferSprite;

        public void Dispose()
        {
            BufferSolid?.Dispose();
            BufferWater?.Dispose();
            BufferSprite?.Dispose();

            BufferWater = null;
            BufferSolid = null;
            BufferSprite = null;
        }
    }
}