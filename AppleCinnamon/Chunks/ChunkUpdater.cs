﻿using System;
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
                var newDefinition = VoxelDefinition.DefinitionByType[voxel];
                var newVoxel = newDefinition.HueFaces != VisibilityFlag.None
                    ? newDefinition.Create(2)
                    : newDefinition.Create();


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
            // locally darkening sunlight vertically
            var darknessSources = new Queue<DarknessSource>();
            if (address.RelativeVoxelIndex.Y > 1 && address.Chunk.GetVoxel(address.RelativeVoxelIndex - Int3.UnitY).Sunlight == 15)
            {
                foreach (var sunlightRelativeIndex in LightingService.Sunlight(address, 0, true))
                {
                    darknessSources.Enqueue(new DarknessSource(new VoxelChunkAddress(address.Chunk, sunlightRelativeIndex), oldVoxel.SetSunlight(15)));
                }
            }

            // globally propagate darkness 
            darknessSources.Enqueue(new DarknessSource(address, oldVoxel));
            var lightSources = LightingService.GlobalPropagateDarkness(darknessSources);

            // locally propagate sunlight vertically
            var upperVoxelIndex = address.RelativeVoxelIndex + Int3.UnitY;
            if (upperVoxelIndex.Y < address.Chunk.CurrentHeight - 1 && address.Chunk.GetVoxel(upperVoxelIndex).Sunlight == 15)
            {
                foreach (var sunlightSources in LightingService.Sunlight(address.Chunk, upperVoxelIndex, 15, true))
                {
                    lightSources.Add(new VoxelChunkAddress(address.Chunk, sunlightSources));
                }
            }

            // add all beighbor as target
            foreach (var neighborLightSourceAddress in LightingService.GetAllLightSourceNeighbor(address, newVoxel))
            {
                lightSources.Add(neighborLightSourceAddress);
            }

            // enqueue the new voxel itself - as an emitter it could be a light source as well
            lightSources.Add(address);

            // globally propagate lightness
            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource);
            }
        }
    }
}
