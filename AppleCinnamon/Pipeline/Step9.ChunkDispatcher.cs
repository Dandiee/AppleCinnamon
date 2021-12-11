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
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly HashSet<Int2> _dispatchedChunks;
        
        public ChunkDispatcher(Device device)
        {
            _chunkBuilder = new ChunkBuilder(device);
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
            _dispatchedChunks = new HashSet<Int2>();
        }


        public override Chunk Process(Chunk chunk)
        {
            _chunkBuilder.BuildChunk(chunk);
            chunk.State = ChunkState.Displayed;
            return chunk;
        }

    }
}
