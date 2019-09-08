using SharpDX;

namespace AppleCinnamon.Collision
{
    public class VoxelRayCollisionResult
    {
        public Int3 AbsoluteVoxelIndex { get; }
        public Int3 Direction { get; }

        public VoxelRayCollisionResult(Int3 absoluteVoxelIndex, Int3 direction)
        {
            AbsoluteVoxelIndex = absoluteVoxelIndex;
            Direction = direction;
        }
    }
}