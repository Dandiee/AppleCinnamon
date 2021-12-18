using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppleCinnamon.Helper;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public static readonly List<PipelineBlock> Blocks = new();
        public readonly int PipelineStepIndex;
        public static readonly ConcurrentDictionary<Type, long> ElapsedTimes = new ();
        public abstract RawColor4 DebugColor { get;  }

        protected PipelineBlock()
        {
            PipelineStepIndex = Blocks.Count;
            Blocks.Add(this);
        }
    }

    public abstract class PipelineBlock<TInput, TOutput> : PipelineBlock
    {
        private static RawColor4 DefaultColor = new(1, 0, 0, 1);
        public override RawColor4 DebugColor => DefaultColor;

        private readonly Type _type;

        protected PipelineBlock()
        {
            _type = GetType();
        }

        public virtual TOutput Execute(TInput input)
        {
            if (input is Chunk c && c.ShouldBeDeadByNow)
            {

            }

            var sw = Stopwatch.StartNew();
            var result = Process(input);
            sw.Stop();


            ElapsedTimes.TryGetValue(_type, out var ms);
            ElapsedTimes[_type] = ms + sw.ElapsedMilliseconds;

            return result;

        }

        public abstract TOutput Process(TInput input);
    }


    public abstract class TransformChunkPipelineBlock : PipelineBlock<Chunk, Chunk>
    {
        public override Chunk Execute(Chunk input)
        {
            if (input.PipelineStep > PipelineStepIndex)
            {
                return input;
            }

            input.PipelineStep = PipelineStepIndex;
            var result = base.Execute(input);
            return result;
        }
    }

    public class ChunkPoolPipelineBlock : PipelineBlock<Chunk, IEnumerable<Chunk>>
    {
        protected readonly ConcurrentDictionary<Int2, Chunk> Chunks = new();
        protected readonly HashSet<Int2> DispatchedChunks = new();
        private static readonly List<ChunkPoolPipelineBlock> Instances = new();

        public ChunkPoolPipelineBlock()
        {
            Instances.Add(this);
        }


        public static void RemoveChunk(Int2 chunkIndex)
        {
            foreach (var instance in Instances)
            {
                instance.Chunks.TryRemove(chunkIndex, out _);
                instance.DispatchedChunks.Remove(chunkIndex);
            }
        }

        public override IEnumerable<Chunk> Execute(Chunk input)
        {
            input.PipelineStep = PipelineStepIndex;
            var result = base.Execute(input);
            return result;
        }

        public override IEnumerable<Chunk> Process(Chunk chunk)
        {
            if (DispatchedChunks.Contains(chunk.ChunkIndex))
            {
            }

            if (!Chunks.TryAdd(chunk.ChunkIndex, chunk))
            {
                //throw new Exception("The chunk is already in the pool");
            }

            foreach (var n in chunk.Neighbors)
            {
                if (n.Neighbors.All(a => a != null && Chunks.ContainsKey(a.ChunkIndex)))
                {
                    if (!DispatchedChunks.Contains(n.ChunkIndex))
                    {
                        DispatchedChunks.Add(n.ChunkIndex);
                        yield return n;
                    }
                }
            }
        }
    }

    
}
