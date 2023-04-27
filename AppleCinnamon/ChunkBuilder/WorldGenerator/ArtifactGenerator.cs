using System;
using AppleCinnamon.Options;
using SharpDX;

namespace AppleCinnamon.ChunkBuilder.WorldGenerator;

public static class ArtifactGenerator
{
    private static readonly VoxelDefinition[] FlowersAndSuch =
    {
        VoxelDefinition.FlowerRed, VoxelDefinition.FlowerYellow, VoxelDefinition.MushroomBrown, VoxelDefinition.MushroomRed,
    };

    public static Chunk Generate(Chunk chunk)
    {
        var rnd = new Random(chunk.ChunkIndex.GetHashCode());
        foreach (var flatIndex in chunk.BuildingContext.TopMostLandVoxels)
        {
            var index = chunk.FromFlatIndex(flatIndex);

            var top = chunk.GetFlatIndex(index.X, index.Y + 1, index.Z);

            if (index.Y >= WorldGeneratorOptions.WATER_LEVEL && index.Y < WorldGeneratorOptions.WATER_LEVEL + 16)
            {
                if (rnd.Next() % 80 == 0)
                {
                    Artifacts.Tree(rnd, chunk, index);
                }
                else if (rnd.Next() % 3 == 0)
                {
                    chunk.SetSafe(top, VoxelDefinition.Weed.Create(2));
                }
                else if (rnd.Next() % 50 == 0)
                {
                    var flowerType = FlowersAndSuch[rnd.Next(0, FlowersAndSuch.Length)];
                    chunk.SetSafe(top, flowerType.Create());
                }
                else if (rnd.Next() % 30 == 0)
                {
                    //chunk.SetSafe(flatIndex, VoxelDefinition.SunflowerBottom.Create());
                    //chunk.SetSafe(top, VoxelDefinition.SunflowerTop.Create());
                }
            }
        }

        return chunk;

        

        foreach (var flatIndex in chunk.BuildingContext.TopMostLandVoxels)
        {
            var index = chunk.FromFlatIndex(flatIndex);

            if (rnd.Next() % 30 == 0)
            {
                Artifacts.Tree(rnd, chunk, index);
            }
            else
            {
                var voxel = chunk.Voxels[flatIndex];
                if (voxel.BlockType == 0)
                {
                    if (rnd.Next() % 3 == 0)
                    {
                        chunk.SetSafe(flatIndex, VoxelDefinition.Weed.Create(2));
                    }
                    else if (rnd.Next() % 50 == 0)
                    {
                        var flowerType = FlowersAndSuch[rnd.Next(0, FlowersAndSuch.Length)];
                        chunk.SetSafe(flatIndex, flowerType.Create());
                    }
                    else if (rnd.Next() % 30 == 0)
                    {
                        var top = chunk.GetFlatIndex(index.X, index.Y + 1, index.Z);
                        chunk.SetSafe(flatIndex, VoxelDefinition.SunflowerBottom.Create());
                        chunk.SetSafe(top, VoxelDefinition.SunflowerTop.Create());
                    }
                }
            }
        }

        // cleanup
        chunk.BuildingContext.TopMostLandVoxels.Clear();

        return chunk;
    }
}