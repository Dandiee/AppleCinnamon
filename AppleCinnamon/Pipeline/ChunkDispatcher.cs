using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Helper;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkDispatcher
    {
        private readonly ChunkBuilder _chunkBuilder;
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly HashSet<Int2> _dispatchedChunks;
        public ChunkDispatcher()
        {
            _chunkBuilder = new ChunkBuilder();
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
            _dispatchedChunks = new HashSet<Int2>();
        }


        public DataflowContext<Chunk> Dispatch(DataflowContext<Chunk> context)
        {
            var chunk = context.Payload;
            var sw = Stopwatch.StartNew();
            _chunkBuilder.BuildChunk(context.Device, context.Payload);
            context.Payload.State = ChunkState.Displayed;
            sw.Stop();
            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(ChunkDispatcher));
        }

    }
}
