using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public static readonly List<PipelineBlock> Blocks = new();
        public readonly int PipelineStepIndex;
        public static readonly ConcurrentDictionary<Type, long> ElapsedTimes = new ();

        protected PipelineBlock()
        {
            PipelineStepIndex = Blocks.Count;
            Blocks.Add(this);
        }
    }

    public abstract class PipelineBlock<TInput, TOutput> : PipelineBlock
    {
        private readonly Type _type;

        protected PipelineBlock()
        {
            _type = GetType();
        }

        public virtual TOutput Execute(TInput input)
        {
            var sw = Stopwatch.StartNew();
            var result = Process(input);
            sw.Stop();


            ElapsedTimes.TryGetValue(_type, out var ms);
            ElapsedTimes[_type] = ms + sw.ElapsedMilliseconds;

            return result;

        }

        public abstract TOutput Process(TInput input);
    }


    public abstract class TransformChunkPipelineBlock<TOutput> : PipelineBlock<Chunk, TOutput>
    {
        public override TOutput Execute(Chunk input)
        {
            input.PipelineStep = PipelineStepIndex;
            var result = base.Execute(input);
            return result;
        }
    }

    public abstract class ChunkPoolPipelineBlock : PipelineBlock<Chunk, IEnumerable<Chunk>>
    {
        public override IEnumerable<Chunk> Execute(Chunk input)
        {
            input.PipelineStep = PipelineStepIndex;
            var result = base.Execute(input);
            return result;
        }
    }
}
