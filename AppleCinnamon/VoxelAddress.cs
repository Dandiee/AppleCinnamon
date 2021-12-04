using System.Drawing.Design;
using AppleCinnamon.System;
using Microsoft.Win32.SafeHandles;
using SharpDX;

namespace AppleCinnamon
{
    public struct VoxelAddress
    {
        public static readonly VoxelAddress Zero = new VoxelAddress();

        public Int2 ChunkIndex { get; }
        public Int3 RelativeVoxelIndex { get; }

        public VoxelAddress(Int2 chunkIndex, Int3 relativeVoxelIndex)
        {
            ChunkIndex = chunkIndex;
            RelativeVoxelIndex = relativeVoxelIndex;
        }
    }
}