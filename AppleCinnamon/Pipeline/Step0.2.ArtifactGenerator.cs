using System;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Services;
using AppleCinnamon.Settings;

namespace AppleCinnamon.Pipeline
{
    public sealed class ArtifactGenerator : PipelineBlock<Chunk, Chunk>
    {
        private static readonly Random Rnd = new(4578);

        public override Chunk Process(Chunk chunk)
        {
            foreach (var flatIndex in chunk.BuildingContext.TopMostLandVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

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
                        else if (Rnd.Next() % 100 == 0)
                        {
                            chunk.SetSafe(flatIndex, VoxelDefinition.Flower.Create());
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
