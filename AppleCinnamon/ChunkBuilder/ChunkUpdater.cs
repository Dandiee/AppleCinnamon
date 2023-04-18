using System.Collections.Generic;
using AppleCinnamon.Common;
using AppleCinnamon.Options;
using SharpDX;
using SharpDX.Direct3D11;

namespace AppleCinnamon.ChunkBuilder
{
    public static class ChunkUpdater
    {
        public static void SetVoxel(VoxelChunkAddress address, byte voxel, Device device)
        {
            if (address.RelativeVoxelIndex.Y >= address.Chunk.CurrentHeight)
            {
                address.Chunk.ExtendUpward(address.RelativeVoxelIndex.Y);
            }

            var oldVoxel = address.Chunk.GetVoxel(address.RelativeVoxelIndex);
            var oldDefinition = oldVoxel.GetDefinition();
            var newDefinition = VoxelDefinition.DefinitionByType[voxel];
            var newVoxel = newDefinition.HueFaces != VisibilityFlag.None
                ? newDefinition.Create(2)
                : newDefinition.Create();

            if (oldDefinition.IsBlock || newDefinition.IsBlock)
            {
                var affectedChunks = address.Chunk.GetNeighborChunkIndexes(address.RelativeVoxelIndex.X, address.RelativeVoxelIndex.Z);
                foreach (var affectedChunk in affectedChunks)
                {
                    affectedChunk.BuildingContext.SetAllChanged();
                }
            }

            address.Chunk.SetSafe(address.RelativeVoxelIndex, newVoxel);

            UpdateVisibilityFlags(address.Chunk, oldVoxel, newVoxel, address.RelativeVoxelIndex);
            UpdateLighting(address, oldVoxel, newVoxel);

            foreach (var neighbor in address.Chunk.Neighbors)
            {
                ChunkDispatcher.BuildChunk(neighbor, device);
            }
        }

        private static void UpdateVisibilityFlags(Chunk chunk, Voxel oldVoxel, Voxel newVoxel, Int3 relativeIndex)
        {
            var flatIndex = chunk.GetFlatIndex(relativeIndex);
            var newDefinition = newVoxel.GetDefinition();
            var oldDefinition = oldVoxel.GetDefinition();

            var newVisibilityFlag = VisibilityFlag.None;
            var hadVisibility = chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out _);

            foreach (var direction in BlockUpdateDirection.All)
            {
                var neighbor = relativeIndex + direction.Step;
                var neighborExists = chunk.TryGetLocalWithNeighborChunk(neighbor.X, neighbor.Y, neighbor.Z, out var neighborAddress, out var neighborVoxel);
                var neighborDefinition = neighborVoxel.GetDefinition();

                // old one was visible
                if (oldDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection, direction.Direction))
                {
                    // but its not visible anymore
                    if (!newDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection, direction.Direction))
                    {
                        //chunk.BuildingContext.VisibilityFlags[flatIndex] ^= direction.Item2;
                        if (hadVisibility)
                        {
                            chunk.BuildingContext.Faces[(byte)direction.Face].VoxelCount--;
                        }
                    }
                }

