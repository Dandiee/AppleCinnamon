﻿using System.Diagnostics;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVoxel(int flatIndex, Voxel voxel) => Voxels[flatIndex] = voxel;


        public Voxel GetLocalWithNeighborChunk(int i, int j, int k, out VoxelChunkAddress address)
        {
            if (j < 0)
            {
                address = VoxelChunkAddress.Zero;
                return Voxel.Bedrock;
            }

            if (j >= CurrentHeight)
            {
                address = VoxelChunkAddress.Zero;
                return Voxel.SunBlock;
            }

            var cx = (int)(-((i & 0b10000000_00000000_00000000_00000000) >> 31) + ((i / SizeXy)));
            var cy = (int)(-((k & 0b10000000_00000000_00000000_00000000) >> 31) + ((k / SizeXy)));

            var chunk = Neighbors[Help.GetChunkFlatIndex(cx, cy)];
            address = new VoxelChunkAddress(chunk, new Int3(i & (SizeXy - 1), j, k & (SizeXy - 1)));

            return chunk.CurrentHeight <= j
                ? Voxel.SunBlock
                : chunk.GetVoxel(Help.GetFlatIndex(address.RelativeVoxelIndex.X, j, address.RelativeVoxelIndex.Z, chunk.CurrentHeight));
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
                : chunk.GetVoxel(Help.GetFlatIndex(i & (SizeXy - 1), j, k & (SizeXy - 1), chunk.CurrentHeight));
        }


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
                        var oldFlatIndex = Help.GetFlatIndex(i, j, k, CurrentHeight);
                        var newFlatIndex = Help.GetFlatIndex(i, j, k, expectedHeight);
                        newVoxels[newFlatIndex] = GetVoxel(oldFlatIndex);
                    }
                }
            }

            BuildingContext.VisibilityFlags = BuildingContext.VisibilityFlags.ToDictionary(kvp => kvp.Key.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight), kvp => kvp.Value);
            BuildingContext.TopMostWaterVoxels = BuildingContext.TopMostWaterVoxels.Select(s => s.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight)).ToList();
            BuildingContext.SpriteBlocks = BuildingContext.SpriteBlocks.Select(s => s.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight)).ToList();
            BuildingContext.SingleSidedSpriteBlocks = BuildingContext.SingleSidedSpriteBlocks.Select(s => s.ToIndex(CurrentHeight).ToFlatIndex(expectedHeight)).ToList();
            sw.Stop();


            Voxels = newVoxels;

            for (var i = 0; i < SizeXy; i++)
            {
                for (var k = 0; k < SizeXy; k++)
                {
                    for (var j = expectedHeight - 1; j >= CurrentHeight; j--)
                    {
                        SetVoxel(Help.GetFlatIndex(i, j, k, expectedHeight), Voxel.SunBlock);
                    }
                }
            }

            CurrentHeight = expectedHeight;
            UpdateBoundingBox();
        }

        public void SetSafe(Int3 index, Voxel newVoxel) => SetSafe(Help.GetFlatIndex(index, CurrentHeight), newVoxel);
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
        
        public static bool TryGetVoxelAddress(Int3 absoluteVoxelIndex, out VoxelAddress address)
        {
            if (!Help.TryGetChunkIndexByAbsoluteVoxelIndex(absoluteVoxelIndex, out var chunkIndex))
            {
                address = VoxelAddress.Zero;
                return false;
            }

            var voxelIndex = new Int3(absoluteVoxelIndex.X & SizeXy - 1, absoluteVoxelIndex.Y, absoluteVoxelIndex.Z & SizeXy - 1);
            address = new VoxelAddress(chunkIndex, voxelIndex);
            return true;
        }
    }
}
