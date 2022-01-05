﻿using System;
using AppleCinnamon.Services;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Pipeline
{
    public sealed class ArtifactGenerator : IChunkTransformer
    {
        private static readonly VoxelDefinition[] FlowersAndSuch = 
        {
            VoxelDefinition.FlowerRed, VoxelDefinition.FlowerYellow, VoxelDefinition.MushroomBrown, VoxelDefinition.MushroomRed,
        };

        public Chunk Transform(Chunk chunk)
        {
            var rnd = new Random(chunk.ChunkIndex.GetHashCode());

            foreach (var flatIndex in chunk.BuildingContext.TopMostLandVoxels)
            {
                var index = chunk.FromFlatIndex(flatIndex);

                if (rnd.Next() % 70000 == 0)
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
                    }
                }
            }

            CleanUp(chunk);
            return chunk;
        }
        

        private void CleanUp(Chunk chunk)
        {
            chunk.BuildingContext.TopMostLandVoxels = null;
        }
    }
}