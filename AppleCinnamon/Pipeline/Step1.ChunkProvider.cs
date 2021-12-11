using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkProvider : PipelineBlock<Int2, Chunk>
    {
        private readonly VoxelLoader _voxelLoader;

        public ChunkProvider(int seed)
        {
            _voxelLoader = new VoxelLoader(seed);
        }

        public override Chunk Process(Int2 input) => _voxelLoader.GetVoxels(input);
    }
}
