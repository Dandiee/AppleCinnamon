using System;
using AppleCinnamon.Vertices;

namespace AppleCinnamon
{
    public sealed class ChunkBuffers : IDisposable
    {
        public BufferDefinition<VertexSolidBlock> BufferSolid;
        public BufferDefinition<VertexWater> BufferWater;
        public BufferDefinition<VertexSprite> BufferSprite;

        //public ChunkBuffers(BufferDefinition<VertexSolidBlock> bufferSolid, BufferDefinition<VertexWater> bufferWater, BufferDefinition<VertexSprite> bufferSprite)
        //{
        //    BufferSolid = bufferSolid;
        //    BufferWater = bufferWater;
        //    BufferSprite = bufferSprite;
        //}

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