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
                    ? Voxel.SunBlock
                    : source.sourceChunk.GetVoxel(source.sourceIndex.ToFlatIndex(source.sourceChunk.CurrentHeight));
                var sourceDefinition = sourceVoxel.GetDefinition();

                foreach (var direction in LightDirections.All)
                {
                    var targetIndex = source.sourceIndex + direction.Step;
                    var targetVoxel = source.sourceChunk.GetLocalWithNeighbor(targetIndex.X, targetIndex.Y, targetIndex.Z, out var targetAddress);
                    var targetDefinition = targetVoxel.GetDefinition();

                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, direction.Direction);
                    if (brightnessLoss != 0)
                    {
                        if (targetVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss || targetVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                        {
                            var targetChunk = source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];

                            // todo: talán ha nem írnánk és olvasnánk ugyanazt a memóriát hétezerszer észnélkül az segítene... csak talán...
                            if (targetVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss)
                            {
                                targetChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(targetChunk.CurrentHeight), targetVoxel.SetSunlight((byte)(sourceVoxel.Sunlight - brightnessLoss)));
                            }

                            if (targetVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                            {
                                targetChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(targetChunk.CurrentHeight), targetVoxel.SetCustomLight((byte)(sourceVoxel.EmittedLight - brightnessLoss)));
                            }

                            queue.Enqueue(new GlobalLighntessPropogationRecord(targetChunk, targetAddress.RelativeVoxelIndex));
                        }
                    }
                }
            }
        }

        public static void LocalPropagate(Chunk chunk, Queue<int> localLightSources)
        {
            while (localLightSources.Count > 0)
            {
                var lightSourceFlatIndex = localLightSources.Dequeue();
                var sourceVoxel = chunk.GetVoxel(lightSourceFlatIndex);
                var sourceDefinition = sourceVoxel.GetDefinition();
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
                                var neighborFlatIndex = Help.GetFlatIndex(neighborX, neighborY, neighborZ, chunk.CurrentHeight);
                                var neighborVoxel = chunk.GetVoxel(neighborFlatIndex);
                                var neighborDefinition = neighborVoxel.GetDefinition();
                                var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, neighborDefinition, direction.Direction);

                                if (brightnessLoss != 0)
                                {
                                    var hasChanged = false;
                                    if (neighborVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss)
                                    {
                                        hasChanged = true;
                                        chunk.SetVoxel(neighborFlatIndex, neighborVoxel.SetSunlight((byte)(sourceVoxel.Sunlight - brightnessLoss)));
                                    }

                                    if (neighborVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                                    {
                                        hasChanged = true;
                                        chunk.SetVoxel(neighborFlatIndex, neighborVoxel.SetCustomLight((byte) (sourceVoxel.EmittedLight - brightnessLoss)));
                                    }

                                    if (hasChanged)
                                    {
                                        localLightSources.Enqueue(neighborFlatIndex);
                                    }
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
                    var targetVoxel = source.sourceChunk.GetLocalWithNeighbor(source.sourceIndex.X + direction.Step.X, source.sourceIndex.Y + direction.Step.Y, source.sourceIndex.Z + direction.Step.Z, out var targetAddress);
                    var targetDefinition = targetVoxel.GetDefinition();
                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(record.sourceDefinition, targetDefinition, direction.Direction);

                    if (brightnessLoss > 0)
                    {


                        if (targetVoxel.Sunlight >= source.oldVoxel.Sunlight )
                        {
                            source.lightSources.Add(new Tuple<Chunk, Int3>(source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)], targetAddress.RelativeVoxelIndex));
                        }
                        else  if (targetVoxel.Sunlight > 0)
                        {
                            var newChunk = source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                            newChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(newChunk.CurrentHeight), targetVoxel.SetSunlight(0));
                            queue.Enqueue(new DarknessPropogationRecord(newChunk, targetAddress.RelativeVoxelIndex, source.lightSources, targetVoxel, targetDefinition));
                        }


                        if (targetVoxel.EmittedLight >= source.oldVoxel.EmittedLight)
                        {
                            source.lightSources.Add(new Tuple<Chunk, Int3>(source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)], targetAddress.RelativeVoxelIndex));
                        }
                        else if (targetVoxel.EmittedLight > 0)
                        {
                            var newChunk = source.sourceChunk.Neighbors[Help.GetChunkFlatIndex(targetAddress.ChunkIndex)];
                            newChunk.SetVoxel(targetAddress.RelativeVoxelIndex.ToFlatIndex(newChunk.CurrentHeight), targetVoxel.SetCustomLight(0));
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
                var definition = voxel.GetDefinition();

                // TODO: ez nem jó, nem elég csak azt mondani hogy > 0
                //if (definition.TransmittanceQuarters[(byte)Face.Bottom] > 0)
                if (definition.Type == 0)
                {
                    chunk.SetVoxel(flatIndex, voxel.SetSunlight(toLightness));

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