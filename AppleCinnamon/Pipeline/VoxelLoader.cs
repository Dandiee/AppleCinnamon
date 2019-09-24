using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class VoxelLoader
    {
        private readonly VoxelGenerator _voxelGenerator;

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
