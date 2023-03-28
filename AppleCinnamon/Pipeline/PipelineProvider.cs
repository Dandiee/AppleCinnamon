using System.Threading.Tasks.Dataflow;
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

        public PipelineBlock CreatePipeline(int maxDegreeOfParallelism, ChunkManager chunkManager, out NeighborAssigner assigner)
        {
            var multiThreaded = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 16 };//maxDegreeOfParallelism};
            var singleThreaded = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1, };
           
            var pipeline = new ChunkTransformBlock(_terrainGenerator, multiThreaded)
                .LinkTo(new TransformManyPipelineBlock(_neighborAssigner.Process, singleThreaded)) // 169
                .LinkTo(new ChunkTransformBlock(_artifactGenerator, singleThreaded))
                .LinkTo(new DefaultChunkPoolPipelineBlock()) // 165
                .LinkTo(new ChunkTransformBlock(_localFinalizer, multiThreaded))
                .LinkTo(new DefaultChunkPoolPipelineBlock()) // 161
                .LinkTo(new ChunkTransformBlock(_globalFinalizer, singleThreaded))
                .LinkTo(new DefaultChunkPoolPipelineBlock()) // 157
                .LinkTo(new ChunkTransformBlock(_chunkDispatcher, multiThreaded))
                .SinkTo(chunkManager.Finalize);
            
            assigner = _neighborAssigner;

            return pipeline;
        }
    }
    
}
