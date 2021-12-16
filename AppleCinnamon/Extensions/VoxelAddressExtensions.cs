using AppleCinnamon.Helper;

namespace AppleCinnamon.Extensions
{
    public static class VoxelAddressExtensions
    {
        public static void SetVoxel(this VoxelChunkAddress address, Chunk chunk, Voxel voxel)
        {
            var targetFlatIndex = Help.GetFlatIndex(address.RelativeVoxelIndex, address.Chunk.CurrentHeight);
            address.Chunk.SetSafe(targetFlatIndex, voxel);
        }
    }
}
