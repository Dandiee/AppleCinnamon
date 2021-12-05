using System;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

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

            _daniNoise = new DaniNoise(8, _random);
            // Noise.Seed = seed;
        }

        public Voxel[] GenerateVoxels(Int2 chunkIndex)
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


            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var height = heatMap[i, k];


                    for (var j = 0; j < height - 1; j++)
                    {
                        voxels[Help.GetFlatIndex(i, j, k, currentHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
                    }

                    if (height < 100) // water level
                    {
                        for (var j = height; j < 100 - 1; j++)
                        {
                            voxels[Help.GetFlatIndex(i, j, k, currentHeight)] =
                                new Voxel(VoxelDefinition.Water.Type, 0);
                        }
                    }

                    voxels[Help.GetFlatIndex(i, height - 2, k, currentHeight)] =
                        new Voxel(
                            height > (128 + _random.Next(5))
                                ? VoxelDefinition.Snow.Type
                                : VoxelDefinition.Grass.Type, 0);

                    // voxels[i + Chunk.SizeXy * (height + Chunk.Height * k)] = new Voxel(4, 0);
                }
            }

            //for (var i = 0; i < Chunk.SizeXy; i++)
            //{
            //    for (var j = 0; j < Chunk.Height; j++)
            //    {
            //        for (var k = 0; k < Chunk.SizeXy; k++)
            //        {
            //            var originalVoxel = voxels[i + Chunk.SizeXy * (j + Chunk.Height * k)];

            //            //if (originalVoxel.Block == 0)
            //            {

            //                var verticalScale = 1f;

            //                if (j > 100)
            //                {
            //                    if (j > 160)
            //                    {
            //                        verticalScale = 0;
            //                    }
            //                    else
            //                    {
            //                        verticalScale = (160f - j) / 60f;
            //                    }

            //                }



            //                var rnd = (byte) (Noise.CalcPixel3D(
            //                                      chunkIndex.X * Chunk.SizeXy + i + 500,
            //                                      j,
            //                                      chunkIndex.Y * Chunk.SizeXy + k + 500,
            //                                      0.01f) * 0.7 * verticalScale);

            //                var isBlock = rnd > 128;

            //                if (isBlock)
            //                {
            //                    voxels[i + Chunk.SizeXy * (j + Chunk.Height * k)] = new Voxel(VoxelDefinition.Snow.Type, 0);
            //                }
            //            }
            //        }
            //    }
            //}

            //if (chunkIndex == Int2.Zero)
            //{
            //    for (var i = 0; i < 16; i++)
            //    {
            //        for (var k = 0; k < 16; k++)
            //        {
            //            voxels[i + Chunk.SizeXy * (60 + Chunk.Height * k)] = new Voxel(1, 0);
            //        }
            //    }
            //}

            return voxels;
        }
    }
}
