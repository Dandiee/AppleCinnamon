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

            var leftChunk = chunk.Neighbours[new Int2(-1, 0)];
            var rightChunk = chunk.Neighbours[new Int2(1, 0)];
            var frontChunk = chunk.Neighbours[new Int2(0, -1)];
            var backChunk = chunk.Neighbours[new Int2(0, 1)];


            foreach (var flatIndex in chunk.BuildingContext.Left.PendingVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);
                var neighbour = leftChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : leftChunk.GetVoxel(Help.GetFlatIndex(Chunk.SizeXy - 1, index.Y, index.Z, leftChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.BuildingContext.VisibilityFlags[flatIndex] = visibility | VisibilityFlag.Left;
                    chunk.BuildingContext.Left.VoxelCount++;
                }
            }

            foreach (var flatIndex in chunk.BuildingContext.Right.PendingVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var neighbour = rightChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : rightChunk.GetVoxel(Help.GetFlatIndex(0, index.Y, index.Z, rightChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.BuildingContext.VisibilityFlags[flatIndex] = visibility | VisibilityFlag.Right;
                    chunk.BuildingContext.Right.VoxelCount++;
                }
            }

            foreach (var flatIndex in chunk.BuildingContext.Front.PendingVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var neighbour = frontChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : frontChunk.GetVoxel(Help.GetFlatIndex(index.X, index.Y, Chunk.SizeXy - 1, frontChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.BuildingContext.VisibilityFlags[flatIndex] = visibility | VisibilityFlag.Front;
                    chunk.BuildingContext.Front.VoxelCount++;
                }
            }

            foreach (var flatIndex in chunk.BuildingContext.Back.PendingVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var neighbour = backChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : backChunk.GetVoxel(Help.GetFlatIndex(index.X, index.Y, 0, backChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.BuildingContext.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.BuildingContext.VisibilityFlags[flatIndex] = visibility | VisibilityFlag.Back;
                    chunk.BuildingContext.Back.VoxelCount++;
                }

            }

            sw.Stop();

            CleanUpMemory(chunk);


            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(GlobalVisibilityFinalizer));
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
