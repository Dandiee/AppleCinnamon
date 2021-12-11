using System;
using System.Collections.Generic;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkProvider : PipelineBlock<Int2, Chunk>
    {
        private readonly VoxelLoader _voxelLoader;
        private readonly HashSet<Int2> _requestedIndicies = new();

        public ChunkProvider(int seed)
        {
            _voxelLoader = new VoxelLoader(seed);
        }

        public override Chunk Process(Int2 input)
        {
            if (_requestedIndicies.Contains(input))
            {
                throw new Exception("bad juju");
            }

            _requestedIndicies.Add(input);

            return _voxelLoader.GetVoxels(input);
        }
    }
}
