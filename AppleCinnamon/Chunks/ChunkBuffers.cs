using AppleCinnamon.Vertices;

namespace AppleCinnamon
{
    public sealed class ChunkBuffers
    {
        public readonly BufferDefinition<VertexSolidBlock> BufferSolid;
        public readonly BufferDefinition<VertexWater> BufferWater;
        public readonly BufferDefinition<VertexSprite> BufferSprite;

        public ChunkBuffers(BufferDefinition<VertexSolidBlock> bufferSolid, BufferDefinition<VertexWater> bufferWater, BufferDefinition<VertexSprite> bufferSprite)
        {
            BufferSolid = bufferSolid;
            BufferWater = bufferWater;
            BufferSprite = bufferSprite;
        }
    }
}