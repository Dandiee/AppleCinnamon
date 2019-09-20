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

                var oldVoxel = chunk.GetLocalVoxel(address.Value.RelativeVoxelIndex);
                var newVoxel = new Voxel(voxel, 0);
                chunk.SetLocalVoxel(address.Value.RelativeVoxelIndex, newVoxel);
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

            foreach (var direction in RemoveMapping)
            {
                var neighbour = relativeIndex + direction.Item1;
                var neighbourVoxel =
                    chunk.GetLocalWithNeighbours(neighbour.X, neighbour.Y, neighbour.Z, out var neighbourAddress);
                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbourVoxel.Block];

                if (!neighbourDefinition.IsTransparent)
                {
                    var neighbourChunk = chunk.Neighbours[neighbourAddress.ChunkIndex];
                    var neighbourIndex = neighbourAddress.RelativeVoxelIndex.ToIndex();
                    neighbourChunk.VisibilityFlags.TryGetValue(neighbourIndex, out var visibility);
                    neighbourChunk.VisibilityFlags[neighbourIndex] = isRemoving
                        ? new VoxelVisibility((byte)(visibility.VisibilityFlags + direction.Item2), 0)
                        : new VoxelVisibility((byte)(visibility.VisibilityFlags - direction.Item2), 0);
                }
                else
                {
                    newVisibilityFlag += AddMapping[direction.Item1];
                }
            }

            if (isRemoving)
            {
                chunk.VisibilityFlags.Remove(relativeIndex.ToIndex());
            }
            else
            {
                chunk.VisibilityFlags[relativeIndex.ToIndex()] = new VoxelVisibility((byte)newVisibilityFlag, 0);
            }

        }
    }
}
