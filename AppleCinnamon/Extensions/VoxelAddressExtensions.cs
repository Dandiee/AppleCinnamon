using AppleCinnamon.Helper;

namespace AppleCinnamon.Extensions
{
    public static class VoxelAddressExtensions
    {
        public static void SetVoxel(this VoxelAddress address, Chunk chunk, Voxel voxel)
        {
            var targetChunk = chunk.Neighbors[Help.GetChunkFlatIndex(address.ChunkIndex)];
            var targetFlatIndex = Help.GetFlatIndex(address.RelativeVoxelIndex, targetChunk.CurrentHeight);
            targetChunk.SetSafe(targetFlatIndex, voxel);
        }
    }
}
