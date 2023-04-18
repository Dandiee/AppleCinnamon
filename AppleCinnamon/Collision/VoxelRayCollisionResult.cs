using AppleCinnamon.Extensions;
using AppleCinnamon.Options;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public sealed class VoxelRayCollisionResult
    {
        public Int3 AbsoluteVoxelIndex { get; }
        public VoxelChunkAddress Address { get; }
        public Int3 Direction { get; }
        public VoxelDefinition Definition { get; }
        public Voxel Voxel { get; }
        public BoundingBox BoundingBox { get; }


        public VoxelRayCollisionResult(Int3 absoluteVoxelIndex, VoxelChunkAddress address, Int3 direction, VoxelDefinition definition, Voxel voxel)
        {
            AbsoluteVoxelIndex = absoluteVoxelIndex;
            Address = address;
            Direction = direction;
            Definition = definition;
            Voxel = voxel;
            var position = absoluteVoxelIndex.ToVector3() + definition.Offset;
            
            var size = definition.Size/2 + new Vector3(.005f);
            BoundingBox = new BoundingBox(position - size, position + size);
        }
    }
}