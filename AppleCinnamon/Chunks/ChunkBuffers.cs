using System;
using AppleCinnamon.Vertices;
using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public sealed class ChunkBuffers
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

        public void Dispose(Device device)
        {
            BufferSolid?.Dispose(device);
            BufferWater?.Dispose(device);
            BufferSprite?.Dispose(device);

            BufferWater = null;
            BufferSolid = null;
            BufferSprite = null;
        }
    }
}