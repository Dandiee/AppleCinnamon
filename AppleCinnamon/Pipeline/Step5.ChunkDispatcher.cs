using SharpDX.Direct3D11;

namespace AppleCinnamon
{
    public sealed class ChunkDispatcher
    {
        private readonly Device _device;
        
        public ChunkDispatcher(Device device)
        {
            _device = device;
        }

        public ChunkDispatcher()
        {
        }

        public Chunk Transform(Chunk chunk)
        {
            // if (Game.Debug) Thread.Sleep(100);
            ChunkBuilder.BuildChunk(chunk, _device ?? Game.Grfx.Device);
            return chunk;
        }
    }
}
