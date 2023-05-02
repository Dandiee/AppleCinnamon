﻿using System;
using AppleCinnamon.Common;
using AppleCinnamon.Options;

namespace AppleCinnamon.ChunkBuilder.WorldGenerator;

public static class TerrainGenerator
{
    private static readonly DaniNoise ContinentNoise = 
        new(new SimplexOptions(14, 10, 1.1f, 0.5441703, 7.1219765e-05, 4535));
    
    private static readonly DaniNoise MountainNoise = 
        new(new SimplexOptions(14, 10, 2.5337195, 0.4681816, 3.2019507E-05, 5768));

    private static readonly DaniNoise WaterNoise = new(WorldGeneratorOptions.RiverNoiseOptions);

    private static int GetHeight(int i, int k)
    {
        var c = Noises.Continent.Compute(i, k);
        var e = Noises.Erosion.Compute(i, k);
        var p = Noises.Peaks.Compute(i, k);

        var c1 = SplineSegment.Continental.GetValue((float)c);
        var c2 = SplineSegment.Erosion.GetValue((float)e);
        var c3 = SplineSegment.PeaksAndRivers.GetValue((float)p);

        var product = (c1 + c2 + c3) / 3f;

        if (c1 < -1f || c2 > 1f)
        {

        }

        if (c1 < -0.9f || c2 > 0.9f)
        {

        }

        if (product <= 0)
        {

        }

        return 110 + (int)(100 * product);
    }

    public static Chunk Generate(Chunk chunk)
    {
        // if (Game.Debug) Thread.Sleep(100);
        var rnd = new Random(chunk.ChunkIndex.GetHashCode());

        var chunkSizeXz = new Int2(GameOptions.CHUNK_SIZE, GameOptions.CHUNK_SIZE);
        var heatMap = new int[GameOptions.CHUNK_SIZE, GameOptions.CHUNK_SIZE];
        var maxHeight = WorldGeneratorOptions.WATER_LEVEL + 1;

        for (var i = 0; i < GameOptions.CHUNK_SIZE; i++)
        {
            for (var k = 0; k < GameOptions.CHUNK_SIZE; k++)
            {
                var coordinates = chunk.ChunkIndex * chunkSizeXz + new Int2(i, k);
                var height = GetHeight(coordinates.X, coordinates.Y);

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

                if (height <= 0) height = 1;

                chunk.BuildingContext.TopMostLandVoxels.Add(chunk.GetFlatIndex(i, height, k));

                for (var j = height; j > 0; j--)
                {
                    chunk.SetVoxel(i, j, k, VoxelDefinition.Grass.Create(2));
                }

                const int waterLevel = 80;

                for (var j = waterLevel; j > height; j--)
                {
                    chunk.SetVoxel(i, j, k, VoxelDefinition.Water.Create());
                }

                if (height < waterLevel)
                {
                    chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, waterLevel, k));
                }

                //if (height <= WorldGeneratorOptions.WATER_LEVEL)
                //{
                //    chunk.SetVoxel(i, height, k, VoxelDefinition.Sand.Create(2));
                //}
                //else if (height + rnd.Next() % 3 >= WorldGeneratorOptions.SNOW_LEVEL)
                //{
                //    chunk.SetVoxel(i, height, k, VoxelDefinition.Snow.Create(2));
                //}
                //else
                //{
                //    chunk.SetVoxel(i, height, k, VoxelDefinition.Grass.Create(2));
                //}

                //var dirtBlocks = rnd.Next(1, 4);

                //for (var j = height - 1; j > height - dirtBlocks; j--)
                //{
                //    chunk.SetVoxel(i, j, k, VoxelDefinition.Dirt.Create());
                //}

                //for (var j = height - dirtBlocks; j > -1; j--)
                //{
                //    chunk.SetVoxel(i, j, k, VoxelDefinition.Stone.Create());
                //}

                //if (height <= WorldGeneratorOptions.WATER_LEVEL)
                //{
                //    for (var j = WorldGeneratorOptions.WATER_LEVEL; j > height; j--)
                //    {
                //        chunk.SetVoxel(i, j, k, VoxelDefinition.Water.Create());
                //        chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldGeneratorOptions.WATER_LEVEL, k));
                //    }
                //}
                // ----------
                //var waterRandom = WaterNoise.Compute(i + chunk.ChunkIndex.X * GameOptions.CHUNK_SIZE, k + chunk.ChunkIndex.Y * GameOptions.CHUNK_SIZE);
                var isWater = false;
                //if (Math.Abs(waterRandom - 128) <= 2)
                //{
                //    isWater = true;
                //    var min = Math.Min(height, WorldGeneratorOptions.WATER_LEVEL);
                //    var max = Math.Max(height, WorldGeneratorOptions.WATER_LEVEL) - 1;
                //
                //    for (var j = min; j <= max; j++)
                //    {
                //        chunk.SetVoxel(i, j, k,
                //            j <= WorldGeneratorOptions.WATER_LEVEL
                //                ? VoxelDefinition.Water.Create()
                //                : VoxelDefinition.Air.Create());
                //    }
                //
                //    chunk.SetVoxel(i, min - 1, k, VoxelDefinition.Sand.Create());
                //    chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldGeneratorOptions.WATER_LEVEL, k));
                //}
                //
                //if (height < WorldGeneratorOptions.WATER_LEVEL)
                //{
                //    isWater = true;
                //    for (var j = height; j < WorldGeneratorOptions.WATER_LEVEL; j++)
                //    {
                //        chunk.SetVoxel(i, j, k, VoxelDefinition.Water.Create());
                //    }
                //
                //    chunk.BuildingContext.TopMostWaterVoxels.Add(chunk.GetFlatIndex(i, WorldGeneratorOptions.WATER_LEVEL - 1, k));
                //}
                //if (!isWater)
                //{
                //    chunk.BuildingContext.TopMostLandVoxels.Add(chunk.GetFlatIndex(i, height, k));
                //}
            }
        }

        return chunk;
    }
}