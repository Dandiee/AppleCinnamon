using System;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Pipeline
{
    public sealed class TerrainGenerator : PipelineBlock<Int2, Chunk>
    {
        private readonly DaniNoise _noise;
        private static readonly DaniNoise _waterNoise = new(WorldSettings.RiverNoiseOptions);

        public TerrainGenerator(DaniNoise noise)
        {
            _noise = noise;
        }

        public override Chunk Process(Int2 chunkIndex)
        {
            var chunkSizeXz = new Int2(Chunk.SizeXy, Chunk.SizeXy);
            var heatMap = new int[Chunk.SizeXy, Chunk.SizeXy];
            var maxHeight = WorldSettings.WaterLevel + 1;
            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var coordinates = chunkIndex * chunkSizeXz + new Int2(i, k);
                    var height = (byte) ((_noise.Compute(coordinates.X, coordinates.Y)));

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
                        voxels[Help.GetFlatIndex(i, j, k, currentHeight)] =
                            j == height - 1
                                ? VoxelDefinition.Grass.Create(2)
                                : VoxelDefinition.Dirt.Create();
                    }

                    var waterRandom = _waterNoise.Compute(i + chunkIndex.X * Chunk.SizeXy, k + chunkIndex.Y * Chunk.SizeXy);
                    var isWater = false;
                    if (Math.Abs(waterRandom - 128) <= 2)
                    {
                        isWater = true;
                        var min = Math.Min(height, WorldSettings.WaterLevel);
                        var max = Math.Max(height, WorldSettings.WaterLevel) - 1;
                        
                        for (var j = min; j <= max; j++)
                        {
                            var fi = Help.GetFlatIndex(i, j, k, currentHeight);
                            if (j <= WorldSettings.WaterLevel)
                            {
                                voxels[fi] = VoxelDefinition.Water.Create();
                            }
                            else voxels[fi] = VoxelDefinition.Air.Create();
                        }

                        voxels[Help.GetFlatIndex(i, min - 1, k, currentHeight)] = VoxelDefinition.Sand.Create();

                        chunk.TopMostWaterVoxels.Add(Help.GetFlatIndex(i, WorldSettings.WaterLevel, k, currentHeight));
                    }

                    if (height < WorldSettings.WaterLevel)
                    {
                        isWater = true;
                        for (var j = height; j < WorldSettings.WaterLevel; j++)
                        {
                            var flatIndex = Help.GetFlatIndex(i, j, k, currentHeight);
                            voxels[flatIndex] = VoxelDefinition.Water.Create();
                        }

                        chunk.TopMostWaterVoxels.Add(Help.GetFlatIndex(i, WorldSettings.WaterLevel - 1, k, currentHeight));
                    }
                    if (!isWater)
                    {
                        chunk.TopMostLandVoxels.Add(Help.GetFlatIndex(i, height, k, currentHeight));
                    }
                }
            }

            return chunk;
        }
    }
}
