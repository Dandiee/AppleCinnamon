using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon
{
    public interface IChunkUpdater
    {
        void SetVoxel(Int3 absoluteIndex, byte voxel);
    }

    public sealed class ChunkUpdater : IChunkUpdater
    {
        public static readonly Tuple<Int3, int>[] RemoveMapping =
        {
            new Tuple<Int3, int>(Int3.UnitY, 2),
            new Tuple<Int3, int>(-Int3.UnitY, 1),
            new Tuple<Int3, int>(-Int3.UnitX, 8),
            new Tuple<Int3, int>(Int3.UnitX, 4),
            new Tuple<Int3, int>(-Int3.UnitZ, 32),
            new Tuple<Int3, int>(Int3.UnitZ, 16)
        };

        public static readonly Dictionary<Int3, int> AddMapping = new Dictionary<Int3, int>
        {
            {Int3.UnitY, 1},
            {-Int3.UnitY, 2},
            {-Int3.UnitX, 4},
            {Int3.UnitX, 8},
            {-Int3.UnitZ, 16},
            { Int3.UnitZ, 32},
        };


        public static readonly Dictionary<Int3, Face> FaceMapping = new Dictionary<Int3, Face>
        {
            {Int3.UnitY, Face.Top},
            {-Int3.UnitY, Face.Bottom},
            {-Int3.UnitX, Face.Left},
            {Int3.UnitX, Face.Right},
            {-Int3.UnitZ, Face.Front},
            { Int3.UnitZ, Face.Back},
        };




        public static readonly Dictionary<Face, Face> OppositeMapping = new Dictionary<Face, Face>
        {
            {Face.Top, Face.Bottom},
            {Face.Bottom, Face.Top},
            {Face.Left, Face.Right},
            {Face.Right, Face.Left},
            {Face.Front, Face.Back},
            {Face.Back, Face.Front},
        };



        private bool _isUpdateInProgress;

        private readonly Graphics _graphics;
        private readonly IChunkManager _chunkManager;
        private readonly IChunkBuilder _chunkBuilder;
        private readonly ILightUpdater _lightUpdater;

        public ChunkUpdater(
            Graphics graphics,
            IChunkManager chunkManager)
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
                var oldVoxel = chunk.Voxels[flatIndex];
                var newVoxel = new Voxel(voxel, 0);
                chunk.Voxels[flatIndex] = newVoxel;

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
            var isRemoving = newVoxel.Block == 0;
            var newVisibilityFlag = 0;
            chunk.VisibilityFlags.TryGetValue(relativeIndex.ToFlatIndex(chunk.CurrentHeight), out var oldVisibilityFlag);

            foreach (var direction in RemoveMapping)
            {
                var neighbour = relativeIndex + direction.Item1;
                var neighbourVoxel =
                    chunk.GetLocalWithNeighbours(neighbour.X, neighbour.Y, neighbour.Z, out var neighbourAddress);
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbourVoxel.Block];
                var face = FaceMapping[direction.Item1];

                if (!neighbourDefinition.IsTransparent)
                {
                    var neighbourChunk = chunk.Neighbours[neighbourAddress.ChunkIndex];
                    var neighbourIndex = neighbourAddress.RelativeVoxelIndex.ToFlatIndex(neighbourChunk.CurrentHeight);
                    neighbourChunk.VisibilityFlags.TryGetValue(neighbourIndex, out var visibility);

                    if (isRemoving)
                    {
                        neighbourChunk.VisibilityFlags[neighbourIndex] = (byte) (visibility + direction.Item2);
                        neighbourChunk.VoxelCount[OppositeMapping[face]]++;
                    }
                    else
                    {
                        neighbourChunk.VisibilityFlags[neighbourIndex] = (byte)(visibility - direction.Item2);
                        neighbourChunk.VoxelCount[OppositeMapping[face]]--;
                    }
                }
                else
                {
                    newVisibilityFlag += AddMapping[direction.Item1];
                    chunk.VoxelCount[face]++;
                }
            }

            if (isRemoving)
            {
                foreach (var addMapping in AddMapping)
                {
                    if ((oldVisibilityFlag & addMapping.Value) == addMapping.Value) // the given face was visible so far
                    {
                        var face = FaceMapping[addMapping.Key];
                        chunk.VoxelCount[face]--;
                    }
                }

                chunk.VisibilityFlags.Remove(relativeIndex.ToFlatIndex(chunk.CurrentHeight));
            }
            else
            {
                chunk.VisibilityFlags[relativeIndex.ToFlatIndex(chunk.CurrentHeight)] = (byte)newVisibilityFlag;
            }

        }
    }
}
