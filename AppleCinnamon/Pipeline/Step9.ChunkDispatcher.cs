using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkDispatcher : PipelineBlock<Chunk, Chunk>
    {
        private readonly ChunkBuilder _chunkBuilder;
        
        public ChunkDispatcher(Device device)
        {
            _chunkBuilder = new ChunkBuilder(device);
        }

        public override Chunk Process(Chunk chunk)
        {
            _chunkBuilder.BuildChunk(chunk);
            return chunk;
        }

    }
}
