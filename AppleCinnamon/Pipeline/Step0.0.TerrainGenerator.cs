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
                    var height = (byte)((_noise.Compute(coordinates.X, coordinates.Y)));

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
            var chunk = new Chunk(chunkIndex, voxels);

            for (var i = 0; i < Chunk.SizeXy; i++)
            {
                for (var k = 0; k < Chunk.SizeXy; k++)
                {
                    var height = heatMap[i, k];

                    for (var j = 0; j <= height - 1; j++)
                    {
                        chunk.SetVoxel(i, j, k,
                            j == height - 1
                                ? VoxelDefinition.Grass.Create(2)
                                : VoxelDefinition.Dirt.Create());
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
                            chunk.SetVoxel(i, j, k,
                                j <= WorldSettings.WaterLevel
                                    ? VoxelDefinition.Water.Create()
                                    : VoxelDefinition.Air.Create());
                        }

                        chunk.SetVoxel(i, min - 1, k, VoxelDefinition.Sand.Create());
                        chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldSettings.WaterLevel, k));
                    }

                    if (height < WorldSettings.WaterLevel)
                    {
                        isWater = true;
                        for (var j = height; j < WorldSettings.WaterLevel; j++)
                        {
                            chunk.SetVoxel(i, j, k, VoxelDefinition.Water.Create());
                        }

                        chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldSettings.WaterLevel - 1, k));
                    }
                    if (!isWater)
                    {
                        chunk.BuildingContext.TopMostLandVoxels.Add(chunk.GetFlatIndex(i, height, k));
                    }
                }
            }

            return chunk;
        }
    }
}
