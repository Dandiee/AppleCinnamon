using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class NeighborAssigner : PipelineBlock<Chunk, IEnumerable<Chunk>>
    {
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks = new();

        public override IEnumerable<Chunk> Process(Chunk chunk)
        {
            _chunks.TryAdd(chunk.ChunkIndex, chunk);
            chunk.SetNeighbor(0, 0, chunk);
            var chunks = GetFinishedChunks(chunk).ToList();
            return chunks;
        }

        private IEnumerable<Chunk> GetFinishedChunks(Chunk chunk)
        {
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    var absoluteNeighborIndex = new Int2(i + chunk.ChunkIndex.X, j + chunk.ChunkIndex.Y);

                    if (_chunks.TryGetValue(absoluteNeighborIndex, out var neighborChunk))
                    {
                        chunk.SetNeighbor(i, j, neighborChunk);
                        neighborChunk.SetNeighbor(i * -1, j * -1, chunk);

                        if (neighborChunk.Neighbors.All(a => a != null))
                        {
                            yield return neighborChunk;
                        }
                    }
                }
            }

            if (chunk.Neighbors.All(a => a != null))
            {
                yield return chunk;
            }
        }

    }
}
