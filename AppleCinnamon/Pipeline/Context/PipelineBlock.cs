using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public PipelineBlock Head { get; protected set; }
        public int PipelineStepIndex { get; protected set; }
        public readonly List<IMonitoredBlock> MonitoredBlocks = new();
    }

    public abstract class PipelineBlock<TInput, TOutput> : PipelineBlock
    {
        public IPropagatorBlock<TInput, TOutput> TransformBlock { get; protected set; }

        public PipelineBlock<TOutput, TNewOutput> LinkTo<TNewOutput>(PipelineBlock<TOutput, TNewOutput> next)
        {
            TransformBlock.LinkTo(next.TransformBlock);
            next.PipelineStepIndex = PipelineStepIndex + 1;
            next.Head = Head ?? next.Head ?? this;

            if (this is IMonitoredBlock monitoredBlock)
            {
                next.Head.MonitoredBlocks.Add(monitoredBlock);
            }

            return next;
        }

        public void LinkTo(Action<TOutput> action)
        {
            var actionBlock = new ActionBlock<TOutput>(action);
            TransformBlock.LinkTo(actionBlock);
        }
    }

    public interface IMonitoredBlock
    {
        string Name { get; }
        long ElapsedTime { get; }
    }

    public class TransformPipelineBlock<TInput, TOutput> : PipelineBlock<TInput, TOutput>, IMonitoredBlock
    {
        public string Name { get; }
        private readonly Func<TInput, TOutput> _func;
        private readonly Stopwatch _stopwatch;
        public long ElapsedTime => _stopwatch.ElapsedMilliseconds;

        public TransformPipelineBlock(Func<TInput, TOutput> func, string name, ExecutionDataflowBlockOptions options)
        {
            Name = name;
            _func = func;
            TransformBlock = new TransformBlock<TInput, TOutput>(s => Process(s), options);
            _stopwatch = new Stopwatch();
        }

        protected virtual TOutput Process(TInput input)
        {
            _stopwatch.Start();
            var result = _func(input);
            _stopwatch.Stop();
            return result;
        }
    }

    public class ChunkTransformBlock : TransformPipelineBlock<Chunk, Chunk>
    {
        public ChunkTransformBlock(Func<Chunk, Chunk> func, string name, ExecutionDataflowBlockOptions options)
            : base(func, name, options) { }
        public ChunkTransformBlock(IChunkTransformer transformer, ExecutionDataflowBlockOptions options) 
            : this(transformer.Transform, transformer.GetType().Name, options) { }
        
        protected override Chunk Process(Chunk chunk)
        {
            if (chunk.PipelineStep != PipelineStepIndex - 1)
            {
                return chunk;
            }

            chunk.PipelineStep++;
            
            return base.Process(chunk);
        }
    }

    public class TransformManyPipelineBlock : PipelineBlock<Chunk, Chunk>
    {
        protected Func<Chunk, IEnumerable<Chunk>> Func;

        protected TransformManyPipelineBlock() { }
        public TransformManyPipelineBlock(Func<Chunk, IEnumerable<Chunk>> func, ExecutionDataflowBlockOptions options)
        {
            Func = func;
            TransformBlock = new TransformManyBlock<Chunk, Chunk>(Process, options);
        }

        protected IEnumerable<Chunk> Process(Chunk chunk)
        {
            // a disposed and reloaded neighbor may re-proc an already processed chunk
            // in which case we dont want to demote the pipeline step
            if (chunk.PipelineStep == PipelineStepIndex - 1)
            {
                chunk.PipelineStep++;
            }

            var result = Func(chunk);
            return result;
        }
    }

    public class DefaultChunkPoolPipelineBlock : TransformManyPipelineBlock
    {
        public DefaultChunkPoolPipelineBlock()
        {
            Func = Pool;
            TransformBlock = new TransformManyBlock<Chunk, Chunk>(Process, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism =  1} );
        }

        public IEnumerable<Chunk> Pool(Chunk chunk)
        {
            // a disposed and reloaded neighbor may re-proc an already processed chunk
            // in which case we don't want to demote the pipeline step
            if (chunk.PipelineStep == PipelineStepIndex - 1)
            {
                chunk.PipelineStep++;
            }


            // it might be null in case the chunk was marked for deletion in the background
            if (chunk.Neighbors != null)
            {
                foreach (var n in chunk.Neighbors)
                {
                    // okay it starts to get a bit funky here with the race condition
                    if (n.Neighbors != null)
                    {
                        if (!n.Neighbors.Any(s => s == null || s.PipelineStep < chunk.PipelineStep))
                        {
                            yield return n;
                        }
                    }
                }
            }
        }
    }
}

