using System.Linq;
using AppleCinnamon.Helper;
using SharpDX;

namespace AppleCinnamon
{
    public sealed partial class Chunk
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

       

        public void ExtendUpward(int heightToFit)
        {
            var expectedSlices = heightToFit / SliceHeight + 1;
            var expectedHeight = expectedSlices * SliceHeight;

            var newVoxels = new Voxel[SizeXy * expectedHeight * SizeXy];

            // In case of local sliced array addressing, a simple copy does the trick - Array.Copy(Voxels, newVoxels, Voxels.Length);
            for (var i = 0; i < SizeXy; i++)
            {
                for (var j = 0; j < CurrentHeight; j++)
                {
                    for (var k = 0; k < SizeXy; k++)
                    {
                        var oldFlatIndex = GetFlatIndex(i, j, k, CurrentHeight);
                        var newFlatIndex = GetFlatIndex(i, j, k, expectedHeight);
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
