using System;
using System.Collections.Generic;
using AppleCinnamon.Chunks;
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

    public sealed class VoxelChunkAddressComparer : IEqualityComparer<VoxelChunkAddress>
    {
        public static readonly VoxelChunkAddressComparer Default = new();

        public bool Equals(VoxelChunkAddress x, VoxelChunkAddress y)
        {
            return Equals(x.Chunk, y.Chunk) && x.RelativeVoxelIndex.Equals(y.RelativeVoxelIndex);
        }

        public int GetHashCode(VoxelChunkAddress obj)
        {
            return HashCode.Combine(obj.Chunk, obj.RelativeVoxelIndex);
        }
    }
}