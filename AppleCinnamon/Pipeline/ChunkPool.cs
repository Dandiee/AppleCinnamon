using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class ChunkPool
    {
        private readonly ConcurrentDictionary<Int2, Chunk> _chunks;

        public ChunkPool()
        {
            _chunks = new ConcurrentDictionary<Int2, Chunk>();
        }

        public static HashSet<Int2> checkehc = new();
        public static HashSet<Int2> removedcd = new();
        public IEnumerable<DataflowContext<Chunk>> Process(DataflowContext<Chunk> context)
        {
            var chunk = context.Payload;

            if (!_chunks.TryAdd(chunk.ChunkIndex, chunk))
            {
                
                yield break;
            }
            else
            {
                checkehc.Add(chunk.ChunkIndex);
            }

            var readyChunks = Setneighbors(chunk).ToList();

            foreach (var readyChunk in readyChunks)
            {
                if (_chunks.TryRemove(readyChunk.ChunkIndex, out _))
                {
                    removedcd.Add(readyChunk.ChunkIndex);
                    yield return new DataflowContext<Chunk>(context, readyChunk);
                }
                else
                {
                    //throw new Exception("asdasdasd");
                }
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
