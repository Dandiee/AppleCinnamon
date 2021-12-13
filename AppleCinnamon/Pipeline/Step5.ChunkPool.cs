using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkPool : PipelineBlock<Chunk, IEnumerable<Chunk>>
    {
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly HashSet<Int2> _dispatchedChunks;

        public ChunkPool()
        {
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
            _dispatchedChunks = new HashSet<Int2>();
        }


        public override IEnumerable<Chunk> Process(Chunk chunk)
        {
            if (!_chunks.TryAdd(chunk.ChunkIndex, chunk))
            {
                throw new Exception("The chunk is already in the pool");
            }

            foreach (var n in chunk.Neighbors)
            {
                if (n.Neighbors.All(a => a != null && _chunks.ContainsKey(a.ChunkIndex)))
                {
                    if (_dispatchedChunks.Contains(n.ChunkIndex))
                    {
                        throw new Exception("asdasd");
                    }
                    _dispatchedChunks.Add(n.ChunkIndex);
                    yield return n;
                }
            }
        }

    }
}
