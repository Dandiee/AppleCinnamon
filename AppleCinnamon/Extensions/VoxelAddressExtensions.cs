using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Extensions
{
    public static class VoxelAddressExtensions
    {
        public static void SetVoxel(this VoxelAddress address, Chunk chunk, Voxel voxel)
        {
            var targetChunk = chunk.Neighbors[Help.GetChunkFlatIndex(address.ChunkIndex)];
            var targetFlatIndex = Help.GetFlatIndex(address.RelativeVoxelIndex, targetChunk.CurrentHeight);
            targetChunk.Voxels[targetFlatIndex] = voxel;
        }

        public static Voxel GetVoxel(this VoxelAddress address, Chunk chunk)
        {
            var targetChunk = chunk.Neighbors[Help.GetChunkFlatIndex(address.ChunkIndex)];
            var targetFlatIndex = Help.GetFlatIndex(address.RelativeVoxelIndex, targetChunk.CurrentHeight);
            return targetChunk.Voxels[targetFlatIndex];
        }
    }
}
