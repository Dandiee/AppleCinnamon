using System;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Pipeline
{
    public sealed class PipelineProvider
    {

        private readonly ChunkDispatcher _chunkDispatcher;
        private readonly LocalLightPropagationService _localLightPropagationService;
        private readonly ChunkProvider _chunkProvider;
        private readonly GlobalLightFinalizer _globalLightFinalizer;
        private readonly ChunkPool _chunkPool;
        private readonly FullScanner _fullScanner;
        private readonly GlobalVisibilityFinalizer _globalVisibilityFinalizer;
        private readonly LocalSunlightInitializer _localSunlightInitializer;
        private readonly BuildPool _buildPool;



        public PipelineProvider(Device device)
        {
            _chunkDispatcher = new ChunkDispatcher(device);
            _localLightPropagationService = new LocalLightPropagationService();
            _chunkProvider = new ChunkProvider(921207);
            _globalLightFinalizer = new GlobalLightFinalizer();
            _chunkPool = new ChunkPool();
            _fullScanner = new FullScanner();
            _globalVisibilityFinalizer = new GlobalVisibilityFinalizer();
            _localSunlightInitializer = new LocalSunlightInitializer();
            _buildPool = new BuildPool();
        }

        public TransformBlock<Int2, Chunk> CreatePipeline(int maxDegreeOfParallelism, Action<Chunk> successCallback)
        {
            var dataflowOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var pipeline = new TransformBlock<Int2, Chunk>(_chunkProvider.Execute, dataflowOptions);
            var sunlightInitializer = new TransformBlock<Chunk, Chunk>(_localSunlightInitializer.Execute, dataflowOptions);

            var fullScan = new TransformBlock<Chunk, Chunk>(_fullScanner.Execute, dataflowOptions);
            var localLightPropagation = new TransformBlock<Chunk, Chunk>(_localLightPropagationService.Execute, dataflowOptions);
            var chunkPool = new TransformManyBlock<Chunk, Chunk>(_chunkPool.Execute, dataflowOptions);
            var globalVisibility = new TransformBlock<Chunk, Chunk>(_globalVisibilityFinalizer.Execute, dataflowOptions);
            var lightFinalizer = new TransformBlock<Chunk, Chunk>(_globalLightFinalizer.Execute, dataflowOptions);
            var buildPool = new TransformManyBlock<Chunk, Chunk>(_buildPool.Execute, dataflowOptions);
            var dispatcher = new TransformBlock<Chunk, Chunk>(_chunkDispatcher.Execute, dataflowOptions);
            var finalizer = new ActionBlock<Chunk>(successCallback, dataflowOptions);

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
