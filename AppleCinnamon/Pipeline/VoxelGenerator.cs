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
            var voxels = new Voxel[Chunk.Size.X * Chunk.Size.Y * Chunk.Size.Z];
            var chunkSizeXz = new Int2(Chunk.Size.X, Chunk.Size.Z);
            for (var i = 0; i < Chunk.Size.X; i++)
            {
                for (var k = 0; k < Chunk.Size.Z; k++)
                {
                    var coord = chunkIndex * chunkSizeXz + new Int2(i, k);
                    var height = (byte)((_daniNoise.Compute(coord.X, coord.Y) + 128) * 0.7) + 32;

                    // height = 51;
                    

                    for (var j = 0; j < height - 1; j++)
                    {
                        voxels[i + Chunk.Size.X * (j + Chunk.Size.Y * k)] = new Voxel(VoxelDefinition.Stone.Type, 0);
                    }

                    if (height < 100) // water level
                    {
                        for (var j = height; j < 100- 1; j++)
                        {
                            voxels[i + Chunk.Size.X * (j + Chunk.Size.Y * k)] =
                                new Voxel(VoxelDefinition.Water.Type, 0);
                        }
                    }

                    voxels[i + Chunk.Size.X * (height - 1 + Chunk.Size.Y * k)] = 
                        new Voxel(
                            height > (128 + _random.Next(5))
                            ? VoxelDefinition.Snow.Type
                            : VoxelDefinition.Grass.Type, 0);

                    // voxels[i + Chunk.Size.X * (height + Chunk.Size.Y * k)] = new Voxel(4, 0);
                }
            }

            for (var i = 0; i < Chunk.Size.X; i++)
            {
                for (var j = 0; j < Chunk.Size.Y; j++)
                {
                    for (var k = 0; k < Chunk.Size.Z; k++)
                    {
                        var originalVoxel = voxels[i + Chunk.Size.X * (j + Chunk.Size.Y * k)];
                        if (originalVoxel.Block == 0)
                        {
                            var rnd = (byte) (Noise.CalcPixel3D(
                                                  chunkIndex.X * Chunk.Size.X + i + 500,
                                                  j,
                                                  chunkIndex.Y * Chunk.Size.Z + k + 500,
                                                  0.01f) * 0.7);

                            var isBlock = rnd > 128;

                            if (isBlock)
                            {
                                voxels[i + Chunk.Size.X * (j + Chunk.Size.Y * k)] = new Voxel(VoxelDefinition.Snow.Type, 0);
                            }
                        }
                    }
                }
            }

            //if (chunkIndex == Int2.Zero)
            //{
            //    for (var i = 0; i < 16; i++)
            //    {
            //        for (var k = 0; k < 16; k++)
            //        {
            //            voxels[i + Chunk.Size.X * (60 + Chunk.Size.Y * k)] = new Voxel(1, 0);
            //        }
            //    }
            //}

            return voxels;
        }
    }
}
