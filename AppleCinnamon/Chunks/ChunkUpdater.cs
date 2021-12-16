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
                var newDefinition = VoxelDefinition.DefinitionByType[voxel];
                var newVoxel = newDefinition.HueFaces != VisibilityFlag.None
                    ? newDefinition.Create(2)
                    : newDefinition.Create();


                address.Chunk.SetSafe(address.RelativeVoxelIndex, newVoxel);

                UpdateVisibilityFlags(address.Chunk, oldVoxel, newVoxel, address.RelativeVoxelIndex);
                UpdateLighting(address.Chunk, address.RelativeVoxelIndex, oldVoxel, newVoxel);
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
                var neighborVoxel = chunk.GetLocalWithNeighborChunk(neighbor.X, neighbor.Y, neighbor.Z, out var neighborAddress, out var neighborExists);
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
                    if (neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction,
                        direction.OppositeDirection))
                    {
                        // and its not visible anymore
                        if (!neighborDefinition.IsFaceVisible(newDefinition, direction.Direction,
                            direction.OppositeDirection))
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
                        }
                    }
                    else
                    {
                        neighborAddress.Chunk.BuildingContext.VisibilityFlags[neighborIndex] =
                            neighborOldVisibilityFlag;
                    }
                }
            }

            if (newVisibilityFlag == VisibilityFlag.None)
            {
                if (hadVisibility)
                {
                    chunk.BuildingContext.VisibilityFlags.Remove(flatIndex);
                }
            }
            else
            {
                chunk.BuildingContext.VisibilityFlags[flatIndex] = newVisibilityFlag;
            }
        }

        private void UpdateLighting(Chunk chunk, Int3 relativeIndex, Voxel oldVoxel, Voxel newVoxel)
        {
            // locally darkening sunlight vertically
            var darknessSources = new Queue<LightingService.DarknessPropogationRecord>();
            var lowerVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y - 1, relativeIndex.Z);
            if (lowerVoxelIndex.Y > 0 && chunk.GetVoxel(lowerVoxelIndex).Sunlight == 15)
            {
                foreach (var sunlightRelativeIndex in LightingService.Sunlight(chunk, relativeIndex, 0))
                {
                    var voxel = chunk.GetVoxel(sunlightRelativeIndex);
                    var definition = voxel.GetDefinition();
                    darknessSources.Enqueue(new LightingService.DarknessPropogationRecord(chunk, sunlightRelativeIndex, voxel.SetSunlight(0),
                        definition, voxel.SetSunlight(15), definition));
                }
            }

            // globally propagate darkness
            darknessSources.Enqueue(new LightingService.DarknessPropogationRecord(chunk, relativeIndex, newVoxel, newVoxel.GetDefinition(), oldVoxel,
                oldVoxel.GetDefinition()));
            var lightSources = LightingService.GlobalPropagateDarkness(darknessSources);

            // locally propagate sunlight vertically
            var upperVoxelIndex = new Int3(relativeIndex.X, relativeIndex.Y + 1, relativeIndex.Z);
            if (upperVoxelIndex.Y < chunk.CurrentHeight && chunk.GetVoxel(upperVoxelIndex).Sunlight == 15)
            {
                foreach (var sunlightSources in LightingService.Sunlight(chunk, upperVoxelIndex, 15))
                {
                    lightSources.Add(new Tuple<Chunk, Int3>(chunk, sunlightSources));
                }
            }

            // enqueue the new voxel itself - as an emitter it could be a light source as well
            lightSources.Add(new Tuple<Chunk, Int3>(chunk, relativeIndex));

            // globally propagate lightness
            foreach (var lightSource in lightSources)
            {
                LightingService.GlobalPropagate(lightSource.Item1, lightSource.Item2);
            }
        }
    }
}
