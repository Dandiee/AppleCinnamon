using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public class VoxelRayCollisionResult
    {
        public Int3 AbsoluteVoxelIndex { get; }
        public Int3 Direction { get; }
        public VoxelDefinition Definition { get; }
        public Voxel Voxel { get; }
        public BoundingBox BoundingBox { get; }


        public VoxelRayCollisionResult(Int3 absoluteVoxelIndex, Int3 direction, VoxelDefinition definition, Voxel voxel)
        {
            AbsoluteVoxelIndex = absoluteVoxelIndex;
            Direction = direction;
            Definition = definition;
            Voxel = voxel;
            var position = absoluteVoxelIndex.ToVector3();
            var size = definition.Size / 2f * 1.01f;
            BoundingBox = new BoundingBox(position - size, position + size);
        }
    }
}