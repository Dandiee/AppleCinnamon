using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
    public sealed class ChunkUpdater
    {
        private bool _isUpdateInProgress;

        private readonly Graphics _graphics;
        private readonly ChunkManager _chunkManager;

        public ChunkUpdater(
            Graphics graphics,
            ChunkManager chunkManager)
        {
            _graphics = graphics;
            _chunkManager = chunkManager;
        }


        public void SetVoxel(Int3 absoluteIndex, byte voxel)
        {
            if (_isUpdateInProgress)
            {
                return;
            }

            if (_chunkManager.TryGetVoxelAddress(absoluteIndex, out var address))
            {
                _isUpdateInProgress = true;

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
                ChunkBuilder.BuildChunk(address.Chunk, _graphics.Device);

                Task.WaitAll(ChunkManager.GetSurroundingChunks(2).Select(chunkIndex =>
                {
                    if (chunkIndex != Int2.Zero && _chunkManager.TryGetChunk(chunkIndex + address.Chunk.ChunkIndex, out var chunkToReload))
                    {
                        return Task.Run(() => ChunkBuilder.BuildChunk(chunkToReload, _graphics.Device));
                    }
                    return Task.CompletedTask;

                }).ToArray());

                _isUpdateInProgress = false;
            }
        }

        private void UpdateVisibilityFlags(Chunk chunk, Voxel oldVoxel, Voxel newVoxel, Int3 relativeIndex)
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

        private void UpdateLighting(VoxelChunkAddress address, Voxel oldVoxel, Voxel newVoxel)
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
}
