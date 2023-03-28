using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace AppleCinnamon.Pipeline.Context
{
    public abstract class PipelineBlock
    {
        public string Name { get; private set; }
        public PipelineBlock Head { get; protected set; }
        public int PipelineStepIndex { get; protected set; }
        public readonly List<IMonitoredBlock> MonitoredBlocks = new();

        public IPropagatorBlock<Chunk, Chunk> TransformBlock { get; protected set; }

        protected PipelineBlock(string name)
        {
            Name = name;
        }

        public PipelineBlock LinkTo(PipelineBlock next)
        {
            TransformBlock.LinkTo(next.TransformBlock);
            next.PipelineStepIndex = PipelineStepIndex + 1;
            next.Head = Head ?? next.Head ?? this;
            next.Name += $" ({next.PipelineStepIndex})";
            if (this is IMonitoredBlock monitoredBlock)
            {
                next.Head.MonitoredBlocks.Add(monitoredBlock);
            }

            return next;
        }

        public PipelineBlock SinkTo(Action<Chunk> action)
        {
            TransformBlock.LinkTo(new ActionBlock<Chunk>(action));
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
        private readonly Func<Chunk, Chunk> _func;
        private readonly Stopwatch _stopwatch;
        public long ElapsedTime => _stopwatch.ElapsedMilliseconds;

        public ChunkTransformBlock(IChunkTransformer transformer, ExecutionDataflowBlockOptions options)
         : base(transformer.GetType().Name)
        {
            _func = transformer.Transform;
            TransformBlock = new TransformBlock<Chunk, Chunk>(Process, options);
            _stopwatch = new Stopwatch();
        }

        protected virtual Chunk Process(Chunk input)
        {
            if (PipelineStepIndex > 0 && input.PipelineStep >= PipelineStepIndex)
            {
                input.History.Add($"Shortcut from: {Name}");
                return input;
            }

            input.History.Add(Name);

            Interlocked.Increment(ref input.PipelineStep);

            _stopwatch.Start();
            var result = _func(input);
            _stopwatch.Stop();
            return result;
        }
    }

    public class TransformManyPipelineBlock : PipelineBlock
    {
        protected Func<Chunk, IEnumerable<Chunk>> Callback;

        public TransformManyPipelineBlock(IChunksTransformer transformer, ExecutionDataflowBlockOptions options)
         : base(transformer.GetType().Name)
        {
            Callback = transformer.TransformMany;
            transformer.Owner = this;
            TransformBlock = new TransformManyBlock<Chunk, Chunk>(Process, options);
        }

        protected IEnumerable<Chunk> Process(Chunk chunk)
        {
            if (chunk.PipelineStep == PipelineStepIndex - 1)
            {
                Interlocked.Increment(ref chunk.PipelineStep);
            }

            chunk.History.Add(Name);

            return Callback(chunk);
        }
    }
}

