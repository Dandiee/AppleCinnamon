using System;
using AppleCinnamon.Helper;
using AppleCinnamon.Settings;
using SimplexNoise;

namespace AppleCinnamon.Pipeline
{
    public sealed class VoxelGenerator
    {
        private const int Offset = int.MaxValue / 2;
        private readonly Random _random;

        private readonly DaniNoise _daniNoise;

        public VoxelGenerator(int seed)
        {
            _random = new Random(seed);
            Noise.Seed = seed;
            _daniNoise = new DaniNoise(8, _random);
            // Noise.Seed = seed;
        }

        public Chunk GenerateVoxels(Int2 chunkIndex)
        {
            var chunkSizeXz = new Int2(Chunk.SizeXy, Chunk.SizeXy);

            var heatMap = new int[Chunk.SizeXy, Chunk.SizeXy];
            var maxHeight = 101;
            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var coord = chunkIndex * chunkSizeXz + new Int2(i, k);
                    var height = (byte)((_daniNoise.Compute(coord.X, coord.Y) + 128) * 0.7) + 32;

                    heatMap[i, k] = height;
                    if (maxHeight < height)
                    {
                        maxHeight = height;
                    }
                }
            }


            var initialSlicesCount = maxHeight / Chunk.SliceHeight + 1;
            var voxels = new Voxel[Chunk.SizeXy * initialSlicesCount * Chunk.SliceHeight * Chunk.SizeXy];
            var currentHeight = initialSlicesCount * Chunk.SliceHeight;

            var chunk = new Chunk(chunkIndex, voxels);
            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var height = heatMap[i, k];


                    for (var j = 0; j <= height - 1; j++)
                    {
                        voxels[Help.GetFlatIndex(i, j, k, currentHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
                    }

                    if (height < 100) // water level
                    {
                        for (var j = height; j < 100; j++)
                        {
                            var flatIndex = Help.GetFlatIndex(i, j, k, currentHeight);
                            voxels[flatIndex] = new Voxel(VoxelDefinition.Water.Type, 0);
                        }

                        chunk.TopMostWaterVoxels.Add(Help.GetFlatIndex(i, 99, k, currentHeight));
                    }


                    var isSnow = height > (128 + _random.Next(5));

                    voxels[Help.GetFlatIndex(i, height - 1, k, currentHeight)] =
                        isSnow
                            ? new Voxel(VoxelDefinition.Snow.Type, 0)
                            : new Voxel(VoxelDefinition.Grass.Type, 0, (byte) _random.Next(1, 9));

                    if (!isSnow && _random.Next(5) == 0)
                    {
                        var flatIndex = Help.GetFlatIndex(i, height, k, currentHeight);

                        var isFlower = _random.Next() % 2 == 0;
                        var targetType = isFlower ? VoxelDefinition.SlabBottom : VoxelDefinition.Weed;

                        voxels[flatIndex] = new Voxel(targetType.Type, 0);
                        if (targetType.IsSprite)
                        {
                            chunk.BuildingContext.SpriteBlocks.Add(flatIndex);
                        }
                    }

                        // new Voxel(
                        //     height > (128 + _random.Next(5))
                        //         ? VoxelDefinition.Snow.Type
                        //         : VoxelDefinition.Grass.Type, 0);

                    // voxels[i + Chunk.SizeXy * (height + Chunk.Height * k)] = new Voxel(4, 0);
                }
            }

            return chunk;
        }






        public Chunk GenerateVoxels3D(Int2 chunkIndex)
        {
            var maxHeight = 128;
            var voxels = new Voxel[Chunk.SizeXy * Chunk.SizeXy * maxHeight];

            var chunk = new Chunk(chunkIndex, voxels);


            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    for (var j = 0; j <= maxHeight - 1; j++)
                    {

                        var pixel = Noise.CalcPixel3D(i + (chunk.ChunkIndex.X * Chunk.SizeXy), j, k + chunk.ChunkIndex.Y * Chunk.SizeXy, .015f);
                        if (pixel < 128)// && pixel > 16)
                        {
                            voxels[Help.GetFlatIndex(i, j, k, maxHeight)] = new Voxel(VoxelDefinition.Sand.Type, 0);
                        }
                    }

                    voxels[Help.GetFlatIndex(i, 1, k, maxHeight)] = new Voxel(VoxelDefinition.Sand.Type, 0);

                }
            }

            return chunk;
        }

        public Chunk GenerateVoxelsMock(Int2 chunkIndex)
        {
            var maxHeight = 128;
            var voxels = new Voxel[Chunk.SizeXy * Chunk.SizeXy * maxHeight];

            var chunk = new Chunk(chunkIndex, voxels);


            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {

                    if (chunk.ChunkIndex == new Int2(-1, 0))
                    {
                        voxels[Help.GetFlatIndex(i, 3, k, maxHeight)] = new Voxel(VoxelDefinition.Sand.Type, 0);
                    }


                    voxels[Help.GetFlatIndex(i, 1, k, maxHeight)] = new Voxel(VoxelDefinition.Sand.Type, 0);

                }
            }

            //if (chunk.ChunkIndex == Int2.Zero)
            //{
            //    voxels[Help.GetFlatIndex(0, 3, 8, maxHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
            //    voxels[Help.GetFlatIndex(1, 3, 8, maxHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
            //    voxels[Help.GetFlatIndex(2, 3, 8, maxHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
            //    voxels[Help.GetFlatIndex(3, 3, 8, maxHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
            //}

           

            return chunk;
        }
    }
}
