using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public static readonly IDictionary<Type, long> ElapsedTimes = new Dictionary<Type, long>();
    }

    public abstract class PipelineBlock<TInput, TOutput> : PipelineBlock
    {
        private readonly Type _type;

        protected PipelineBlock()
        {
            _type = GetType();
        }

        public TOutput Execute(TInput input)
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
}
