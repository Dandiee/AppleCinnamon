﻿using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AppleCinnamon
{
    public sealed class Pipeline
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


        private readonly ChunkManager _chunkManager;

        public Pipeline(ChunkManager chunkManager)
        {
            _chunkManager = chunkManager;

            var terrain = new TerrainGenerator(new DaniNoise(WorldSettings.HighMapNoiseOptions));
            var artifact = new ArtifactGenerator();
            var local = new LocalFinalizer();
            var global = new GlobalFinalizer();

            TerrainStage = new PipelineStage("Terrain", terrain.Transform, NeighborAssigner, MDoP);
            ArtifactStage = new PipelineStage("Artifact", artifact.Transform, chk => Staging(1, chk));
            LocalStage = new PipelineStage("Local", local.Transform, chk => Staging(2, chk), MDoP);
            GlobalStage = new PipelineStage("Global", global.Transform, chk => Staging(3, chk));

            Stages = new[] { TerrainStage, ArtifactStage, LocalStage, GlobalStage };

            BuildPipeline();
        }

        public void Post(Chunk chunk) => TerrainStage.Transform.Post(chunk);

        public void Suspend(Action onSuspendedCallback)
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
                    onSuspendedCallback();
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
            var dispatcher = new ChunkDispatcher();

            Dispatcher = new TransformBlock<Chunk, Chunk>(dispatcher.Transform, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = MDoP});
            FinishBlock = new ActionBlock<Chunk>(_chunkManager.Finalize);

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
            chunk.History.Add($"{chunk.Stage} - {Stages[stageIndex].Name}");
            chunk.Stage++;

            foreach (var neighbor in chunk.Neighbors)
            {
                if (neighbor != null
                    && neighbor.Stage == stageIndex + 1
                    && neighbor.Neighbors.All(s => s != null && s.Stage >= stageIndex + 1))
                {
                    //if (Stages[stageIndex].ReturnedIndexes.TryAdd(neighbor.ChunkIndex, null))
                    if (Stages[stageIndex].ReturnedIndexes2.Add(neighbor.ChunkIndex))
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
                //stage.ReturnedIndexes.TryRemove(chunkIndex, out var _);
                stage.ReturnedIndexes2.Remove(chunkIndex);
            }
        }
    }

    public enum PipelineState
    {
        Running,
        Stopping,
        Stopped,
    }
}
