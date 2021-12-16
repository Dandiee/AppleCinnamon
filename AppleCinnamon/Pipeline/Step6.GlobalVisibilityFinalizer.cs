using System;
using System.Linq;
using AppleCinnamon.Helper;
using AppleCinnamon.Pipeline.Context;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalVisibilityFinalizer : PipelineBlock<Chunk, Chunk>
    {
        public override Chunk Process(Chunk chunk)
        {
            if (chunk.Neighbors.Any(a => a == null))
            {
                throw new Exception("nooooo waaaay");
            }

            var leftChunk = chunk.GetNeighbor(-1, 0);
            var rightChunk = chunk.GetNeighbor(1, 0);
            var frontChunk = chunk.GetNeighbor(0, -1);
            var backChunk = chunk.GetNeighbor(0, 1);

            ProcessSide(chunk, leftChunk, chunk.BuildingContext.Left);
            ProcessSide(chunk, rightChunk, chunk.BuildingContext.Right);
            ProcessSide(chunk, frontChunk, chunk.BuildingContext.Front);
            ProcessSide(chunk, backChunk, chunk.BuildingContext.Back);
            
            CleanUpMemory(chunk);
            return chunk;
        }


        [InlineMethod.Inline]
        private static void ProcessSide(Chunk chunk, Chunk neighborChunk, FaceBuildingContext context)
        {
            foreach (var flatIndex in context.PendingVoxels)
            {
                var index = chunk.FromFlatIndex(flatIndex);
                var voxel = chunk.Voxels[flatIndex];
                var neighbor = neighborChunk.CurrentHeight <= index.Y
                    ? Voxel.SunBlock
                    : neighborChunk.GetVoxel(context.GetNeighborIndex(index, neighborChunk.CurrentHeight));

                var voxelDefinition = voxel.GetDefinition();
                if (voxelDefinition.IsBlock)
                {
                    var neighborDefinition = neighbor.GetDefinition();
                    if ((neighborDefinition.CoverFlags & context.OppositeDirection) == 0) 
                    {
                        chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                        chunk.BuildingContext.VisibilityFlags[flatIndex] = visibility | context.Direction;
                        context.VoxelCount++;
                    }
                }
            }
        }

        private void CleanUpMemory(Chunk chunk)
        {
            foreach (var face in chunk.BuildingContext.Faces)
            {
                face.PendingVoxels.Clear();
            }
        }
    }
}
