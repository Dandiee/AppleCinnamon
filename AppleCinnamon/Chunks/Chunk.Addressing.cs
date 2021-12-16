using System.Runtime.CompilerServices;
using SharpDX;

namespace AppleCinnamon
{
    public partial class Chunk
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Voxel GetVoxel(int flatIndex) => Voxels[flatIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Voxel GetVoxel(Int3 ijk) => Voxels[GetFlatIndex(ijk.X, ijk.Y, ijk.Z, CurrentHeight)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Voxel GetVoxel(int i, int j, int k) => Voxels[GetFlatIndex(i, j, k, CurrentHeight)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(int flatIndex, Voxel voxel) => Voxels[flatIndex] = voxel;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(Int3 ijk, Voxel voxel) => Voxels[GetFlatIndex(ijk.X, ijk.Y, ijk.Z, CurrentHeight)] = voxel;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(int i, int j, int k, Voxel voxel) => Voxels[GetFlatIndex(i, j, k, CurrentHeight)] = voxel;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetFlatIndex(int i, int j, int k) => GetFlatIndex(i, j, k, CurrentHeight);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetFlatIndex(Int3 ijk) => GetFlatIndex(ijk.X, ijk.Y, ijk.Z, CurrentHeight);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int GetFlatIndex(int i, int j, int k, int height) => i + SizeXy * (j + height * k);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int3 FromFlatIndex(int flatIndex, int height)
        {
            var k = flatIndex / (SizeXy * height);
            var j = (flatIndex - k * SizeXy * height) / SizeXy;
            var i = flatIndex - (k * SizeXy * height + j * SizeXy);

            return new Int3(i, j, k);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 FromFlatIndex(int flatIndex) => FromFlatIndex(flatIndex, CurrentHeight);

        public Voxel GetLocalWithNeighborChunk(int i, int j, int k, out VoxelChunkAddress address, out bool isExists)
        {
            if (j < 0)
            {
                address = VoxelChunkAddress.Zero;
                isExists = false;
                return Voxel.Bedrock;
            }

            if (j >= CurrentHeight)
            {
                address = VoxelChunkAddress.Zero;
                isExists = false;
                return Voxel.SunBlock;
            }

            var cx = (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy)));
            var cy = (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy)));

            var chunk = GetNeighbor(cx, cy);
            address = new VoxelChunkAddress(chunk, new Int3(i & (SizeXy - 1), j, k & (SizeXy - 1)));
            isExists = true;
            return chunk.CurrentHeight <= j
                ? Voxel.SunBlock
                : chunk.GetVoxel(address.RelativeVoxelIndex.X, j, address.RelativeVoxelIndex.Z);
        }

        public VoxelChunkAddress GetAddressChunk(Int3 ijk) => GetAddressChunk(ijk.X, ijk.Y, ijk.Z);
        public VoxelChunkAddress GetAddressChunk(int i, int j, int k)
        {
            var cx = (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy)));
            var cy = (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy)));
            return new VoxelChunkAddress(GetNeighbor(cx, cy), new Int3(i & (SizeXy - 1), j, k & (SizeXy - 1)));
        }

        [InlineMethod.Inline]
        public Voxel GetLocalWithNeighbor(int i, int j, int k)
        {
            if (j < 0)
            {
                return Voxel.Bedrock;
            }

            var chunk = GetNeighbor(
                (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy))),
                (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy))));

            return chunk.CurrentHeight <= j
                ? Voxel.SunBlock
                : chunk.GetVoxel(i & (SizeXy - 1), j, k & (SizeXy - 1));
        }

        public static int GetChunkFlatIndex(int i, int j) => 3 * i + j + 4;
        public Chunk GetNeighbor(int i, int j) => Neighbors[GetChunkFlatIndex(i, j)];
        public void SetNeighbor(int i, int j, Chunk neighborChunk) => Neighbors[GetChunkFlatIndex(i, j)] = neighborChunk;

        private static int ConvertFlatIndex(int originalFlatIndex, int originalHeight, int targetHeight)
        {
            var oldIndex = FromFlatIndex(originalFlatIndex, originalHeight);
            var newFlatIndex = GetFlatIndex(oldIndex.X, oldIndex.Y, oldIndex.Z, targetHeight);
            return newFlatIndex;
        }

        public void SetSafe(int i, int j, int k, Voxel newVoxel) => SetSafe(GetFlatIndex(i, j, k), newVoxel);
        public void SetSafe(Int3 index, Voxel newVoxel) => SetSafe(index.X, index.Y, index.Z, newVoxel);
        public void SetSafe(int flatIndex, Voxel newVoxel)
        {
            var oldVoxel = Voxels[flatIndex];
            var oldVoxelDefinition = oldVoxel.GetDefinition();
            if (oldVoxelDefinition.IsSprite)
            {
                if (oldVoxelDefinition.IsOriented) BuildingContext.SingleSidedSpriteBlocks.Remove(flatIndex);
                else BuildingContext.SpriteBlocks.Remove(flatIndex);
            }

            var newVoxelDefinition = newVoxel.GetDefinition();
            if (newVoxelDefinition.IsSprite)
            {
                if (newVoxelDefinition.IsOriented) BuildingContext.SingleSidedSpriteBlocks.Add(flatIndex);
                else BuildingContext.SpriteBlocks.Add(flatIndex);
            }

            Voxels[flatIndex] = newVoxel;
        }
    }
}
