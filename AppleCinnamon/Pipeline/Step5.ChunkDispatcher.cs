using AppleCinnamon.ChunkBuilders;

namespace AppleCinnamon
{
    public sealed class ChunkDispatcher
    {
        private readonly Graphics _grfx;
        
        public ChunkDispatcher(Graphics grfx)
        {
            _grfx = grfx;
        }
        
        public Chunk Transform(Chunk chunk)
        {
            ChunkBuilder.BuildChunk(chunk, _grfx.Device);
            return chunk;
        }
    }
}
