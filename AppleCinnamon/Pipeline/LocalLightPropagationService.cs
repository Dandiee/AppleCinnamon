using System.Collections.Generic;
using System.Diagnostics;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class LocalLightPropagationService
    {
        public static readonly Int3[] Directions =
        {
            Int3.UnitY, -Int3.UnitY, -Int3.UnitX, Int3.UnitX, -Int3.UnitZ, Int3.UnitZ
        };


        public DataflowContext<Chunk> InitializeLocalLight(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            InitializeLocalLight(chunk, chunk.BuildingContext.LightPropagationVoxels);

            //while (chunk.BuildingContext.LightPropagationVoxels.Count > 0)
            //{
            //    var lightSourceFlatIndex = chunk.BuildingContext.LightPropagationVoxels.Dequeue();
            //    var sourceVoxel = chunk.GetVoxel(lightSourceFlatIndex);
            //    var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];
            //    var index = lightSourceFlatIndex.ToIndex(chunk.CurrentHeight);



            //    foreach (var direction in LightDirections.All)
            //    {
            //        var neighborX = index.X + direction.Step.X;
            //        if ((neighborX & Chunk.SizeXy) == 0)
            //        {
            //            var neighborY = index.Y + direction.Step.Y;
            //            if (neighborY > 0 && neighborY < chunk.CurrentHeight)
            //            {
            //                var neighborZ = index.Z + direction.Step.Z;
            //                if ((neighborZ & Chunk.SizeXy) == 0)
            //                {
            //                    var neighborFlatIndex = Help.GetFlatIndex(neighborX, neighborY, neighborZ, chunk.CurrentHeight);
            //                    var neighborVoxel = chunk.GetVoxelNoInline(neighborFlatIndex);
            //                    var neighborDefinition = VoxelDefinition.DefinitionByType[neighborVoxel.Block];

            //                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, neighborDefinition, direction.Direction);
            //                    if (brightnessLoss != 0 && neighborVoxel.Lightness < sourceVoxel.Lightness - brightnessLoss)
            //                    {
            //                        chunk.SetVoxelNoInline(neighborFlatIndex, new Voxel(neighborVoxel.Block, (byte)(sourceVoxel.Lightness - brightnessLoss)));
            //                        chunk.BuildingContext.LightPropagationVoxels.Enqueue(neighborFlatIndex);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            sw.Stop();

            return new DataflowContext<Chunk>(context, context.Payload, sw.ElapsedMilliseconds,
                nameof(LocalLightPropagationService));
        }


        public static void InitializeLocalLight(Chunk chunk, int localLightSources)
        {
            var queue = new Queue<int>();
            queue.Enqueue(localLightSources);
            InitializeLocalLight(chunk, queue);
        }

        public static void InitializeLocalLight(Chunk chunk, Queue<int> localLightSources)
        {
            while (localLightSources.Count > 0)
            {
                var lightSourceFlatIndex = localLightSources.Dequeue();
                var sourceVoxel = chunk.GetVoxel(lightSourceFlatIndex);
                var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];
                var index = lightSourceFlatIndex.ToIndex(chunk.CurrentHeight);

                foreach (var direction in LightDirections.All)
                {
                    var neighborX = index.X + direction.Step.X;
                    if ((neighborX & Chunk.SizeXy) == 0)
                    {
                        var neighborY = index.Y + direction.Step.Y;
                        if (neighborY > 0 && neighborY < chunk.CurrentHeight)
                        {
                            var neighborZ = index.Z + direction.Step.Z;
                            if ((neighborZ & Chunk.SizeXy) == 0)
                            {
                                var neighborFlatIndex =
                                    Help.GetFlatIndex(neighborX, neighborY, neighborZ, chunk.CurrentHeight);
                                var neighborVoxel = chunk.GetVoxelNoInline(neighborFlatIndex);
                                var neighborDefinition = VoxelDefinition.DefinitionByType[neighborVoxel.Block];

                                if (neighborDefinition.Type == VoxelDefinition.SlabBottom.Type)
                                {

                                }

                                var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition,
                                    neighborDefinition, direction.Direction);
                                if (brightnessLoss != 0 &&
                                    neighborVoxel.Lightness < sourceVoxel.Lightness - brightnessLoss)
                                {
                                    chunk.SetVoxelNoInline(neighborFlatIndex,
                                        neighborVoxel.SetLight((byte) (sourceVoxel.Lightness - brightnessLoss)));
                                    localLightSources.Enqueue(neighborFlatIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

        public class GlobalLighntessPropogationRecord
        {
            public Chunk sourceChunk;
            public Int3 sourceIndex;

            public GlobalLighntessPropogationRecord(Chunk soureChunk, Int3 sourceIndex)
            {
                this.sourceChunk = soureChunk;
                this.sourceIndex = sourceIndex;
            }
        }

        public static void GlobalPropagateLightness(Queue<GlobalLighntessPropogationRecord> queue)
        {
            while (queue.Count > 0)
            {
                var source = queue.Dequeue();
                var sourceVoxel = source.sourceChunk.CurrentHeight <= source.sourceIndex.Y
                    ? Voxel.Air
                    : source.sourceChunk.GetVoxel(source.sourceIndex.ToFlatIndex(source.sourceChunk.CurrentHeight));
                var sourceDefinition = VoxelDefinition.DefinitionByType[sourceVoxel.Block];

                foreach (var direction in LightDirections.All)
                {
                    var targetIndex = source.sourceIndex + direction.Step;
                    var targetVoxel = source.sourceChunk.GetLocalWithneighbors(targetIndex.X, targetIndex.Y,
                        targetIndex.Z, out var targetAddress);
                    var targetDefinition = targetVoxel.GetDefinition();

                    var brightnessLoss =
                        VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, direction.Direction);
                    if (brightnessLoss != 0 && targetVoxel.Lightness < sourceVoxel.Lightness - brightnessLoss)
                    {
                        var targetChunk =
                            source.sourceChunk.neighbors2[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                        targetChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(targetChunk.CurrentHeight),
                            targetVoxel.SetLight((byte)(sourceVoxel.Lightness - brightnessLoss)));
                        queue.Enqueue(
                            new GlobalLighntessPropogationRecord(targetChunk, targetAddress.RelativeVoxelIndex));
                    }
                }
            }
        }

        public static void GlobalPropagateLightness(Chunk chunk, Int3 relativeIndex)
        {
            var queue = new Queue<GlobalLighntessPropogationRecord>();
            queue.Enqueue(new GlobalLighntessPropogationRecord(chunk, relativeIndex));
            GlobalPropagateLightness(queue);
        }
    }
}
