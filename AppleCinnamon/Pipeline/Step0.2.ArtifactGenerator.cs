using System;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Services;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Pipeline
{
    public sealed class ArtifactGenerator : TransformChunkPipelineBlock
    {
        private static readonly Random Rnd = new(4578);

        private static readonly VoxelDefinition[] FlowersAndSuch = 
        {
            VoxelDefinition.FlowerRed, VoxelDefinition.FlowerYellow, VoxelDefinition.MushroomBrown, VoxelDefinition.MushroomRed,
        };

        public override Chunk Process(Chunk chunk)
        {
            foreach (var flatIndex in chunk.BuildingContext.TopMostLandVoxels)
            {
                var index = chunk.FromFlatIndex(flatIndex);

                if (Rnd.Next() % 70 == 0)
                {
                    Artifacts.Tree(chunk, index);
                }
                else
                {
                    var voxel = chunk.Voxels[flatIndex];
                    if (voxel.BlockType == 0)
                    {
                        if (Rnd.Next() % 3 == 0)
                        {
                            chunk.SetSafe(flatIndex, VoxelDefinition.Weed.Create(2));
                        }
                        else if (Rnd.Next() % 50 == 0)
                        {
                            var flowerType = FlowersAndSuch[Rnd.Next(0, FlowersAndSuch.Length)];
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
