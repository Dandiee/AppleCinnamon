using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public int PipelineStepIndex { get; protected set; }
    }

    public abstract class PipelineBlock<TInput, TOutput> : PipelineBlock
    {
        public IPropagatorBlock<TInput, TOutput> TransformBlock { get; protected set; }

        public PipelineBlock<TOutput, TNewOutput> LinkTo<TNewOutput>(PipelineBlock<TOutput, TNewOutput> next)
        {
            TransformBlock.LinkTo(next.TransformBlock);
            next.PipelineStepIndex = PipelineStepIndex + 1;


            return next;
        }
    }

    public class TransformPipelineBlock<TInput, TOutput> : PipelineBlock<TInput, TOutput>
    {
        private readonly Func<TInput, TOutput> _func;



        public TransformPipelineBlock(Func<TInput, TOutput> func)
        {
            _func = func;
            TransformBlock = new TransformBlock<TInput, TOutput>(s => Process(s), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }

        private TOutput Process(TInput input)
        {
            if (input is Chunk c)
            {
                if (c.PipelineStep != PipelineStepIndex - 1)
                {
                    if (input is TOutput output)
                    {
                        return output;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                c.PipelineStep++;
                var result = _func(input);
                return result;
            }

            return _func(input);
        }
    }

    public class TransformManyPipelineBlock<TInput, TOutput> : PipelineBlock<TInput, TOutput>
    {
        private readonly Func<TInput, IEnumerable<TOutput>> _func;

        public TransformManyPipelineBlock(Func<TInput, IEnumerable<TOutput>> func)
        {
            _func = func;
            TransformBlock = new TransformManyBlock<TInput, TOutput>(s => Process(s), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }

        private IEnumerable<TOutput> Process(TInput input)
        {
            if (input is Chunk c)
            {
                c.PipelineStep++;
            }

            var result = _func(input);

            return result;
        }
    }

    public class ChunkPoolPipelineBlock : PipelineBlock<Chunk, Chunk>
    {
        private readonly int _expectedInputIndex;

        public ChunkPoolPipelineBlock(int expectedInputIndex)
        {
            _expectedInputIndex = expectedInputIndex;
            TransformBlock = new TransformManyBlock<Chunk, Chunk>(Pool, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }

        public IEnumerable<Chunk> Pool(Chunk chunk)
        {
            // a disposed and reloaded neighbor may re-proc an already processed chunk
            // in which case we dont want to demote the pipeline step
            if (chunk.PipelineStep == PipelineStepIndex - 1)
            {
                chunk.PipelineStep++;
            }

            foreach (var n in chunk.Neighbors)
            {
                if (!n.Neighbors.Any(s => s == null || s.PipelineStep < chunk.PipelineStep))
                {
                    //if (n.PipelineStep == chunk.PipelineStep)
                    {
                        yield return n;
                    }
                }
            }
        }
    }
}

