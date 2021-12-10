using System.Collections.Concurrent;
using System.Diagnostics;
using AppleCinnamon.Helper;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkProvider
    {
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly VoxelLoader _voxelLoader;

        public ChunkProvider(int seed)
        {
            _voxelLoader = new VoxelLoader(seed);
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
        }

        public DataflowContext<Chunk> GetChunk(DataflowContext<Int2> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = _voxelLoader.GetVoxels(context.Payload);
            //var chunk = new Chunk(context.Payload, voxels);
            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(ChunkProvider));
        }
    }
}
