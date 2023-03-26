using SharpDX.Direct3D11;
using System.Threading;

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
            // if (Game.Debug) Thread.Sleep(100);
            ChunkBuilder.BuildChunk(chunk, _device);
            return chunk;
        }
    }
}
