using System;
using AppleCinnamon.Common;
using AppleCinnamon.Options;

namespace AppleCinnamon.ChunkBuilder.WorldGenerator;

public static class TerrainGenerator
{
    private static readonly DaniNoise Noise = new(WorldGeneratorOptions.HighMapNoiseOptions);
    private static readonly DaniNoise WaterNoise = new(WorldGeneratorOptions.RiverNoiseOptions);

    public static Chunk Generate(Chunk chunk)
    {
        // if (Game.Debug) Thread.Sleep(100);

        var chunkSizeXz = new Int2(GameOptions.CHUNK_SIZE, GameOptions.CHUNK_SIZE);
        var heatMap = new int[GameOptions.CHUNK_SIZE, GameOptions.CHUNK_SIZE];
        var maxHeight = WorldGeneratorOptions.WATER_LEVEL + 1;
        for (var i = 0; i < GameOptions.CHUNK_SIZE; i++)
        {
            for (var k = 0; k < GameOptions.CHUNK_SIZE; k++)
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
        var initialSlicesCount = maxHeight / GameOptions.SLICE_HEIGHT + 1;
        chunk.Voxels = new Voxel[GameOptions.CHUNK_SIZE * initialSlicesCount * GameOptions.SLICE_HEIGHT * GameOptions.CHUNK_SIZE];
        chunk.CurrentHeight = chunk.Voxels.Length / GameOptions.SLICE_AREA * GameOptions.SLICE_HEIGHT;
        chunk.UpdateBoundingBox();
        chunk.ChunkIndexVector = chunk.BoundingBox.Center;

        for (var i = 0; i < GameOptions.CHUNK_SIZE; i++)
        {
            for (var k = 0; k < GameOptions.CHUNK_SIZE; k++)
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

                var waterRandom = WaterNoise.Compute(i + chunk.ChunkIndex.X * GameOptions.CHUNK_SIZE, k + chunk.ChunkIndex.Y * GameOptions.CHUNK_SIZE);
                var isWater = false;
                if (Math.Abs(waterRandom - 128) <= 2)
                {
                    isWater = true;
                    var min = Math.Min(height, WorldGeneratorOptions.WATER_LEVEL);
                    var max = Math.Max(height, WorldGeneratorOptions.WATER_LEVEL) - 1;

                    for (var j = min; j <= max; j++)
                    {
                        chunk.SetVoxel(i, j, k,
                            j <= WorldGeneratorOptions.WATER_LEVEL
                                ? VoxelDefinition.Water.Create()
                                : VoxelDefinition.Air.Create());
                    }

                    chunk.SetVoxel(i, min - 1, k, VoxelDefinition.Sand.Create());
                    chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldGeneratorOptions.WATER_LEVEL, k));
                }

                if (height < WorldGeneratorOptions.WATER_LEVEL)
                {
                    isWater = true;
                    for (var j = height; j < WorldGeneratorOptions.WATER_LEVEL; j++)
                    {
                        chunk.SetVoxel(i, j, k, VoxelDefinition.Water.Create());
                    }

                    chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldGeneratorOptions.WATER_LEVEL - 1, k));
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