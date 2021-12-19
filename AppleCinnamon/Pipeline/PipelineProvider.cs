using System;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;
using SharpDX.Direct3D11;

namespace AppleCinnamon.Pipeline
{
    public sealed class PipelineProvider
    {

        private readonly TerrainGenerator _terrainGenerator;
        private readonly NeighborAssigner _neighborAssigner;
        private readonly ArtifactGenerator _artifactGenerator;
        private readonly LocalFinalizer _localFinalizer;
        private readonly GlobalFinalizer _globalFinalizer;
        private readonly ChunkDispatcher _chunkDispatcher;
        
        public PipelineProvider(Device device)
        {
            _terrainGenerator = new TerrainGenerator(new DaniNoise(WorldSettings.HighMapNoiseOptions));
            _neighborAssigner = new NeighborAssigner();
            _artifactGenerator = new ArtifactGenerator();
            _localFinalizer = new LocalFinalizer();
            _globalFinalizer = new GlobalFinalizer();
            _chunkDispatcher = new ChunkDispatcher(device);
        }

        public TransformPipelineBlock<Int2, Chunk> CreatePipeline(int maxDegreeOfParallelism, Action<Chunk> successCallback, out NeighborAssigner assigner)
        {
            var multiThreaded = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism
            };

            var singleThreaded = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1
            };

            
            var terrainGenerator = new TransformPipelineBlock<Int2, Chunk>(_terrainGenerator.Process);
            terrainGenerator
                .LinkTo(new TransformManyPipelineBlock<Chunk, Chunk>(_neighborAssigner.Process))
                .LinkTo(new TransformPipelineBlock<Chunk, Chunk>(_artifactGenerator.Process))
                .LinkTo(new ChunkPoolPipelineBlock(2))
                .LinkTo(new TransformPipelineBlock<Chunk, Chunk>(_localFinalizer.Process))
                .LinkTo(new ChunkPoolPipelineBlock(4))
                .LinkTo(new TransformPipelineBlock<Chunk, Chunk>(_globalFinalizer.Process))
                .LinkTo(new ChunkPoolPipelineBlock(6))
                .LinkTo(new TransformPipelineBlock<Chunk, Chunk>(_chunkDispatcher.Process))
                .LinkTo(new TransformPipelineBlock<Chunk, Chunk>(c =>
                {
                    successCallback(c);
                    return c;
                }));

            
            assigner = _neighborAssigner;

            return terrainGenerator;
        }
    }
}
