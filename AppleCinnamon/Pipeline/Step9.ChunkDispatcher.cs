using AppleCinnamon.Pipeline.Context;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkDispatcher : PipelineBlock<Chunk, Chunk>
    {
        private readonly Device _device;
        
        public ChunkDispatcher(Device device)
        {
            _device = device;
        }

        public override Chunk Process(Chunk chunk)
        {
            ChunkBuilder.BuildChunk(chunk, _device);
            return chunk;
        }

    }
}
