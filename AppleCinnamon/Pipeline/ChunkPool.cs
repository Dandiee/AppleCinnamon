﻿using System;
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

        public IEnumerable<DataflowContext<Chunk>> Process(DataflowContext<Chunk> context)
        {
            var chunk = context.Payload;

            if (!_chunks.TryAdd(chunk.ChunkIndex, chunk))
            {
                yield break;
            }

            var readyChunks = SetNeighbours(chunk);

            foreach (var readyChunk in readyChunks)
            {
                if (_chunks.TryRemove(readyChunk.ChunkIndex, out _))
                {
                    yield return new DataflowContext<Chunk>(context, readyChunk);
                }
                else
                {
                    throw new Exception("asdasdasd");
                }
            }
        }


        private IEnumerable<Chunk> SetNeighbours(Chunk chunk)
        {
            for (var i = -1; i <= 1; i++)
            {
                for (var k = -1; k <= 1; k++)
                {
                    var neighbourChunkIndex = new Int2(chunk.ChunkIndex.X + i, chunk.ChunkIndex.Y + k);
                    if (_chunks.TryGetValue(neighbourChunkIndex, out var neighbourChunk))
                    {
                        var index = new Int2(i, k);
                        var inverseIndex = -index;
                        
                        //neighbourChunk.Neighbours[inverseIndex] = chunk;
                        neighbourChunk.Neighbours2[Help.GetChunkFlatIndex(inverseIndex)] = chunk;

                        //chunk.Neighbours[index] = neighbourChunk;
                        chunk.Neighbours2[Help.GetChunkFlatIndex(index)] = neighbourChunk;

                        //if (neighbourChunk.Neighbours.Count == 9 &&
                        if (neighbourChunk.Neighbours2.Count(s =>s != null) == 9 &&
                            neighbourChunk.State < ChunkState.DispatchedToDisplay)
                        {
                            neighbourChunk.State = ChunkState.DispatchedToDisplay;
                            yield return neighbourChunk;
                        }

                        //if (chunk.Neighbours.Count == 9 &&
                        if (chunk.Neighbours2.Count(s => s != null) == 9 &&
                            chunk.State < ChunkState.DispatchedToDisplay && chunk != neighbourChunk)
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
