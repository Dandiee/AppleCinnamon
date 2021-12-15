using System;
using AppleCinnamon.Extensions;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
using AppleCinnamon.Services;
using AppleCinnamon.Settings;
using SharpDX;

namespace AppleCinnamon.Pipeline
{
    public sealed class ArtifactGenerator : PipelineBlock<Chunk, Chunk>
    {
        private static Random _rnd = new Random(4578);

        public override Chunk Process(Chunk chunk)
        {
            foreach (var flatIndex in chunk.BuildingContext.TopMostLandVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                if (_rnd.Next() % 70 == 0)
                {
                    Artifacts.Tree(chunk, index);
                }
                else
                {
                    var voxel = chunk.Voxels[flatIndex];
                    if (voxel.BlockType == 0)
                    {
                        if (_rnd.Next() % 3 == 0)
                        {
                            chunk.Voxels[flatIndex] = VoxelDefinition.Weed.Create(2);
                            chunk.BuildingContext.SpriteBlocks.Add(flatIndex);
                        }
                        else if (_rnd.Next() % 100 == 0)
                        {
                            chunk.Voxels[flatIndex] = VoxelDefinition.Flower.Create();
                            chunk.BuildingContext.SpriteBlocks.Add(flatIndex);
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
