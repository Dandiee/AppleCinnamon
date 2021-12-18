using System.Collections.Generic;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public static class LightingService
    {
        public static void GlobalPropagate(VoxelChunkAddress address) => GlobalPropagate(new Queue<VoxelChunkAddress>(new[] {address}));

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
                    var isTargetExists = source.Chunk.TryGetLocalWithNeighborChunk(source.RelativeVoxelIndex + direction.Step, out var targetAddress, out var targetVoxel);
                    if (isTargetExists)
                    {
                        var targetDefinition = targetVoxel.GetDefinition();

                        if (targetAddress.RelativeVoxelIndex == new Int3(0, 131, 1))
                        {

                        }


                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, direction.Direction);
                        if (brightnessLoss != 0)
                        {
                            if (targetVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss || targetVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                            {
                                // todo: talán ha nem írnánk és olvasnánk ugyanazt a memóriát hétezerszer észnélkül az segítene... csak talán...
                                if (targetVoxel.Sunlight < sourceVoxel.Sunlight - brightnessLoss)
                                {
                                    targetAddress.Chunk.SetVoxelTracking(targetAddress.RelativeVoxelIndex, targetVoxel.SetSunlight((byte)(sourceVoxel.Sunlight - brightnessLoss)), targetDefinition);
                                }

                                if (targetVoxel.EmittedLight < sourceVoxel.EmittedLight - brightnessLoss)
                                {
                                    targetAddress.Chunk.SetVoxelTracking(targetAddress.RelativeVoxelIndex, targetVoxel.SetCustomLight((byte)(sourceVoxel.EmittedLight - brightnessLoss)), targetDefinition);
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
                                        chunk.SetVoxel(neighborFlatIndex, neighborVoxel.SetCustomLight((byte)(sourceVoxel.EmittedLight - brightnessLoss)));
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


        public static HashSet<VoxelChunkAddress> GlobalPropagateDarkness(Queue<DarknessSource> queue)
        {
            var brightSpots = new HashSet<VoxelChunkAddress>(VoxelChunkAddressComparer.Default);

            while (queue.Count > 0)
            {
                var source = queue.Dequeue();

                foreach (var direction in LightDirections.All)
                {
                    if (source.Address.Chunk.TryGetLocalWithNeighborChunk(source.Address.RelativeVoxelIndex + direction.Step, out var targetAddress, out var targetVoxel))
                    {
                        if (VoxelDefinition.GetBrightnessLoss(source.OldVoxel.GetDefinition(), targetVoxel.GetDefinition(), direction.Direction) > 0)
                        {
                            var newTargetCompositeLight = targetVoxel.CompositeLight;
                            var isBrightSpot = false;

                            // Sunlight case
                            if (targetVoxel.Sunlight >= source.OldVoxel.Sunlight)
                            {
                                if (targetVoxel.Sunlight > 0)
                                {
                                    isBrightSpot = true;
                                }
                            }
                            else if (targetVoxel.Sunlight > 0)
                            {
                                newTargetCompositeLight &= 0b11110000;
                            }

                            // Emitted light case
                            if (targetVoxel.EmittedLight >= source.OldVoxel.EmittedLight)
                            {
                                if (targetVoxel.EmittedLight > 0)
                                {
                                    isBrightSpot = true;
                                }
                            }
                            else if (targetVoxel.EmittedLight > 0)
                            {
                                newTargetCompositeLight &= 0b00001111;
                            }


                            if (isBrightSpot)
                            {
                                brightSpots.Add(targetAddress);
                            }

                            if (newTargetCompositeLight != targetVoxel.CompositeLight)
                            {
                                targetAddress.Chunk.SetVoxelTracking(targetAddress.RelativeVoxelIndex, targetVoxel.SetCompositeLight(newTargetCompositeLight), targetVoxel.GetDefinition());
                                queue.Enqueue(new DarknessSource(targetAddress, targetVoxel));
                            }
                        }
                    }
                }
            }

            return brightSpots;
        }

        public static IEnumerable<Int3> Sunlight(VoxelChunkAddress address, byte toLightness, bool isChangeTracking = false)
         => Sunlight(address.Chunk, address.RelativeVoxelIndex, toLightness, isChangeTracking);

        public static IEnumerable<Int3> Sunlight(Chunk chunk, Int3 relativeIndex, byte toLightness, bool isChangeTracking = false)
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
                    if (isChangeTracking)
                    {
                        chunk.SetVoxelTracking(flatIndex, voxel.SetSunlight(toLightness), definition);
                    }
                    else
                    {
                        chunk.SetVoxel(flatIndex, voxel.SetSunlight(toLightness));
                    }

                    yield return new Int3(relativeIndex.X, j, relativeIndex.Z);
                }
                else break;
            }
        }

        public static IEnumerable<VoxelChunkAddress> GetAllLightSourceNeighbor(VoxelChunkAddress targetAddress, Voxel targetVoxel)
        {
            foreach (var direction in LightDirections.All)
            {
                var sourceIndex = targetAddress.RelativeVoxelIndex + direction.Step;
                if (targetAddress.Chunk.TryGetLocalWithNeighborChunk(sourceIndex, out var sourceAddress, out var sourceVoxel) && sourceVoxel.CompositeLight > 0)
                {
                    var sourceDefinition = sourceVoxel.GetDefinition();
                    var targetDefinition = targetVoxel.GetDefinition();
                    var brightnessLoss = VoxelDefinition.GetBrightnessLoss(sourceDefinition, targetDefinition, direction.Direction);
                    if (brightnessLoss > 0)
                    {
                        yield return sourceAddress;
                    }
                }
            }
        }

        
    }
}