using SharpDX;

namespace AppleCinnamon
{
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