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

        public PipelineBlock CreatePipeline(int maxDegreeOfParallelism, ChunkManager chunkManager)
        {
            var mt = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 16 };//maxDegreeOfParallelism};
            var st = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1, };
           
            return      new ChunkTransformBlock(_terrainGenerator, mt)
                .LinkTo(new TransformManyPipelineBlock(_neighborAssigner, st)) // 169
                .LinkTo(new ChunkTransformBlock(_artifactGenerator, st))
                .LinkTo(new TransformManyPipelineBlock(new Pool(), st)) // 165
                .LinkTo(new ChunkTransformBlock(_localFinalizer, mt))
                .LinkTo(new TransformManyPipelineBlock(new Pool(), st)) // 165
                .LinkTo(new ChunkTransformBlock(_globalFinalizer, st))
                .LinkTo(new TransformManyPipelineBlock(new Pool(), st)) // 165
                .LinkTo(new ChunkTransformBlock(_chunkDispatcher, mt))
                .SinkTo(chunkManager.Finalize);
        }
    }
    
}
