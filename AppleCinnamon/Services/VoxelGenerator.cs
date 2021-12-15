using System;
using AppleCinnamon.Helper;
using AppleCinnamon.Services;
using AppleCinnamon.Settings;
using SimplexNoise;

namespace AppleCinnamon.Pipeline
{
    public sealed class VoxelGenerator
    {
        private readonly DaniNoise _daniNoise;

        public VoxelGenerator()
        {
            _daniNoise = new DaniNoise(WorldSettings.HighMapNoiseOptions);
        }
        
        public Chunk GenerateVoxels(Int2 chunkIndex)
        {
            var chunkSizeXz = new Int2(Chunk.SizeXy, Chunk.SizeXy);
            var heatMap = new int[Chunk.SizeXy, Chunk.SizeXy];
            var maxHeight = WorldSettings.WaterLevel + 1;
            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var coord = chunkIndex * chunkSizeXz + new Int2(i, k);
                    var height = (byte)((_daniNoise.Compute(coord.X, coord.Y)));

                    heatMap[i, k] = height;
                    if (maxHeight < height)
                    {
                        maxHeight = height;
                    }
                }
            }

            maxHeight += 32;


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
                        voxels[Help.GetFlatIndex(i, j, k, currentHeight)] = VoxelDefinition.Grass.Create(2);
                    }

                    if (false)
                    {
                        //voxels.Tree(i, height, k, currentHeight);
                    }

                    if (height < WorldSettings.WaterLevel)
                    {
                        for (var j = height; j < WorldSettings.WaterLevel; j++)
                        {
                            var flatIndex = Help.GetFlatIndex(i, j, k, currentHeight);
                            voxels[flatIndex] = VoxelDefinition.Water.Create();
                        }

                        chunk.TopMostWaterVoxels.Add(Help.GetFlatIndex(i, WorldSettings.WaterLevel - 1, k, currentHeight));
                    }

                    /*
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
                    }*/

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
                            voxels[Help.GetFlatIndex(i, j, k, maxHeight)] = VoxelDefinition.Sand.Create();
                        }
                    }

                    voxels[Help.GetFlatIndex(i, 1, k, maxHeight)] = VoxelDefinition.Sand.Create();

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

                    if (chunk.ChunkIndex == new Int2(1, 1))
                    {
                        voxels[Help.GetFlatIndex(i, 4, k, maxHeight)] = VoxelDefinition.Stone.Create();
                    }

                    if (chunk.ChunkIndex == new Int2(2, 1))
                    {
                        voxels[Help.GetFlatIndex(i, 6, k, maxHeight)] = VoxelDefinition.Stone.Create();
                    }


                    voxels[Help.GetFlatIndex(i, 1, k, maxHeight)] = VoxelDefinition.Stone.Create();

                }
            }

            return chunk;
        }
    }
}
