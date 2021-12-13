using System;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Pipeline
{
    public sealed class PipelineProvider
    {

        private readonly TerrainGenerator _terrainGenerator;
        private readonly NeighborAssigner _neighborAssigner;
        private readonly ArtifactGenerator _artifactGenerator;

        
        private readonly LocalSunlightInitializer _localSunlightInitializer;
        private readonly FullScanner _fullScanner;
        private readonly LocalLightPropagationService _localLightPropagationService;
        private readonly ChunkPool _chunkPool;

        private readonly GlobalVisibilityFinalizer _globalVisibilityFinalizer;
        private readonly GlobalLightFinalizer _globalLightFinalizer;
        private readonly BuildPool _buildPool;

        private readonly ChunkDispatcher _chunkDispatcher;
        
        public PipelineProvider(Device device)
        {
            _terrainGenerator = new TerrainGenerator(new DaniNoise(WorldSettings.HighMapNoiseOptions));
            _neighborAssigner = new NeighborAssigner();
            _artifactGenerator = new ArtifactGenerator();

            _localSunlightInitializer = new LocalSunlightInitializer();
            _fullScanner = new FullScanner();
            _localLightPropagationService = new LocalLightPropagationService();
            _chunkPool = new ChunkPool();

            _globalVisibilityFinalizer = new GlobalVisibilityFinalizer();
            _globalLightFinalizer = new GlobalLightFinalizer();
            _buildPool = new BuildPool();

            _chunkDispatcher = new ChunkDispatcher(device);
        }

        public TransformBlock<Int2, Chunk> CreatePipeline(int maxDegreeOfParallelism, Action<Chunk> successCallback)
        {
            var dataflowOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var pipeline = new TransformBlock<Int2, Chunk>(_terrainGenerator.Execute, dataflowOptions);
            var neighborAssigner = new TransformManyBlock<Chunk, Chunk>(_neighborAssigner.Execute, dataflowOptions);
            var artifactGenerator = new TransformBlock<Chunk, Chunk>(_artifactGenerator.Execute, dataflowOptions);

            var sunlightInitializer = new TransformBlock<Chunk, Chunk>(_localSunlightInitializer.Execute, dataflowOptions);
            var fullScan = new TransformBlock<Chunk, Chunk>(_fullScanner.Execute, dataflowOptions);
            var localLightPropagation = new TransformBlock<Chunk, Chunk>(_localLightPropagationService.Execute, dataflowOptions);
            var chunkPool = new TransformManyBlock<Chunk, Chunk>(_chunkPool.Execute, dataflowOptions);
            
            var globalVisibility = new TransformBlock<Chunk, Chunk>(_globalVisibilityFinalizer.Execute, dataflowOptions);
            var lightFinalizer = new TransformBlock<Chunk, Chunk>(_globalLightFinalizer.Execute, dataflowOptions);
            var buildPool = new TransformManyBlock<Chunk, Chunk>(_buildPool.Execute, dataflowOptions);
            
            var dispatcher = new TransformBlock<Chunk, Chunk>(_chunkDispatcher.Execute, dataflowOptions);
            
            var finalizer = new ActionBlock<Chunk>(successCallback, dataflowOptions);

            pipeline.LinkTo(neighborAssigner);
            neighborAssigner.LinkTo(artifactGenerator);
            artifactGenerator.LinkTo(sunlightInitializer);
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
