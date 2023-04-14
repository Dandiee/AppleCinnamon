using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AppleCinnamon.Settings;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int GetFlatIndex(int i, int j, int k, int height) => i + WorldSettings.ChunkSize * (j + height * k);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int3 FromFlatIndex(int flatIndex, int height)
        {
            var k = flatIndex / (WorldSettings.ChunkSize * height);
            var j = (flatIndex - k * WorldSettings.ChunkSize * height) / WorldSettings.ChunkSize;
            var i = flatIndex - (k * WorldSettings.ChunkSize * height + j * WorldSettings.ChunkSize);

            return new Int3(i, j, k);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Int3 FromFlatIndex(int flatIndex) => FromFlatIndex(flatIndex, CurrentHeight);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void UpdateVoxelLighting(Int3 ijk, Voxel voxel, VoxelDefinition definition) => UpdateVoxelLighting(ijk.X, ijk.Y, ijk.Z, voxel, definition);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateVoxelLighting(int i, int j, int k, Voxel voxel, VoxelDefinition definition)
        {
            BuildingContext.SetAllChanged();

            foreach (var chunk in GetNeighborChunkIndexes(i, k))
                chunk.BuildingContext.SetAllChanged();


            SetVoxel(i, j, k, voxel);
        }

        public IEnumerable<Chunk> GetNeighborChunkIndexes(int i, int j)
        {
            if (i == 0)
                yield return GetNeighbor(-1, 0);
            if (i == WorldSettings.ChunkSize - 1)
                yield return GetNeighbor(1, 0);
            if (j == 0)
                yield return GetNeighbor(0, -1);
            if (j == WorldSettings.ChunkSize - 1)
                yield return GetNeighbor(0, 1);

            if (i == 0 && j == 0)
                yield return GetNeighbor(-1, -1);
            if (i == WorldSettings.ChunkSize - 1 && j == WorldSettings.ChunkSize - 1)
                yield return GetNeighbor(1, 1);
            if (i == 0 && j == WorldSettings.ChunkSize - 1)
                yield return GetNeighbor(-1, 1);
            if (i == WorldSettings.ChunkSize - 1 && j == 0)
                yield return GetNeighbor(1, -1);
        }

        public bool TryGetLocalWithNeighborChunk(Int3 ijk, out VoxelChunkAddress address, out Voxel voxel) => TryGetLocalWithNeighborChunk(ijk.X, ijk.Y, ijk.Z, out address, out voxel);
        public bool TryGetLocalWithNeighborChunk(int i, int j, int k, out VoxelChunkAddress address, out Voxel voxel)
        {
            if (j < 0)
            {
                address = VoxelChunkAddress.Zero;
                voxel = Voxel.Bedrock;

                return false;
            }

            var cx = (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / WorldSettings.ChunkSize)));
            var cy = (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / WorldSettings.ChunkSize)));

            var chunk = GetNeighbor(cx, cy);
            if (chunk.CurrentHeight <= j)
            {
                address = VoxelChunkAddress.Zero;
                voxel = Voxel.SunBlock;

                return false;
            }
            else
            {
                address = new VoxelChunkAddress(chunk, new Int3(i & (WorldSettings.ChunkSize - 1), j, k & (WorldSettings.ChunkSize - 1)));
                voxel = chunk.GetVoxel(address.RelativeVoxelIndex.X, j, address.RelativeVoxelIndex.Z);

                return true;
            }
        }

        public VoxelChunkAddress GetAddressChunk(Int3 ijk) => GetAddressChunk(ijk.X, ijk.Y, ijk.Z);
        public VoxelChunkAddress GetAddressChunk(int i, int j, int k)
        {
            var cx = (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / WorldSettings.ChunkSize)));
            var cy = (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / WorldSettings.ChunkSize)));
            return new VoxelChunkAddress(GetNeighbor(cx, cy), new Int3(i & (WorldSettings.ChunkSize - 1), j, k & (WorldSettings.ChunkSize - 1)));
        }

        [InlineMethod.Inline]
        public Voxel GetLocalWithNeighbor(int i, int j, int k)
        {
            if (j < 0)
            {
                return Voxel.Bedrock;
            }

            var chunk = GetNeighbor(
                (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / WorldSettings.ChunkSize))),
                (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / WorldSettings.ChunkSize))));

            return chunk.CurrentHeight <= j
                ? Voxel.SunBlock
                : chunk.GetVoxel(i & (WorldSettings.ChunkSize - 1), j, k & (WorldSettings.ChunkSize - 1));
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
                BuildingContext.IsSpriteChanged = true;
            }

            var newVoxelDefinition = newVoxel.GetDefinition();
            if (newVoxelDefinition.IsSprite)
            {
                if (newVoxelDefinition.IsOriented) BuildingContext.SingleSidedSpriteBlocks.Add(flatIndex);
                else BuildingContext.SpriteBlocks.Add(flatIndex);
                BuildingContext.IsSpriteChanged = true;
            }

            Voxels[flatIndex] = newVoxel;
        }


    }
}