                // old one wasnt visible
                if (!oldDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection, direction.Direction))
                {
                    // but its visible now
                    if (newDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection, direction.Direction))
                    {
                        newVisibilityFlag |= direction.Direction;
                        chunk.BuildingContext.Faces[(byte)direction.Face].VoxelCount++;
                    }
                }

                if (neighborExists)
                {
                    var neighborIndex = neighborAddress.Chunk.GetFlatIndex(neighborAddress.RelativeVoxelIndex);
                    var hadNeighborVisibility =
                        neighborAddress.Chunk.BuildingContext.VisibilityFlags.TryGetValue(neighborIndex,
                            out var neighborOldVisibilityFlag);

                    // neighbor was visible
                    if (neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction, direction.OppositeDirection))
                    {
                        // and its not visible anymore
                        if (!neighborDefinition.IsFaceVisible(newDefinition, direction.Direction, direction.OppositeDirection))
                        {
                            neighborOldVisibilityFlag ^= direction.OppositeDirection;
                            neighborAddress.Chunk.BuildingContext.Faces[(byte)direction.OppositeFace].VoxelCount--;
                        }
                    }

                    // neighbor wasnt visible
                    if (!neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction,
                        direction.OppositeDirection))
                    {
                        // but its visible now
                        if (neighborDefinition.IsFaceVisible(newDefinition, direction.Direction,
                            direction.OppositeDirection))
                        {
                            neighborOldVisibilityFlag |= direction.OppositeDirection;
                            neighborAddress.Chunk.BuildingContext.Faces[(byte)direction.OppositeFace].VoxelCount++;
                        }
                    }

                    if (neighborOldVisibilityFlag == VisibilityFlag.None)
                    {
                        if (hadNeighborVisibility)
                        {
                            neighborAddress.Chunk.BuildingContext.VisibilityFlags.Remove(neighborIndex);
                            neighborAddress.Chunk.BuildingContext.IsSolidChanged = true;
                        }
                    }
                    else
                    {
                        neighborAddress.Chunk.BuildingContext.VisibilityFlags[neighborIndex] = neighborOldVisibilityFlag;
                        neighborAddress.Chunk.BuildingContext.IsSolidChanged = true;
                    }
                }
            }

            if (newVisibilityFlag == VisibilityFlag.None)
            {
                if (hadVisibility)
                {
                    chunk.BuildingContext.VisibilityFlags.Remove(flatIndex);
                    chunk.BuildingContext.IsSolidChanged = true;
                }
            }
            else
            {
                chunk.BuildingContext.VisibilityFlags[flatIndex] = newVisibilityFlag;
                chunk.BuildingContext.IsSolidChanged = true;
            }
        }

        private static void UpdateLighting(VoxelChunkAddress address, Voxel oldVoxel, Voxel newVoxel)
        {
            // there are multiple steps which are generating dark spots
            var darknessSources = new Queue<DarknessSource>();

            // SUNLIGHT DARKENING: in case the lower voxel is/was under the sky, the sunlight must be deleted downward
            if (address.RelativeVoxelIndex.Y > 1 && address.Chunk.GetVoxel(address.RelativeVoxelIndex - Int3.UnitY).Sunlight == 15)
            {
                foreach (var sunlightRelativeIndex in LightingService.Sunlight(address, 0, true))
                {
                    darknessSources.Enqueue(new DarknessSource(new VoxelChunkAddress(address.Chunk, sunlightRelativeIndex), oldVoxel.SetSunlight(15)));
                }
            }

            // ROOT DARKENING: the root cause voxel always suspect of darkening
            darknessSources.Enqueue(new DarknessSource(address, oldVoxel));

            // PROPAGATE DARKNESS
            var lightSources = LightingService.GlobalPropagateDarkness(darknessSources);

            // SUNLIGHT PROPAGATION: in case the upper voxel is/was under the sky, the sunlight must be propagated
            var upperVoxelIndex = address.RelativeVoxelIndex + Int3.UnitY;
            if (upperVoxelIndex.Y < address.Chunk.CurrentHeight - 1 && address.Chunk.GetVoxel(upperVoxelIndex).Sunlight == 15)
            {
                foreach (var sunlightSources in LightingService.Sunlight(address.Chunk, upperVoxelIndex, 15, true))
                {
                    lightSources.Add(new VoxelChunkAddress(address.Chunk, sunlightSources));
                }
            }

            // NEIGHBOR SOURCES: in case a block was removed and was not under the sky, all the neighbors are potential light sources
            foreach (var neighborLightSourceAddress in LightingService.GetAllLightSourceNeighbor(address, newVoxel))
            {
                lightSources.Add(neighborLightSourceAddress);
            }

            // ROOT PROPAGATE: the root case voxel always suspect of propagation
            lightSources.Add(address);

            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource);
            }
        }
    }

    public readonly struct BlockUpdateDirection
    {
        private static readonly IReadOnlyDictionary<VisibilityFlag, Face> Mapping = new Dictionary<VisibilityFlag, Face>
        {
            [VisibilityFlag.Top] = Face.Top,
            [VisibilityFlag.Bottom] = Face.Bottom,
            [VisibilityFlag.Left] = Face.Left,
            [VisibilityFlag.Right] = Face.Right,
            [VisibilityFlag.Front] = Face.Front,
            [VisibilityFlag.Back] = Face.Back,
        };

        public readonly Int3 Step;
        public readonly VisibilityFlag Direction;
        public readonly VisibilityFlag OppositeDirection;
        public readonly Face Face;
        public readonly Face OppositeFace;

        private BlockUpdateDirection(Int3 step, VisibilityFlag direction, VisibilityFlag oppositeDirection)
        {
            Step = step;
            Direction = direction;
            OppositeDirection = oppositeDirection;
            Face = Mapping[direction];
            OppositeFace = Mapping[oppositeDirection];
        }

        public static BlockUpdateDirection[] All = {
            new (Int3.UnitY, VisibilityFlag.Top, VisibilityFlag.Bottom),
            new ( -Int3.UnitY, VisibilityFlag.Bottom, VisibilityFlag.Top),
            new (-Int3.UnitX, VisibilityFlag.Left, VisibilityFlag.Right),
            new (Int3.UnitX, VisibilityFlag.Right, VisibilityFlag.Left),
            new (-Int3.UnitZ, VisibilityFlag.Front, VisibilityFlag.Back),
            new (Int3.UnitZ, VisibilityFlag.Back, VisibilityFlag.Front)
        };
    }
}
