namespace AppleCinnamon.Extensions
{
    public static class VoxelAddressExtensions
    {
        public static void SetVoxel(this VoxelChunkAddress address, Voxel voxel)
        {
            address.Chunk.SetSafe(address.RelativeVoxelIndex, voxel);
        }
    }
}
