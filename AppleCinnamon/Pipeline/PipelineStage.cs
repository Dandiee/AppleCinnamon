using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;

namespace AppleCinnamon
{
    public sealed class PipelineStage
    {
        public string Name { get; }
        public TransformBlock<Chunk, Chunk> Transform { get; set; }
        public TransformManyBlock<Chunk, Chunk> Staging { get; set; }

        public BufferBlock<Chunk> Buffer { get; }

        public PipelineStage Next { get; set; }

        private readonly Func<Chunk, Chunk> _transformCallback;
        private readonly Func<Chunk, IEnumerable<Chunk>> _stagingCallback;
        private readonly ExecutionDataflowBlockOptions _transformOptions;

        private IDisposable _transformToStagingLink;
        private IDisposable _transformToBufferLink;

        public readonly ConcurrentDictionary<Int2, object> ReturnedIndexes;
        public readonly HashSet<Int2> ReturnedIndexes2;

        public PipelineStage(
            string name,
            Func<Chunk, Chunk> transformCallback,
            Func<Chunk, IEnumerable<Chunk>> stagingCallback,
            int mDoP = 1)
        {
            Name = name;
            _transformCallback = transformCallback;
            _stagingCallback = stagingCallback;

            Buffer = new BufferBlock<Chunk>();
            _transformOptions = new() { MaxDegreeOfParallelism = mDoP };
            ReturnedIndexes = new ();
            ReturnedIndexes2 = new();
        }

        public PipelineStage LinkTo(PipelineStage stage)
        {
            Staging.LinkTo(stage.Transform, Pipeline.PropagateCompletionOptions);
            Next = stage;
            return Next;
        }

        public void CreateBlocks()
        {
            Transform = new TransformBlock<Chunk, Chunk>(_transformCallback, _transformOptions);
            Staging = new TransformManyBlock<Chunk, Chunk>(_stagingCallback);
            _transformToStagingLink = Transform.LinkTo(Staging);
        }

        public void RequestSuspend()
        {
            _transformToStagingLink.Dispose();
            _transformToBufferLink = Transform.LinkTo(Buffer);
            Staging.Complete();
            Transform.Completion.ContinueWith(_ => _transformToBufferLink.Dispose());
        }

        public void FlushBuffer()
        {
            while (Buffer.TryReceive(null, out var chunk))
            {
                if (chunk.State == ChunkState.New)
                {
                    Staging.Post(chunk);
                }
            }
        }
    }
}
