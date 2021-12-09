using System.Diagnostics;
using AppleCinnamon.Settings;
using AppleCinnamon.System;

namespace AppleCinnamon.Pipeline
{
    public sealed class GlobalVisibilityFinalizer
    {
        public DataflowContext<Chunk> Process(DataflowContext<Chunk> context)
        {
            var sw = Stopwatch.StartNew();
            var chunk = context.Payload;

            var leftChunk = chunk.neighbors2[Help.GetChunkFlatIndex(-1, 0)];
            var rightChunk = chunk.neighbors2[Help.GetChunkFlatIndex(1, 0)];
            var frontChunk = chunk.neighbors2[Help.GetChunkFlatIndex(0, -1)];
            var backChunk = chunk.neighbors2[Help.GetChunkFlatIndex(0, 1)];

            ProcessSide(chunk, leftChunk, chunk.BuildingContext.Left);
            ProcessSide(chunk, rightChunk, chunk.BuildingContext.Right);
            ProcessSide(chunk, frontChunk, chunk.BuildingContext.Front);
            ProcessSide(chunk, backChunk, chunk.BuildingContext.Back);
            

            sw.Stop();

            CleanUpMemory(chunk);


            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(GlobalVisibilityFinalizer));
        }


        [InlineMethod.Inline]
        private static void ProcessSide(Chunk chunk, Chunk neighborChunk, FaceBuildingContext context)
        {
            foreach (var flatIndex in context.PendingVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);
                var voxel = chunk.Voxels[flatIndex];
                var neighbor = neighborChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : neighborChunk.GetVoxel(context.GetNeighborIndex(index, neighborChunk.CurrentHeight));

                var voxelDefinition = VoxelDefinition.DefinitionByType[voxel.Block];
                if (voxelDefinition.IsBlock)
                {
                    var neighborDefinition = VoxelDefinition.DefinitionByType[neighbor.Block];
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
