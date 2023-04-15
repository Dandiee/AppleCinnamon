using System;
using AppleCinnamon.Common;
using AppleCinnamon.Settings;

namespace AppleCinnamon.ChunkGenerators
{
    public static class TerrainGenerator
    {
        private static readonly DaniNoise Noise = new(WorldSettings.HighMapNoiseOptions);
        private static readonly DaniNoise WaterNoise = new(WorldSettings.RiverNoiseOptions);

        public static Chunk Generate(Chunk chunk)
        {
            // if (Game.Debug) Thread.Sleep(100);

            var chunkSizeXz = new Int2(WorldSettings.ChunkSize, WorldSettings.ChunkSize);
            var heatMap = new int[WorldSettings.ChunkSize, WorldSettings.ChunkSize];
            var maxHeight = WorldSettings.WaterLevel + 1;
            for (var i = 0; i < WorldSettings.ChunkSize; i++)
            {
                for (var k = 0; k < WorldSettings.ChunkSize; k++)
                {
                    var coordinates = chunk.ChunkIndex * chunkSizeXz + new Int2(i, k);
                    var height = (byte)Noise.Compute(coordinates.X, coordinates.Y);

                    heatMap[i, k] = height;
                    if (maxHeight < height)
                    {
                        maxHeight = height;
                    }
                }
            }

            maxHeight += 64;
            var initialSlicesCount = maxHeight / Chunk.SliceHeight + 1;
            chunk.Voxels = new Voxel[WorldSettings.ChunkSize * initialSlicesCount * Chunk.SliceHeight * WorldSettings.ChunkSize];
            chunk.CurrentHeight = chunk.Voxels.Length / Chunk.SliceArea * Chunk.SliceHeight;
            chunk.UpdateBoundingBox();
            chunk.ChunkIndexVector = chunk.BoundingBox.Center;

            for (var i = 0; i < WorldSettings.ChunkSize; i++)
            {
                for (var k = 0; k < WorldSettings.ChunkSize; k++)
                {
                    var height = heatMap[i, k];

                    if (height <= 0)
                    {
                        height = 1;
                    }

                    for (var j = 0; j <= height - 1; j++)
                    {
                        chunk.SetVoxel(i, j, k,
                            j == height - 1
                                ? VoxelDefinition.Grass.Create(2)
                                : VoxelDefinition.Dirt.Create());
                    }

                    var waterRandom = WaterNoise.Compute(i + chunk.ChunkIndex.X * WorldSettings.ChunkSize, k + chunk.ChunkIndex.Y * WorldSettings.ChunkSize);
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
