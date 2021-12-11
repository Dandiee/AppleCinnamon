using AppleCinnamon.Helper;

namespace AppleCinnamon.Pipeline
{
    public sealed class VoxelLoader
    {
        private readonly VoxelGenerator _voxelGenerator;

        public VoxelLoader(int seed)
        {
            _voxelGenerator = new VoxelGenerator(seed);
        }

        public Chunk GetVoxels(Int2 chunkIndex)
        {
            //return _voxelGenerator.GenerateVoxelsMock(chunkIndex);
            return _voxelGenerator.GenerateVoxels3D(chunkIndex);
            return _voxelGenerator.GenerateVoxels(chunkIndex);
        }
    }
}
