using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AppleCinnamon.Pipeline;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
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
        public static readonly Tuple<Int3, VisibilityFlag>[] RemoveMapping =
        {
            new(Int3.UnitY, VisibilityFlag.Bottom),
            new(-Int3.UnitY, VisibilityFlag.Top),
            new(-Int3.UnitX, VisibilityFlag.Right),
            new(Int3.UnitX, VisibilityFlag.Left),
            new(-Int3.UnitZ, VisibilityFlag.Back),
            new(Int3.UnitZ, VisibilityFlag.Front)
        };

        public static readonly Dictionary<Int3, VisibilityFlag> AddMapping = new()
        {
            { Int3.UnitY, VisibilityFlag.Top },
            { -Int3.UnitY, VisibilityFlag.Bottom },
            { -Int3.UnitX, VisibilityFlag.Left },
            { Int3.UnitX, VisibilityFlag.Right },
            { -Int3.UnitZ, VisibilityFlag.Front },
            { Int3.UnitZ, VisibilityFlag.Back },
        };


        public static readonly Dictionary<Int3, Face> FaceMapping = new()
        {
            { Int3.UnitY, Face.Top },
            { -Int3.UnitY, Face.Bottom },
            { -Int3.UnitX, Face.Left },
            { Int3.UnitX, Face.Right },
            { -Int3.UnitZ, Face.Front },
            { Int3.UnitZ, Face.Back },
        };




        public static readonly Dictionary<Face, Face> OppositeMapping = new()
        {
            { Face.Top, Face.Bottom },
            { Face.Bottom, Face.Top },
            { Face.Left, Face.Right },
            { Face.Right, Face.Left },
            { Face.Front, Face.Back },
            { Face.Back, Face.Front },
        };



        private bool _isUpdateInProgress;

        private readonly Graphics _graphics;
        private readonly ChunkManager _chunkManager;
        private readonly ChunkBuilder _chunkBuilder;
        private readonly LightUpdater _lightUpdater;

        public ChunkUpdater(
            Graphics graphics,
            ChunkManager chunkManager)
        {
            _graphics = graphics;
            _chunkManager = chunkManager;
            _chunkBuilder = new ChunkBuilder();
            _lightUpdater = new LightUpdater();
        }

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

                if (address.Value.RelativeVoxelIndex.Y >= chunk.CurrentHeight)
                {
                    chunk.ExtendUpward(address.Value.RelativeVoxelIndex.Y);
                }

                var flatIndex = address.Value.RelativeVoxelIndex.ToFlatIndex(chunk.CurrentHeight);
                var oldVoxel = chunk.GetVoxel(flatIndex);
                var newVoxel = new Voxel(voxel, VoxelDefinition.DefinitionByType[voxel].LightEmitting);
                chunk.SetVoxel(flatIndex, newVoxel);

                UpdateVisibilityFlags(chunk, oldVoxel, newVoxel, address.Value.RelativeVoxelIndex);
                _lightUpdater.UpdateLighting(chunk, address.Value.RelativeVoxelIndex, oldVoxel, newVoxel);
                _chunkBuilder.BuildChunk(_graphics.Device, chunk);

                Task.WaitAll(ChunkManager.GetSurroundingChunks(2).Select(chunkIndex =>
                {
                    if (chunkIndex != Int2.Zero &&
                        _chunkManager.TryGetChunk(chunkIndex + chunk.ChunkIndex, out var chunkToReload))
                    {
                        return Task.Run(() => _chunkBuilder.BuildChunk(_graphics.Device, chunkToReload));
                    }

                    return Task.CompletedTask;
                }).ToArray());


                _isUpdateInProgress = false;
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

                var neighborChunk = chunk.neighbors2[Help.GetChunkFlatIndex(neighborAddress.ChunkIndex)];
                var neighborIndex = neighborAddress.RelativeVoxelIndex.ToFlatIndex(neighborChunk.CurrentHeight);



                // old one was visible
                if (oldDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection))
                {
                    // but its not visible anymore
                    if (!newDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection))
                    {
                        //chunk.BuildingContext.VisibilityFlags[flatIndex] ^= direction.Item2;
                        if (hadVisibility)
                        {
                            chunk.BuildingContext.Faces[(byte) direction.Face].VoxelCount--;
                        }
                    }
                }

                // old one wasnt visible
                if (!oldDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection))
                {
                    // but its visible now
                    if (newDefinition.IsFaceVisible(neighborDefinition, direction.OppositeDirection))
                    {
                        newVisibilityFlag |= direction.Direction;
                        chunk.BuildingContext.Faces[(byte)direction.Face].VoxelCount++;
                    }
                }

                var hadNeighborVisibility = neighborChunk.BuildingContext.VisibilityFlags.TryGetValue(neighborIndex, out var neighborOldVisibilityFlag);


                // neighbor was visible

                if (neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction))
                {
                    // and its not visible anymore
                    if (!neighborDefinition.IsFaceVisible(newDefinition, direction.Direction))
                    {
                        neighborOldVisibilityFlag ^= direction.OppositeDirection;
                        neighborChunk.BuildingContext.Faces[(byte)direction.OppositeFace].VoxelCount--;
                    }
                }

                // neighbor wasnt visible

                if (!neighborDefinition.IsFaceVisible(oldDefinition, direction.Direction))
                {
                    // but its visible now
                    if (neighborDefinition.IsFaceVisible(newDefinition, direction.Direction))
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
