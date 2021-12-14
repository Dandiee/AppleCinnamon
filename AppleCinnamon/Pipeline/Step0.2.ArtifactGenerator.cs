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
            foreach (var flatIndex in chunk.TopMostLandVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);


                if (_rnd.Next() % 300 == 0)
                {
                    Artifacts.Tree(chunk, index);
                }
                else
                {
                    //if (_rnd.Next() % 100 == 0)
                    //{
                    //    var fi = Help.GetFlatIndex(relativeIndex.X, relativeIndex.Y, relativeIndex.Z, chunk.CurrentHeight);
                    //    chunk.Voxels[fi] = new Voxel(VoxelDefinition.Weed.Type, 0, 2);
                    //    chunk.BuildingContext.SpriteBlocks.Add(fi);
                    //}
                    //else if (_rnd.Next() % 100 == 0)
                    //{
                    //    var fi = Help.GetFlatIndex(relativeIndex.X, relativeIndex.Y, relativeIndex.Z, chunk.CurrentHeight);
                    //    chunk.Voxels[fi] = new Voxel(VoxelDefinition.Flower.Type, 0, 0);
                    //    chunk.BuildingContext.SpriteBlocks.Add(fi);
                    //}
                }
            }

            CleanUp(chunk);
            return chunk;
        }
        

        private void CleanUp(Chunk chunk)
        {
            chunk.TopMostLandVoxels = null;
        }
    }
}
