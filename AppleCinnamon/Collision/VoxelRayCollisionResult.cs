using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public sealed class VoxelRayCollisionResult
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
            var position = absoluteVoxelIndex.ToVector3() + definition.Offset;
            
            var size = definition.Size/2 + new Vector3(.005f);
            BoundingBox = new BoundingBox(position - size, position + size);
        }
    }
}