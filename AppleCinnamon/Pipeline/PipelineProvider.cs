using System;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;

namespace AppleCinnamon.Pipeline
{
    public sealed class PipelineProvider
    {

        private readonly ChunkDispatcher _chunkDispatcher;
        private readonly LocalLightPropagationService _localLightPropagationService;
        private readonly ChunkProvider _chunkProvider;
        private readonly LightFinalizer _lightFinalizer;
        private readonly ChunkPool _chunkPool;
        private readonly FullScanner _fullScanner;
        private readonly GlobalVisibilityFinalizer _globalVisibilityFinalizer;
        private readonly LocalSunlightInitializer _localSunlightInitializer;
        private readonly BuildPool _buildPool;



        public PipelineProvider()
        {
            _chunkDispatcher = new ChunkDispatcher();
            _localLightPropagationService = new LocalLightPropagationService();
            _chunkProvider = new ChunkProvider(921207);
            _lightFinalizer = new LightFinalizer();
            _chunkPool = new ChunkPool();
            _fullScanner = new FullScanner();
            _globalVisibilityFinalizer = new GlobalVisibilityFinalizer();
            _localSunlightInitializer = new LocalSunlightInitializer();
            _buildPool = new BuildPool();
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
            var buildPool = new TransformManyBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_buildPool.Process, dataflowOptions);
            var dispatcher = new TransformBlock<DataflowContext<Chunk>, DataflowContext<Chunk>>(_chunkDispatcher.Dispatch, dataflowOptions);
            var finalizer = new ActionBlock<DataflowContext<Chunk>>(successCallback, dataflowOptions);

            pipeline.LinkTo(sunlightInitializer);
            sunlightInitializer.LinkTo(fullScan);
            fullScan.LinkTo(localLightPropagation);
            localLightPropagation.LinkTo(chunkPool);
            chunkPool.LinkTo(globalVisibility);
            globalVisibility.LinkTo(lightFinalizer);
            lightFinalizer.LinkTo(buildPool);
            buildPool.LinkTo(dispatcher);
            dispatcher.LinkTo(finalizer);

            return pipeline;
        }
    }
}
