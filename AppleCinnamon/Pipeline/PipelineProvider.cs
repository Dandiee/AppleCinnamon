﻿using System;
using System.Threading.Tasks;
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
            var multiThreaded = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism};
            var singleThreaded = new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1};

            var terrainGenerator = new TransformPipelineBlock<Int2, Chunk>(_terrainGenerator.Process, nameof(TerrainGenerator), multiThreaded);
            terrainGenerator
                .LinkTo(new TransformManyPipelineBlock(_neighborAssigner.Process, singleThreaded))
                .LinkTo(new ChunkTransformBlock(_artifactGenerator, singleThreaded))
                .LinkTo(new DefaultChunkPoolPipelineBlock())
                .LinkTo(new ChunkTransformBlock(_localFinalizer, multiThreaded))
                .LinkTo(new DefaultChunkPoolPipelineBlock())
                .LinkTo(new ChunkTransformBlock(_globalFinalizer, singleThreaded))
                .LinkTo(new DefaultChunkPoolPipelineBlock())
                .LinkTo(new ChunkTransformBlock(_chunkDispatcher, multiThreaded))
                .LinkTo(new ChunkTransformBlock(c =>
                {
                    successCallback(c);
                    return c;
                }, "Finalizer", singleThreaded));

            
            assigner = _neighborAssigner;

            return terrainGenerator;
        }
    }
}
