using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Helper;

namespace AppleCinnamon.Pipeline
{
    public sealed class BuildPool
    {
        private readonly ChunkBuilder _chunkBuilder;
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly HashSet<Int2> _dispatchedChunks;
        public BuildPool()
        {
            _chunkBuilder = new ChunkBuilder();
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
            _dispatchedChunks = new HashSet<Int2>();
        }


        public IEnumerable<DataflowContext<Chunk>> Process(DataflowContext<Chunk> context)
        {
            var chunk = context.Payload;
            if (!_chunks.TryAdd(chunk.ChunkIndex, chunk))
            {
                throw new Exception("The chunk is already in the pool");
            }

            //if (chunk.neighbors2.All(a => _chunks.ContainsKey(a.ChunkIndex)))
            //{
            //    yield return new DataflowContext<Chunk>(context, context.Payload, 0, nameof(BuildPool));
            //}

            foreach (var n in _chunks.Values)
            {
                if (n.neighbors2.All(a => a != null && _chunks.ContainsKey(a.ChunkIndex)))
                {
                    if (!_dispatchedChunks.Contains(n.ChunkIndex))
                    {
                        _dispatchedChunks.Add(n.ChunkIndex);
                        yield return new DataflowContext<Chunk>(context, n, 0, nameof(BuildPool));
                    }
                }
            }
            //
            //foreach (var n in chunk.neighbors2)
            //{
            //    if (n.neighbors2.All(a => a != null && _chunks.ContainsKey(a.ChunkIndex)))
            //    {
            //        yield return new DataflowContext<Chunk>(context, n, 0, nameof(BuildPool));
            //    }
            //}

            /*
            if (chunk.neighbors2.All(a => _chunks.ContainsKey(a.ChunkIndex)))
            {
                var sw = Stopwatch.StartNew();
                context.Payload.State = ChunkState.Displayed;
                sw.Stop();
                yield return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(BuildPool));
            }

            foreach (var n in chunk.neighbors2)
            {
                if (n.neighbors2.All(a => a != null && _chunks.ContainsKey(a.ChunkIndex)))
                {
                    var sw = Stopwatch.StartNew();
                    context.Payload.State = ChunkState.Displayed;
                    sw.Stop();
                    //context.Debug.Add(nameof(ChunkDispatcher), sw.ElapsedMilliseconds);
                    yield return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds, nameof(BuildPool));
                }
            }*/
        }
    }
}
