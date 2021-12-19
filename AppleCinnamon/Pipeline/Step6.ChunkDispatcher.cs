using SharpDX.Direct3D11;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkDispatcher : IChunkTransformer
    {
        private readonly Device _device;
        
        public ChunkDispatcher(Device device)
        {
            _device = device;
        }

        public Chunk Transform(Chunk chunk)
        {
            ChunkBuilder.BuildChunk(chunk, _device);
            return chunk;
        }
    }
}
