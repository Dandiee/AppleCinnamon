using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Collision
{
    public class VoxelRayCollisionResult
    {
        public Int3 AbsoluteVoxelIndex { get; }
        public Int3 Direction { get; }
        public VoxelDefinition Definition { get; }


        public VoxelRayCollisionResult(Int3 absoluteVoxelIndex, Int3 direction, VoxelDefinition definition)
        {
            AbsoluteVoxelIndex = absoluteVoxelIndex;
            Direction = direction;
            Definition = definition;
        }
    }
}