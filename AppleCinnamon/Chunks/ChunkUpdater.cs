﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon
{
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

    public sealed class ChunkUpdater
    {
        private bool _isUpdateInProgress;

        private readonly ChunkManager _chunkManager;
        private readonly ChunkBuilder _chunkBuilder;
        private readonly LightUpdater _lightUpdater;

        public ChunkUpdater(
            Graphics graphics,
            ChunkManager chunkManager)
        {
            _chunkManager = chunkManager;
            _chunkBuilder = new ChunkBuilder(graphics.Device);
            _lightUpdater = new LightUpdater();
        }

        private static readonly Random _random = new(456);

        public static long a;
        public static long b;
        public void SetVoxel(Int3 absoluteIndex, byte voxel)
        {
            if (_isUpdateInProgress)
            {
                return;
            }

            var address = Chunk.GetVoxelAddress(absoluteIndex);
            if (address.HasValue && _chunkManager.TryGetChunk(address.Value.ChunkIndex, out var chunk))
            {
                _isUpdateInProgress = true;

                var sw = Stopwatch.StartNew();
                if (address.Value.RelativeVoxelIndex.Y >= chunk.CurrentHeight)
                {
                    chunk.ExtendUpward(address.Value.RelativeVoxelIndex.Y);
                }



                var flatIndex = address.Value.RelativeVoxelIndex.ToFlatIndex(chunk.CurrentHeight);
                var oldVoxel = chunk.GetVoxel(flatIndex);
                var newDefinition = VoxelDefinition.DefinitionByType[voxel];
                var newVoxel = newDefinition.HueFaces != VisibilityFlag.None
                    ? new Voxel(voxel, newDefinition.LightEmitting, (byte) _random.Next(1, 8))
                    : new Voxel(voxel, newDefinition.LightEmitting);


                chunk.SetVoxel(flatIndex, newVoxel);

                UpdateVisibilityFlags(chunk, oldVoxel, newVoxel, address.Value.RelativeVoxelIndex);
                UpdateSprites(chunk, oldVoxel, newVoxel, address.Value.RelativeVoxelIndex);
                _lightUpdater.UpdateLighting(chunk, address.Value.RelativeVoxelIndex, oldVoxel, newVoxel);
                _chunkBuilder.BuildChunk(chunk);
                sw.Stop();
                a += sw.ElapsedMilliseconds;

                sw.Restart();
                Task.WaitAll(ChunkManager.GetSurroundingChunks(2).Select(chunkIndex =>
                {
                    if (chunkIndex != Int2.Zero && _chunkManager.TryGetChunk(chunkIndex + chunk.ChunkIndex, out var chunkToReload))
                    {
                        return Task.Run(() => _chunkBuilder.BuildChunk(chunkToReload));
                    }

                    sw.Stop();
                    return Task.CompletedTask;

                }).ToArray());
                b += sw.ElapsedMilliseconds;
                

                _isUpdateInProgress = false;
            }
        }

        private void UpdateSprites(Chunk chunk, Voxel oldVoxel, Voxel newVoxel, Int3 relativeIndex)
        {
            var oldDefinition = VoxelDefinition.DefinitionByType[oldVoxel.Block];
            var newDefinition = VoxelDefinition.DefinitionByType[newVoxel.Block];
            var flatIndex = relativeIndex.ToFlatIndex(chunk.CurrentHeight);



            if (oldDefinition.IsSprite && !newDefinition.IsSprite)
            {
                if (oldDefinition.IsOriented)
                {
                    chunk.BuildingContext.SingleSidedSpriteBlocks.Remove(flatIndex);
                }
                else
                {
                    chunk.BuildingContext.SpriteBlocks.Remove(flatIndex);
                }
            }
            else if (!oldDefinition.IsSprite && newDefinition.IsSprite)
            {
                if (newDefinition.IsOriented)
                {
                    chunk.BuildingContext.SingleSidedSpriteBlocks.Add(flatIndex);
                }
                else
                {
                    chunk.BuildingContext.SpriteBlocks.Add(flatIndex);
                }
            }
        }

        private void UpdateVisibilityFlags(Chunk chunk, Voxel oldVoxel, Voxel newVoxel, Int3 relativeIndex)
        {
            var flatIndex = relativeIndex.ToFlatIndex(chunk.CurrentHeight);
            var newDefinition = VoxelDefinition.DefinitionByType[newVoxel.Block];
            var oldDefinition = VoxelDefinition.DefinitionByType[oldVoxel.Block];

            var newVisibilityFlag = VisibilityFlag.None;
            var hadVisibility = chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out var oldVisibilityFlag);

            foreach (var direction in BlockUpdateDirection.All)
            {
                var neighbor = relativeIndex + direction.Step;
                var neighborVoxel =
                    chunk.GetLocalWithneighbors(neighbor.X, neighbor.Y, neighbor.Z, out var neighborAddress);
                var neighborDefinition = VoxelDefinition.DefinitionByType[neighborVoxel.Block];

                var neighborChunk = chunk.Neighbors[Help.GetChunkFlatIndex(neighborAddress.ChunkIndex)];
                var neighborIndex = neighborAddress.RelativeVoxelIndex.ToFlatIndex(neighborChunk.CurrentHeight);



                // old one was visible
                if (oldDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection, direction.Direction))
                {
                    // but its not visible anymore
                    if (!newDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection, direction.Direction))
                    {
                        //chunk.BuildingContext.VisibilityFlags[flatIndex] ^= direction.Item2;
                        if (hadVisibility)
                        {
                            chunk.BuildingContext.Faces[(byte) direction.Face].VoxelCount--;
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

                var hadNeighborVisibility = neighborChunk.BuildingContext.VisibilityFlags.TryGetValue(neighborIndex, out var neighborOldVisibilityFlag);


                // neighbor was visible

                if (neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction, direction.OppositeDirection))
                {
                    // and its not visible anymore
                    if (!neighborDefinition.IsFaceVisible(newDefinition, direction.Direction, direction.OppositeDirection))
                    {
                        neighborOldVisibilityFlag ^= direction.OppositeDirection;
                        neighborChunk.BuildingContext.Faces[(byte)direction.OppositeFace].VoxelCount--;
                    }
                }

                // neighbor wasnt visible

                if (!neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction, direction.OppositeDirection))
                {
                    // but its visible now
                    if (neighborDefinition.IsFaceVisible(newDefinition, direction.Direction, direction.OppositeDirection))
                    {
                        neighborOldVisibilityFlag |= direction.OppositeDirection;
                        neighborChunk.BuildingContext.Faces[(byte)direction.OppositeFace].VoxelCount++;
                    }
                }

                if (neighborOldVisibilityFlag == VisibilityFlag.None)
                {
                    if (hadNeighborVisibility)
                    {
                        neighborChunk.BuildingContext.VisibilityFlags.Remove(neighborIndex);
                    }
                }
                else
                {
                    neighborChunk.BuildingContext.VisibilityFlags[neighborIndex] = neighborOldVisibilityFlag;
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
    }
}