using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon
{
    public struct VoxelAddress
    {
        public static readonly VoxelAddress Zero = new();

        public Int2 ChunkIndex { get; }
        public Int3 RelativeVoxelIndex { get; }

        public VoxelAddress(Int2 chunkIndex, Int3 relativeVoxelIndex)
        {
            ChunkIndex = chunkIndex;
            RelativeVoxelIndex = relativeVoxelIndex;
        }
    }


    public readonly struct VoxelChunkAddress
    {
        public static readonly VoxelChunkAddress Zero = new();

        public readonly Chunk Chunk;
        public readonly Int3 RelativeVoxelIndex;

        public VoxelChunkAddress(Chunk chunk, Int3 relativeVoxelIndex)
        {
            Chunk = chunk;
            RelativeVoxelIndex = relativeVoxelIndex;
        }
    }
}