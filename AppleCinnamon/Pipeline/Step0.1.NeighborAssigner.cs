﻿using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Pipeline
{
    public sealed class NeighborAssigner : ChunkPoolPipelineBlock
    {
        private static readonly RawColor4 Color = new(1, 1, 0, 1);
        public override RawColor4 DebugColor => Color;

        public IEnumerable<Chunk> ChunkList => Chunks.Values;

        public override IEnumerable<Chunk> Process(Chunk chunk)
        {
            Chunks.TryAdd(chunk.ChunkIndex, chunk);
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

                    if (Chunks.TryGetValue(absoluteNeighborIndex, out var neighborChunk))
                    {
                        chunk.SetNeighbor(i, j, neighborChunk);
                        neighborChunk.SetNeighbor(i * -1, j * -1, chunk);

                        if (!DispatchedChunks.Contains(neighborChunk.ChunkIndex))
                        {
                            if (neighborChunk.Neighbors.All(a => a != null))
                            {
                                DispatchedChunks.Add(neighborChunk.ChunkIndex);
                                yield return neighborChunk;
                            }
                        }
                    }
                }
            }

            if (!DispatchedChunks.Contains(chunk.ChunkIndex))
            {
                if (chunk.Neighbors.All(a => a != null))
                {
                    DispatchedChunks.Add(chunk.ChunkIndex);
                    yield return chunk;
                }
            }
        }

    }
}
