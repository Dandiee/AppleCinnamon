using System.Collections.Concurrent;
using System.Diagnostics;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public interface IChunkProvider
    {
        DataflowContext<Chunk> GetChunk(DataflowContext<Int2> context);
    }

    public sealed class ChunkProvider : IChunkProvider
    {
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly IVoxelLoader _voxelLoader;

        public ChunkProvider(int seed)
        {
            _voxelLoader = new VoxelLoader(seed);
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
        }

        public DataflowContext<Chunk> GetChunk(DataflowContext<Int2> context)
        {
            var sw = Stopwatch.StartNew();
            var voxels = _voxelLoader.GetVoxels(context.Payload);
            var chunk = new Chunk(context.Payload, voxels);
            sw.Stop();

            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(ChunkProvider));
        }
    }
}
