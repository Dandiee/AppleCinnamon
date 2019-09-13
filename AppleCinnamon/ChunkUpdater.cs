using System.Linq;
using System.Threading.Tasks;
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
    }
}
