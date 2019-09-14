using System;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public interface IPipelineProvider
    {
        TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> CreatePipeline(int maxDegreeOfParallelism,
            Action<DataflowContext<Chunk>> successCallback);
    }

    public sealed class PipelineProvider : IPipelineProvider
    {

        private readonly IChunkDispatcher _chunkDispatcher;
        private readonly ILightPropagationService _lightPropagationService;
        private readonly IChunkProvider _chunkProvider;
        private readonly ILightFinalizer _lightFinalizer;
        private readonly IChunkPool _chunkPool;
        private readonly ExperimentalStep _experimentalStep;
        private readonly GlobalVisibility _globalVisibility;
        private readonly ILocalSunlightInitializer _localSunlightInitializer;


        public PipelineProvider()
        {
            _chunkDispatcher = new ChunkDispatcher();
            _lightPropagationService = new LightPropagationService();
            _chunkProvider = new ChunkProvider(921207);
            _lightFinalizer = new LightFinalizer();
            _chunkPool = new ChunkPool();
            _experimentalStep = new ExperimentalStep();
            _globalVisibility = new GlobalVisibility();
            _localSunlightInitializer = new LocalSunlightInitializer();
        }

        public TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>> CreatePipeline(int maxDegreeOfParallelism, Action<DataflowContext<Chunk>> successCallback)
        {
            var dataflowOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var pipeline = new TransformBlock<DataflowContext<Int2>, DataflowContext<Chunk>>(_chunkProvider.GetChunk, dataflowOptions);
            var sunlightInitializer = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_localSunlightInitializer.Process, dataflowOptions);
            var experimental = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_experimentalStep.Process, dataflowOptions);
            var lighter = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightPropagationService.InitializeLocalLight, dataflowOptions);
            var pool = new TransformManyBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkPool.Process, dataflowOptions);
            var globalVisibility = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_globalVisibility.Process, dataflowOptions);
            var lightFinalizer = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightFinalizer.Finalize, dataflowOptions);
            var dispatcher = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkDispatcher.Dispatch, dataflowOptions);
            var finalizer = new ActionBlock<DataflowContext<Chunk>>(successCallback, dataflowOptions);

            pipeline.LinkTo(sunlightInitializer);
            sunlightInitializer.LinkTo(experimental);
            experimental.LinkTo(lighter);
            lighter.LinkTo(pool);
            pool.LinkTo(globalVisibility);
            globalVisibility.LinkTo(lightFinalizer);
            lightFinalizer.LinkTo(dispatcher);
            dispatcher.LinkTo(finalizer);

            return pipeline;
        }
    }
}
