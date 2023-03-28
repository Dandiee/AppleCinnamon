using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public PipelineBlock Head { get; protected set; }
        public int PipelineStepIndex { get; protected set; }
        public readonly List<IMonitoredBlock> MonitoredBlocks = new();

        public IPropagatorBlock<Chunk, Chunk> TransformBlock { get; protected set; }

        public PipelineBlock LinkTo(PipelineBlock next)
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

        public PipelineBlock LinkTo(Action<Chunk> action)
        {
            var actionBlock = new ActionBlock<Chunk>(action);
            TransformBlock.LinkTo(actionBlock);
            return Head;
        }
    }

    public interface IMonitoredBlock
    {
        string Name { get; }
        long ElapsedTime { get; }
    }

    public class ChunkTransformBlock : PipelineBlock, IMonitoredBlock
    {
        public string Name { get; }
        private readonly Func<Chunk, Chunk> _func;
        private readonly Stopwatch _stopwatch;
        public long ElapsedTime => _stopwatch.ElapsedMilliseconds;

        public ChunkTransformBlock(IChunkTransformer transformer, ExecutionDataflowBlockOptions options)
        {
            Name = transformer.GetType().Name;
            _func = transformer.Transform;
            TransformBlock = new TransformBlock<Chunk, Chunk>(Process, options);
            _stopwatch = new Stopwatch();
        }

        protected virtual Chunk Process(Chunk input)
        {
            if (PipelineStepIndex > 0 && input.PipelineStep != PipelineStepIndex - 1)
            {
                return input;
            }

            input.PipelineStep++;

            _stopwatch.Start();
            var result = _func(input);
            _stopwatch.Stop();
            return result;
        }
    }

    public class TransformManyPipelineBlock : PipelineBlock
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
            TransformBlock = new TransformManyBlock<Chunk, Chunk>(Process, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }

        public IEnumerable<Chunk> Pool(Chunk chunk)
        {
            // chunk reached a pool => one less chunk in transformation
            Interlocked.Decrement(ref ChunkManager.InProcessChunks);

            // massacre condition
            ChunkManager.WaitForDeletionEvent.WaitOne(); 

            if (chunk.PipelineStep == PipelineStepIndex - 1)
            {
                chunk.PipelineStep++;
            }

            foreach (var neighbor in chunk.Neighbors)
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

