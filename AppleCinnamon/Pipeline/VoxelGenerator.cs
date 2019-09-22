using System;
using AppleCinnamon.Settings;
using AppleCinnamon.System;
using SimplexNoise;

namespace AppleCinnamon.Pipeline
{
    public interface IVoxelGenerator
    {
        Voxel[] GenerateVoxels(Int2 chunkIndex);
    }

    public sealed class VoxelGenerator : IVoxelGenerator
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
            var voxels = new Voxel[Chunk.SizeXy * Chunk.SliceHeight * Chunk.SizeXy];
            var currentHeight = Chunk.SliceHeight;

            var chunkSizeXz = new Int2(Chunk.SizeXy, Chunk.SizeXy);
            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var coord = chunkIndex * chunkSizeXz + new Int2(i, k);
                    var height = (byte)((_daniNoise.Compute(coord.X, coord.Y) + 128) * 0.7) + 32;

                    // height = 51;
                    

                    for (var j = 0; j < height - 1; j++)
                    {
                        if (j >= currentHeight)
                        {

                            var requiredSlice = (j / Chunk.SliceHeight) + 1;
                            var newHeight = requiredSlice * Chunk.SliceHeight;

                            var newArray = new Voxel[Chunk.SizeXy * newHeight * Chunk.SizeXy];
                            //Array.Copy(voxels, newArray, voxels.Length);
                            for (var x = 0; x < Chunk.SizeXy; x++)
                            {
                                for (var y = 0; y < currentHeight; y++)
                                {
                                    for (var z = 0; z < Chunk.SizeXy; z++)
                                    {
                                        var oldFlatIndex = Help.GetFlatIndex(x, y, z, currentHeight);
                                        var newFlatIndex = Help.GetFlatIndex(x, y, z, newHeight);
                                        newArray[newFlatIndex] = voxels[oldFlatIndex];
                                    }
                                }
                            }
                            voxels = newArray;
                            currentHeight = newHeight;
                        }

                        voxels[Help.GetFlatIndex(i, j, k, currentHeight)] = new Voxel(VoxelDefinition.Stone.Type, 0);
                    }

                    if (height < 100) // water level
                    {
                        for (var j = height; j < 64- 1; j++)
                        {
                            if (j >= currentHeight)
                            {
                                var requiredSlice = (j / Chunk.SliceHeight) + 1;
                                var newHeight = requiredSlice * Chunk.SliceHeight;

                                var newArray = new Voxel[Chunk.SizeXy * newHeight * Chunk.SizeXy];
                                //Array.Copy(voxels, newArray, voxels.Length);
                                for (var x = 0; x < Chunk.SizeXy; x++)
                                {
                                    for (var y = 0; y < currentHeight; y++)
                                    {
                                        for (var z = 0; z < Chunk.SizeXy; z++)
                                        {
                                            var oldFlatIndex = Help.GetFlatIndex(x, y, z, currentHeight);
                                            var newFlatIndex = Help.GetFlatIndex(x, y, z, newHeight);
                                            newArray[newFlatIndex] = voxels[oldFlatIndex];
                                        }
                                    }
                                }
                                voxels = newArray;
                                currentHeight = newHeight;
                            }

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
