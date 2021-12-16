using System;
using System.Collections.Generic;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public static class LightingService
    {
        public static void GlobalPropagate(Chunk chunk, Int3 relativeIndex)
            => GlobalPropagate(new Queue<VoxelChunkAddress>(new[]
                {new VoxelChunkAddress(chunk, relativeIndex)}));

        public static void GlobalPropagate(Queue<VoxelChunkAddress> queue)
        {
            while (queue.Count > 0)
            {
                var source = queue.Dequeue();
                var sourceVoxel = source.Chunk.CurrentHeight <= source.RelativeVoxelIndex.Y
                    ? Voxel.SunBlock
                    : source.Chunk.GetVoxel(source.RelativeVoxelIndex);
                var sourceDefinition = sourceVoxel.GetDefinition();

                foreach (var direction in LightDirections.All)
                {
                    var targetIndex = source.RelativeVoxelIndex + direction.Step;
                    var targetVoxel = source.Chunk.GetLocalWithNeighborChunk(targetIndex.X, targetIndex.Y, targetIndex.Z, out var targetAddress, out var isTargetExists);
                    if (isTargetExists)
                    {
                        var targetDefinition = targetVoxel.GetDefinition();

                        var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, direction.Direction);
                        if (brightnessLoss != 0)
                        {
                            if (targetVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss || targetVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                            {
                                // todo: talán ha nem írnánk és olvasnánk ugyanazt a memóriát hétezerszer észnélkül az segítene... csak talán...
                                if (targetVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss)
                                {
                                    targetAddress.Chunk.SetVoxel(targetAddress.RelativeVoxelIndex, targetVoxel.SetSunlight((byte)(sourceVoxel.Sunlight - brightnessLoss)));
                                }

                                if (targetVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                                {
                                    targetAddress.Chunk.SetVoxel(targetAddress.RelativeVoxelIndex, targetVoxel.SetCustomLight((byte)(sourceVoxel.EmittedLight - brightnessLoss)));
                                }

                                queue.Enqueue(new VoxelChunkAddress(targetAddress.Chunk, targetAddress.RelativeVoxelIndex));
                            }
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
                var index = chunk.FromFlatIndex(lightSourceFlatIndex);

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
                                var neighborFlatIndex = chunk.GetFlatIndex(neighborX, neighborY, neighborZ);
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

        public static List<Tuple<Chunk, Int3>> GlobalPropagateDarkness(Queue<DarknessPropogationRecord> queue)
        {
            var brightSpots = new List<Tuple<Chunk, Int3>>();

            while (queue.Count > 0)
            {
                var source = queue.Dequeue();

                foreach (var direction in LightDirections.All)
                {
                    var targetVoxel = source.sourceChunk.GetLocalWithNeighborChunk(source.sourceIndex.X + direction.Step.X, source.sourceIndex.Y + direction.Step.Y, 
                        source.sourceIndex.Z + direction.Step.Z, out var targetAddress, out var isTargetExists);
                    if (isTargetExists)
                    {
                        var targetDefinition = targetVoxel.GetDefinition();
                        var brightnessLoss = VoxelDefinition.GetBrightnessLoss(source.NewVoxelDefinition, targetDefinition, direction.Direction);

                        if (brightnessLoss > 0)
                        {
                            if (targetVoxel.Sunlight >= source.OldVoxel.Sunlight )
                            {
                                brightSpots.Add(new Tuple<Chunk, Int3>(targetAddress.Chunk, targetAddress.RelativeVoxelIndex));
                            }
                            else  if (targetVoxel.Sunlight > 0)
                            {
                                var targetNewVoxel = targetVoxel.SetSunlight(0);
                                targetAddress.Chunk.SetVoxel(targetAddress.RelativeVoxelIndex, targetNewVoxel);
                                queue.Enqueue(new DarknessPropogationRecord(targetAddress.Chunk, targetAddress.RelativeVoxelIndex, targetNewVoxel, targetDefinition, 
                                    targetVoxel, targetDefinition));
                            }


                            if (targetVoxel.EmittedLight >= source.OldVoxel.EmittedLight)
                            {
                                brightSpots.Add(new Tuple<Chunk, Int3>(targetAddress.Chunk, targetAddress.RelativeVoxelIndex));
                            }
                            else if (targetVoxel.EmittedLight > 0)
                            {
                                var targetNewVoxel = targetVoxel.SetCustomLight(0);
                                targetAddress.Chunk.SetVoxel(targetAddress.RelativeVoxelIndex, targetNewVoxel);
                                queue.Enqueue(new DarknessPropogationRecord(targetAddress.Chunk, targetAddress.RelativeVoxelIndex, targetNewVoxel, targetDefinition, 
                                    targetVoxel, targetDefinition ));
                            }
                        }
                    }
                }
            }

            return brightSpots;
        }

        public static IEnumerable<Int3> Sunlight(Chunk chunk, Int3 relativeIndex, byte toLightness)
        {
            for (var j = relativeIndex.Y - 1; j > 0; j--)
            {
                var flatIndex = chunk.GetFlatIndex(relativeIndex.X, j, relativeIndex.Z);
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
            //public List<Tuple<Chunk, Int3>> lightSources;
            public Voxel NewVoxel;
            public VoxelDefinition NewVoxelDefinition;

            public Voxel OldVoxel;
            public VoxelDefinition OldVoxelDefinition;

            public DarknessPropogationRecord(Chunk sourceChunk, Int3 sourceIndex, Voxel newVoxel, VoxelDefinition newVoxelDefinition, Voxel oldVoxel, VoxelDefinition oldVoxelDefinition)
            {
                this.sourceChunk = sourceChunk;
                this.sourceIndex = sourceIndex;
                //this.lightSources = lightSources;
                
                NewVoxel = newVoxel;
                NewVoxelDefinition = newVoxelDefinition;

                OldVoxel = oldVoxel;
                OldVoxelDefinition = oldVoxelDefinition;
            }
        }
    }
}