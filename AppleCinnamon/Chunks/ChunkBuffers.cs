using System;
using AppleCinnamon.Vertices;
using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public sealed class ChunkBuffers : IDisposable
    {
        public Chunk Owner { get; }

        public BufferDefinition<VertexSolidBlock> BufferSolid;
        public BufferDefinition<VertexWater> BufferWater;
        public BufferDefinition<VertexSprite> BufferSprite;

        public ChunkBuffers(Chunk chunk)
        {
            Owner = chunk;
        }

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