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
        private readonly ILocalLightPropagationService _localLightPropagationService;
        private readonly IChunkProvider _chunkProvider;
        private readonly ILightFinalizer _lightFinalizer;
        private readonly IChunkPool _chunkPool;
        private readonly IFullScanner _fullScanner;
        private readonly IGlobalVisibilityFinalizer _globalVisibilityFinalizer;
        private readonly ILocalSunlightInitializer _localSunlightInitializer;


        public PipelineProvider()
        {
            _chunkDispatcher = new ChunkDispatcher();
            _localLightPropagationService = new LocalLocalLightPropagationService();
            _chunkProvider = new ChunkProvider(921207);
            _lightFinalizer = new LightFinalizer();
            _chunkPool = new ChunkPool();
            _fullScanner = new FullScanner();
            _globalVisibilityFinalizer = new GlobalVisibilityFinalizer();
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
            var fullScan = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_fullScanner.Process, dataflowOptions);
            var localLightPropagation = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_localLightPropagationService.InitializeLocalLight, dataflowOptions);
            var chunkPool = new TransformManyBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkPool.Process, dataflowOptions);
            var globalVisibility = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_globalVisibilityFinalizer.Process, dataflowOptions);
            var lightFinalizer = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_lightFinalizer.Finalize, dataflowOptions);
            var dispatcher = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkDispatcher.Dispatch, dataflowOptions);
            var finalizer = new ActionBlock<DataflowContext<Chunk>>(successCallback, dataflowOptions);

            pipeline.LinkTo(sunlightInitializer);
            sunlightInitializer.LinkTo(fullScan);
            fullScan.LinkTo(localLightPropagation);
            localLightPropagation.LinkTo(chunkPool);
            chunkPool.LinkTo(globalVisibility);
            globalVisibility.LinkTo(lightFinalizer);
            lightFinalizer.LinkTo(dispatcher);
            dispatcher.LinkTo(finalizer);

            return pipeline;
        }
    }
}
