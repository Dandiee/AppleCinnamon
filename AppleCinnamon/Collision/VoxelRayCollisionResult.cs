using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public class VoxelRayCollisionResult
    {
        public Int3 AbsoluteVoxelIndex { get; }
        public Int3 Direction { get; }
        public VoxelDefinition Definition { get; }
        public Voxel Voxel { get; }


        public VoxelRayCollisionResult(Int3 absoluteVoxelIndex, Int3 direction, VoxelDefinition definition, Voxel voxel)
        {
            AbsoluteVoxelIndex = absoluteVoxelIndex;
            Direction = direction;
            Definition = definition;
            Voxel = voxel;
        }
    }
}