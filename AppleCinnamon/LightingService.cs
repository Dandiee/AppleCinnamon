using System;
using System.Collections.Generic;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
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

    public static class LightingService
    {
        public static void GlobalPropagate(Chunk chunk, Int3 relativeIndex)
            => GlobalPropagate(new Queue<GlobalLighntessPropogationRecord>(new[]
                {new GlobalLighntessPropogationRecord(chunk, relativeIndex)}));

        public static void GlobalPropagate(Queue<GlobalLighntessPropogationRecord> queue)
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
                            source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                        targetChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(targetChunk.CurrentHeight),
                            targetVoxel.SetLight((byte) (sourceVoxel.Lightness - brightnessLoss)));
                        queue.Enqueue(
                            new GlobalLighntessPropogationRecord(targetChunk, targetAddress.RelativeVoxelIndex));
                    }
                }
            }
        }

        public static void LocalPropagate(Chunk chunk, int localLightSources)
            => LocalPropagate(chunk, new Queue<int>(new[] {localLightSources}));

        public static void LocalPropagate(Chunk chunk, Queue<int> localLightSources)
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

        public static void GlobalPropagateDarkness(DarknessPropogationRecord record)
        {
            var queue = new Queue<DarknessPropogationRecord>();
            queue.Enqueue(record);

            while (queue.Count > 0)
            {
                var source = queue.Dequeue();

                foreach (var direction in LightDirections.All)
                {
                    var targetVoxel = source.sourceChunk.GetLocalWithneighbors(source.sourceIndex.X + direction.Step.X, source.sourceIndex.Y + direction.Step.Y, source.sourceIndex.Z + direction.Step.Z, out var targetAddress);
                    var targetDefinition = targetVoxel.GetDefinition();
                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(record.sourceDefinition, targetDefinition, direction.Direction);

                    if (brightnessLoss > 0)
                    {
                        if (targetVoxel.Lightness >= source.oldVoxel.Lightness)
                        {
                            source.lightSources.Add(new Tuple<Chunk, Int3>(
                                source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)],
                                targetAddress.RelativeVoxelIndex));
                        }
                        else if (targetVoxel.Lightness > 0)
                        {
                            var newChunk = source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                            newChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(newChunk.CurrentHeight), targetVoxel.SetLight(0));
                            queue.Enqueue(new DarknessPropogationRecord(newChunk, targetAddress.RelativeVoxelIndex, source.lightSources, targetVoxel, targetDefinition));
                        }
                    }
                }
            }

        }

        public static IEnumerable<Int3> Sunlight(Chunk chunk, Int3 relativeIndex, byte toLightness)
        {
            for (var j = relativeIndex.Y - 1; j > 0; j--)
            {
                var flatIndex = Help.GetFlatIndex(relativeIndex.X, j, relativeIndex.Z, chunk.CurrentHeight);
                var voxel = chunk.Voxels[flatIndex];
                var definition = VoxelDefinition.DefinitionByType[voxel.Block];
                
                // TODO: ez nem jó, nem elég csak azt mondani hogy > 0
                if (definition.TransmittanceQuarters[(byte)Face.Bottom] > 0)
                {
                    chunk.SetVoxel(flatIndex, voxel.SetLight(toLightness));

                    yield return new Int3(relativeIndex.X, j, relativeIndex.Z);
                }
                else break;
            }
        }

        public struct DarknessPropogationRecord
        {
            public Chunk sourceChunk;
            public Int3 sourceIndex;
            public List<Tuple<Chunk, Int3>> lightSources;
            public Voxel oldVoxel;
            public VoxelDefinition sourceDefinition;

            public DarknessPropogationRecord(Chunk sourceChunk, Int3 sourceIndex, List<Tuple<Chunk, Int3>> lightSources,
                Voxel oldVoxel, VoxelDefinition sourceDefinition)
            {
                this.sourceChunk = sourceChunk;
                this.sourceIndex = sourceIndex;
                this.lightSources = lightSources;
                this.oldVoxel = oldVoxel;
                this.sourceDefinition = sourceDefinition;
            }
        }
    }
}