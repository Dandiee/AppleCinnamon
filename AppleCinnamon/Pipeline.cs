using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AppleCinnamon.Common;
using SharpDX.Direct3D11;
using AppleCinnamon.ChunkBuilders;
using AppleCinnamon.ChunkGenerators;

namespace AppleCinnamon
{
    // https://app.diagrams.net/#G1bx3H_GUN2TbOa55sV8_Cw-4TyGCXFWeh
    public sealed partial class Pipeline
    {
        public static readonly DataflowLinkOptions PropagateCompletionOptions = new() { PropagateCompletion = true };
        public static readonly int MDoP = Environment.ProcessorCount;

        public PipelineState State { get; private set; } = PipelineState.Running;
        public TransformBlock<Chunk, Chunk> Dispatcher { get; private set; }
        public ActionBlock<Chunk> FinishBlock { get; private set; }

        public PipelineStage TerrainStage { get; }
        public PipelineStage ArtifactStage { get; }
        public PipelineStage LocalStage { get; }
        public PipelineStage GlobalStage { get; }

        public PipelineStage[] Stages { get; }

        public TimeSpan TimeSpentInTransform { get; private set; }

        private readonly Device _device;
        private readonly Action<Chunk> _finishMove;

        public Pipeline(Action<Chunk> finishMove, Device device)
        {
            _finishMove = finishMove;
            _device = device;

            TerrainStage = new PipelineStage("Terrain", TerrainGenerator.Generate, NeighborAssigner, MDoP);
            ArtifactStage = new PipelineStage("Artifact", ArtifactGenerator.Generate, chk => Staging(1, chk));
            LocalStage = new PipelineStage("Local", LocalBuild, chk => Staging(2, chk), MDoP);
            GlobalStage = new PipelineStage("Global", GlobalBuild, chk => Staging(3, chk));

            Stages = new[] { TerrainStage, ArtifactStage, LocalStage, GlobalStage };

            BuildPipeline();

            SetupDebugContext();
        }

        public void Post(Chunk chunk) => TerrainStage.Transform.Post(chunk);

        public void Suspend()
        {
            State = PipelineState.Stopping;

            TerrainStage.Transform.Complete();
            foreach (var stage in Stages)
            {
                stage.RequestSuspend();
            }

            var completionTasks = Stages
                .Select(s => s.Transform.Completion)
                .Concat(new[] { FinishBlock.Completion });

            Task.WhenAll(completionTasks)
                .ContinueWith(_ =>
                {
                    State = PipelineState.Stopped;
                });
        }

        public void Resume()
        {
            BuildPipeline();
            foreach (var stage in Stages)
            {
                stage.FlushBuffer();
            }

            State = PipelineState.Running;
        }

        private void BuildPipeline()
        {
            Dispatcher = new TransformBlock<Chunk, Chunk>(BenchmarkedDispatcher, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MDoP });
            FinishBlock = new ActionBlock<Chunk>(_finishMove);

            foreach (var stage in Stages)
            {
                stage.CreateBlocks();
            }

            TerrainStage
                .LinkTo(ArtifactStage)
                .LinkTo(LocalStage)
                .LinkTo(GlobalStage);

            GlobalStage.Staging.LinkTo(Dispatcher, PropagateCompletionOptions);
            Dispatcher.LinkTo(FinishBlock, PropagateCompletionOptions);
        }

        public IEnumerable<Chunk> Staging(int stageIndex, Chunk chunk)
        {
            chunk.Stage++;

            foreach (var neighbor in chunk.Neighbors)
            {
                if (neighbor != null
                    && neighbor.Stage == stageIndex + 1
                    && neighbor.Neighbors.All(s => s != null && s.Stage >= stageIndex + 1))
                {
                    if (Stages[stageIndex].ReturnedIndexes.Add(neighbor.ChunkIndex))
                    {
                        yield return neighbor;
                    }
                }
            }
        }

        public IEnumerable<Chunk> NeighborAssigner(Chunk chunk)
        {
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    var absoluteNeighborIndex = new Int2(i + chunk.ChunkIndex.X, j + chunk.ChunkIndex.Y);

                    if (ChunkManager.Chunks.TryGetValue(absoluteNeighborIndex, out var neighborChunk))
                    {
                        neighborChunk.SetNeighbor(i * -1, j * -1, chunk);
                        chunk.SetNeighbor(i, j, neighborChunk);
                    }
                }
            }

            return Staging(0, chunk);
        }

        public void RemoveItem(Int2 chunkIndex)
        {
            foreach (var stage in Stages)
            {
                stage.ReturnedIndexes.Remove(chunkIndex);
            }
        }

        private Chunk LocalBuild(Chunk chunk)
        {
            LightingService.InitializeSunlight(chunk);
            FullScanner.FullScan(chunk);
            LightingService.LocalPropagate(chunk, chunk.BuildingContext.LightPropagationVoxels);

            return chunk;
        }

        private Chunk GlobalBuild(Chunk chunk)
        {
            GlobalVisibilityFinalizer.FinalizeGlobalVisibility(chunk);
            GlobalLightFinalizer.FinalizeGlobalLighting(chunk);

            return chunk;
        }

        private Chunk BenchmarkedDispatcher(Chunk chunk)
        {
            var sw = Stopwatch.StartNew();
            ChunkBuilders.ChunkDispatcher.BuildChunk(chunk, _device);
            sw.Stop();
            TimeSpentInTransform += sw.Elapsed;
            return chunk;
        }
    }

    public enum PipelineState
    {
        Running,
        Stopping,
        Stopped,
    }
}
