using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.Helper;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkPool
    {
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;
        private readonly HashSet<Int2> _dispatchedChunks;

        public ChunkPool()
        {
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

            var finished = SetNeighbors(chunk).ToList();
            return finished.Select(s => new DataflowContext<Chunk>(context, s));
        }

        private IEnumerable<Chunk> SetNeighbors(Chunk chunk)
        {
            chunk.neighbors2[Help.GetChunkFlatIndex(0, 0)] = chunk;

            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    var absoluteNeighborIndex = new Int2(i + chunk.ChunkIndex.X, j + chunk.ChunkIndex.Y);

                    if (_chunks.TryGetValue(absoluteNeighborIndex, out var neighborChunk))
                    {
                        var relativeNeighborFlatIndex = Help.GetChunkFlatIndex(i, j);

                        chunk.neighbors2[relativeNeighborFlatIndex] = neighborChunk;
                        neighborChunk.neighbors2[Help.GetChunkFlatIndex(i * -1, j * -1)] = chunk;

                        if (_dispatchedChunks.Contains(neighborChunk.ChunkIndex))
                        {
                            throw new Exception("That should be dispatched by now");
                        }

                        if (neighborChunk.neighbors2.All(a => a != null))
                        {
                            if (!_chunks.Remove(neighborChunk.ChunkIndex, out _))
                            {
                                throw new Exception("This souldh be remivable");
                            }

                            if (neighborChunk.neighbors2.Any(s => s.neighbors2 == null))
                            {
                                throw new Exception("This souldh be remivable");
                            }

                            yield return neighborChunk;
                        }
                        
                    }
                }
            }

            if (_dispatchedChunks.Contains(chunk.ChunkIndex))
            {
                throw new Exception("That should be dispatched by now");
            }

            if (chunk.neighbors2.All(a => a != null))
            {
                if (!_chunks.Remove(chunk.ChunkIndex, out _))
                {
                    throw new Exception("This souldh be remivable");
                }

                if (chunk.neighbors2.Any(s => s.neighbors2 == null))
                {
                    throw new Exception("This souldh be remivable");
                }

                yield return chunk;
            }

        }


        private IEnumerable<Chunk> Setneighbors(Chunk chunk)
        {
            for (var i = -1; i <= 1; i++)
            {
                for (var k = -1; k <= 1; k++)
                {
                    var neighborChunkIndex = new Int2(chunk.ChunkIndex.X + i, chunk.ChunkIndex.Y + k);
                    if (_chunks.TryGetValue(neighborChunkIndex, out var neighborChunk))
                    {
                        var index = new Int2(i, k);
                        var inverseIndex = -index;
                        
                        //neighborChunk.neighbors[inverseIndex] = chunk;
                        neighborChunk.neighbors2[Help.GetChunkFlatIndex(inverseIndex)] = chunk;

                        //chunk.neighbors[index] = neighborChunk;
                        chunk.neighbors2[Help.GetChunkFlatIndex(index)] = neighborChunk;

                        //if (neighborChunk.neighbors.Count == 9 &&
                        if (neighborChunk.neighbors2.Count(s =>s != null) == 9 &&
                            neighborChunk.State < ChunkState.DispatchedToDisplay)
                        {
                            neighborChunk.State = ChunkState.DispatchedToDisplay;
                            yield return neighborChunk;
                        }

                        //if (chunk.neighbors.Count == 9 &&
                        if (chunk.neighbors2.Count(s => s != null) == 9 &&
                            chunk.State < ChunkState.DispatchedToDisplay && chunk != neighborChunk)
                        {
                            chunk.State = ChunkState.DispatchedToDisplay;
                            yield return chunk;
                        }
                    }
                }
            }
        }
    }
}
