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


            foreach (var flatIndex in chunk.PendingLeftVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);
                var neighbour = leftChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : leftChunk.GetVoxel(Help.GetFlatIndex(Chunk.SizeXy - 1, index.Y, index.Z, leftChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 4);
                    chunk.VoxelCount.Left.Value++;
                }
            }

            foreach (var flatIndex in chunk.PendingRightVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var neighbour = rightChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : rightChunk.GetVoxel(Help.GetFlatIndex(0, index.Y, index.Z, rightChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 8);
                    chunk.VoxelCount.Right.Value++;
                }
            }

            foreach (var flatIndex in chunk.PendingFrontVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var neighbour = frontChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : frontChunk.GetVoxel(Help.GetFlatIndex(index.X, index.Y, Chunk.SizeXy - 1, frontChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 16);
                    chunk.VoxelCount.Front.Value++;
                }
            }

            foreach (var flatIndex in chunk.PendingBackVoxels)
            {
                var index = flatIndex.ToIndex(chunk.CurrentHeight);

                var neighbour = backChunk.CurrentHeight <= index.Y
                    ? Voxel.Air
                    : backChunk.GetVoxel(Help.GetFlatIndex(index.X, index.Y, 0, backChunk.CurrentHeight));

                var neighbourDefinition = VoxelDefinition.DefinitionByType[neighbour.Block];
                if (neighbourDefinition.IsTransparent)
                {
                    chunk.VisibilityFlags.TryGetValue(flatIndex, out var visibility);
                    chunk.VisibilityFlags[flatIndex] = (byte)(visibility + 32);
                    chunk.VoxelCount.Back.Value++;
                }

            }

            sw.Stop();

            CleanUpMemory(chunk);


            return new DataflowContext<Chunk>(context, chunk, sw.ElapsedMilliseconds, nameof(GlobalVisibilityFinalizer));
        }

        private void CleanUpMemory(Chunk chunk)
        {
            chunk.PendingLeftVoxels.Clear();
            chunk.PendingRightVoxels.Clear();
            chunk.PendingFrontVoxels.Clear();
            chunk.PendingBackVoxels.Clear();


            chunk.PendingLeftVoxels = null;
            chunk.PendingRightVoxels = null;
            chunk.PendingFrontVoxels = null;
            chunk.PendingBackVoxels = null;
        }
    }
}
