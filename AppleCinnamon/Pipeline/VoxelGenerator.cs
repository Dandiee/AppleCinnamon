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

        public VoxelGenerator(int seed)
        {
            Noise.Seed = seed;
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
                    var height = 64 + (byte)(Noise.CalcPixel2D(coord.X + 500, coord.Y + 500, 0.01f) * 0.09);

                   // height = 51;
                    

                    for (var j = 0; j < height; j++)
                    {
                        voxels[i + Chunk.Size.X * (j + Chunk.Size.Y * k)] = new Voxel(3, 0);
                    }

                    voxels[i + Chunk.Size.X * (height + Chunk.Size.Y * k)] = new Voxel(4, 0);
                }
            }

            //if (chunkIndex.X + chunkIndex.Y < 2)
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
