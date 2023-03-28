using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class NeighborAssigner : IChunksTransformer
    {
        public static readonly HashSet<Int2> EmittedChunks = new();

        public PipelineBlock Owner { get; set; }

        public IEnumerable<Chunk> TransformMany(Chunk chunk)
        {
            if (chunk.IsTimeToDie)
                return Enumerable.Empty<Chunk>();

            chunk.SetNeighbor(0, 0, chunk);
            var chunks = GetFinishedChunks(chunk).ToList();
            return chunks;
        }

        private IEnumerable<Chunk> GetFinishedChunks(Chunk chunk)
        {
            //Interlocked.Decrement(ref ChunkManager.InProcessChunks);
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    var absoluteNeighborIndex = new Int2(i + chunk.ChunkIndex.X, j + chunk.ChunkIndex.Y);

                    if (ChunkManager.Chunks.TryGetValue(absoluteNeighborIndex, out var neighborChunk))
                    {
                        chunk.SetNeighbor(i, j, neighborChunk);
                        neighborChunk.SetNeighbor(i * -1, j * -1, chunk);

                        if (EmittedChunks.Contains(neighborChunk.ChunkIndex))
                            continue;

                        if (neighborChunk.Neighbors.All(a => a != null && a.PipelineStep >= Owner.PipelineStepIndex))
                        {
                            if (EmittedChunks.Add(neighborChunk.ChunkIndex))
                            {
                                Interlocked.Increment(ref ChunkManager.InProcessChunks);
                                yield return neighborChunk;
                            }
                        }
                    }
                }
            }

            if (chunk.Neighbors.All(a => a != null && a.PipelineStep >= Owner.PipelineStepIndex))
            {
                if (EmittedChunks.Add(chunk.ChunkIndex))
                {
                    Interlocked.Increment(ref ChunkManager.InProcessChunks);
                    yield return chunk;
                }
            }
        }
    }
}
