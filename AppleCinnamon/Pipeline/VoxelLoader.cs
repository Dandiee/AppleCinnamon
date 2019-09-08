using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public interface IVoxelLoader
    {
        Voxel[] GetVoxels(Int2 chunkIndex);
    }

    public sealed class VoxelLoader : IVoxelLoader
    {
        private readonly IVoxelGenerator _voxelGenerator;

        public VoxelLoader(int seed)
        {
            _voxelGenerator = new VoxelGenerator(seed);
        }

        public Voxel[] GetVoxels(Int2 chunkIndex)
        {
            return _voxelGenerator.GenerateVoxels(chunkIndex);
        }
    }
}
