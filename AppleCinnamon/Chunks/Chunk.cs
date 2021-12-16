using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using AppleCinnamon.Helper;
using SharpDX;
using Help = AppleCinnamon.Helper.Help;

namespace AppleCinnamon
{
    public sealed class Chunk
    {
        public const int SliceHeight = 16;
        public const int SizeXy = 16;
        public const int SliceArea = SizeXy * SizeXy * SliceHeight;

        public Voxel[] Voxels;
        public BoundingBox BoundingBox;
        public int CurrentHeight;

        public readonly ChunkBuildingContext BuildingContext;
        public readonly Int2 ChunkIndex;
        public readonly Vector3 OffsetVector;
        public readonly Int2 Offset;
        public readonly Chunk[] Neighbors;
        public readonly Vector3 ChunkIndexVector;

        public ChunkBuffers Buffers { get; set; }
        public Vector3 Center { get; private set; }
        public Vector2 Center2d { get; private set; }


        public Chunk(Int2 chunkIndex, Voxel[] voxels)
        {
            Neighbors = new Chunk[9];
            Voxels = voxels;
            BuildingContext = new ChunkBuildingContext();
            CurrentHeight = (voxels.Length / SliceArea) * SliceHeight;
            ChunkIndex = chunkIndex;
            Offset = chunkIndex * new Int2(SizeXy, SizeXy);
            OffsetVector = new Vector3(Offset.X, 0, Offset.Y);
            ChunkIndexVector = BoundingBox.Center;

            UpdateBoundingBox();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Voxel GetVoxel(int flatIndex) => Voxels[flatIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Voxel GetVoxel(Int3 ijk) => Voxels[ToFlatIndex(ijk.X, ijk.Y, ijk.Z, CurrentHeight)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public Voxel GetVoxel(int i, int j, int k) => Voxels[ToFlatIndex(i, j, k, CurrentHeight)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(int flatIndex, Voxel voxel) => Voxels[flatIndex] = voxel;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(Int3 ijk, Voxel voxel) => Voxels[ToFlatIndex(ijk.X, ijk.Y, ijk.Z, CurrentHeight)] = voxel;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(int i, int j, int k, Voxel voxel) => Voxels[ToFlatIndex(i, j, k, CurrentHeight)] = voxel;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetFlatIndex(int i, int j, int k) => ToFlatIndex(i,j,k, CurrentHeight);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int ToFlatIndex(Int3 ijk) => ToFlatIndex(ijk.X, ijk.Y, ijk.Z, CurrentHeight);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public int ToFlatIndex(Int3 ijk, int height) => ToFlatIndex(ijk.X, ijk.Y, ijk.Z, height);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static int ToFlatIndex(int i, int j, int k, int height) => i + SizeXy * (j + height * k);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static Int3 FromFlatIndex(int flatIndex, int height)
        {
            var k = flatIndex / (Chunk.SizeXy * height);
            var j = (flatIndex - k * Chunk.SizeXy * height) / Chunk.SizeXy;
            var i = flatIndex - (k * Chunk.SizeXy * height + j * Chunk.SizeXy);

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

            var chunk = Neighbors[Help.GetChunkFlatIndex(cx, cy)];
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
            return new VoxelChunkAddress(Neighbors[Help.GetChunkFlatIndex(cx, cy)],
                new Int3(i & (SizeXy - 1), j, k & (SizeXy - 1)));
        }

        [InlineMethod.Inline]
        public Voxel GetLocalWithNeighbor(int i, int j, int k)
        {
            if (j < 0)
            {
                return Voxel.Bedrock;
            }

            var chunk = Neighbors[Help.GetChunkFlatIndex(
                (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy))),
                (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy))))];

            return chunk.CurrentHeight <= j
                ? Voxel.SunBlock
                : chunk.GetVoxel(i & (SizeXy - 1), j, k & (SizeXy - 1));
        }

        public Chunk GetNeighbor(Int3 neighborIndex) => Neighbors[Help.GetChunkFlatIndex(neighborIndex.X, neighborIndex.Y)];
        public Chunk GetNeighbor(int i, int j) => Neighbors[Help.GetChunkFlatIndex(i, j)];

        public void ExtendUpward(int heightToFit)
        {
            var expectedSlices = heightToFit / SliceHeight + 1;
            var expectedHeight = expectedSlices * SliceHeight;

            var newVoxels = new Voxel[SizeXy * expectedHeight * SizeXy];

            // In case of local sliced array addressing, a simple copy does the trick
            // Array.Copy(Voxels, newVoxels, Voxels.Length);
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < SizeXy; i++)
            {
                for (var j = 0; j < CurrentHeight; j++)
                {
                    for (var k = 0; k < SizeXy; k++)
                    {
                        var oldFlatIndex = ToFlatIndex(i, j, k, CurrentHeight);
                        var newFlatIndex = ToFlatIndex(i, j, k, expectedHeight);
                        newVoxels[newFlatIndex] = GetVoxel(oldFlatIndex);
                    }
                }
            }

            BuildingContext.VisibilityFlags = BuildingContext.VisibilityFlags.ToDictionary(kvp => ConvertFlatIndex(kvp.Key, CurrentHeight, expectedHeight), kvp => kvp.Value);
            BuildingContext.TopMostWaterVoxels = BuildingContext.TopMostWaterVoxels.Select(s => ConvertFlatIndex(s, CurrentHeight, expectedHeight)).ToList();
            BuildingContext.SpriteBlocks = BuildingContext.SpriteBlocks.Select(s => ConvertFlatIndex(s, CurrentHeight, expectedHeight)).ToList();
            BuildingContext.SingleSidedSpriteBlocks = BuildingContext.SingleSidedSpriteBlocks.Select(s => ConvertFlatIndex(s, CurrentHeight, expectedHeight)).ToList();

            Voxels = newVoxels;
            var originalHeight = CurrentHeight;
            CurrentHeight = expectedHeight;

            for (var i = 0; i < SizeXy; i++)
            {
                for (var k = 0; k < SizeXy; k++)
                {
                    for (var j = expectedHeight - 1; j >= originalHeight; j--)
                    {
                        SetVoxel(i, j, k, Voxel.SunBlock);
                    }
                }
            }
            UpdateBoundingBox();
        }

        private static int ConvertFlatIndex(int originalFlatIndex, int originalHeight, int targetHeight)
        {
            var oldIndex = FromFlatIndex(originalFlatIndex, originalHeight);
            var newFlatIndex = ToFlatIndex(oldIndex.X, oldIndex.Y, oldIndex.Z, targetHeight);
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

        private void UpdateBoundingBox()
        {
            var size = new Vector3(SizeXy, CurrentHeight, SizeXy) / 2f;
            var position = new Vector3(SizeXy / 2f - .5f + SizeXy * ChunkIndex.X, CurrentHeight / 2f - .5f, SizeXy / 2f - .5f + SizeXy * ChunkIndex.Y);

            BoundingBox = new BoundingBox(position - size, position + size);
            Center = position;
            Center2d = new Vector2(position.X, position.Z);
        }
        
        
    }
}
