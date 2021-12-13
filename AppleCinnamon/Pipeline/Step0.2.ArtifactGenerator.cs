using System;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;
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



                if (_rnd.Next() % 16 == 0)
                {
                    Tree(chunk, index);
                }
                else
                {
                    //if (_rnd.Next() % 100 == 0)
                    //{
                    //    var fi = Help.GetFlatIndex(index.X, index.Y, index.Z, chunk.CurrentHeight);
                    //    chunk.Voxels[fi] = new Voxel(VoxelDefinition.Weed.Type, 0, 2);
                    //    chunk.BuildingContext.SpriteBlocks.Add(fi);
                    //}
                    //else if (_rnd.Next() % 100 == 0)
                    //{
                    //    var fi = Help.GetFlatIndex(index.X, index.Y, index.Z, chunk.CurrentHeight);
                    //    chunk.Voxels[fi] = new Voxel(VoxelDefinition.Flower.Type, 0, 0);
                    //    chunk.BuildingContext.SpriteBlocks.Add(fi);
                    //}
                }
            }


            return chunk;
        }

        public static void Tree(Chunk chunk, Int3 index)
        {
            var trunkHeight = _rnd.Next(3, 12);

            for (var j = 0; j < trunkHeight; j++)
            {
                var flatIndex = Help.GetFlatIndex(index.X, index.Y + j, index.Z, chunk.CurrentHeight);
                chunk.Voxels[flatIndex] = new Voxel(VoxelDefinition.Wood1.Type, 0);
            }

            var leavesSize = 9;
            var max = leavesSize - 1;

            var offset = new Int3(leavesSize / -2, trunkHeight, leavesSize / -2);

            for (var i = 0; i < leavesSize; i++)
            {
                for (var j = 0; j < leavesSize; j++)
                {
                    for (var k = 0; k < leavesSize; k++)
                    {
                        if ((i == 0 || i == max) && (j == 0 || k == 0 || j == max || k == max))
                        {
                            continue;
                        }

                        if ((j == 0 || j == max) && (i == 0 || k == 0 || i == max || k == max))
                        {
                            continue;
                        }

                        if ((k == 0 || k == max) && (i == 0 || j == 0 || i == max || j == max))
                        {
                            continue;
                        }

                        var targetGlobalIndex = index + new Int3(i, j, k) + offset;

                        chunk.GetLocalWithneighbors(targetGlobalIndex, out var address);
                        var targetChunk = chunk.Neighbors[Help.GetChunkFlatIndex(address.ChunkIndex)];
                        var targetFlatIndex = Help.GetFlatIndex(address.RelativeVoxelIndex, targetChunk.CurrentHeight);

                        targetChunk.Voxels[targetFlatIndex] = new Voxel(VoxelDefinition.Leaves.Type, 0, 2);
                    }
                }
            }
        }

        private void CleanUp(Chunk chunk)
        {
            chunk.TopMostLandVoxels = null;
        }
    }
}
