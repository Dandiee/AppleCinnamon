using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;
using Microsoft.VisualBasic;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class Pool : IChunksTransformer
    {
        public PipelineBlock Owner { get; set; }

        public IEnumerable<Chunk> TransformMany(Chunk chunk)
        {
            // chunk reached a pool => one less chunk in transformation
            Interlocked.Decrement(ref ChunkManager.InProcessChunks);

            // massacre condition
            ChunkManager.WaitForDeletionEvent.WaitOne();

            foreach (var neighbor in chunk.Neighbors)
            {
                if (neighbor.PipelineStep == Owner.PipelineStepIndex)
                {
                    if (neighbor != null && !neighbor.Neighbors.Any(s => s == null || s.PipelineStep < chunk.PipelineStep))
                    {
                        // if a chunk is emitted from the pool its back in the transformation
                        Interlocked.Increment(ref ChunkManager.InProcessChunks);
                        yield return neighbor;
                    }
                }
            }
        }
    }
}
