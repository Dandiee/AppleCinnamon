using AppleCinnamon.System;
using SharpDX;

namespace AppleCinnamon
{
    public struct VoxelAddress
    {
        public Int2 ChunkIndex { get; }
        public Int3 RelativeVoxelIndex { get; }

        public VoxelAddress(Int2 chunkIndex, Int3 relativeVoxelIndex)
        {
            ChunkIndex = chunkIndex;
            RelativeVoxelIndex = relativeVoxelIndex;
        }
    }
}