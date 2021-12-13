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

                if (_rnd.Next() % 50 == 1)
                {
                    Tree(chunk, index);
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
                chunk.Voxels[flatIndex] = new Voxel(VoxelDefinition.Wood.Type, 0);
            }

            var leavesSize = 4;

            for (var i = leavesSize / -2; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    for (var k = leavesSize / -2; k < 4; k++)
                    {
                        var targetGlobalIndex = index + new Int3(i, j + trunkHeight, k);

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
