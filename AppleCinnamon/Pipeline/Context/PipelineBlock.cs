using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public static readonly ConcurrentDictionary<Type, long> ElapsedTimes = new ();
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


    public abstract class TransformChunkPipelineBlock : PipelineBlock<Chunk, Chunk>
    {
        public static readonly List<TransformChunkPipelineBlock> Blocks = new();

        public readonly int PipelineStepIndex;

        protected TransformChunkPipelineBlock()
        {
            PipelineStepIndex = Blocks.Count;
            Blocks.Add(this);
        }

        public override Chunk Execute(Chunk input)
        {
            input.PipelineStep = PipelineStepIndex;
            var result = base.Execute(input);
            return result;
        }
    }
}
