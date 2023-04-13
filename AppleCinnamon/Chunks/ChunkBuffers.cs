using System;
using AppleCinnamon.Vertices;
using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public sealed class ChunkBuffers
    {
        public Chunk Owner { get; }

        public BufferDefinition<VertexSolidBlock> BufferSolid;
        public BufferDefinition<VertexWater> BufferWater;
        public BufferDefinition<VertexSprite> BufferSprite;

        //public ChunkBuffers(BufferDefinition<VertexSolidBlock> bufferSolid, BufferDefinition<VertexWater> bufferWater, BufferDefinition<VertexSprite> bufferSprite)
        //{
        //    BufferSolid = bufferSolid;
        //    BufferWater = bufferWater;
        //    BufferSprite = bufferSprite;
        //}

        public ChunkBuffers(Chunk chunk)
        {
            Owner = chunk;
        }

        public void Dispose(Device device)
        {
            BufferSolid?.Dispose(device);
            BufferWater?.Dispose(device);
            BufferSprite?.Dispose(device);

            BufferWater = null;
            BufferSolid = null;
            BufferSprite = null;
        }

        ~ChunkBuffers()
        {
            //ChunkManager.Graveyard.Add(Owner);
            //ChunkManager.DeadChunks.TryRemove(Owner.ChunkIndex, out _);
        }
    }
}